<!--
  DRAFT SKELETON — Computers & Chemical Engineering (full-length software/tool article).
  Bracketed [[...]] items are placeholders. Bulleted notes under each heading are
  authoring guidance, not final prose. Do NOT invent results: pull validated numbers
  and figures from docs/techref/ (validation.rst, figures/) or the test suite.
-->

# [[Working title]]

Candidates:
1. An open, CAPE-OPEN gas-permeation membrane unit operation for process simulators
2. A portable CAPE-OPEN unit operation for multicomponent gas-permeation membranes
   with real-gas and non-isothermal effects

**Authors:** [[Author One]]$^{a}$, [[…]]
**Affiliations:** $^{a}$[[Affiliation — placeholder]]
**Corresponding author:** [[name, email]]

---

## Highlights
<!-- Elsevier highlights: 3–5 bullets, ≤85 characters each. -->
- [[Open-source CAPE-OPEN membrane unit op runs in any compliant process simulator]]
- [[Thermodynamics delegated to the flowsheet property package via the Material Object]]
- [[Real-gas fugacity driving force + adiabatic non-isothermal (JT) layer]]
- [[Validated against multiple literature benchmarks]]

## Abstract
<!-- ~150–250 words, structured: problem → approach → what the software does →
     validation → significance. Write LAST, after the case study is chosen. -->
[[Placeholder abstract.]]

## Keywords
gas separation membranes; solution–diffusion; CAPE-OPEN; process simulation;
open-source software; [[+2–3]]

---

## 1. Introduction
- Membrane gas separation is industrially important (NG sweetening, H₂ recovery, air
  separation, biogas upgrading, post-combustion CO₂) — cite `BakerLokhandwala2008`,
  `Merkel2010`, `Bernardo2009`.
- **Gap:** rigorous multicomponent permeation models exist (Weller–Steiner, Shindo,
  Pan, Dias) but are typically locked in standalone codes; general process simulators
  ship weak/absent membrane units, so system-level studies are hard.
- **Standard:** CAPE-OPEN decouples unit operations from the simulator; a compliant
  unit runs in COCO/COFE, Aspen, UniSim, etc. — cite `cocolan_uo`.
- **This work's contributions** (state explicitly):
  1. an open, portable CAPE-OPEN unit op for multicomponent gas permeation;
  2. thermodynamics delegated to the host property package (Material Object);
  3. real-gas fugacity driving force + optional adiabatic non-isothermal layer;
  4. validation across independent literature benchmarks + a flowsheet case study.
- Relation to prior open tools: DWSIM membrane, MemPy (`dejaco2020`), Aziaba
  (`aziaba2022`) — what is different here (portability + thermo delegation).

## 2. Model formulation
- Solution–diffusion basis; permeance vs permeability/thickness — `wijmans1995`,
  `baker2004`. Reuse equations from `docs/techref/solution_diffusion.rst`.
- Multicomponent **cross-flow** ODEs (local permeate composition) — `weller1950`,
  `geankoplis` §13.6, `shindo1985`. Reuse `docs/techref/crossflow.rst`.
- **Co-/counter-current** plug-flow patterns — `shindo1985`, `pan1986`. Reuse
  `docs/techref/flow_patterns.rst`.
- **Driving force:** ideal partial-pressure vs **real-gas fugacity** (φ from the PME).
  Reuse `docs/techref/` real-gas section.
- **Non-isothermal** adiabatic energy balance / Joule–Thomson — reuse
  `docs/techref/nonisothermal.rst` and `fontoura2022` for context.
- Assumptions/limitations table (isothermal default, no channel pressure drop, etc.).

## 3. Numerical solution
- Stage-cut march: classical **RK4** (default 2000 steps) —
  `src/MembraneCore/Models/CrossFlowModel.cs`.
- Area march / position profiles: explicit **forward Euler** (default 4000 steps).
- Local permeate solver (singularity-free) — `src/MembraneCore/Solvers/LocalPermeateSolver.cs`.
- Design vs rating: **bisection** back-solving area from a target stage cut.
- Reuse `docs/techref/solution_methods.rst`.

## 4. Software architecture and implementation
- **Core/adapter split:** `MembraneCore` (PME-agnostic physics) + `Membrane.CapeOpen`
  (COM/CAPE-OPEN adapter). Architecture figure: `docs/techref/figures/tikz_arch.png`.
- CAPE-OPEN adapter: ports, parameters, `Validate`/`Calculate`, `IPersistStreamInit`.
- **Thermo delegation:** enthalpy & fugacity via the Material Object (`EnthalpyProvider`,
  `MaterialObjectAdapter`); graceful fallback to isothermal/ideal if the PME can't deliver.
- Portability: net48/x64 COM in-process server; per-user (HKCU) or per-machine (HKLM)
  registration; vendored CAPE-OPEN PIA; no-installer portable package; CI gate that
  solves example flowsheets headlessly in COFEStand.
- Reproducibility: MIT licence; public repo; test suite.

## 5. Validation
<!-- Pull exact numbers/figures from docs/techref/validation.rst — do NOT retype from memory. -->
- Analytic cross-flow vs Geankoplis Weller–Steiner (Ex. 13.6-1) — `weller1950`, `geankoplis`.
- Shindo multicomponent cases (cross-/counter-current) — `shindo1985`.
- Dias spiral-wound cases — `diaspinto2020`.
- Aziaba hollow-fibre counter-current (DWSIM cross-check) — `aziaba2022`.
- MemPy air-separation experiment cross-check — `dejaco2020`.
- **Real-gas vs ideal-gas** driving-force study (own PR-EOS benchmark): quantify the
  stage-cut discrepancy vs pressure. Figure: `docs/techref/figures/val_ig_rg.png`.
- Summarize the automated test suite (count + what each class covers).

## 6. Illustrative example / case study
> **PARKED — decision pending.** Candidate showcases: (a) CO₂/CH₄ NG sweetening at
> high pressure; (b) H₂ recovery/purification; (c) O₂/N₂ air separation; (d) multi-stage
> with recycle + compression. Purpose: demonstrate *in-simulator* value — the membrane
> solved alongside compressors/exchangers using the host's thermodynamics. Flowsheet,
> results, and figures to be produced once chosen.

## 7. Discussion
- What the interoperability enables (system-level trade studies, no re-implementation).
- Positioning vs. DWSIM/MemPy/bespoke Aspen models (feature/portability comparison table).
- Limitations: 2D model (experimental/unused), channel pressure drop, first-order φ.

## 8. Conclusions and future work
- Recap contributions; the multi-unit-op repository direction (see repo restructure note).

## Code availability
- Repository: [[URL once public]] — licence: MIT.
- Archived release: [[Zenodo DOI]] (version [[vX.Y.Z]]).
- Documentation: technical reference in `docs/techref/` (HTML + PDF).

## Acknowledgements / Funding / CRediT / Declaration of competing interests
[[Placeholders — fill at submission.]]

## References
See `references.bib`.
