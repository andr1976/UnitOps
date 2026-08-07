# Overnight Summary — Membrane Unit Operation

**For:** morning review. **Session:** 2026-08-06 → 07 (autonomous).

## TL;DR
Model chosen, implemented, and validated. A CAPE-OPEN 1.0 membrane unit operation now **builds clean in
Release (0 warnings / 0 errors)** and **passes all 26 tests** (23 physics + 3 headless COM integration).
The physics reproduces the literature reference (Shindo cross-flow) to ~2×10⁻⁴. Not yet done: a live COFE
round-trip, the `Edit()` GUI, the exact Dias "2D" refinement, and the non-isothermal extension.

## Decision (MemPy out; chose between baseline and Dias-Pinto)
**Selected: Dias-Pinto cross-flow** (`J. Membr. Sci.` 613 (2020) 118278) over the Aziaba/DWSIM baseline.
Rationale (full table in `docs/01-model-comparison-and-decision.md`): true cross-flow spiral-wound physics,
trivially portable (no NLP/solver), robust numerics, natural-gas/CO₂ (offshore) relevance, clean upgrade
path to non-isothermal (Part III), and — decisive for your "experimental validation matters" steer —
it reproduces **Shindo's** cross-flow, which Shindo validated against compiled **experimental** data.

## What was built (`src/`, solution `MembraneUnitOp.sln`)
- **`MembraneCore`** (netstandard2.0) — `LocalPermeateSolver` (Dias Eq. 20–22, reformulated singularity-free)
  and `CrossFlowModel` (stage-cut and area march). Pure, deterministic, PME-agnostic.
- **`MembraneCore.Tests`** (net8.0) — 23 tests: physics invariant (flux ratio), Shindo Tables 4 & 5
  reproduction, mass balance, monotonicity, convergence, area↔θ consistency.
- **`Membrane.CapeOpen`** (net48/x64) — the CO 1.0 Unit Operation: `ICapeUnit`, `ICapeIdentification`,
  `ICapeUtilities`, `ICapeUnitReport`, `IPersistStreamInit`; three material ports; parameter grid
  (permeate pressure, area, flow pattern, per-compound permeances, stage-cut output); reads feed / writes +
  flashes both outlets via the PME Material Object; component-order alignment by compound id; ECape error
  mapping; file logging; COM registration writing the CAPE-OPEN CATIDs + CapeDescription.
- **`Membrane.CapeOpen.Tests`** (net48) — 3 tests driving the full lifecycle through a mock Material Object
  (no COFE needed): confirms delegation to `CrossFlowModel`, outlet writes/flashes, stage-cut output, mass
  balance, and `ECapeBadInvOrder` when Calculate precedes Validate.

## Validation (see `docs/03-validation-and-findings.md`)
- Case 1 (NH₃/H₂/N₂) and Case 2 (H₂/CH₄/CO/N₂/CO₂) cross-flow reproduce Shindo's permeate compositions to
  ~2×10⁻⁴ (e.g. Case 2 H₂ 0.4708 vs 0.4707).

### Two findings worth your eye
1. **Table 1 typo:** Case-2 `S_H₂` is printed `4.80×10⁻⁹` but must be `4.80×10⁻¹⁰` — the printed value gives
   ~3× the reported H₂ enrichment and contradicts microporous-glass Knudsen selectivity; `10⁻¹⁰` reproduces
   Shindo to 4 decimals. Tests use `10⁻¹⁰`. (Please sanity-check against your copy of the paper.)
2. **The paper's own "2D model" (Tables 6/7) separates ~0.05–0.09 *less* than cross-flow** at the same stage
   cut. An accumulated-permeate coupling I tried reduces to cross-flow and does **not** reproduce it; the
   extra permeate-side mechanism isn't determinable from the paper alone. Shipping uses the validated
   cross-flow; `TwoDimensionalModel` is retained but marked EXPERIMENTAL and unused by the adapter.

## Decisions I made autonomously (flag if you'd choose differently)
- Ship **cross-flow** (validated) rather than block on the unreproducible "2D model" refinement.
- Used `S_H₂ = 4.80×10⁻¹⁰` for Case 2 (typo correction, justified above).
- Config via COFE's **parameter grid** (permeances discovered from the feed) instead of a custom `Edit()`
  GUI for now — functional without extra UI; an Avalonia editor can slot into `Edit()` later.
- New CLSID `B2E8A6C1-…` / ProgId `ORS.MembraneUnitOperation.1`; vendor "ORS Consulting" in CapeDescription.
- Did **not** run `git init`/commit (per our commit-only-when-asked rule). Say the word and I'll initialise
  a repo and commit this with a clean history. A `.gitignore` is in place.

## Next steps (need you / an interactive session)
1. **Live COFE test** — register (`src/Membrane.CapeOpen/bin/x64/Release/register.bat`, as Admin) and drop
   the unit into a flowsheet with a property package; confirm palette appearance, a CO₂/CH₄ solve, and
   save/load persistence. (Registration/COFE couldn't be done headlessly overnight.)
2. **`Edit()` GUI** — optional Avalonia editor for compound/permeance entry (per the cape-open skill).
3. **Flow patterns** — add validated counter-current & co-current 1D (Shindo Tables 4/5 give targets),
   exposed via the existing FlowPattern option parameter.
4. **Non-isothermal (Part III)** — energy balance + Joule–Thomson. **Formulation now recovered** from the
   open-access Part IV (all equations in `docs/05-non-isothermal-part3-formulation.md`); Part III itself
   isn't needed to implement. Add an energy port and delegate Cp/enthalpy/JT-coefficient (μ from
   −(1/cp)(∂H/∂P)_T on PME enthalpy) + a dew-point flash to the PME. **Correction to an earlier note:** the
   whole Dias series (incl. Part III) uses a **partial-pressure** driving force — real-gas enters only via
   virial *properties*, never a fugacity-corrected flux — so there is **no fugacity driving-force change**;
   the non-isothermal step adds the energy balance + property calls, and needs the 2D grid working first.
5. **Resolve the "2D model" gap** — obtain the Dias reference code or Part III's fuller treatment to nail the
   ~0.05 difference, if that refinement is wanted.

## How to build / test / register
```
cd src
dotnet build MembraneUnitOp.sln -c Release
dotnet test  MembraneUnitOp.sln            # 26 pass
```
Register for COFE (Admin): `src/Membrane.CapeOpen/bin/x64/Release/register.bat`.
Runtime log (for PME debugging): `membrane_capeopen.log` next to the DLL.
