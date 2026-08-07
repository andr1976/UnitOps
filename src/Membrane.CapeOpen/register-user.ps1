#requires -version 3
# Per-user (no Administrator) COM registration of the ORS membrane unit operation for CAPE-OPEN / COFE.
# Writes to HKCU\Software\Classes, which COM merges over HKLM\Software\Classes for the current user.
# Run from the folder containing Membrane.CapeOpen.dll:  powershell -ExecutionPolicy Bypass -File register-user.ps1
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $scriptDir 'Membrane.CapeOpen.dll'
if (-not (Test-Path $dll)) { Write-Error "Not found: $dll  (build x64 Release first)"; exit 1 }

$clsid    = '{B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44}'
$progId   = 'ORS.MembraneUnitOperation.1'
$typeName = 'Membrane.CapeOpen.MembraneUnitOperation'
$name     = 'ORS Membrane (Gas Permeation, Cross-Flow)'
$desc     = 'Spiral-wound gas-permeation membrane (cross-flow, solution-diffusion, isothermal). Thermodynamics delegated to the flowsheet property package.'
$runtime  = 'v4.0.30319'
$cats     = @(
  '{678C09A5-7D66-11D2-A67D-00105A42887F}',  # CAPE-OPEN Unit Operation
  '{4150C28A-EE06-403F-A871-87AFEC38A249}',  # Consumes Thermo
  '{0D562DC8-EA8E-4210-AB39-B66513C0CD09}',  # Supports Thermo 1.0
  '{4667023A-5A8E-4CCA-AB6D-9D78C5112FED}'   # Supports Thermo 1.1
)

$asmName  = [System.Reflection.AssemblyName]::GetAssemblyName($dll)
$asmFull  = $asmName.FullName
$asmVer   = $asmName.Version.ToString()
$codebase = ([System.Uri]$dll).AbsoluteUri

$base   = 'HKCU:\Software\Classes'
$clsKey = "$base\CLSID\$clsid"

function Set-Default($path, $value) {
  if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
  Set-ItemProperty -Path $path -Name '(default)' -Value $value
}
function Set-Val($path, $n, $v) {
  if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
  Set-ItemProperty -Path $path -Name $n -Value $v
}

Set-Default $clsKey $name

$inproc = "$clsKey\InprocServer32"
Set-Default $inproc 'mscoree.dll'
Set-Val $inproc 'ThreadingModel' 'Both'
Set-Val $inproc 'Class'          $typeName
Set-Val $inproc 'Assembly'       $asmFull
Set-Val $inproc 'RuntimeVersion' $runtime
Set-Val $inproc 'CodeBase'       $codebase

$verKey = "$inproc\$asmVer"
Set-Val $verKey 'Class'          $typeName
Set-Val $verKey 'Assembly'       $asmFull
Set-Val $verKey 'RuntimeVersion' $runtime
Set-Val $verKey 'CodeBase'       $codebase

Set-Default "$clsKey\ProgId" $progId
foreach ($c in $cats) { New-Item -Path "$clsKey\Implemented Categories\$c" -Force | Out-Null }

$cd = "$clsKey\CapeDescription"
Set-Val $cd 'Name'          $name
Set-Val $cd 'Description'    $desc
Set-Val $cd 'CapeVersion'   '1.0'
Set-Val $cd 'About'         'ORS-Consulting membrane unit operation. Cross-flow gas permeation.'
Set-Val $cd 'VersionNumber' '1.0.0'
Set-Val $cd 'Vendor'        'ORS Consulting'

$pk = "$base\$progId"
Set-Default $pk $name
Set-Default "$pk\CLSID" $clsid

Write-Host "Registered (per-user): $name"
Write-Host "  CLSID    $clsid"
Write-Host "  Assembly $asmFull"
Write-Host "  CodeBase $codebase"
Write-Host "Open/restart COFE; the unit appears in the unit-operation palette. Unregister with unregister-user.ps1."
