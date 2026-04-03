#!/usr/bin/env pwsh
# Start.ps1 — Start the backend (Docker) and launch the desktop app

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "Starting Docker containers (PostgreSQL + API)..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" up -d --build

Write-Host "Waiting for API to be ready..." -ForegroundColor Cyan
$maxWait = 60
$elapsed = 0
do {
    Start-Sleep -Seconds 3
    $elapsed += 3
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5100/api/categories" -UseBasicParsing -ErrorAction SilentlyContinue
        if ($r.StatusCode -eq 200) { break }
    } catch {}
    Write-Host "  Still waiting... ($elapsed s)" -ForegroundColor Gray
} while ($elapsed -lt $maxWait)

$exePath = Join-Path $root "src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish\LasanthaPOS.Desktop.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "Desktop app not built yet. Building now..." -ForegroundColor Yellow
    dotnet publish "$root\src\LasanthaPOS.Desktop\LasanthaPOS.Desktop.csproj" -c Release -o "$root\src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish"
}

Write-Host "Launching Lasantha POS..." -ForegroundColor Green
Start-Process $exePath
