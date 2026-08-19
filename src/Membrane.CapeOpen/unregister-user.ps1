#requires -version 3
# Remove the COM registration written by register-user.ps1.
#   powershell -ExecutionPolicy Bypass -File unregister-user.ps1            (HKCU, no admin)
#   powershell -ExecutionPolicy Bypass -File unregister-user.ps1 -Machine   (HKLM, Administrator)
param([switch]$Machine)
$ErrorActionPreference = 'SilentlyContinue'
$clsid  = '{B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44}'
$progId = 'Membrane.MembraneUnitOperation.1'
$base   = if ($Machine) { 'HKLM:\Software\Classes' } else { 'HKCU:\Software\Classes' }
Remove-Item -Path "$base\CLSID\$clsid" -Recurse -Force
Remove-Item -Path "$base\$progId" -Recurse -Force
$scope = if ($Machine) { 'per-machine (HKLM)' } else { 'per-user (HKCU)' }
Write-Host "Unregistered $scope : $progId  $clsid"
