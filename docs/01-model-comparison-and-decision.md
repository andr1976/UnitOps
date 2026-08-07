# Membrane Unit Operation — Model Comparison & Decision

**Date:** 2026-08-06
**Context:** Converting a gas-permeation membrane unit operation to a CAPE-OPEN 1.0 compliant
unit operation usable in COFE. Ground truth = `cape-open/CO_Unit_Operations_v6.25.pdf` (+ errata).
The CAPE-OPEN interface/COM layer is model-agnostic; this document decides the **calculation core**.

## Candidates evaluated

| # | Family | Source | Status |
|---|--------|--------|--------|
| A | **Baseline** — perfectly-mixed cells-in-series SDM | Aziaba et al., *Membranes* 12 (2022) 1186; DWSIM `Membrane.vb` | Considered |
| B | **MemPy** — spiral-wound, PR real-gas, IPOPT NLP | DeJaco et al., *AIChE J* 2020; `MemPy-1.0` | **Rejected** (user) |
| C | **Dias-Pinto** — iterative 2D cross-flow (+ non-isothermal Part III) | Dias et al., *J. Membr. Sci.* 613 (2020) 118278; Parts II–IV | **SELECTED** |

MemPy was rejected by the user (its value is a monolithic Pyomo/IPOPT NLP that cannot ship in a
COM DLL; delegating thermo to the PME would force ~10^5–10^6 out-of-process property calls per
2D solve, and even the 1D port needs a bespoke Newton). Decision is therefore **A vs C**.

## Head-to-head: Baseline (A) vs Dias-Pinto (C)

| Criterion | A — Baseline (Aziaba/DWSIM) | C — Dias-Pinto Part I | Winner |
|---|---|---|---|
| **Flow model** | Perfectly-mixed **cells-in-series** (1 cell = CSTR; ≥5 ≈ plug flow). Co/counter-current. **Gas cross-flow branch is empty (unimplemented).** | True **2D cross-flow** spiral-wound leaf (retentate ∥ length, permeate ∥ width) + a 1D Shindo model with counter/co/cross/one-side-mixing/perfect-mixing modes. | **C** |
| **Driving force / gas law** | Ideal-gas, partial pressure. Isothermal. Constant permeance. | Ideal-gas, partial pressure. Isothermal. Constant permeance. (Same tier.) | tie |
| **Numerics / robustness** | Successive substitution; **no clamping of x/y**, counter-current `ln((pxf−pyp)/(pxr−pyi))` **blows up** on sign reversal / p→0; **silent** non-convergence (writes partial results). | Single **explicit forward sweep, no outer iteration** (cross-flow); only per-node **single-variable monotonic root** (Eq. 20) → 10-line Newton/bisection. Sub-0.1 s. Deterministic. | **C** |
| **External solver dependency** | None (but fragile). | **None** — no NLP/IPOPT. | tie (C cleaner) |
| **PME property-call cost** | ~0 (2 outlet flashes only). | ~0 for Part I (partial pressures). ~2·Nx·Ny φ-calls only if upgraded to real-gas. | tie |
| **Experimental validation in the source paper** | Direct vs **experiments**: Sada (CO2/O2/N2, <0.84%), Chowdhury (H2/N2/CH4/Ar, ~97.5% H2), Park & Koch (pervaporation). But gas cases are **graphical**; only pervaporation TC1 is an exact numeric table. | Method verified vs **Shindo** reference model with **clean numeric tables** (Tables 4–7) = excellent regression fixtures; industrial CO2 cases (natural gas Case 3; flue gas Case 4 ≈ **90% CO2 recovery**, matches MTR/Polaris). Validation is model-vs-reference + industrial, less raw-experimental. | A (raw) / C (fixturable) |
| **Offshore / domain fit** | Generic + pervaporation-leaning. | **Natural-gas CO2 removal, Petrobras** — directly offshore-relevant. | **C** |
| **Non-isothermal upgrade path** (user interest) | Pervaporation temperature path in code is **buggy and never applied**; effectively isothermal-only. | **Part III (ChERD 177, 2022)** = *the same 2D model + energy balance + Joule–Thomson + inter-stream heat exchange*, built on an **offshore benchmark**. Clean, documented seam. | **C** |
| **Portability of code** | VB.NET + heavy DWSIM base-class/property-package/UI plumbing to strip. | Pure algebra + 1D root; no framework entanglement. | **C** |

## Decision — **C (Dias-Pinto Part I framework)**

Dias-Pinto wins on flow-model fidelity, numerical robustness, offshore relevance, testability, and —
decisively for the stated interest — a documented, same-authors **non-isothermal upgrade (Part III)**.

### Honoring the "experimental validation is important" directive

The one axis where the baseline looks stronger is *raw* experimental comparison. Choosing C does **not**
sacrifice this, because the Dias-Pinto framework **includes the 1D Shindo model in counter/co/cross modes**.
We therefore validate the C# port against **both**:

1. **Reference-model regression (exact):** Dias-Pinto Tables 4–7 (Shindo 1D counter/cross cases 1–2, and
   the 2D cross-flow cases 1–2) — machine-checkable to ±0.01 on permeate fractions and stage cut.
2. **Experimental datasets (inherited from the baseline's paper):** run the **1D counter-current** mode on
   **Sada** (CO2/O2/N2) and **Chowdhury** (H2/N2/CH4/Ar), comparing to the reported experimental
   stage-cut/purity (digitizing the stage-cut curves where needed). Optionally the pervaporation TC1.

This gives stronger validation coverage than either paper alone: established-reference verification **and**
experimental agreement, in one test suite.

## Scope & upgrade roadmap

- **Phase 1 (tonight's target):** isothermal, ideal-gas, partial-pressure, constant-permeance core —
  **2D cross-flow** + **1D Shindo (counter/co/cross)** — fully unit-tested against fixtures above.
- **Phase 2 (seam ready, not built):** swap partial pressure → **fugacity** (real-gas) by injecting PME
  `ICapeThermoPropertyRoutine` φ-calls at the flux law and local-equilibrium solver.
- **Phase 3 (seam ready, not built):** **non-isothermal** energy balance + Joule–Thomson (Part III), using
  the PME's enthalpy / Cp / JT-coefficient. The flux law and per-node solver are structured so both bolt on.

## Key implementation cautions (from source analysis)

- **Permeance units:** Dias-Pinto Table 1 quotes S_i in mol·m⁻²·s⁻¹·Pa⁻¹ = a **permeance**; the paper's
  groups also divide by thickness `l`, which double-counts. **Treat S_i as a permeance**; use
  `J_i = S_i·(x_i·P_r − y_i·P_p)` and `Sx = S_m·P_r·L/(F_f·h_x)`, `Sy = S_m·P_r·W/(F_f·h_y)` (no extra 1/l).
- **Discretization sign:** the printed PDE (Eqs. 3–4) has a leading-minus typo inconsistent with the
  discretized recursion (Eqs. 23–24) and Fig. 2. **Use the discretized recursion** (retentate loses along +x,
  permeate gains along +y).
- **Grid:** 1D needs only 4–5 points; 2D uses 50×50 (rel. error 0.01% vs 200×200 reference).
- **CAPE-OPEN:** implement **CO 1.0 Unit Operation** (CATID `{678C09A5-7D66-11D2-A67D-00105A42887F}`),
  register `Consumes_Thermo` + `SupportsThermodynamics10/11`; read feed read-only, flash both outlets;
  never set inlet MOs; honor the zero-flow-stream composition rule (errata §2.4).
