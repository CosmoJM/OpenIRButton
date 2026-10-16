# ============================================================================
# install.ps1 - deploys the Incident Response Button
#
# Runs as SYSTEM via Intune, or manually from an elevated PowerShell window.
# Installs the app, copies contacts.json, and sets it to start at every sign-in.
#
# ADMIN: bump $Version every time you ship a new build, and set the Intune
#        detection rule to match it exactly (see SETUP-GUIDE.md).
# ============================================================================

$Version = '1.0.0'

$ErrorActionPreference = 'Stop'

# Resolve the real 64-bit Program Files even when launched from 32-bit PowerShell
$PF = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }

$InstallDir = "$PF\IncidentButton"
$ConfigDir  = "$env:ProgramData\IncidentButton"
$Exe        = Join-Path $InstallDir 'IncidentButton.exe'
$SrcExe     = Join-Path $PSScriptRoot 'IncidentButton.exe'
$SrcCs      = Join-Path $PSScriptRoot 'IncidentButton.cs'
$SrcIco     = Join-Path $PSScriptRoot 'app.ico'
$SrcJson    = Join-Path $PSScriptRoot 'contacts.json'

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path $ConfigDir  | Out-Null

# Stop any running copy so the exe can be replaced on upgrades
Get-Process IncidentButton -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

if (Test-Path $SrcExe) {
    Copy-Item $SrcExe $Exe -Force
} elseif (Test-Path $SrcCs) {
    # Fallback: compile on the endpoint if no prebuilt exe was packaged
    $csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    $a = @('/nologo', '/target:winexe', '/platform:anycpu', "/out:$Exe")
    if (Test-Path $SrcIco) { $a += "/win32icon:$SrcIco"; $a += "/resource:$SrcIco,app.ico" }
    $a += $SrcCs
    & $csc $a
    if ($LASTEXITCODE -ne 0) { throw "csc.exe compile failed with exit code $LASTEXITCODE" }
} else {
    throw 'Neither IncidentButton.exe nor IncidentButton.cs found in package.'
}

if (Test-Path $SrcJson) { Copy-Item $SrcJson (Join-Path $ConfigDir 'contacts.json') -Force }

# Start for every user at sign-in
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' `
    -Name 'IncidentButton' -Value "`"$Exe`"" -Type String

# Written last - this file is what the Intune detection rule checks
Set-Content -Path (Join-Path $InstallDir 'version.txt') -Value $Version

# Launch now for whoever is signed in, so the icon appears without a reboot
try {
    $user = (Get-CimInstance Win32_ComputerSystem).UserName
    if ($user) {
        $action    = New-ScheduledTaskAction -Execute $Exe
        $principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive
        Register-ScheduledTask -TaskName 'IncidentButton-FirstRun' `
            -Action $action -Principal $principal -Force | Out-Null
        Start-ScheduledTask -TaskName 'IncidentButton-FirstRun'
        Start-Sleep -Seconds 3
        Unregister-ScheduledTask -TaskName 'IncidentButton-FirstRun' -Confirm:$false
    }
} catch { Write-Warning "Could not launch for current user: $_ (it will start at next sign-in)" }

Write-Host "Install complete (version $Version)."
exit 0
