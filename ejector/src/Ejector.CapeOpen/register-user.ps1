#requires -version 3
# COM registration of the ejector unit operation for CAPE-OPEN.
#
# The unit ships as a .NET 8 assembly activated through the generated native comhost shim
# (Ejector.CapeOpen.comhost.dll). Registering InprocServer32 -> comhost.dll (rather than mscoree)
# makes .NET-based hosts such as DWSIM receive a real COM RCW; native hosts (COCO/COFE) are
# unaffected. comhost's own DllRegisterServer does NOT write the CAPE-OPEN CATIDs or CapeDescription,
# so this script writes the full set of keys directly.
#
# Requires the .NET 8 Desktop runtime. Run from the folder that contains Ejector.CapeOpen.comhost.dll:
#   powershell -ExecutionPolicy Bypass -File register-user.ps1             (HKCU, no admin)
#   powershell -ExecutionPolicy Bypass -File register-user.ps1 -Machine    (HKLM, Administrator)
param([switch]$Machine)
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$comhost = Join-Path $scriptDir 'Ejector.CapeOpen.comhost.dll'
if (-not (Test-Path $comhost)) { Write-Error "Not found: $comhost  (build net8 x64 Release first)"; exit 1 }

$clsid  = '{661320FB-F98D-47CB-9DFD-6A84D72680A9}'
$progId = 'Ejector.EjectorUnitOperation.1'
$name   = 'Ejector (1D real-fluid)'
$desc   = 'Ejector / jet pump / eductor: one-dimensional real-fluid model with constant-area mixing and homogeneous-equilibrium flashing flow. Thermodynamics from the flowsheet property package.'
$cats   = @(
  '{678C09A5-7D66-11D2-A67D-00105A42887F}',  # CAPE-OPEN Unit Operation
  '{4150C28A-EE06-403F-A871-87AFEC38A249}',  # Consumes Thermo
  '{0D562DC8-EA8E-4210-AB39-B66513C0CD09}',  # Supports Thermo 1.0
  '{4667023A-5A8E-4CCA-AB6D-9D78C5112FED}'   # Supports Thermo 1.1
)
$base   = if ($Machine) { 'HKLM:\Software\Classes' } else { 'HKCU:\Software\Classes' }
$clsKey = "$base\CLSID\$clsid"

function Set-Default($path, $value) {
  if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
  New-ItemProperty -Path $path -Name '(default)' -Value $value -PropertyType String -Force | Out-Null
}
function Set-Val($path, $n, $v) {
  if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
  New-ItemProperty -Path $path -Name $n -Value $v -PropertyType String -Force | Out-Null
}

if (Test-Path $clsKey)         { Remove-Item -Path $clsKey -Recurse -Force }
if (Test-Path "$base\$progId") { Remove-Item -Path "$base\$progId" -Recurse -Force }

Set-Default $clsKey $name
$inproc = "$clsKey\InprocServer32"
Set-Default $inproc $comhost
Set-Val $inproc 'ThreadingModel' 'Both'
Set-Default "$clsKey\ProgId" $progId
foreach ($c in $cats) { New-Item -Path "$clsKey\Implemented Categories\$c" -Force | Out-Null }
$cd = "$clsKey\CapeDescription"
Set-Val $cd 'Name'          $name
Set-Val $cd 'Description'   $desc
Set-Val $cd 'CapeVersion'   '1.0'
Set-Val $cd 'About'         'Ejector unit operation. One-dimensional real-fluid ejector / jet pump.'
Set-Val $cd 'VersionNumber' '0.1.0'
Set-Val $cd 'Vendor'        'Anders Andreasen'
$pk = "$base\$progId"
Set-Default $pk $name
Set-Default "$pk\CLSID" $clsid

$scope = if ($Machine) { 'per-machine (HKLM)' } else { 'per-user (HKCU)' }
Write-Host "Registered $scope : $name"
Write-Host "  CLSID  $clsid"
Write-Host "  Server $comhost"
Write-Host "Unregister with unregister-user.ps1 (add -Machine to match)."
