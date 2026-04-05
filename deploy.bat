@echo off
setlocal EnableDelayedExpansion
title Lasantha POS — Deploy

:: ============================================================
::  deploy.bat
::  Pull latest code, install dependencies, build and deploy
::  backend (Docker) and desktop (WPF) for Lasantha POS.
:: ============================================================

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
set "DESKTOP_PROJ=%ROOT%\src\LasanthaPOS.Desktop\LasanthaPOS.Desktop.csproj"
set "PUBLISH_DIR=%ROOT%\src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish"
set "EXE_PATH=%PUBLISH_DIR%\LasanthaPOS.Desktop.exe"
set "ERRORS=0"

call :log_header "LASANTHA POS — DEPLOY SCRIPT"

:: ============================================================
:: STEP 1 — Git: pull latest code from main
:: ============================================================
call :log_step "STEP 1" "Pulling latest code from main branch"

where git >nul 2>&1
if errorlevel 1 (
    call :log_warn "Git not found in PATH — skipping pull. Install Git and re-run."
) else (
    git -C "%ROOT%" fetch origin main 2>&1
    if errorlevel 1 (
        call :log_warn "git fetch failed — continuing with local code."
    ) else (
        git -C "%ROOT%" checkout main 2>&1
        git -C "%ROOT%" pull origin main 2>&1
        if errorlevel 1 (
            call :log_warn "git pull failed — continuing with local code."
        ) else (
            call :log_ok "Code up to date."
        )
    )
)

:: ============================================================
:: STEP 2 — Chocolatey: ensure choco is installed
:: ============================================================
call :log_step "STEP 2" "Checking Chocolatey"

where choco >nul 2>&1
if errorlevel 1 (
    call :log_warn "Chocolatey not found. Installing now (requires admin rights)..."
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "Set-ExecutionPolicy Bypass -Scope Process -Force; ^
         [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12; ^
         iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
    if errorlevel 1 (
        call :log_error "Chocolatey installation failed. Some dependencies may not install correctly."
        set "ERRORS=1"
    ) else (
        :: Refresh PATH so choco is available in this session
        call :refresh_path
        call :log_ok "Chocolatey installed."
    )
) else (
    call :log_ok "Chocolatey is available."
    call :log_info "Upgrading Chocolatey itself..."
    choco upgrade chocolatey -y --no-progress >nul 2>&1
)

:: ============================================================
:: STEP 3 — Dependencies: Git, .NET 10 SDK, Docker Desktop
:: ============================================================
call :log_step "STEP 3" "Checking and installing dependencies"

:: --- Git ---
call :ensure_choco_pkg "git" "git"

:: Refresh after potential git install
call :refresh_path

:: --- .NET 10 SDK ---
call :log_info "Checking .NET 10 SDK..."
dotnet --list-sdks 2>nul | findstr /B "10\." >nul 2>&1
if errorlevel 1 (
    call :log_warn ".NET 10 SDK not found. Installing via Chocolatey..."
    choco install dotnet-sdk --version 10.0.100 -y --no-progress
    if errorlevel 1 (
        call :log_error ".NET 10 SDK install failed. Cannot build desktop app."
        set "ERRORS=1"
    ) else (
        call :refresh_path
        call :log_ok ".NET 10 SDK installed."
    )
) else (
    call :log_ok ".NET 10 SDK is available."
)

:: --- Docker Desktop ---
call :log_info "Checking Docker..."
where docker >nul 2>&1
if errorlevel 1 (
    call :log_warn "Docker not found. Installing Docker Desktop via Chocolatey..."
    choco install docker-desktop -y --no-progress
    if errorlevel 1 (
        call :log_error "Docker Desktop install failed. Backend containers cannot start."
        set "ERRORS=1"
    ) else (
        call :refresh_path
        call :log_ok "Docker Desktop installed. You may need to reboot before first use."
    )
) else (
    call :log_ok "Docker is available."
    :: Upgrade Docker Desktop if already installed
    choco upgrade docker-desktop -y --no-progress >nul 2>&1
)

:: ============================================================
:: STEP 4 — Docker: start/rebuild backend containers
:: ============================================================
call :log_step "STEP 4" "Building and starting backend containers (PostgreSQL + API)"

where docker >nul 2>&1
if errorlevel 1 (
    call :log_error "Docker is not available — cannot start backend. Aborting container step."
    set "ERRORS=1"
    goto :build_desktop
)

:: Check Docker daemon is running
docker info >nul 2>&1
if errorlevel 1 (
    call :log_warn "Docker daemon not running. Attempting to start Docker Desktop..."
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    call :log_info "Waiting 30 seconds for Docker to initialise..."
    timeout /t 30 /nobreak >nul
    docker info >nul 2>&1
    if errorlevel 1 (
        call :log_error "Docker daemon still not available. Start Docker Desktop manually and re-run."
        set "ERRORS=1"
        goto :build_desktop
    )
)

:: Pull latest base images then rebuild
call :log_info "Pulling latest base images..."
docker compose -f "%ROOT%\docker-compose.yml" pull --quiet 2>&1

call :log_info "Building and starting containers..."
docker compose -f "%ROOT%\docker-compose.yml" up -d --build --remove-orphans
if errorlevel 1 (
    call :log_error "docker compose up failed."
    set "ERRORS=1"
) else (
    call :log_ok "Backend containers are running."
)

:: Wait for API health
call :log_info "Waiting for API to become ready (up to 90 s)..."
set /a "waited=0"
:wait_api
    timeout /t 5 /nobreak >nul
    set /a "waited+=5"
    powershell -NoProfile -Command ^
        "try { $r=(Invoke-WebRequest 'http://localhost:5100/api/categories' -UseBasicParsing -TimeoutSec 3 -EA Stop).StatusCode; exit ($r -eq 200 ? 0 : 1) } catch { exit 1 }" >nul 2>&1
    if not errorlevel 1 (
        call :log_ok "API is responding on http://localhost:5100"
        goto :build_desktop
    )
    if !waited! lss 90 (
        call :log_info "  Still waiting... (!waited! s)"
        goto :wait_api
    )
    call :log_warn "API did not respond within 90 seconds — check Docker logs."

:build_desktop
:: ============================================================
:: STEP 5 — Build and publish WPF Desktop app
:: ============================================================
call :log_step "STEP 5" "Building and publishing WPF Desktop app"

where dotnet >nul 2>&1
if errorlevel 1 (
    call :log_error "dotnet CLI not found — cannot build desktop app."
    set "ERRORS=1"
    goto :create_shortcut
)

call :log_info "Restoring NuGet packages..."
dotnet restore "%DESKTOP_PROJ%"
if errorlevel 1 (
    call :log_error "dotnet restore failed."
    set "ERRORS=1"
    goto :create_shortcut
)

call :log_info "Publishing self-contained desktop app..."
dotnet publish "%DESKTOP_PROJ%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o "%PUBLISH_DIR%"
if errorlevel 1 (
    call :log_error "dotnet publish failed."
    set "ERRORS=1"
    goto :create_shortcut
)
call :log_ok "Desktop app published to: %PUBLISH_DIR%"

:create_shortcut
:: ============================================================
:: STEP 6 — Create / update desktop shortcut
:: ============================================================
call :log_step "STEP 6" "Creating desktop shortcut"

if exist "%EXE_PATH%" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\CreateShortcut.ps1"
    if errorlevel 1 (
        call :log_warn "Shortcut creation failed — run CreateShortcut.ps1 manually."
    ) else (
        call :log_ok "Desktop shortcut updated."
    )
) else (
    call :log_warn "Executable not found — shortcut skipped."
)

:: ============================================================
:: STEP 7 — Launch the desktop app
:: ============================================================
call :log_step "STEP 7" "Launching Lasantha POS"

if exist "%EXE_PATH%" (
    call :log_info "Starting application..."
    start "" "%EXE_PATH%"
    call :log_ok "Application launched."
) else (
    call :log_warn "Executable not found at: %EXE_PATH%"
    call :log_warn "Build may have failed — check errors above."
)

:: ============================================================
:: DONE
:: ============================================================
echo.
if "%ERRORS%"=="0" (
    call :log_header "DEPLOY COMPLETE — NO ERRORS"
) else (
    call :log_header "DEPLOY FINISHED WITH WARNINGS/ERRORS — REVIEW OUTPUT ABOVE"
)
echo.
pause
endlocal
goto :eof

:: ============================================================
:: SUBROUTINES
:: ============================================================

:ensure_choco_pkg
:: %1 = display name, %2 = choco package id
call :log_info "Checking %~1..."
where %~2 >nul 2>&1
if errorlevel 1 (
    call :log_warn "%~1 not found. Installing via Chocolatey..."
    choco install %~2 -y --no-progress
    if errorlevel 1 (
        call :log_error "%~1 install failed."
        set "ERRORS=1"
    ) else (
        call :refresh_path
        call :log_ok "%~1 installed."
    )
) else (
    call :log_ok "%~1 is available."
    choco upgrade %~2 -y --no-progress >nul 2>&1
)
goto :eof

:refresh_path
:: Reload PATH from registry so newly installed tools are available
for /f "usebackq tokens=2,*" %%A in (
    `reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path 2^>nul`
) do set "SYS_PATH=%%B"
for /f "usebackq tokens=2,*" %%A in (
    `reg query "HKCU\Environment" /v Path 2^>nul`
) do set "USR_PATH=%%B"
set "PATH=%SYS_PATH%;%USR_PATH%"
goto :eof

:log_header
echo.
echo ================================================================
echo   %~1
echo ================================================================
echo.
goto :eof

:log_step
echo.
echo [%~1] %~2
echo ----------------------------------------------------------------
goto :eof

:log_ok
echo [  OK  ] %~1
goto :eof

:log_warn
echo [ WARN ] %~1
goto :eof

:log_error
echo [ERROR ] %~1
goto :eof

:log_info
echo [  ..  ] %~1
goto :eof
