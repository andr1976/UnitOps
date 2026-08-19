# UnitOps — CAPE-OPEN Membrane Unit Operation

A CAPE-OPEN 1.0 compliant **gas-permeation membrane** unit operation, plus its validated
calculation core. It is a COM component **verified in COCO/COFE and DWSIM**, built as a .NET 8
assembly activated through the comhost shim so both native and .NET-based hosts load it.

## Layout

```
cape-open/     CAPE-OPEN Unit Operation spec (ground truth) + errata
membrane/      Source models studied: Aziaba/DWSIM (baseline), MemPy, Dias-Pinto (chosen)
docs/          Decision, plan, validation & findings, morning summary
src/           The implementation (see below)
```

## src/ — solution `MembraneUnitOp.sln`

| Project | Target | Role |
|---|---|---|
| `MembraneCore` | netstandard2.0 | Pure physics engine (cross-flow permeation). No COM/CAPE-OPEN deps. |
| `MembraneCore.Tests` | net8.0 (xUnit) | Physics validation vs literature (Shindo/Dias). |
| `Membrane.CapeOpen` | net8.0-windows, x64 (comhost) | CAPE-OPEN 1.0 Unit Operation adapter (COM). Delegates physics to Core, thermo to the PME. |
| `Membrane.CapeOpen.Tests` | net8.0-windows (xUnit) | Headless end-to-end adapter tests via a mock Material Object. |

## Model

Cross-flow, isothermal, ideal-gas, partial-pressure driving force, constant permeance
(Dias et al., *J. Membr. Sci.* 613 (2020) 118278). Chosen over the Aziaba/DWSIM baseline and MemPy —
see `docs/01-model-comparison-and-decision.md`. Validated: reproduces Shindo's cross-flow (the paper's
experimentally-grounded reference) to ~2×10⁻⁴; see `docs/03-validation-and-findings.md`.

Ports: **Feed** (inlet) → **Retentate** + **Permeate** (outlets).
Parameters: permeate pressure, membrane area, flow pattern, and one permeance per compound (discovered
from the feed); output: stage cut. Thermodynamic flashes are delegated to the flowsheet property package.

## Build & test

```sh
cd src
dotnet build MembraneUnitOp.sln -c Release      # requires the .NET 8 SDK
dotnet test  MembraneUnitOp.sln                 # 56 tests
```

## Register for COCO/COFE and DWSIM (Windows)

Requires the **.NET 8 Desktop runtime**. Registration points the COM `InprocServer32` at the generated
`Membrane.CapeOpen.comhost.dll` shim — so .NET-based hosts (DWSIM) receive a real COM object, not the raw
managed instance — and adds the CAPE-OPEN CATIDs + CapeDescription.

```sh
cd src/Membrane.CapeOpen/bin/Release/net8.0-windows/win-x64
powershell -ExecutionPolicy Bypass -File register-user.ps1     # per-user (HKCU), no admin
# or, as Administrator, per-machine (HKLM):   register.bat
```
The unit then appears in the unit-operation palette as *"Membrane (Gas Permeation, Cross-Flow)"* in both
COCO/COFE and DWSIM. `unregister-user.ps1` (add `-Machine` for HKLM) or `unregister.bat` removes it.

## Status

Physics core and CAPE-OPEN adapter are implemented, build clean, and pass all unit + headless integration
tests. Not yet done (see `docs/04-morning-summary.md`): live COFE round-trip, the `Edit()` GUI, the exact
Dias "2D model" refinement, and the non-isothermal (Part III) extension.

## License

Released under the [MIT License](LICENSE). © 2026 Anders Andreasen.

The bundled `lib/CAPE-OPENv1-1-0.dll` is the CO-LaN reference Primary Interop Assembly, redistributed under
its own terms.
