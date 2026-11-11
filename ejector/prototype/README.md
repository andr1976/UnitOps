# Ejector / jet pump — 0D/1D prototype (Python + CoolProp)

This is the prototype for a future CAPE-OPEN ejector unit operation. One model covers gas–gas, liquid–liquid,
liquid-motive/gas-suction and flashing two-phase ejectors, for pure fluids and mixtures.
Every stream is carried as (P, h, z). All properties come from a homogeneous-equilibrium flash.
The model needs only four flash calls (`ph`, `ps`, `pt`, `pq` at composition z, plus the molar mass).
The CAPE-OPEN adapter will map them one-to-one onto PME flashes on a scratch material object.

| File | Purpose |
|---|---|
| `ejector1d.py` | Model: `Thermo`, `Expansion` (nozzle + critical flow), `Ejector.rate()` / `.design()` / `.design_for_lift()` |
| `validate_huang1999.py` | 39-point R141b gas-ejector benchmark (critical ER + critical back pressure) |
| `validate_goncalves2024.py` | Subcooled-liquid CO2 motive ejector (motive flow, ER) |
| `co2_case.py` | Target application: liquid CO2 at 47 bar entraining CO2 vapour at 15 bar |
| `test_ejector1d.py` | 11 regression/verification tests (`python -m pytest -q`, ~3 s) |

## Model

Classical constant-area-mixing 1D ejector (Keenan 1950 → Huang 1999 → Liu & Groll / Banasiak & Hafner for CO2),
written for real fluids:

1. **Motive nozzle.** Expansion with isentropic efficiency η_m. The choked mass flux is G* = max_P ρ(P,h)·u(P) on the HEM path.
   A supersonic exit pressure follows from G(P_e)·A_e = ṁ_m. An unchoked (liquid) nozzle exhausts at P_y.
   An optional **HNE** delayed-flashing factor φ uses quality x = φ·x_eq in the density only
   (cf. the Henry–Fauske / Diener–Schmidt boiling-delay factor). φ = 1 is HEM.
2. **Suction.** Expansion s0 → P_y with η_s, capped at its own choked flux. For liquid suction this cap is the cavitation limit.
3. **Constant-area mixing.** Mass, momentum (η_mix on the inlet momentum flux) and energy give one residual in P3.
   The subsonic, post-shock root is taken. The mixed composition z_t comes from molar flows.
4. **Diffuser.** η_d, outlet velocity → 0.

**Rating** (geometry A_t, A_e, A_mix known; P_out given) gives ṁ_m, ṁ_s, ER, and the regime
(critical / subcritical / backflow). Off-design maps come from sweeping P_out.
**Design** (ṁ_m and ER given) chooses P_y for the maximum outlet pressure and returns the areas
plus non-dimensional groups (A_mix/A_t, lift ratio, Köhler/Elbel ejector efficiency).
**design_for_lift** returns the largest ER that reaches a required P_out.

## Verification and validation

| Check | Result |
|---|---|
| Incompressible limit vs `fluids.liquid_jet_pump` (K = 0) | Q_p and P5 agree to ≤ 0.02 % over the subcritical range |
| Ideal-gas nozzle, N2 | P*/P0 = 0.528 (theory 0.5283) |
| Design → rating round trip | Rating the designed geometry at P_out_design recovers ER within 2 % |
| **Huang et al. 1999, R141b**: 39 points, data from Ma et al. 2017 Table 1 | Fully predictive with Ma's efficiencies (0.95/0.85/0.95/0.85): critical back pressure **MAE 0.9 K**, critical ER **+14 % bias** (MAE 15 %). With η_s = 0.7: ER MAE 6 %, Td* MAE 1.3 K |
| **Gonçalves et al. 2024**: subcooled-liquid CO2 motive at 34.8 bar, 3 runs, geometry from Fig. 5 | Motive flow **+1 / 0 / −2 %** (η_m = 0.9), using the authors' (p, ρ) inlet state. Their CFD gave −1.5 to −5.7 %. ER over-predicted (7–13 % vs 0.6–1.7 %), the same failure as their HEM CFD (~16 %). These points sit at the zero-entrainment edge, where η_mix 0.85 → 0.80 flips them to backflow, so they are a weak test of ER |

## Findings that matter for the liquid-CO2 (47 → 15 bar) application

- **Feasible, but modest lift.** With saturated liquid motive at 47 bar (11.7 °C), saturated vapour at 15 bar, and efficiencies
  0.90/0.85/0.90/0.80, the optimum-design outlet pressures are:

  | ER | 0.05 | 0.1 | 0.2 | 0.4 |
  |---|---|---|---|---|
  | P_out [bar] | 24.3 | 22.2 | 20.0 | 18.0 |

  Ejector efficiency is 0.19–0.35. The outlet quality is 0.27–0.5, i.e. two-phase, so a separator follows.
- **The motive inlet state dominates the sizing.** Gonçalves' measured T (−1.4 °C) and density-implied T (−10.7 °C) differ by 9 K.
  That is enough to change predicted motive flow by **40 %**. At 47 bar, 10 K of subcooling raises G* from 27 to 43 t/m²s.
- **Delayed flashing changes the throat, not the performance.** HNE φ = 0.5 / 0.2 raises G* by 44 % / 127 % (throat diameter 6.9 → 4.6 mm),
  while P_out at ER = 0.2 moves by only 0.1 bar. The literature disagrees on the direction of HEM error for subcritical motive
  (Bodys 2020: HEM over-predicts; Gonçalves 2024: within 6 %). Calibrate C_d or φ on data, then size.
- **Dry ice is not a risk at 15 bar.** The minimum static pressure is about 14 bar, 2.7× the triple point (5.18 bar, −56.6 °C).
  Every result reports `solid_margin` = P_min / P_triple. A pure-fluid flash below P_triple raises `SolidPhaseRisk`; CoolProp has no solid phase.
  The suction would need to fall to about 6.5 bar before the margin approaches 1.
  For mixtures, freeze-out needs a solid–fluid equilibrium model, i.e. the PME.

### Duty case: 13.66 kg/s motive, 2 kg/s saturated vapour at 15 bar (ER 0.146), 28 bar required

All figures are optimum-design results. "Typical" efficiencies are 0.90/0.85/0.90/0.80; "optimistic" are 0.95/0.95/0.95/0.90.

| Motive | P_out at ER 0.146 (typical / optimistic) | Suction reachable at 28 bar (typical) | Outlet vapour |
|---|---|---|---|
| 47 bar, saturated liquid | 21.0 / 23.5 bar | ER 0.005: 28 bar is the zero-entrainment limit | 5.5 kg/s |
| 47 bar liquid pumped to 90–100 bar | 24.9–25.9 / 28.5–29.6 bar | n/a | n/a |
| **84.5 bar, 32 °C (dense supercritical)** | **26.9 / 30.8 bar** | 1.64 kg/s; 16.7 kg/s motive needed for 2 kg/s | ~8 kg/s |

The flashing motive adds vapour, so the outlet carries 2.8–4× the vapour that is sucked in. The receiving system must handle this load.
The 84.5 bar case lies in the regime where HEM is most reliable and most R744 data exist. A ±2 bar outlet window still sits inside the efficiency uncertainty, hence the calibration below.

### Calibration on R744 multi-ejector data (in progress)

`calibrate_r744.py` fits η_s, η_mix and η_d in rating mode to measured ER, with optional leave-one-out cross-validation.
The points are the 13 Giacomelli et al. 2019 points (motive about 95 bar, 30–38 °C), taken from the Ringstad 2020 appendix and stored in
`data/ringstad2020_giacomelli.csv`. The appendix gives no geometry, so the fit waits on the published dimensions (`data/giacomelli2019_geometry.json`).

The appendix table also has transcription defects, flagged in the CSV:
- The suction pressures of the first Palacz block duplicate Giacomelli rows 9–13.
- Two outlet pressures are printed in bar instead of MPa.
- The Haida block has no ER column.

The Palacz and Haida rows are therefore not used.

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
