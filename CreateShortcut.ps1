# CreateShortcut.ps1
# Run this once after building the application to create a desktop shortcut.

$ErrorActionPreference = "Stop"

$appDir = $PSScriptRoot
$exePath = Join-Path $appDir "src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish\LasanthaPOS.Desktop.exe"
$iconPath = Join-Path $appDir "src\LasanthaPOS.Desktop\bin\Release\net10.0-windows\publish\LasanthaPOS.Desktop.exe"
$shortcutPath = [System.IO.Path]::Combine([Environment]::GetFolderPath("Desktop"), "Lasantha POS.lnk")

$WScript = New-Object -ComObject WScript.Shell
$shortcut = $WScript.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.IconLocation = $iconPath
$shortcut.WorkingDirectory = Split-Path $exePath
$shortcut.Description = "Lasantha Electronics POS System"
$shortcut.WindowStyle = 1
$shortcut.Save()

Write-Host "Desktop shortcut created at: $shortcutPath" -ForegroundColor Green
