# UnitOps — CAPE-OPEN Membrane Unit Operation

A CAPE-OPEN 1.0 compliant **gas-permeation membrane** unit operation for COFE/COCO, plus its validated
calculation core. Converts the membrane model to a COM component usable in any CAPE-OPEN flowsheet
environment.

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
| `Membrane.CapeOpen` | net48, x64 | CAPE-OPEN 1.0 Unit Operation adapter (COM). Delegates physics to Core, thermo to the PME. |
| `Membrane.CapeOpen.Tests` | net48 (xUnit) | Headless end-to-end adapter tests via a mock Material Object. |

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
dotnet build MembraneUnitOp.sln -c Release      # requires .NET SDK + .NET Framework 4.8 targeting pack
dotnet test  MembraneUnitOp.sln                 # 26 tests
```

## Register for COFE (Windows, Administrator)

```sh
cd src/Membrane.CapeOpen/bin/x64/Release
register.bat            # regasm /codebase; adds CAPE-OPEN CATIDs + CapeDescription
```
The unit then appears in COFE's unit-operation palette as *"Membrane (Gas Permeation, Cross-Flow)"*.
`unregister.bat` removes it.

## Status

Physics core and CAPE-OPEN adapter are implemented, build clean, and pass all unit + headless integration
tests. Not yet done (see `docs/04-morning-summary.md`): live COFE round-trip, the `Edit()` GUI, the exact
Dias "2D model" refinement, and the non-isothermal (Part III) extension.

## License

Released under the [MIT License](LICENSE). © 2026 Anders Andreasen.

The bundled `lib/CAPE-OPENv1-1-0.dll` is the CO-LaN reference Primary Interop Assembly, redistributed under
its own terms.
