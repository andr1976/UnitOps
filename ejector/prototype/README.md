# Ejector / jet pump — 0D/1D prototype (Python + CoolProp)

This is the prototype for a future CAPE-OPEN ejector unit operation. One model covers gas–gas, liquid–liquid,
liquid-motive/gas-suction and flashing two-phase ejectors, for pure fluids and mixtures.
Every stream is carried as (P, h, z). All properties come from a homogeneous-equilibrium flash.
The model needs only four flash calls (`ph`, `ps`, `pt`, `pq` at composition z, plus the molar mass).
The CAPE-OPEN unit (`../src`, C#) maps them one-to-one onto PME flashes on a scratch material object;
its engine is a port of this code and is regression-tested against it (`export_regression.py`).

| File | Purpose |
|---|---|
| `ejector1d.py` | Model: `Thermo`, `Expansion` (nozzle + critical flow), `Ejector.rate()` / `.rate_flows()` (both flows given) / `.design()` / `.design_for_lift()` |
| `validate_huang1999.py` | 39-point R141b gas-ejector benchmark (critical ER + critical back pressure) |
| `validate_goncalves2024.py` | Subcooled-liquid CO2 motive ejector (motive flow, ER) |
| `co2_case.py` | Target application: liquid CO2 at 47 bar entraining CO2 vapour at 15 bar |
| `calibrate_r744.py` | Two-stage calibration on Giacomelli 2019 (42 R744 points, ~1 min) |
| `test_ejector1d.py` | 32 regression/verification tests (`python -m pytest -q`) |
| `export_regression.py` | Reference results (31 cases) for the C# engine tests (`../src/EjectorCore.Tests`) |
| `water_air.py` | Steam/water + dry air properties (psychrometric: IAPWS water, CoolProp air, ideal mixing) for steam-jet and liquid-jet cases |
| `validation_sets.py` | Every validation set as model cases (Giacomelli, Huang, Falat, Lawrence, Shestopalov); parallel evaluation and fits |
| `calibrate_sets.py` | Both mixing models fitted on every set, plus leave-one-set-out transfer (evidence for the techref parameter guide) |
| `validate_bodys2020.py` | Motive flow of 150 SINTEF R744 points (liquid motive 47-70 bar): C_d and HNE φ required |
| `validate_gea.py` | GEA catalogue design charts (steam/air, air/air, water/air) against the optimum design |
| `data/` | Validation data with provenance headers (Giacomelli, Bodys, Falat, Lawrence, Shestopalov, GEA) |

The full technical reference (theory, numerics, CAPE-OPEN architecture, validation) is the
Sphinx project in [`../techref`](../techref). Its figures and tables are regenerated from this code by
`techref/figures/gen_data.py`.

## Model

1D ejector with constant-area (default) or constant-pressure mixing (Keenan 1950 → Huang 1999 → Liu & Groll /
Banasiak & Hafner for CO2), written for real fluids:

1. **Motive nozzle.** Isentropic to the throat; isentropic efficiency η_m on the expansion beyond it. The choked mass flux is G* = max_P ρ(P,h)·u(P) on the isentropic HEM path, times a discharge coefficient C_d.
   A supersonic exit pressure follows from G(P_e)·A_e = ṁ_m. An unchoked (liquid) nozzle exhausts at P_y.
   An optional **HNE** delayed-flashing factor φ uses quality x = φ·x_eq in the density only
   (cf. the Henry–Fauske / Diener–Schmidt boiling-delay factor). φ = 1 is HEM.
2. **Suction.** Expansion s0 → P_y with η_s, capped at its own choked flux. For liquid suction this cap is the cavitation limit.
3. **Mixing.** `mixing="CAM"` (constant area): mass, momentum (η_mix on the inlet momentum flux) and energy give one
   residual in P3; the subsonic, post-shock root is taken. `mixing="CPM"` (constant pressure, Huang 1999 with real
   fluids): the motive core takes φ_p of its isentropic area at a hypothetical throat, the suction passes the rest, the
   streams mix at P_y (η_mix on momentum), then a loss-free normal shock. The critical point is the entrainment
   maximum over P_y (compound choking). The mixed composition z_t comes from molar flows.
4. **Diffuser.** η_d, outlet velocity → 0.

**Rating** (geometry A_t, A_e, A_mix known; P_out given) gives ṁ_m, ṁ_s, ER, and the regime
(critical / subcritical / backflow). Off-design maps come from sweeping P_out.
**Design** (ṁ_m and ER given) chooses P_y for the maximum outlet pressure and returns the areas
plus non-dimensional groups (A_mix/A_t, lift ratio, Köhler/Elbel ejector efficiency).
**design_for_lift** returns the largest ER that reaches a required P_out.

Which mixing model and parameters to use, by application, with the evidence: techref chapter *Parameter
Guide* (`python ../techref/figures/gen_data.py guide`).

## Verification and validation

| Check | Result |
|---|---|
| Incompressible limit vs `fluids.liquid_jet_pump` (K = 0) | Q_p and P5 agree to ≤ 0.02 % over the subcritical range |
| Ideal-gas nozzle, N2 | P*/P0 = 0.528 (theory 0.5283) |
| Design → rating round trip | Rating the designed geometry at P_out_design recovers ER within 2 % |
| **Huang et al. 1999, R141b**: 39 points, data from Ma et al. 2017 Table 1 | Fully predictive with Ma's efficiencies (0.95/0.85/0.95/0.85): critical back pressure **MAE 0.47 K**, critical ER **+11 % bias** (MAE 11.6 %). With η_s = 0.7: ER MAE 5.6 %, Td* MAE 0.72 K |
| **Gonçalves et al. 2024**: subcooled-liquid CO2 motive at 34.8 bar, 3 runs, geometry from Fig. 5 | Motive flow **+6.8 / +5.8 / +3.3 %** (isentropic HEM throat, C_d = 1), using the authors' (p, ρ) inlet state, i.e. C_d ≈ 0.95 for this 1.5 mm nozzle. Their HEM CFD gave −1.5 to −5.7 %. ER over-predicted (11–16 % vs 0.6–1.7 %), the same failure as their HEM CFD (~16 %). These points sit near the zero-entrainment edge (η_mix 0.90 → 0.80 cuts ER to 4–9 %), so they are a weak test of ER |

## Findings that matter for the liquid-CO2 (47 → 15 bar) application

- **Feasible, but modest lift.** With saturated liquid motive at 47 bar (11.7 °C), saturated vapour at 15 bar, and efficiencies
  0.90/0.85/0.90/0.80, the optimum-design outlet pressures are:

  | ER | 0.05 | 0.1 | 0.2 | 0.3 | 0.5 |
  |---|---|---|---|---|---|
  | P_out [bar] | 24.6 | 22.5 | 20.2 | 18.9 | 17.6 |

  Ejector efficiency is 0.20–0.36. The outlet quality is 0.26–0.5, i.e. two-phase, so a separator follows.
- **The motive inlet state dominates the sizing.** Gonçalves' measured T (−1.4 °C) and density-implied T (−10.7 °C) differ by 9 K.
  That is enough to change predicted motive flow by **40 %**. At 47 bar, 10 K of subcooling raises G* from 29 to 46 t/m²s.
- **Delayed flashing changes the throat, not the performance.** HNE φ = 0.5 / 0.2 raises G* by 44 % / 127 % (throat diameter 6.7 → 5.6 / 4.4 mm),
  while P_out at ER = 0.2 moves by only 0.25–0.5 bar. On the Giacomelli R744 data a single φ fits the motive flows far worse than a single C_d (MAE 6.9 % vs 1.7 %; see the techref). The literature disagrees on the direction of HEM error for subcritical motive
  (Bodys 2020: HEM over-predicts; Gonçalves 2024: within 6 %). Calibrate C_d or φ on data, then size.
- **Dry ice is not a risk at 15 bar.** The minimum static pressure is about 14 bar, 2.7× the triple point (5.18 bar, −56.6 °C).
  Every result reports `solid_margin` = P_min / P_triple. A pure-fluid flash below P_triple raises `SolidPhaseRisk`; CoolProp has no solid phase.
  The suction would need to fall to about 6.5 bar before the margin approaches 1. Below that the suction
  expansion reaches the triple point and `SolidPhaseRisk` is raised (the searches stop at 1.001 P_triple).
  For mixtures, freeze-out needs a solid–fluid equilibrium model, i.e. the PME.

### Duty case: 13.66 kg/s motive, 2 kg/s saturated vapour at 15 bar (ER 0.146), 28 bar required

All figures are optimum-design results. "Typical" efficiencies are 0.90/0.85/0.90/0.80; "optimistic" are 0.95/0.95/0.95/0.90.

| Motive | P_out at ER 0.146 (typical / optimistic) | Suction reachable at 28 bar (typical) | Outlet vapour |
|---|---|---|---|
| 47 bar, saturated liquid | 21.2 / 23.7 bar | ER 0.01: 28 bar is close to the zero-entrainment limit | 5.5 kg/s |
| 47 bar liquid pumped to 90–100 bar | 25.4–26.4 / 28.7–29.8 bar | n/a | 5.2 kg/s |
| 84.5 bar, 32 °C (dense supercritical) | 27.1 / 30.9 bar | 1.71 kg/s; 15.9 kg/s motive needed for 2 kg/s | ~7.9 kg/s |
| **84.5 bar, 32 °C, calibrated (3 R744 sets)** | **22.5–24.2 bar** | 0.19–0.87 kg/s; 31–33 kg/s motive (145 kg/s with the sub/subcooled set) | ~8.2 kg/s |

The flashing motive adds vapour, so the outlet carries 2.8–4× the vapour that is sucked in. The receiving system must handle this load.
The 84.5 bar case lies in the regime where HEM is most reliable and most R744 data exist. A ±2 bar outlet window still sits inside the efficiency uncertainty, hence the calibration below.

### Calibration on R744 ejector data: Giacomelli et al. 2019

`calibrate_r744.py` uses the 42 points of Giacomelli et al., Int. J. Refrig. 107 (2019) 326, stored in `data/giacomelli2019.csv`.
The source is Tables 3–4 of the open accepted manuscript. Every row passes the check ER = ṁ_s/ṁ_m.

- **Motive:** 71–104 bar, from supercritical through near-critical to subcooled liquid. **Suction:** 24–39 bar.
- **Outlet:** the P_EJ5 tap at the diffuser end.
- **Geometry (Fig. 2):** throat 0.9 mm, nozzle exit 1.03 mm, mixing section 2 × 16.9 mm, diffuser 5° to 10 mm.

The Ringstad 2020 appendix compilation was dropped. It has no geometry and has transcription defects.

| Step | Result |
|---|---|
| Uncalibrated (0.90/0.85/0.90/0.80) | ṁ_m MAE 12 %, ṁ_s MAE 31 %, ER MAE 0.134 |
| Stage 1: motive C_d on ṁ_m | **C_d = 1.138**: the isentropic HEM throat under-predicts this 0.9 mm nozzle by ~12 % (cause not identifiable from the data: delayed flashing and the as-built throat act alike; one HNE φ fits much worse). After the fit, MAE 1.7 %; bias +1.1 / −2.5 / −2.4 % for supercritical / subcritical / subcooled inlets |
| Stage 2: η_s, η_mix, η_d on ṁ_s | **η_s = 0.61, η_mix = 0.825, η_d = 0.82** (±0.036 / ±0.012 / ±0.032). ṁ_s MAE 5.9 % (bias +0.2 %), ER MAE 0.017 |
| Cross-check (fit on one inlet regime, predict the other) | supercritical fit → subcritical/subcooled: ṁ_s MAE 5.6 %; reverse: 14.9 % |

Worst points: cases 15 and 21, at the lowest suction pressure (~24 bar, lift 1.6), are −49 % and −19 %.

The efficiencies are partly correlated (|ρ| up to 0.77) and regime-dependent: the subcritical/subcooled
set is 0.87 / 0.77 / 0.79 (regime and lift are confounded: on the supercritical points within the sub/subcooled
lift range the cross-prediction error is 4.6 %). An earlier fit (0.65 / 0.89 / 0.64) was distorted by a rating bug. Critical-regime
results took the first feasible point of the P_y scan, so the critical ER depended on `n_scan` (case 1: 0.192
at 15 points to 0.216 at 320). `rate()` now bisects the feasibility boundary, and the result is independent of
`n_scan`.

**Applied to the duty case** (84.5 bar / 32 °C motive, 15 bar suction) with the three fitted sets:
- Outlet pressure at ER 0.146: **22.5–24.2 bar**.
- At 28 bar: **0.19–0.87 kg/s** suction with 13.66 kg/s motive. Entraining 2 kg/s needs **31–33 kg/s** motive
  (all-point and supercritical sets), or about 145 kg/s with the subcritical-fitted set (28 bar only at ER 0.014).
- eta_m is fixed at 0.90 and not identified by the data, yet 0.8 / 0.9 / 1.0 gives 22.8 / 24.0 / 25.3 bar: the
  uncertainty is at least ±1.3 bar on top of the three-set band. Verdict: probably short of 28 bar.

Both the 15 bar suction and the 1.87 lift lie outside the calibration range, and the model under-predicts entrainment
as lift rises (residual vs lift r = -0.43), which biases the verdict towards "no". Scale-up from a 2 mm to a ~45 mm
mixing section is also untested.

## Mixtures

`Thermo("CO2&Nitrogen")` together with `inlet(..., z=...)` works. The mixed composition is tracked,
and the HNE factor uses the flash's own phase densities. CoolProp mixture flashes cost about 22 ms
against 6 µs for pure CO2, so one design takes about 25 s (~1,200 flashes). A pure-fluid design takes about 25 ms.
CoolProp PS flashes for mixtures can fail far below the operating range; the choke scan skips those points.
For production use with PME flashes, which cost about 1 ms each through COM, the next step is to tabulate the
motive and suction isentropes once (1D in P) and cache the mixing-section (P, h) flashes.

## Open items

1. Liquid-motive/gas-suction validation with **non-condensable** suction: Cunningham 1974 (JFE 96:203) is paywalled.
   Hatziavramidis' (SPE) liquid/gas equations could serve as an analytical cross-check through a non-flashing thermo.
2. Bodys et al. 2020 (IJR 114:32) has 150 subcritical-motive (50–70 bar) points on SINTEF geometries,
   closest to 47 bar. The accepted manuscript is on SINTEF/NVA, but the download returned 403 to the research agent.
3. Mixing-length and optimum-throat guidance from Flügel 1939 (NACA TM-982) for design mode (L_mix/D_mix).
4. Friction in the mixing section and an explicit diffuser exit area (outlet kinetic energy).
