#requires -version 3
# Remove the per-user COM registration written by register-user.ps1 (no Administrator needed).
$ErrorActionPreference = 'SilentlyContinue'
$clsid  = '{B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44}'
$progId = 'ORS.MembraneUnitOperation.1'
Remove-Item -Path "HKCU:\Software\Classes\CLSID\$clsid" -Recurse -Force
Remove-Item -Path "HKCU:\Software\Classes\$progId" -Recurse -Force
Write-Host "Unregistered (per-user): $progId  $clsid"
