# ============================================================================
# Build.ps1 - compiles IncidentButton.exe
#
# Run this from the folder containing IncidentButton.cs.
# No SDK or Visual Studio required: it uses the C# compiler that ships with
# the .NET Framework on every Windows 10/11 machine.
#
#   .\Build.ps1          - build only
#   .\Build.ps1 -Run     - build, then launch it so you can see the result
# ============================================================================

param([switch]$Run)

$ErrorActionPreference = 'Stop'

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$src = Join-Path $PSScriptRoot 'IncidentButton.cs'
$ico = Join-Path $PSScriptRoot 'app.ico'
$exe = Join-Path $PSScriptRoot 'IncidentButton.exe'

if (-not (Test-Path $csc)) { throw "C# compiler not found at $csc" }
if (-not (Test-Path $src)) { throw "IncidentButton.cs not found in $PSScriptRoot" }

Get-Process IncidentButton -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$cscArgs = @('/nologo', '/codepage:65001', '/target:winexe', '/platform:anycpu', "/out:$exe")
if (Test-Path $ico) {
    $cscArgs += "/win32icon:$ico"          # icon shown in Explorer and Task Manager
    $cscArgs += "/resource:$ico,app.ico"   # same icon embedded for the tray and window
} else {
    Write-Warning "app.ico not found - the app will use the built-in Windows shield icon."
}
$cscArgs += $src

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw "Compile failed with exit code $LASTEXITCODE" }

Write-Host "Built: $exe" -ForegroundColor Green
Get-Item $exe | Select-Object LastWriteTime, Length | Format-List

if ($Run) {
    Write-Host "Launching..." -ForegroundColor Cyan
    Start-Process $exe
}
