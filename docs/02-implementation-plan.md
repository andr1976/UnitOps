# Implementation Plan — CAPE-OPEN Membrane Unit Operation

## Architecture (adapter pattern)

Three projects under `src/`, so the QA-critical physics is testable independently of COM/COFE:

```
src/MembraneUnitOp.sln
├── MembraneCore            (netstandard2.0)  — pure physics engine, NO COM/CAPE-OPEN/UI
├── MembraneCore.Tests      (net8.0, xUnit)   — validation vs paper fixtures  ← tonight's TDD loop
└── Membrane.CapeOpen       (net48, x64)      — CO 1.0 Unit Operation adapter; delegates to Core
```

Rationale: `MembraneCore` targets **netstandard2.0** so it loads into both .NET Framework 4.8 (the COM
adapter) and modern .NET (the test project). Tests run via `dotnet test` with the installed SDK 9 — no
VS/COFE needed to verify the physics. The COM adapter is a thin translation layer (read feed → call Core →
write+flash outlets), matching the CAPE-OPEN "interface layer, not a calculation engine" principle.

## MembraneCore design

**Data types**
- `Component` (id/CAS/name/MW) — MW only needed for mass↔mole; identity used to align with the PME.
- `MembraneModelInput` — components[], feed molar flow + mole fractions, feed T, feed pressure `Pr`,
  permeate pressure `Pp`, per-component permeance `S[i]` (mol·m⁻²·s⁻¹·Pa⁻¹), geometry, flow pattern, grid.
- `MembraneGeometry` — L, W, thickness l, channel heights hx/hy, nLeaves, Nx, Ny (area = nLeaves·L·W;
  note: baseline used 2·L·H double-sided — configurable `membraneFacesPerLeaf`).
- `MembraneResult` — retentate & permeate {molar flow, mole fractions}, stage cut θ, per-component
  recovery, permeate/retentate purity, optional 2D fields, convergence diagnostics.

**Physics (Phase 1: isothermal, ideal-gas, partial pressure, constant permeance)**
- `IDrivingForce` → `PartialPressureDrivingForce` computes `p_i,ret = x_i·Pr`, `p_i,perm = y_i·Pp`.
  *(Phase-2 seam: `FugacityDrivingForce` multiplies by φ_i from an `IThermoProvider`.)*
- `LocalPermeateSolver` — solves Dias-Pinto Eq. 20 for the base-component ratio `r = x_b/y_b` via a
  bracketed, monotone **bisection+Newton** root; then Eqs. 21–22 for all y_i. Pure, allocation-light,
  deterministic. Guards: clamp fractions to [0,1], normalize, handle γ→0 and single-component.
- `CrossFlow2DModel` — explicit double sweep over (m=0..Ny, n=0..Nx) using Eqs. 23–24. No outer iteration.
- `OneDimensionalModel` with `FlowPattern ∈ {CoCurrent, CounterCurrent, CrossFlow}`:
  - CoCurrent / CrossFlow → single forward march.
  - CounterCurrent → forward-retentate / backward-permeate relaxation sweeps to a fixed point
    (tol on permeate composition; capped iterations; **throws on non-convergence** — no silent partials).
- `PostProcess` — stage cut, recovery, purity, mass-balance closure.

**Invariants enforced & asserted in tests**
- Per-component mass balance closes to < 1e-6 (feed = retentate + permeate).
- All mole fractions ∈ [0,1], sum to 1.
- Monotonic responses (↑area → ↑stage cut; ↓γ → ↑selectivity effect).
- Deterministic (no RNG, no time, fixed iteration order).

## MembraneCore.Tests (validation suite — honors "experimental validation is important")

1. **Unit/property tests:** local-equilibrium solver correctness & limiting cases; mass-balance closure for
   every model; fraction bounds; monotonicity; determinism (same input → identical output).
2. **Reference-model regression (exact):** Dias-Pinto **Tables 6 & 7** (2D cross-flow, cases 1 & 2) to
   ±0.01 on y_i and θ; **Tables 4 & 5** (1D counter & cross) as cross-checks.
3. **Experimental datasets (inherited):** **Sada** (CO2/O2/N2, counter-current) and **Chowdhury**
   (H2/N2/CH4/Ar, counter-current) feeds+permeances → compare stage-cut/permeate purity to reported
   experimental values. Exact figure points to be digitized (delegated) → tolerance set per data scatter.

## Membrane.CapeOpen (CO 1.0 adapter)

- `MembraneUnitOperation` : `ICapeUnit`, `ICapeIdentification`, `ICapeUtilities`, `IPersistStreamInit`,
  `ICapeUnitReport` (report of results). COM-visible, fixed GUID/ProgId, `ClassInterfaceType.None`.
- Ports (`ICapeUnitPort`+`ICapeIdentification`, exposed via `ICapeCollection`):
  **Feed** (material, inlet), **Retentate** (material, outlet), **Permeate** (material, outlet).
  *(Energy port deferred to non-isothermal phase.)*
- Parameters (`ICapeParameter`+typed `*ParameterSpec` via `ICapeCollection`): permeate pressure,
  geometry (L, W, l, hx, hy, nLeaves), grid (Nx, Ny), flow pattern (option), per-component permeance
  (handled via a real parameter per active compound), plus **output** params: stage cut, area, recoveries.
- `MaterialObjectAdapter`: read feed via `ICapeThermoMaterialObject.GetProp` (T, P, fraction, totalFlow,
  ComponentIds); align component order by id/CAS; write outlets (fraction + totalFlow + P + T) then
  `CalcEquilibrium("TP")`. Never write inlet; honor zero-flow composition rule.
- Lifecycle: `Validate` (ports connected, permeances present & ≥0, geometry > 0) → `Calculate`
  (read → `MembraneCore` → write+flash). Errors mapped to ECape HRESULTs.
- Registration: HKCU per-user; CATID `{678C09A5-…}` + `Consumes_Thermo` + `SupportsThermodynamics10/11`;
  `register.bat` / `unregister.bat`. References the installed PIAs.

## Tonight's execution order (TDD)

1. Scaffold solution + 3 projects; confirm `dotnet build`/`dotnet test` green on an empty test.
2. `LocalPermeateSolver` + tests (Eq. 20–22).
3. `CrossFlow2DModel` + Table 6/7 tests → iterate to green.
4. `OneDimensionalModel` (all patterns) + Table 4/5 + experimental tests → iterate to green.
5. Mass-balance/invariant tests across all models.
6. `Membrane.CapeOpen` adapter (build; COFE test is a morning/interactive step).
7. Docs: test report + morning summary + open questions.
