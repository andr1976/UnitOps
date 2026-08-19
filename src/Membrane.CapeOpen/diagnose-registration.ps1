#requires -version 3
<#
  Diagnose why the membrane CAPE-OPEN unit operation is (not) visible to a PME
  such as Aspen HYSYS or UniSim Design. Run this ON THE MACHINE WHERE THE PME RUNS.

      powershell -ExecutionPolicy Bypass -File diagnose-registration.ps1

  It checks the CLSID registration in all four relevant registry roots (machine/user
  x 64-bit/32-bit view), verifies the CATIDs, CapeDescription and CodeBase, and then
  enumerates every component that implements the CAPE-OPEN Unit Operation category in
  each view -- i.e. exactly what a PME sees when it lists CAPE-OPEN unit operations.
#>

$Clsid    = '{B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44}'
$UnitCat  = '{678C09A5-7D66-11D2-A67D-00105A42887F}'   # CAPE-OPEN 1.0 Unit Operation category
$ExpectCats = @(
  '{678C09A5-7D66-11D2-A67D-00105A42887F}',
  '{4150C28A-EE06-403F-A871-87AFEC38A249}',
  '{0D562DC8-EA8E-4210-AB39-B66513C0CD09}',
  '{4667023A-5A8E-4CCA-AB6D-9D78C5112FED}'
)

function Line($c='-'){ Write-Host ($c * 78) }
Line '='
Write-Host "CAPE-OPEN unit-operation registration diagnostic"
Line '='
Write-Host ("OS is 64-bit          : {0}" -f [Environment]::Is64BitOperatingSystem)
Write-Host ("This PowerShell is 64  : {0}" -f [Environment]::Is64BitProcess)
Write-Host "Target CLSID           : $Clsid"
Write-Host ""

$views = @(
  @{ Hive='LocalMachine'; View='Registry64'; Label='HKLM 64-bit (machine store, what a 64-bit PME reads)' },
  @{ Hive='LocalMachine'; View='Registry32'; Label='HKLM 32-bit / Wow6432Node (what a 32-bit PME reads)' },
  @{ Hive='CurrentUser';  View='Registry64'; Label='HKCU 64-bit (per-user; COFE reads this)' },
  @{ Hive='CurrentUser';  View='Registry32'; Label='HKCU 32-bit / Wow6432Node' }
)

function Open-Base($hive,$view){
  [Microsoft.Win32.RegistryKey]::OpenBaseKey(
    [Microsoft.Win32.RegistryHive]::$hive, [Microsoft.Win32.RegistryView]::$view)
}

foreach($v in $views){
  Line
  Write-Host $v.Label
  $base = Open-Base $v.Hive $v.View
  $cls  = $base.OpenSubKey("Software\Classes\CLSID\$Clsid")
  if(-not $cls){ Write-Host "  CLSID NOT registered here."; continue }
  Write-Host "  CLSID registered.  (default) = '$($cls.GetValue($null))'"

  $inproc = $cls.OpenSubKey('InprocServer32')
  if($inproc){
    $server = $inproc.GetValue($null); $cb = $inproc.GetValue('CodeBase'); $asm = $inproc.GetValue('Assembly')
    Write-Host "  InprocServer32 (default) = '$server'"
    if($server -and $server -match 'mscoree\.dll$'){
      # legacy .NET Framework (mscoree) registration
      Write-Host "  Assembly = '$asm'"
      Write-Host "  CodeBase = '$cb'"
      if($cb){
        try { $p = ([Uri]$cb).LocalPath } catch { $p = $cb }
        Write-Host ("  CodeBase DLL exists      : {0}   ($p)" -f (Test-Path $p))
      } else { Write-Host "  CodeBase MISSING (mscoree cannot locate the assembly unless it is in the GAC)." }
    } else {
      # native / .NET comhost registration: InprocServer32 points straight at the server DLL
      Write-Host ("  Server DLL exists        : {0}" -f (Test-Path $server))
      if($server -match 'comhost\.dll$'){ Write-Host "  (net comhost shim; the .NET 8 runtime + <name>.runtimeconfig.json must sit beside it)" }
    }
  } else { Write-Host "  InprocServer32 MISSING." }

  $ic = $cls.OpenSubKey('Implemented Categories')
  $have = if($ic){ $ic.GetSubKeyNames() } else { @() }
  Write-Host "  Implemented Categories:"
  foreach($c in $ExpectCats){
    $ok = $have -contains $c
    Write-Host ("    {0}  {1}" -f $(if($ok){'[ok]'}else{'[MISSING]'}), $c)
  }
  $cd = $cls.OpenSubKey('CapeDescription')
  if($cd){ Write-Host "  CapeDescription.Name       = '$($cd.GetValue('Name'))'  CapeVersion='$($cd.GetValue('CapeVersion'))'" }
  else   { Write-Host "  CapeDescription MISSING." }
}

# What the PME actually enumerates: every CLSID implementing the Unit Operation category, per view.
foreach($v in $views){
  Line
  Write-Host ("CAPE-OPEN Unit Operations visible in {0}:" -f $v.Label)
  $base = Open-Base $v.Hive $v.View
  $root = $base.OpenSubKey('Software\Classes\CLSID')
  if(-not $root){ Write-Host "  (no CLSID hive)"; continue }
  $found = @(); $mine = $false
  foreach($sub in $root.GetSubKeyNames()){
    try {
      $k = $root.OpenSubKey("$sub\Implemented Categories\$UnitCat")
      if($k){
        $nm = $root.OpenSubKey("$sub\CapeDescription")
        $label = if($nm){ $nm.GetValue('Name') } else { '' }
        $found += ("    {0}  {1}" -f $sub, $label)
        if($sub -ieq $Clsid){ $mine = $true }
      }
    } catch {}
  }
  if($found.Count){ $found | ForEach-Object { Write-Host $_ } } else { Write-Host "    (none)" }
  Write-Host ("  --> OUR unit is {0} in this view." -f $(if($mine){'PRESENT'}else{'ABSENT'}))
}

Line '='
Write-Host "Reading:"
Write-Host " * PME is 64-bit and our unit is ABSENT from 'HKLM 64-bit' but PRESENT in an HKCU view"
Write-Host "     -> register machine-wide: run register.bat as Administrator here (HKLM)."
Write-Host " * PME is 32-bit (server shows only in Program Files (x86); process is 32-bit)"
Write-Host "     -> an x64 server cannot load; a 32-bit (x86) build + 32-bit registration is required."
Write-Host " * CLSID present in the right view but CodeBase DLL missing / path wrong"
Write-Host "     -> it will enumerate but fail to instantiate; fix the DLL path and re-register here."
Write-Host " * InprocServer32 -> *.comhost.dll but the .NET 8 runtime is missing"
Write-Host "     -> it enumerates but activation fails; install the .NET 8 Desktop runtime."
Write-Host " * Works in COFE (native) but not DWSIM (.NET host)"
Write-Host "     -> you are on the old mscoree registration; use the comhost build + register-user.ps1."
Line '='
