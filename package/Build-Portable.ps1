<#
.SYNOPSIS
    Assemble the Membrane CAPE-OPEN unit operation into a portable, no-installer ZIP.

.DESCRIPTION
    Stages the built COM adapter, its managed dependency, the vendored CAPE-OPEN 1.1 PIA, the
    per-user / per-machine registration scripts, the example flowsheet(s), a README, and the
    technical-reference docs (if built) into a single self-contained folder, then zips it.

    The result registers per-user (HKCU, no admin) via register-user.ps1 or per-machine (HKLM,
    admin) via register.bat. It is NOT an installer -- the user just extracts and runs a script.

    Builds the adapter first if the output is not already present.

.PARAMETER Version
    Version string embedded in the ZIP name, staging folder name, and README.

.PARAMETER Configuration
    Build configuration to stage from (default Release).

.EXAMPLE
    ./package/Build-Portable.ps1 -Version 1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo    = Split-Path -Parent $PSScriptRoot          # package/ -> repo root
$adapter = Join-Path $repo 'src/Membrane.CapeOpen'
$outDir  = Join-Path $PSScriptRoot 'Output'
$pkgName = "Membrane-Portable-$Version"
$stage   = Join-Path $outDir $pkgName
$zipPath = Join-Path $outDir "$pkgName.zip"

function Find-Built([string]$fileName) {
    $bin = Join-Path $adapter 'bin'
    if (-not (Test-Path $bin)) { return $null }
    Get-ChildItem $bin -Recurse -Filter $fileName -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\$Configuration\\" -and $_.FullName -match 'net8\.0-windows' } |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

# --- 1. Ensure the adapter is built ---
$dll = Find-Built 'Membrane.CapeOpen.dll'
if (-not $dll) {
    Write-Host "Membrane.CapeOpen.dll not found under bin/$Configuration -- building..."
    & dotnet build (Join-Path $adapter 'Membrane.CapeOpen.csproj') -c $Configuration -v minimal
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }
    $dll = Find-Built 'Membrane.CapeOpen.dll'
}
if (-not $dll) { throw "Membrane.CapeOpen.dll still not found after build." }
$binDir = $dll.DirectoryName
Write-Host "Staging from: $binDir"

# --- 2. Clean staging ---
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

# --- 3. Core binaries (self-contained: adapter + physics + PIA) ---
$must = @(
    (Join-Path $binDir 'Membrane.CapeOpen.dll'),
    (Join-Path $binDir 'Membrane.CapeOpen.comhost.dll'),        # native activation shim (registered as InprocServer32)
    (Join-Path $binDir 'Membrane.CapeOpen.runtimeconfig.json'), # tells comhost which .NET runtime to load
    (Join-Path $binDir 'Membrane.CapeOpen.deps.json'),          # dependency manifest for the comhost load
    (Join-Path $binDir 'MembraneCore.dll'),
    (Join-Path $repo   'lib/CAPE-OPENv1-1-0.dll')
)
foreach ($f in $must) {
    if (-not (Test-Path $f)) { throw "Required file missing: $f" }
    Copy-Item $f $stage -Force
}
# .pdb if present (harmless, aids diagnostics)
Get-ChildItem $binDir -Filter '*.pdb' -ErrorAction SilentlyContinue | ForEach-Object { Copy-Item $_.FullName $stage -Force }

# --- 4. Registration scripts (shipped for the user to run) ---
foreach ($s in 'register.bat','unregister.bat','register-user.ps1','unregister-user.ps1') {
    $p = Join-Path $adapter $s
    if (Test-Path $p) { Copy-Item $p $stage -Force } else { Write-Warning "Missing registration script: $s" }
}

# --- 5. Example flowsheet(s) ---
$fsDir = Join-Path $stage 'Flowsheets'
$fsSrc = Get-ChildItem (Join-Path $repo 'flowsheet') -Filter '*.fsd' -ErrorAction SilentlyContinue
if ($fsSrc) {
    New-Item -ItemType Directory -Path $fsDir -Force | Out-Null
    $fsSrc | ForEach-Object { Copy-Item $_.FullName $fsDir -Force }
}

# --- 6. Technical reference docs (if built locally / in CI) ---
$docsDst  = Join-Path $stage 'docs'
$htmlSrc  = Join-Path $repo 'docs/techref/_build/html'
$pdfSrc   = Join-Path $repo 'docs/techref/_build/latex/membrane_techref.pdf'
if (Test-Path $htmlSrc) {
    New-Item -ItemType Directory -Path (Join-Path $docsDst 'techref-html') -Force | Out-Null
    Copy-Item (Join-Path $htmlSrc '*') (Join-Path $docsDst 'techref-html') -Recurse -Force
}
if (Test-Path $pdfSrc) {
    New-Item -ItemType Directory -Path $docsDst -Force | Out-Null
    Copy-Item $pdfSrc (Join-Path $docsDst 'Membrane-TechRef.pdf') -Force
}

# --- 7. README ---
$asmVer = try { [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName).Version.ToString() } catch { 'unknown' }
$readme = @"
Membrane (Gas Permeation, Cross-Flow) - CAPE-OPEN 1.0 Unit Operation
====================================================================
Portable package $Version   (assembly $asmVer)   -   Windows x64, no installer

WHAT THIS IS
  A CAPE-OPEN 1.0 unit operation for spiral-wound / hollow-fibre gas-permeation
  membranes (solution-diffusion, cross-flow / co- / counter-current). Verified in
  COCO/COFE and DWSIM. Thermodynamics are taken FROM the flowsheet's property
  package via the Material Object - this unit does not carry its own thermo.

REQUIREMENTS (on the target PC)
  - Windows x64
  - .NET 8 Desktop runtime (the unit is a .NET 8 assembly; a native comhost shim
    lets both native PMEs (COCO/COFE) and .NET-based PMEs (DWSIM) load it)
  - A CAPE-OPEN PME (COCO/COFE or DWSIM) with a property package (e.g. TEA)

INSTALL (choose ONE)
  Per-user, no admin (recommended):
      Right-click 'register-user.ps1' > Run with PowerShell
      (or:  powershell -ExecutionPolicy Bypass -File register-user.ps1)
  Per-machine, admin:
      Right-click 'register.bat' > Run as administrator

  Extract this folder to a STABLE location first (e.g. %LOCALAPPDATA%\Programs\Membrane).
  The registration records the current path; if you move the folder, re-run the script.

USE
  In COCO/COFE or DWSIM the block appears in the unit-operation palette as
  "Membrane (Gas Permeation, Cross-Flow)". Add it, connect a Feed inlet and
  Retentate + Permeate outlets, set per-component permeances and operating conditions
  in the parameter grid, and solve.

EXAMPLE
  Flowsheets\Flowsheet1.fsd - validated 8-bar propane/propylene cross-flow case
  (uses COCO's TEA property package).

UNINSTALL
  Per-user:     powershell -ExecutionPolicy Bypass -File unregister-user.ps1
  Per-machine:  Run 'unregister.bat' as administrator

DOCS
  docs\ contains the Technical Reference (methods, architecture, validation) if bundled.

(c) 2026 Anders Andreasen. Licensed under the MIT License. Bundled CAPE-OPENv1-1-0.dll is the CO-LaN reference PIA (redistributable).
"@
Set-Content -Path (Join-Path $stage 'README.txt') -Value $readme -Encoding UTF8

# --- 8. Zip ---
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $stage -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Portable package contents:"
Get-ChildItem $stage -Recurse | ForEach-Object { "   $($_.FullName.Substring($stage.Length + 1))" }
Write-Host ""
Write-Host "ZIP: $zipPath  ($([math]::Round((Get-Item $zipPath).Length / 1KB)) KB)"
