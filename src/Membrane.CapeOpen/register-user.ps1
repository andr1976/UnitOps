#requires -version 3
# COM registration of the membrane unit operation for CAPE-OPEN.
#
# The unit ships as a .NET 8 assembly activated through the generated native comhost shim
# (Membrane.CapeOpen.comhost.dll). Registering InprocServer32 -> comhost.dll (rather than mscoree)
# makes .NET-based hosts such as DWSIM receive a real COM RCW, so their managed cast to the
# CapeOpen.* interfaces becomes a COM QueryInterface and succeeds; native hosts (COCO/COFE) are
# unaffected. comhost's own DllRegisterServer does NOT write the CAPE-OPEN CATIDs or CapeDescription,
# so this script writes the full set of keys directly.
#
# Requires the .NET 8 Desktop runtime on the machine. Run from the folder that contains
# Membrane.CapeOpen.comhost.dll:
#   powershell -ExecutionPolicy Bypass -File register-user.ps1             (HKCU, no admin)
#   powershell -ExecutionPolicy Bypass -File register-user.ps1 -Machine    (HKLM, Administrator)
param([switch]$Machine)
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$comhost = Join-Path $scriptDir 'Membrane.CapeOpen.comhost.dll'
if (-not (Test-Path $comhost)) { Write-Error "Not found: $comhost  (build net8 x64 Release first)"; exit 1 }

$clsid  = '{B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44}'
$progId = 'Membrane.MembraneUnitOperation.1'
$name   = 'Membrane (Gas Permeation, Cross-Flow)'
$desc   = 'Spiral-wound gas-permeation membrane (cross-flow, solution-diffusion, isothermal). Thermodynamics delegated to the flowsheet property package.'
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

# Clean slate (also clears any earlier net48/mscoree registration under the same CLSID).
if (Test-Path $clsKey)         { Remove-Item -Path $clsKey -Recurse -Force }
if (Test-Path "$base\$progId") { Remove-Item -Path "$base\$progId" -Recurse -Force }

Set-Default $clsKey $name
$inproc = "$clsKey\InprocServer32"
Set-Default $inproc $comhost              # native comhost shim (NOT mscoree)
Set-Val $inproc 'ThreadingModel' 'Both'

Set-Default "$clsKey\ProgId" $progId
foreach ($c in $cats) { New-Item -Path "$clsKey\Implemented Categories\$c" -Force | Out-Null }

$cd = "$clsKey\CapeDescription"
Set-Val $cd 'Name'          $name
Set-Val $cd 'Description'    $desc
Set-Val $cd 'CapeVersion'   '1.0'
Set-Val $cd 'About'         'Membrane unit operation. Cross-flow gas permeation.'
Set-Val $cd 'VersionNumber' '1.0.0'
Set-Val $cd 'Vendor'        'Anders Andreasen'

$pk = "$base\$progId"
Set-Default $pk $name
Set-Default "$pk\CLSID" $clsid

$scope = if ($Machine) { 'per-machine (HKLM)' } else { 'per-user (HKCU)' }
Write-Host "Registered $scope : $name"
Write-Host "  CLSID  $clsid"
Write-Host "  Server $comhost"
Write-Host "  (comhost shim; requires the .NET 8 Desktop runtime). Open/restart COFE or DWSIM."
Write-Host "Unregister with unregister-user.ps1 (add -Machine to match)."
