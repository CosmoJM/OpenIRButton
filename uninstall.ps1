# ============================================================================
# uninstall.ps1 - removes the Incident Response Button
# Runs as SYSTEM via Intune, or manually from an elevated PowerShell window.
# ============================================================================

$ErrorActionPreference = 'SilentlyContinue'

$PF = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }

Get-Process IncidentButton | Stop-Process -Force
Start-Sleep -Milliseconds 500

Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'IncidentButton'
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run' -Name 'IncidentButton'

Remove-Item "$PF\IncidentButton" -Recurse -Force
Remove-Item "${env:ProgramFiles(x86)}\IncidentButton" -Recurse -Force
Remove-Item "$env:ProgramData\IncidentButton" -Recurse -Force

Write-Host 'Uninstall complete.'
exit 0
