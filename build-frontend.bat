@echo off
setlocal EnableDelayedExpansion
title Lasantha POS — Build Frontend

:: ============================================================
::  build-frontend.bat
::  Build and publish the WPF Desktop (frontend) app only.
::  Does NOT touch Docker / backend containers.
:: ============================================================

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
set "DESKTOP_PROJ=%ROOT%\src\LasanthaPOS.Desktop\LasanthaPOS.Desktop.csproj"
set "PUBLISH_DIR=%ROOT%\src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish"
set "EXE_PATH=%PUBLISH_DIR%\LasanthaPOS.Desktop.exe"
set "PS5=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
set "ERRORS=0"

call :log_header "LASANTHA POS — BUILD FRONTEND"

:: ============================================================
:: STEP 1 — Chocolatey: ensure package manager is available
:: ============================================================
call :log_step "STEP 1" "Checking Chocolatey"

where choco >nul 2>&1
if errorlevel 1 (
    call :log_warn "Chocolatey not found. Installing now (requires admin rights)..."
    "%PS5%" -NoProfile -ExecutionPolicy Bypass -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
    if errorlevel 1 (
        call :log_error "Chocolatey installation failed. Install manually from https://chocolatey.org/install"
        set "ERRORS=1"
    ) else (
        call :refresh_path
        call :log_ok "Chocolatey installed."
    )
) else (
    call :log_ok "Chocolatey is available."
    choco upgrade chocolatey -y --no-progress >nul 2>&1
)

:: ============================================================
:: STEP 2 — Dependencies: Git and .NET 10 SDK
:: ============================================================
call :log_step "STEP 2" "Checking and installing dependencies"

:: --- Git ---
:: Args: "Display Name"  "choco-package-id"  "binary-to-check"
call :ensure_choco_pkg "Git" "git" "git"
call :refresh_path

:: --- .NET 10 SDK ---
call :log_info "Checking .NET 10 SDK..."
set "DOTNET10_OK=0"
where dotnet >nul 2>&1
if not errorlevel 1 (
    dotnet --list-sdks 2>nul | findstr /B "10." >nul 2>&1
    if not errorlevel 1 set "DOTNET10_OK=1"
)
if "%DOTNET10_OK%"=="0" (
    call :log_warn ".NET 10 SDK not found. Installing via Microsoft install script..."
    call :install_dotnet10
    call :refresh_path
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "PATH=%ProgramFiles%\dotnet;%PATH%"
        call :log_ok ".NET 10 SDK installed."
    ) else if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
        set "PATH=%USERPROFILE%\.dotnet;%PATH%"
        call :log_ok ".NET 10 SDK installed (user-scoped)."
    ) else (
        call :log_error ".NET 10 SDK install may have failed — dotnet.exe not found."
        set "ERRORS=1"
    )
) else (
    call :log_ok ".NET 10 SDK is available."
)

:: ============================================================
:: STEP 3 — Restore NuGet packages
:: ============================================================
call :log_step "STEP 3" "Restoring NuGet packages"

where dotnet >nul 2>&1
if errorlevel 1 (
    call :log_error "dotnet CLI not found — cannot build desktop app."
    set "ERRORS=1"
    goto :done
)

dotnet restore "%DESKTOP_PROJ%"
if errorlevel 1 (
    call :log_error "dotnet restore failed."
    set "ERRORS=1"
    goto :done
)
call :log_ok "Packages restored."

:: ============================================================
:: STEP 4 — Build and publish
:: ============================================================
call :log_step "STEP 4" "Building and publishing WPF Desktop app"

dotnet publish "%DESKTOP_PROJ%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o "%PUBLISH_DIR%"
if errorlevel 1 (
    call :log_error "dotnet publish failed."
    set "ERRORS=1"
    goto :done
)
call :log_ok "Published to: %PUBLISH_DIR%"

:: ============================================================
:: STEP 5 — Update desktop shortcut
:: ============================================================
call :log_step "STEP 5" "Updating desktop shortcut"

if exist "%EXE_PATH%" (
    if exist "%ROOT%\CreateShortcut.ps1" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\CreateShortcut.ps1"
        if errorlevel 1 (
            call :log_warn "Shortcut creation failed — run CreateShortcut.ps1 manually."
        ) else (
            call :log_ok "Desktop shortcut updated."
        )
    ) else (
        call :log_warn "CreateShortcut.ps1 not found — shortcut skipped."
    )
) else (
    call :log_warn "Executable not found — shortcut skipped."
)

:: ============================================================
:: STEP 6 — Launch
:: ============================================================
call :log_step "STEP 6" "Launching Lasantha POS"

if exist "%EXE_PATH%" (
    call :log_info "Starting application..."
    start "" "%EXE_PATH%"
    call :log_ok "Application launched."
) else (
    call :log_warn "Executable not found at: %EXE_PATH%"
)

:done
:: ============================================================
:: DONE
:: ============================================================
echo.
if "%ERRORS%"=="0" (
    call :log_header "BUILD COMPLETE — NO ERRORS"
) else (
    call :log_header "BUILD FINISHED WITH ERRORS — REVIEW OUTPUT ABOVE"
)
echo.
pause
endlocal
goto :eof

:: ============================================================
:: SUBROUTINES
:: ============================================================

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
echo   [OK]   %~1
goto :eof

:log_info
echo   [..]   %~1
goto :eof

:log_warn
echo   [WARN] %~1
goto :eof

:log_error
echo   [ERR]  %~1
goto :eof

:ensure_choco_pkg
:: %1 = display name   %2 = choco package id   %3 = binary to check in PATH
call :log_info "Checking %~1..."
where %~3 >nul 2>&1
if errorlevel 1 (
    call :log_warn "%~1 not found."
    where choco >nul 2>&1
    if errorlevel 1 (
        call :log_error "Chocolatey not available — cannot auto-install %~1."
        set "ERRORS=1"
    ) else (
        call :log_info "Installing %~1 via Chocolatey..."
        choco install %~2 -y --no-progress
        if errorlevel 1 (
            call :log_error "%~1 install failed."
            set "ERRORS=1"
        ) else (
            call :refresh_path
            call :log_ok "%~1 installed."
        )
    )
) else (
    call :log_ok "%~1 is available."
    where choco >nul 2>&1
    if not errorlevel 1 (
        choco upgrade %~2 -y --no-progress >nul 2>&1
    )
)
goto :eof

:install_dotnet10
:: Download and run Microsoft's official dotnet-install.ps1 (PS5 compatible)
:: Tries system-wide install first; falls back to user-scoped (no admin needed)
set "DI_SCRIPT=%TEMP%\dotnet-install.ps1"
call :log_info "Downloading dotnet-install.ps1 from Microsoft..."
"%PS5%" -NoProfile -ExecutionPolicy Bypass -Command "(New-Object System.Net.WebClient).DownloadFile('https://dot.net/v1/dotnet-install.ps1', '%DI_SCRIPT%')"
if not exist "%DI_SCRIPT%" (
    call :log_error "Failed to download dotnet-install.ps1."
    set "ERRORS=1"
    goto :eof
)
call :log_info "Installing .NET 10 SDK system-wide (requires admin)..."
"%PS5%" -NoProfile -ExecutionPolicy Bypass -File "%DI_SCRIPT%" -Channel 10.0 -InstallDir "%ProgramFiles%\dotnet"
if errorlevel 1 (
    call :log_warn "System-wide install failed. Trying user-scoped install..."
    "%PS5%" -NoProfile -ExecutionPolicy Bypass -File "%DI_SCRIPT%" -Channel 10.0
    if errorlevel 1 (
        call :log_error ".NET 10 SDK installation failed."
        set "ERRORS=1"
    )
)
del "%DI_SCRIPT%" >nul 2>&1
goto :eof

:refresh_path
:: Write expanded PATH values to temp files then read them back.
:: Uses temp files to avoid for/f truncation on very long PATH strings.
:: Uses PS5 (powershell.exe) to ensure it works regardless of pwsh availability.
"%PS5%" -NoProfile -Command "[Environment]::GetEnvironmentVariable('Path','Machine')" > "%TEMP%\_lbpos_syspath.tmp" 2>nul
"%PS5%" -NoProfile -Command "[Environment]::GetEnvironmentVariable('Path','User')"    > "%TEMP%\_lbpos_usrpath.tmp" 2>nul
set "SYS_PATH="
set "USR_PATH="
for /f "usebackq delims=" %%P in ("%TEMP%\_lbpos_syspath.tmp") do set "SYS_PATH=%%P"
for /f "usebackq delims=" %%P in ("%TEMP%\_lbpos_usrpath.tmp") do set "USR_PATH=%%P"
del "%TEMP%\_lbpos_syspath.tmp" "%TEMP%\_lbpos_usrpath.tmp" >nul 2>&1
if defined SYS_PATH (
    if defined USR_PATH (
        set "PATH=%SYS_PATH%;%USR_PATH%"
    ) else (
        set "PATH=%SYS_PATH%"
    )
)
goto :eof
