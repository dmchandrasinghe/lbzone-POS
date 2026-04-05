<#
.SYNOPSIS
    Versioned SQL migration runner for LasanthaPOS (PostgreSQL running in Docker).

.DESCRIPTION
    - Reads V###__description.sql files from the migrations folder in version order.
    - Tracks applied migrations in a 'db_migrations' table inside PostgreSQL.
    - Only applies migrations not yet recorded in that table (idempotent runner).
    - Each migration is wrapped in a single transaction; on failure it rolls back
      and the script exits with code 1 so the caller (deploy.bat) can surface the error.

.PARAMETER ContainerName
    Docker container name for the PostgreSQL instance. Default: lasantha_pos_db

.PARAMETER DbUser
    PostgreSQL user. Default: posuser

.PARAMETER DbName
    PostgreSQL database name. Default: lasantha_pos

.PARAMETER MigrationsDir
    Path to the folder containing V*.sql migration files.
    Default: <script folder>\migrations

.EXAMPLE
    # Run with defaults (called from deploy.bat)
    .\migrate.ps1

    # Run against a different environment
    .\migrate.ps1 -ContainerName staging_db -DbUser appuser -DbName staging_pos

.NOTES
    Compatible : PowerShell 5.1 / 6 / 7
    Requires   : Docker CLI with the target container already running
    Convention : Migration files must be named  V###__description.sql
                 e.g.  V001__initial_schema.sql
                       V002__add_discount_table.sql
#>
param(
    [string]$ContainerName = "lasantha_pos_db",
    [string]$DbUser        = "posuser",
    [string]$DbName        = "lasantha_pos",
    [string]$MigrationsDir = (Join-Path $PSScriptRoot "migrations")
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Logging helpers
# ---------------------------------------------------------------------------
function Write-Ok($msg)   { Write-Host "[  OK  ] $msg" }
function Write-Info($msg) { Write-Host "[  ..  ] $msg" }
function Write-Warn($msg) { Write-Host "[ WARN ] $msg" }
function Write-Err($msg)  { Write-Host "[ERROR ] $msg" }

# ---------------------------------------------------------------------------
# Run SQL (supplied as a string) inside the container via a temp file.
# Using a temp file (docker cp) is more reliable than stdin piping across
# PowerShell versions and Windows encodings.
# ---------------------------------------------------------------------------
function Invoke-Psql {
    param(
        [string]$Sql,
        [switch]$TupleOnly   # use -t -A for single-column scalar output
    )

    $tmp = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        [System.IO.File]::WriteAllText($tmp, $Sql, [System.Text.Encoding]::UTF8)

        docker cp $tmp "${ContainerName}:/tmp/_lbpos_mig.sql" 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "docker cp failed (exit $LASTEXITCODE)" }

        if ($TupleOnly) {
            $out = docker exec $ContainerName psql -U $DbUser -d $DbName `
                       -f /tmp/_lbpos_mig.sql -t -A 2>&1
        } else {
            $out = docker exec $ContainerName psql -U $DbUser -d $DbName `
                       -f /tmp/_lbpos_mig.sql 2>&1
        }
        $ec = $LASTEXITCODE

        docker exec $ContainerName rm -f /tmp/_lbpos_mig.sql 2>&1 | Out-Null

        if ($ec -ne 0) {
            throw "psql exited $ec`n$($out -join "`n")"
        }
        return $out
    }
    finally {
        if (Test-Path $tmp) { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    }
}

# ===========================================================================
# 0. Verify container is running
# ===========================================================================
Write-Host ""
Write-Info "Checking Docker container '$ContainerName'..."
$state = docker inspect --format "{{.State.Running}}" $ContainerName 2>&1
if ($LASTEXITCODE -ne 0 -or "$state".Trim() -ne "true") {
    Write-Err "Container '$ContainerName' is not running. Start the backend and re-run."
    exit 1
}
Write-Ok "Container is running."

# ===========================================================================
# 1. Ensure migration tracking table exists
# ===========================================================================
Write-Info "Ensuring db_migrations tracking table..."
Invoke-Psql @"
CREATE TABLE IF NOT EXISTS db_migrations (
    version     VARCHAR(50)  PRIMARY KEY,
    description TEXT         NOT NULL,
    applied_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
"@ | Out-Null
Write-Ok "Tracking table ready."

# ===========================================================================
# 2. Load already-applied versions
# ===========================================================================
$rows    = Invoke-Psql "SELECT version FROM db_migrations ORDER BY version;" -TupleOnly
$applied = @{}
foreach ($row in $rows) {
    $v = "$row".Trim()
    if ($v -ne '') { $applied[$v] = $true }
}

# ===========================================================================
# 3. Locate migration files
# ===========================================================================
if (-not (Test-Path $MigrationsDir)) {
    Write-Warn "Migrations directory not found: $MigrationsDir"
    exit 0
}

$files = Get-ChildItem -Path $MigrationsDir -Filter "V*.sql" | Sort-Object Name
if ($files.Count -eq 0) {
    Write-Info "No migration files found in: $MigrationsDir"
    exit 0
}
Write-Info "Found $($files.Count) migration file(s)."

# ===========================================================================
# 4. Apply pending migrations in version order
# ===========================================================================
$pendingCount = 0

foreach ($file in $files) {

    # Filename must match  V001__some_description.sql
    if ($file.Name -notmatch '^(V\d+)__(.+)\.sql$') {
        Write-Warn "Skipping '$($file.Name)' — must match V###__description.sql"
        continue
    }

    $version     = $Matches[1].ToUpper()
    $description = $Matches[2] -replace '_', ' '

    if ($applied.ContainsKey($version)) {
        Write-Ok "$version ($description) — already applied, skipped."
        continue
    }

    $pendingCount++
    Write-Host ""
    Write-Info "Applying $version — $description ..."

    # Build a transactional wrapper:
    #   BEGIN
    #   <migration SQL>
    #   INSERT into tracking table
    #   COMMIT
    # Any error inside the transaction causes an automatic rollback via psql's
    # ON_ERROR_STOP=1 (set below) and psql exits non-zero.
    $migrationSql = [System.IO.File]::ReadAllText($file.FullName,
                        [System.Text.Encoding]::UTF8)
    $safeDesc     = $description -replace "'", "''"   # escape for SQL literal

    $wrapper = @"
\set ON_ERROR_STOP 1
BEGIN;

$migrationSql

INSERT INTO db_migrations (version, description)
VALUES ('$version', '$safeDesc')
ON CONFLICT (version) DO NOTHING;

COMMIT;
"@

    try {
        $out = Invoke-Psql $wrapper
        Write-Ok "$version applied successfully."
        # Echo any psql notices (e.g. "CREATE TABLE", "CREATE INDEX")
        $out | Where-Object { "$_" -match '\S' } | ForEach-Object {
            Write-Host "         $_"
        }
    }
    catch {
        Write-Err "$version FAILED — transaction rolled back."
        Write-Err "$_"
        exit 1
    }
}

Write-Host ""
if ($pendingCount -eq 0) {
    Write-Ok "Database is already up to date. No migrations were needed."
} else {
    Write-Ok "Applied $pendingCount migration(s) successfully."
}
exit 0
