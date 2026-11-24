# Adversarial review: 1D real-fluid ejector model and technical reference

Reviewed: `ejector/prototype/*.py` and `ejector/techref/**`, branch `ejector-prototype`, uncommitted working copy.
`ejector1d.py` changed during the review (`rate_flows()` was added at 23:43 on 2026-11-23). Every experiment ran against a
frozen copy of the prototype taken at that time (`ejector1d.py` sha1 `a58c759864054130d79dca2e270705e7462702be`, which
was still the working-copy hash when this report was written). `rate_flows()` was reviewed only for whether it inherits
the defects found in `rate()`.

The reproduction scripts were kept in a temporary working directory (called `<S>` below), not in the
repository; the review regression tests in `prototype/test_ejector1d.py` cover the critical and major findings. Each script imports the frozen copy in `<S>\snap\` through `<S>\common.py`. To run one:
`cd <S> && python <script>.py`. `FIT_ALL` is the all-points R744 set from `giacomelli_fit.csv`
(η_m 0.90, C_d 1.13759, η_s 0.6098, η_mix 0.8254, η_d 0.8196).

Baseline: the 19 tests pass on the snapshot. `giacomelli_sim.csv` and `nozzle_critical.csv` reproduce to 1e-5.

**The C# port already carries the critical defects.** During the review, `validation.rst` gained the section
"Verification of the C# implementation" (lines 414–470). It reports that `ejector/src/EjectorCore` reproduces the Python
model "with zero difference" on 13 cases exported by `prototype/export_regression.py`. The same constructs are in the
port:
- `Ejector.cs:203-207`: unchoked branch, F1;
- `Ejector.cs:211-221`: `aE > aT * 1.0001` and the `pE = pLow` clamp, F2;
- `Ejector.cs:472, 496-497`: 0.995·P_s0 cap with a 30-point scan, and the "zero entrainment" probe at ER = 1e-4, F3;
- `Ejector.cs:109`: 4 % march step, F12.

None of the 13 regression cases exercises F1–F3. They use the Giacomelli geometry (A_e/A_t = 1.31 > C_d, P_y ≪ P_t),
the water jet pump (A_e = A_t), Huang (P_y ≪ P_t), and designs at ER ≥ 0.146. Zero difference therefore says nothing
about these paths. Fix them in the Python reference, add regression cases for them, and re-export.

## Summary of findings

| ID | Severity | Status | Location | Claim |
|---|---|---|---|---|
| F1 | CRITICAL | CONFIRMED | `ejector1d.py:401-411`, `nozzles.rst:106-127` | When A_e > A_t, P_out(P_y) jumps at P_y = P_t. `rate()` then returns "subcritical" at an outlet pressure that differs from the spec by up to 0.42 bar, with no warning. The unchoked branch also breaks continuity at the nozzle exit by a factor A_e/A_t. |
| F2 | CRITICAL | CONFIRMED | `ejector1d.py:412-419`, `nozzles.rst:88-99` | For 1.0001 < A_e/A_t < C_d the exit pressure clamps to 1.001·P_tp. ER nearly doubles (0.19 → 0.36), P_b* rises by 7 bar, and nothing flags it. There is also a jump at A_e/A_t = 1.0001. |
| F3 | CRITICAL | CONFIRMED | `ejector1d.py:615-649`, `gen_data.py:266-272`, `worked_example.rst:76-79,99`, `operating_modes.rst:128`, `solution_methods.rst:121` | Design mode misses the narrow feasible P_y band at small ER (scan capped at 0.995·P_s0). P_out_opt(ER) is not monotone. `design_for_lift` raises a false "not reachable even at zero entrainment". The worked-example entry "0 (unreachable)" is wrong: within the model 28 bar is reached at ER 0.0138. |
| F4 | MAJOR | CONFIRMED | `ejector1d.py:438-445,468-480` | In the critical and backflow regimes, η_ej, `out`, `u_y` and `states` belong to a different operating point from `P_out`/`m_s`. Example: η_ej is reported as 0.21 at 31 bar where the true value is 0.003. |
| F5 | MAJOR | CONFIRMED | `ejector1d.py:113-114,217,336-343,426,619`, `flow_fundamentals.rst:236-243`, `architecture.rst:190` | The triple-point guard is swallowed everywhere. Solutions are truncated at 1.001·P_tp without warning, so σ ≥ 1.001 by construction and the planned "σ ≤ 1" error can never fire. |
| F6 | MAJOR | CONFIRMED | `ejector1d.py:203-207`, `flow_fundamentals.rst:158-202` | The HNE option is not momentum-consistent: velocity comes from the equilibrium enthalpy, density from x = φ·x_eq. G* is overstated by 4–13 % relative to a consistent HNE, and φ is not comparable with the cited Henry–Fauske / Diener–Schmidt factors. |
| F7 | MAJOR | CONFIRMED | `validation.rst:246-248,306-333`, `data_library.rst:162-165` | The cross-regime check is confounded with lift: the sub/subcooled set spans lift 1.04–1.22, the supercritical set up to 1.60. The 14.9 % MAE comes from five high-lift points. "About four standard errors" is really 3.0. |
| F8 | MAJOR | CONFIRMED | `validation.rst:320-326`, `solution_methods.rst:276-281`, `README.md:106-107` | "0.65/0.89/0.64 at practically the same fit quality: the objective is flat" does not hold for the current model: MAE 9.4 % vs 5.9 %, SSR 2.5×. The README contradicts the techref on this point. |
| F9 | MAJOR | CONFIRMED | `worked_example.rst:93-107`, `data_library.rst:162-165` | The calibrated set does not carry over to design mode. η_m is inert in the calibration but moves the duty P_out by ±1.3 bar. η_mix acts on 27–31 % of the impulse in calibration and 53 % in design. The model's bias at high lift points toward "no". The 22.5–24.2 bar band understates the uncertainty. |
| F10 | MAJOR | CONFIRMED | `validation.rst:203-211,483-484` | The recommended R744 set over-predicts the only independent motive data (Gonçalves) by 18–21 %. The conclusion "C_d does not transfer" rests on an inlet state whose ±1 K uncertainty moves the flow by ±4.6 %. |
| F11 | MAJOR | CONFIRMED | `validation.rst:110-159,481-482` | All 39 Huang points are suction-choked, so the predicted ER = G_s*(η_s)(A_mix−A_e)/(G*A_t) and does not depend on η_m, η_mix or η_d. Ma's efficiencies come from a structurally different model. "Gas–gas validated (predictive)" overclaims. |
| F12 | MINOR | CONFIRMED | `ejector1d.py:339`, `operating_modes.rst:85`, `solution_methods.rst:179-180` | The hard-coded 4 % mixing-march step biases P_b* by about +0.11 bar. Some results still depend on a discretisation choice. |
| F13 | MINOR | CONFIRMED | `validation.rst:27,56,62`, `README.md:16` | The test table is wrong: 19 tests, not 13; C_d interval "(1.15, 1.25)" against an actual 1.138 and a test bound of (1.10, 1.18); "within 5 %" against an actual +6.8 %. |
| F14 | MINOR | CONFIRMED | `operating_modes.rst:82-88`, `make_offdesign.py:55` | The caption says backflow is "not shown", but it is plotted. The P_b* line is drawn at the first subcritical grid point. |
| F15 | MINOR | CONFIRMED | `validation.rst:227-228,396-401`, `flow_fundamentals.rst:216-219` | The "subcritical" Giacomelli points are at 83–96 bar (P > P_c), yet they are compared with Ringstad's pressure-based "below 59 bar" class. No calibration point lies below 70.9 bar. |
| F16 | MINOR | CONFIRMED | `figures/tikz_ejector.png`, `nozzles.rst:10-14` | The schematic places nozzle exit e upstream of the mixing inlet y. The model applies P_e·A_e and A_sy = A_mix − A_e at y. |
| F17 | MINOR | CONFIRMED | `ejector1d.py:155-162` | `pq()` stores a molar q in `State.q`, which is documented as a mass fraction. |
| F18 | MINOR | CONFIRMED | `ejector1d.py:7,11`, `architecture.rst:90-99,123-137`, `README.md:122-123` | Stale or inaccurate text: "three-call interface"; Liu & Groll "2012" against bib 2013; the PS fallback "carries over from the reference implementation" (it has none); stale flash counts; README and techref give different mixture costs. |
| F19 | MINOR | SUSPECTED | `ejector1d.py:646-648`, `gen_data.py:271` | `design_for_lift` raises `ValueError`, not `RuntimeError`, when ER_max is reached. The gen_data handler would not catch it. |
| F20 | MINOR | CONFIRMED | `operating_modes.rst:118-119`, `flow_fundamentals.rst:212-214`, `validation.rst:213-217` | Overstatements. "Best ejector the efficiency set allows" holds only within the ideally-expanded, matched-pressure family. "C_d affects P_out little" holds in design mode only. The (P,T)-inlet Gonçalves runs return backflow, which is not stated. |

---

## F1 — CRITICAL — Choked/unchoked switch for a C-D nozzle: P_out jumps and `rate()` returns a wrong outlet pressure

**Evidence.** `ejector1d.py:408-411`:
```python
if P_y >= P_t:
    G = mexp.flux(P_y) * self.Cd_m
    st_e, u_e = mexp.at(P_y)
    return G * A_t, P_y, P_y, st_e, u_e
```
For P_y ≥ P_t the nozzle "is treated as converging". The mass flow uses the throat area A_t, but the jet still occupies A_e in the momentum balance: `P_e*A_e` and `A_sy = A_mix - A_e` in `_pass`. For A_e > A_t this has three consequences:
1. Continuity at the nozzle exit is broken: ρ_e·u_e·A_e/ṁ_m = A_e/A_t.
2. A real C-D nozzle stays choked for P_t < P_back < P_first-critical. For the N2 case below that range is about 5.3 to 8.8 bar.
3. At P_y = P_t the motive impulse switches from the supersonic exit state (P_e from G(P_e)A_e = ṁ) to (P_t, u_t). The mass flow is continuous but P_out is not.

`nozzles.rst:120-123` claims "the flow is continuous when the nozzle starts to choke". This holds only for A_e ≤ 1.0001·A_t.

**Reproduction.**
- `python e2_choke_switch.py`: N2, 10 bar → 6 bar suction, A_e/A_t 1.5, A_mix/A_t 6.
  ```
  P_y/P_t=0.99999 m_m=0.18085 P_e= 1.817 u_e= 475.0 motive impulse= 98.7 N exit continuity 1.000 ER=1.6585 P_out=5.8420 bar
  P_y/P_t=1.00001 m_m=0.18085 P_e= 5.270 u_e= 322.3 motive impulse=114.5 N exit continuity 1.500 ER=1.6584 P_out=6.2503 bar
  ```
- `python e2b.py`: a spec inside the jump returns a different P_out, labelled "subcritical":
  ```
  spec 6.00 bar -> subcritical reported P_out 5.8420 ...   spec 6.10 bar -> subcritical reported P_out 6.2503
  ```
- `python e2d.py`: CO2 with the calibrated Giacomelli geometry (A_e/A_t 1.31), 47 bar saturated-liquid motive, 37 bar suction:
  ```
  spec 35.30: subcritical reported P_out 35.0781 (error -0.222 bar)
  spec 35.50: subcritical reported P_out 35.0781 (error -0.422 bar)
  spec 35.80: subcritical reported P_out 35.9994 (error +0.199 bar)
  ```
- `python e2e.py`: the same happens with real lift (N2, 10 → 5.6 bar, A_mix/A_t 3): P_out jumps from 6.04 to 6.84 bar (lift 1.08 → 1.22).
  `rate(P_out=6.441)` returns subcritical with P_out 6.843 (+0.40 bar).

**Why it matters.** Brent converges onto the discontinuity and `make("subcritical", ...)` overwrites `P_out` with the computed value. A PME would receive an outlet at a pressure it did not specify, labelled as a valid subcritical solution. Any ejector with suction pressure above P_t can hit this: low-ratio thermocompressors, and flashing-liquid motives near their saturation pressure. The `fluids` verification (`validation.rst:92-97`) uses A_e = A_t, so this branch is untested. `rate_flows()` calls the same `_motive_state` and inherits the problem.

**Fix.** For P_t < P_y, keep the throat choked (ṁ = C_d·G*·A_t) until P_y exceeds the subsonic-branch exit pressure where G_sub(P)·A_e = C_d·G*·A_t. Above that pressure use ṁ = G(P_y)·A_e (exit-controlled), capped by the throat. After every Brent solve in `rate()`, assert |P_out_calc − P_out| < tol and raise on failure. Add a regression test with A_e/A_t > 1 that crosses P_t.

## F2 — CRITICAL — Exit-pressure clamp: nozzles with 1.0001 < A_e/A_t < C_d give physically impossible performance, silently

**Evidence.** `ejector1d.py:412-419`: with `Cd_m` > 1 the target exit flux C_d·G*·A_t/A_e exceeds G*, so the bracket fails and `P_e = P_low` (1.001·P_tp for CO2). For A_e ≤ 1.0001·A_t no clamp is applied and P_e = P_t. `nozzles.rst:96-97` says the clamp acts "if A_e/A_t ≥ C_d does not hold". That omits the 1.0001 discontinuity, the size of the effect, and that it is silent.

**Reproduction.** `python e3_clamp.py` (Giacomelli case 1, `FIT_ALL`, only A_e varied):
```
A_e/A_t= 1.0000: critical P_e= 68.77 bar u_e= 101.8 ER=0.1716 Pb*= 39.72 sigma=5.333
A_e/A_t= 1.0002: critical P_e=  5.18 bar u_e= 292.1 ER=0.3593 Pb*= 47.75 sigma=1.001
A_e/A_t= 1.1370: critical P_e=  5.18 bar u_e= 292.1 ER=0.3469 Pb*= 47.22 sigma=1.001
A_e/A_t= 1.1380: critical P_e= 68.74 bar u_e= 101.8 ER=0.1903 Pb*= 40.52 sigma=5.438
```
In the clamped range the jet carries G(P_low)·A_e < ṁ_m. Mass is not conserved at the exit plane, and the momentum gain is fictitious.

**Why it matters.** The techref recommends the R744 presets with C_d = 1.138. With them, any user nozzle with up to about 14 % divergence returns twice the entrainment and 7 bar more critical back pressure, with no error. Only σ = 1.001 hints at it.

**Fix.** Decide now how C_d is applied (open item 1 in `solution_methods.rst:316-319`). Either apply C_d uniformly along the nozzle, or raise a geometry error when A_e/A_t < C_d. Never clamp silently. Remove the 1.0001 special case, or apply the same rule on both sides of it. Add a `warnings`/`flags` field to `Result`.

## F3 — CRITICAL — Design mode at small ER: missed feasible band, non-monotone P_out_opt(ER), false infeasibility, wrong worked-example entry

**Evidence.**
- `design()` (`ejector1d.py:622-628`) scans 30 points up to `s0.P * 0.995` only. Infeasible points score 0, and a bounded Brent search follows between the neighbours of the best point.
- At small ER the feasible P_y window is a narrow band just below P_s0. Elsewhere A_sy is so small that (1−η_mix)·ṁ_m·u_e makes the constant-area section infeasible.
- `design_for_lift` (`:643-644`) declares the duty infeasible if ER = 1e-4 fails. `operating_modes.rst:128` asserts that P_out_opt "falls monotonically with ER". `solution_methods.rst:121-122` claims "the optimum is interior and smooth, so the scan resolution does not limit design mode".
- `gen_data.py:271-272` turns any `RuntimeError`, including "design infeasible: mixing cannot pass the flow", into "ms28 = 0, unreachable".

**Reproduction.**
- `python e14c.py`, sub/subcooled set, 84.5 bar/32 °C motive, 15 bar suction, 13.66 kg/s:
  ```
  ER 0.005: 0.99:infeas 0.995:infeas ... 0.999:infeas  0.9995:28.614 0.9999:26.373
  ER 0.01 : 0.99:infeas 0.995:infeas 0.997:infeas 0.998:28.276 0.999:27.756
  ```
- `python e14d.py` (dense P_y scan used as the optimiser):
  ```
  sub_subcooled ER 0.0050: design() nan | dense scan max 28.653 bar at P_y/P_s0 0.99941
  dense-scan design_for_lift(28 bar): ER 0.0138 -> m_s 0.189 kg/s, motive for 2 kg/s 145 kg/s
  ```
  `e14.py`: `design()` raises "design infeasible" for every ER ≤ 0.015 with this set.
- `python e14e_lift32.py` (all-points set):
  ```
  design_for_lift(32 bar): RuntimeError: required lift not reachable even at zero entrainment
  design(ER=0.0001).P_out = 30.560 bar
  design(ER=0.005).P_out = 33.742 bar
  ```
- `e13_design.py`: `design()` agrees with a 400-point brute force to 1e-4 bar for ER ≥ 0.02 (the doc's claim holds there). P_out_opt is 30.56 bar at ER 1e-4 and 33.74 bar at 0.005, so it is not monotone.

**Why it matters.**
- `worked_example.rst:76-79` reports "0 (unreachable)" and "--", and line 99 says the duty "is impossible with the subcritical-fitted set". Within the model the set reaches 28 bar at ER 0.0138: 0.19 kg/s with 13.66 kg/s motive, or about 145 kg/s motive for 2 kg/s. `README.md:81` repeats the error. The engineering verdict ("no") survives; the stated result and its reason do not.
- `DesignForPressure` in the planned unit would raise false "outside the zero-entrainment limit" errors for any P_out between the ER→0 branch and the peak (30.6–33.7 bar here).

**Fix.**
- Scan P_y up to P_s0·(1−ε) with ε ≤ 1e-4, and refine near P_s0 (log spacing in P_s0 − P_y).
- Before maximising, bracket the feasible window by bisection on feasibility.
- In `design_for_lift`, find max over ER of P_out_opt(ER) first; report "unreachable" only when that maximum is below P_req, then root-find on the descending branch.
- In `gen_data.duty()`, distinguish "infeasible design" from "lift unreachable".
- Reconsider applying η_mix to the motive momentum when nothing is entrained. It makes the ER → 0 limit infeasible, which is a model artefact.

## F4 — MAJOR — Result fields from mixed operating points in the critical and backflow regimes

**Evidence.**
- `ejector1d.py:443`: `make()` computes `eta_ejector` from `r["out"]`. In the critical regime that is the state at P_b*, while line 470 then sets `res.P_out = P_out`.
- Backflow (`:478-480`) keeps `out`, `u_y`, `P_y`, `states` and `eta_ejector` of the last scan point (m_s > 0) and sets only `m_s = ER = 0`.

**Reproduction.** `python e4_result.py` (case 1, `FIT_ALL`):
```
critical @ 31 bar: P_out=31.00 out.P=40.90 out.T=6.17 C out.q=0.500 eta_ej reported=0.2097  eta_ej at actual P_out=0.0029
backflow @50 bar: m_s=0.0 ER=0.0 eta_ej=0.00540 u_y=0.72 ... out.P=48.572 out.h-h_m0=402.1 J/kg (energy balance with m_s=0 requires 0)
```

**Why it matters.** `appendix_capeopen.rst:366-374` plans to expose `EjectorEfficiency` and `Station_*` arrays. In the critical regime η_ej would be overstated (0.21 instead of 0.003 here). The outlet station would show T and x at P_b*, not at the specified P_out. In backflow the energy balance of the reported outlet state is broken.

**Fix.** In the critical regime re-flash the outlet at (P_out, h_0t) and compute η_ej there, keeping P_b* in its own field. In backflow, recompute the outlet with ṁ_s = 0 (h = h_m0) and clear the suction-side fields. Add `P_b_critical` to the `rate()` result, not only to `rate_flows()`.

## F5 — MAJOR — The solid-phase guard never reaches the caller; σ cannot fall below 1.001

**Evidence.**
- `_guard` raises `SolidPhaseRisk`, but every caller on the solution path catches it:
  - `_safe_flux` → NaN (`:217`);
  - `_mix` → None, i.e. "infeasible" (`:342`);
  - `design.neg_pout` → 0 (`:619`).
- All searches are floored at `P_low = 1.001·P_tp` (`:426,511,595`), and the exit clamp lands on P_low (F2). σ = P_min/P_tp is therefore ≥ 1.001 by construction.
- `flow_fundamentals.rst:238-239` says the guard ensures "the solution can never pass through that region silently". `architecture.rst:190` plans a CAPE-OPEN error at σ ≤ 1, which can never trigger.

**Reproduction.** `python e12_readme.py` (last block; 47 bar saturated-liquid motive, design ER 0.2):
```
P_s 6.5: sigma 1.054 P_y 5.459
P_s 5.6: sigma 1.001 P_y 5.185 P_out 9.55
P_s 5.3: sigma 1.001 P_y 5.185 P_out 8.18
```
No exception is raised. The suction expansion is truncated at 1.001·P_tp (its "choke" is the floor of the search), and a valid-looking design comes back. `co2_case.py` section 4 expects an exception that never comes. `e3_clamp.py` shows σ = 1.001 for clamped rating results.

**Fix.** Track whether any search hit the floor, and raise or flag `SolidPhaseRisk` (or set σ ≤ 1) when the converged solution, or a choke location, sits at P_low. Report the error threshold as "σ ≤ 1 + δ", or better as an explicit flag.

## F6 — MAJOR — The HNE correction is not momentum-consistent

**Evidence.** `ejector1d.py:203-207`: the density uses x = φ·x_eq, but u = √(2(h0 − h_eq(P))), which is the equilibrium isentrope (dh = dP/ρ_eq). The flux maximum then does not satisfy the derivation in `flow_fundamentals.rst:70-78`, which needs dh = dP/ρ with the same ρ. The omega / HNE-DS construction cited (`henry1971`, `diener2004`) uses u²/2 = ∫v_HNE dP.

**Reproduction.** `python e6_hne.py`:
```
47 bar sat liq   phi=0.500: model G*= 41.16 t/m2s at 27.96 bar | consistent G*= 36.44 at 30.27 bar | model/consistent=1.129
47 bar sat liq   phi=0.628: model/consistent=1.084
Giacomelli case 37 phi=0.500: model/consistent=1.076
```
The φ = 1 rows agree exactly, which validates the integration. `python e6b_hne_fit.py`: a consistent HNE fitted on the 42 motive flows gives φ = 0.531, MAE 5.82 % (techref: φ = 0.628, MAE 6.9 %).

**Why it matters.**
- The "+44 %" (`flow_fundamentals.rst:200-202`) and φ = 0.628 (`validation.rst:384-388`) belong to this ad-hoc formulation. In a consistent HNE the same φ = 0.5 gives about +28 %.
- `FlashingFactor` will be a user parameter, but its values cannot be compared with literature boiling-delay factors.
- The qualitative conclusion "one C_d fits far better than one φ" survives: 5.8 % against 1.7 %.

**Fix.** Either integrate u²/2 = ∫v_HNE dP along the path (one extra cumulative sum on the tabulated path), or document the option as a heuristic density correction that is not HNE-DS, and drop the implied equivalence with the cited factors.

## F7 — MAJOR — The "regime dependence" conclusion is confounded with lift; statistics overstated

**Evidence.** `validation.rst:327-333`: "The efficiencies depend on the operating regime ... about four standard errors below ... predicts the supercritical ones with 14.9 % MAE."

**Reproduction.** `python e8_stats.py`:
```
lift range sup 1.059-1.601 ; sub/subcooled 1.044-1.217
cross residuals sup points (sub-fit): ... 15: -100.0, 16: -50.4, 21: -61.9, 23: -36.8, 26: -30.2 (all others -2 to -16 %)
sub->sup MAE excluding 15,21,16,23,26: 5.56 ; all-fit MAE on same points 4.54
eta_mix diff 0.0621, SE of diff 0.0205, z = 3.03; diff/SE_sup = 4.04, diff/SE_sub = 4.57
```

**Why it matters.**
- The sub/subcooled training set contains no point above lift 1.22. Predicting the supercritical set is mostly an extrapolation in lift and toward the model's zero-entrainment edge. Without five high-lift points the error matches the reverse direction (5.6 %).
- The evidence therefore does not separate "regime" from "lift extrapolation". That distinction matters directly for the worked example, which is at lift 1.87.
- "About four standard errors" divides by one set's SE. The z of the difference is 3.0, from Gauss–Newton SEs of a 3-parameter fit on n = 15 with a finite-difference Jacobian.

**Fix.** Design the cross-check as a lift-stratified or leave-one-condition-out validation, and report residuals against lift and ER. Quote z for the difference. Phrase the conclusion as "efficiencies are not constant across the tested range (regime and lift confounded)".

## F8 — MAJOR — The "objective is flat" claim is not supported by the current model

**Evidence.** `validation.rst:323-326`: "An earlier version ... found 0.65 / 0.89 / 0.64 at practically the same fit quality: the objective is flat in these directions." The earlier fit used a different model (losses before the throat, C_d = 1.205; `solution_methods.rst:271-272`). `README.md:106-107` instead says that fit "was distorted by a rating bug".

**Reproduction.**
- `python e10_flat.py`:
  ```
  fitted: m_s MAE 5.86 %  bias +0.16 %  SSR(scaled) 0.1432
  earlier: m_s MAE 9.38 %  bias +6.55 %  SSR(scaled) 0.3519
  ```
- `python e10b_profile.py` (η_d profile):
  ```
  eta_d 0.64: SSR 0.2367 | 0.72: 0.1799 | 0.82 (opt): 0.1432 | 0.90: 0.1766 | 1.00: 0.3100
  ```
  This profile is consistent with the Gauss–Newton SE (±0.032). η_d is identified to roughly ±0.05–0.08.

**Fix.** Remove the claim, or replace it with the profile above. Keep the README and techref consistent.

## F9 — MAJOR — Calibrated efficiencies do not carry over to optimum design; the duty uncertainty is understated and its bias points toward "no"

**Evidence.**
- `worked_example.rst:95-100` states "the outlet drops to 22.5–24.2 bar ... The calibration changes the answer from 'marginal' to 'no'".
- `data_library.rst:162-165` says the spread of the three sets "should be carried as an uncertainty band".
- `validation.rst:335-338` notes that the worst points are nearest the duty, but does not give the sign of their residuals.

**Reproduction.**
- `python e19_transfer.py`:
  ```
  eta_m 0.80: Giacomelli m_s MAE 5.77 % | duty P_out(ER 0.146) 22.77 bar
  eta_m 0.90: Giacomelli m_s MAE 5.86 % | duty P_out(ER 0.146) 23.99 bar
  eta_m 1.00: Giacomelli m_s MAE 6.24 % | duty P_out(ER 0.146) 25.33 bar
  Giacomelli case 1: momentum share of F_in 0.31, P_t 68.8 -> P_e 58.9 bar
  duty design: momentum share 0.53, A_e/A_t 3.1, P_e = P_y 13.6 bar
  ```
- `e8_stats.py`: correlation of the all-fit residual with lift over the 27 subcritical-regime points is −0.43. The two highest-lift points (cases 15 and 21) are under-predicted by 49 % and 19 %.

**Why it matters.**
- η_m is fixed (0.90), never identified (the Giacomelli jet expands only 69 → 59 bar beyond the throat), yet it moves the duty answer by ±1.3 bar. That is as large as the whole three-set band.
- η_mix multiplies about twice the share of the inlet impulse in design (ideally expanded jet) as in the calibration (strongly under-expanded jet, where pressure forces dominate).
- The model under-predicts entrainment as lift rises, so extrapolating to lift 1.87 biases the verdict toward "no".
- The "no" may still be right, but the stated band and its causes do not support it at the confidence implied.

**Fix.**
- Propagate the fitted-parameter covariance plus an η_m range into P_out(ER0).
- Show the residual-versus-lift trend.
- State that the efficiencies were fitted on an under-expanded off-design nozzle and applied to an ideally expanded design.
- Present the verdict as "probably no, with outlet X ± Y bar".

## F10 — MAJOR — The independent motive-flow check contradicts the recommended set; the C_d non-transfer claim is weakly supported

**Evidence.** `validation.rst:205-208`: "This 1.5 mm nozzle would need C_d ≈ 0.95 ... a discharge coefficient fitted on one nozzle does not transfer to another." `validation.rst:483-484` lists the CO2 motive as "validated (3 points, motive flow)", which holds only with C_d = 1.

**Reproduction.** `python e16_goncalves.py`:
```
T(p,rho)-1 K: m_m 79.10 g/s (+11.2 %) | +0 K: 75.90 (+6.8 %) | +1 K: 72.52 (+2.0 %) | +3 K: 65.06 (-8.5 %)
rho 990: T -10.36 C (vs -10.73)        rho 994: T -11.10 C
run 1, R744 all-point set: m_m 86.3 vs 71.1 g/s (+21.4 %), ER 0.103 vs 0.0073
```

**Why it matters.**
- The flow sensitivity is 4.6 % per K, or 2.2 % per kg/m³ of measured density. The gap between C_d 0.95 and 1.138 corresponds to about 3.5 K or about 8 kg/m³ of inlet uncertainty, and the authors themselves report a 9 K inconsistency. The data cannot establish non-transferability.
- The only out-of-nozzle data set is mispredicted by 18–21 % with the recommended presets. The "cross-regime check" never leaves the Giacomelli nozzle.

**Fix.** State that the Gonçalves check is inconclusive for C_d, and that no independent validation of the calibrated set exists. Change the coverage row to "motive flow consistent within inlet-state uncertainty (C_d = 1); calibrated set not independently validated".

## F11 — MAJOR — The Huang "gas–gas validated" claim tests one formula for ER, and the efficiencies come from a different model

**Evidence.** `validation.rst:119-123` ("run predictively, with Ma's efficiencies"), `:153-159`, `:481-482` ("gas–gas ejector: validated, 39 points R141b (predictive)"). In `huang1999.csv` every point is critical with P_y = P_ys (suction choke).

**Reproduction.** `python e11_huang_er.py`:
```
{'eta_m':0.95,'eta_s':0.85,'eta_mix':0.95,'eta_d':0.85} regime critical ER 0.36306 Td* 34.71
{'eta_m':0.95,'eta_s':0.85,'eta_mix':0.8,'eta_d':0.7}   regime critical ER 0.36306 Td* 25.82
{'eta_m':0.8,'eta_s':0.85,'eta_mix':0.95,'eta_d':0.85}  regime critical ER 0.36306 Td* 33.03
{'eta_m':0.95,'eta_s':0.7,'eta_mix':0.95,'eta_d':0.85}  regime critical ER 0.32706 Td* 34.29
```
`python e1b.py` shows that all sampled Huang points sit at P_y = P_ys.

**Ma 2017 (local PDF, `docs/ejector/1-s2.0-S0140700716304170-main.pdf`).** Eqs. (1)–(2) apply η_n "in both the converging and diverging sections". The model is a Munday–Bagster constant-pressure model. Table 1 is a "verification ... and the obtained hypothetical throat area ratio": the measured ER* is used to obtain Arh. Ma's η values were therefore never used to predict ER, and their η_n has a different role from this model's η_m (beyond the throat only, C_d = 1).

**Why it matters.** The 11 % ER bias is purely the suction-choke-in-annulus assumption with η_s. Neither off-design (subcritical) gas behaviour nor the mixing model's effect on ER is tested. "The two targets pull η_s in opposite directions, so the efficiencies are not independent of the operating point" (`:157-159`) is a non sequitur. Both targets come from the same operating points, so the tension points to model structure, not operating-point dependence.

**Fix.** Rephrase it as "verification of the suction-choke assumption (ER) and of η_mix/η_d (P_b*) at imposed ER". Note the structural difference from Ma's model. Change the coverage row to "critical mode only".

## F12 — MINOR — The 4 % mixing-march step biases P_b*

**Evidence.** `ejector1d.py:339` (`p * 0.96`). Detection of the feasibility boundary needs R > 0 at a grid node, and P_out has a square-root singularity at the boundary. `solution_methods.rst:179-180` promises "results that do not depend on discretisation choices".

**Reproduction.** `python e1b.py`:
```
case1 step 0.96: ER 0.199193 Pb* 40.9005 bar
case1 step 0.99: ER 0.199284 Pb* 40.8144 bar
case1 step 0.999: ER 0.199293 Pb* 40.7870 bar    (0.9999 identical)
```
`e1_mix_step.py` shows +0.04 to +0.14 bar for seven critical Giacomelli cases, with ER affected below 0.06 %. Huang is unaffected (suction-choked).

**Fix.** Before declaring a trial infeasible after `r < r_prev`, refine the residual maximum with a bounded maximiser and test max R ≥ 0. Then bisect on max R instead of on the grid indicator. The caption value "P_b* ≈ 40.9 bar" becomes 40.8.

## F13 — MINOR — Test table and test counts do not match the suite

- `validation.rst:27` and `README.md:16` say "13 tests". pytest collects 19 (14 before `rate_flows`). The table lacks `test_motive_throat_isentropic_eta_beyond` and the three `rate_flows` tests.
- `validation.rst:62` says "fitted C_d ∈ (1.15, 1.25)". The fitted value is 1.138 and the test asserts `1.10 < cd < 1.18` (`test_ejector1d.py`).
- `validation.rst:56` says "three ... runs within 5 %". The values are +6.8 / +5.8 / +3.3 % (`goncalves2024.csv`), and the test asserts `0 < e < 7.5`.

## F14 — MINOR — Off-design figure

`operating_modes.rst:86-87` says "the model reports backflow (not shown)", but `offdesign_case1.png` plots the backflow segment (`make_offdesign.py:47`). The dotted "P_b*" line is drawn at `Po[k[0]]` (`make_offdesign.py:55`), the first subcritical grid value of 41.0 bar, not at P_b*.

## F15 — MINOR — Regime labels compared with pressure-based categories

The Giacomelli "subcritical" cases 28–36 are at 83.2–96.3 bar (P > P_c = 73.8 bar, T < T_c). The "subcooled" cases are at 70.9–73.6 bar (`e8_stats` / ad-hoc check). `validation.rst:396-401` supports "liquid-like motive needs ~4 % more" with Ringstad's HEM errors of 5.5 % above P_c and 30.5 % below 59 bar. Ringstad's classes are pressure-based, and in those terms most of these points are "above P_c". No calibration point lies below 70.9 bar. The Ringstad numbers themselves are verified (Table 5 of `1-s2.0-S2451904920301670-main.pdf`). Define the regime labels and avoid the cross-classification.

## F16 — MINOR — The schematic contradicts the model geometry

`tikz_ejector.png` shows the nozzle exit e upstream of y, inside the converging suction chamber. The model puts the exit plane at the mixing inlet: the force P_e·A_e acts at y and A_sy = A_mix − A_e (`ejector1d.py:383-392`, `nozzles.rst:10-14`). Either draw e = y, or state that the nozzle-exit position is not modelled. That matters for Huang's geometries, where the exit sits upstream of the constant-area section.

## F17 — MINOR — `pq()` returns a molar quality in a mass-fraction field

`ejector1d.py:162` (`st.q = float(q)`); `State.q` is documented as "vapour MASS fraction" (`:64`). `python e18_mixture_q.py`: `pq(q=0.3)` gives `State.q = 0.3`, against a mass fraction of 0.2804 for CO2/N2. `_out()` itself converts correctly (0.1346 against molar 0.1548).

## F18 — MINOR — Stale or inaccurate text

- `ejector1d.py:7` says "three-call interface". There are four flashes plus M(z) (README and architecture agree on four).
- `ejector1d.py:11` cites "Liu & Groll 2012"; the bib key is `liu2013`.
- `architecture.rst:90-94` says the PS fallback "carries over from the reference implementation". The reference implementation has no fallback (`Thermo.ps` calls CoolProp directly).
- `architecture.rst:123-137` flash counts (2410/2665/1419/15758) against today's 2197/2450/1355/14620 (inline measurement). The mixture cost is 25 ms per flash and 35 s in the techref, against 22 ms and 25 s in `README.md:122-123`.

## F19 — MINOR (SUSPECTED) — `design_for_lift` error type

If `f(hi) > 0` still holds when `hi ≥ ER_max` (`ejector1d.py:646`), `brentq` raises `ValueError` (same signs). `gen_data.duty()` catches only `RuntimeError` (`:271`). This was not reproduced. It needs a duty where the optimum design exceeds P_req at ER = 20.

## F20 — MINOR — Overstatements

- `operating_modes.rst:118-119`: "the best ejector the efficiency set allows". The design is optimal only over P_y within the family with an ideally expanded nozzle (P_e = P_y) and A_mix = A_e + A_sy. A_mix and A_e are not free variables.
- `flow_fundamentals.rst:212-214`: "Their [C_d, φ] effect on the outlet pressure is small". True in design mode, where P_out is independent of C_d. In rating mode C_d changes ṁ_m by 14 % and with it ER and P_out (see F2).
- `validation.rst:213-224`: with the measured-temperature (P,T) inlet all three Gonçalves runs return **backflow** (`goncalves2024.csv`, `p_T` rows), which the text does not mention.

---

## Checked and found correct

- **Mixing balances.** Code (`_mix`, `_pass`) matches eq-mix-momentum, eq-mix-energy and eq-mix-mass term by term. η_mix multiplies only the momentum flux, the same form as Ma 2017 Eq. (25).
- **Subsonic root selection.** It agrees with a 3000-point residual scan in 100 trials over 4 duties, with 0 disagreements and R(P3) unimodal everywhere (`e15_mixroot.py`): subcooled-liquid CO2, Giacomelli case 37, water jet pump, flashing hot water / steam.
- **Rating scan shape.** Feasibility is monotone in P_y (one transition) and P_out(P_y) increases monotonically, for Giacomelli cases 1, 15, 21 and 40 on 400-point scans (`e7_scan.py`). The bisection fix for the scan-resolution dependence of critical ER works.
- **Motive path.** It matches eq-motive-path (`P_loss`). G* is independent of η_m.
- **Choke grid.** The stage-1 calibration (`P_low = 0.2·PS`) and rating (`P_low = 5.19 bar`) give the same G* to 3e-7. The choke search with n = 60 against n = 600 differs by 7e-7 (`e9_grid.py`).
- **Diffuser and conservation.** eq-diffuser matches the code and its root is unique. The overall energy balance is exact in the subcritical and design results. eq-eta-ej is the Köhler/Elbel definition.
- **Mixtures.** The mass quality from phase densities is correct for the mixture flash. `_mixed_z` is correct.
- **Reported numbers.** Every number checked in the `.rst` tables and text reproduces from the CSVs:
  - nozzle table, HNE +44 %;
  - Huang statistics (+11.2/11.6/28.9 %, −0.22/0.47 K, max 1.99 K; η_s = 0.7 row);
  - Gonçalves table;
  - calibration statistics (12.2 %, 1.7 %, biases, 31.1 %, 5.9 %, ER MAE 0.017/0.061, per-regime 6.7/4.5/4.1 %, cross 5.6/14.9 %, 15 critical / 27 subcritical);
  - required-C_d means 1.13/1.17 and φ ranges;
  - duty table and σ range;
  - off-design caption values.
- **Data provenance.** The CSVs reproduce from the snapshot code. The README duty numbers (typical/optimistic, pumped to 90–100 bar, HNE and subcooling G*, dry-ice table) reproduce (`e12_readme.py`).
- **Transcriptions and citations.**
  - Huang data match Ma 2017 Table 1, and Ma's η set 0.95/0.85/0.95/0.85 is confirmed.
  - The Giacomelli rows satisfy ER = MSN/MMN to 0.0009.
  - Ringstad: Table 5 (HEM 5.5 % above P_c, 30.5 % below 59 bar; mixture model 1.1 %), the text on the evaporation coefficient "e", and the appendix defects (P_O 39.95/40.34 in an MPa column; Haida block without ER) are all confirmed.
  - The Flügel (NACA TM-982) and Hatziavramidis summaries are accurate.
- **Cross-references.** Every `:ref:`, `:eq:`, `:numref:` and `:cite:` target resolves.
- **Small checks.** C_d = 1.138 ↔ 0.960 mm; A_e/A_t = 1.31; the √(1+K_n) momentum factor; CO2 triple point 5.18 bar / 216.6 K.
- **Tests.** All 19 tests pass on the snapshot.

**Unverifiable locally (no PDF):** Gonçalves' CFD figures (−1.5 to −5.7 %, ER about 0.16) and the 9 K inconsistency; the Giacomelli geometry (Fig. 2) and its Tables 3–4; the Bodys 2020 and Palacz 2015 statements; Liu & Groll as a source for the η_mix form.

---

## Resolution (2026-11-24, applied to `ejector1d.py`, `EjectorCore` and the techref)

| Finding | Resolution |
|---|---|
| F1 choke switch | Converging–diverging nozzle modelled with three back-pressure ranges: supersonic exit below P_se (normal shock at the exit), shock inside the nozzle with a subsonic exit at P_y up to P_fc, exit-controlled flow above P_fc. Motive flow is continuous everywhere and the exit state is continuous at P_fc. A residual step remains at P_se (0.11 bar for the N2 case; it vanishes for eta_mix = 1, because eta_mix acts on the momentum flux only). `rate()` now raises if Brent lands on a step instead of returning a different P_out. Tests: `test_cd_nozzle_motive_state_continuous`, `test_rate_never_returns_an_unspecified_outlet_pressure`. |
| F2 exit clamp | No clamp: 1.0001 < A_e/A_t < Cd raises a geometry error (also checked in the CAPE-OPEN unit's Validate). Throat-only vs uniform Cd is still an open decision for the user. Test: `test_exit_area_below_cd_raises_instead_of_clamping`. |
| F3 design at small ER | Design scan adds 10 log-spaced points up to 0.9999 P_s0. `design_for_lift` finds the maximum over ER first and reports "unreachable" only above it. The worked example now gives 0.19 kg/s / 145 kg/s for the sub/subcooled set. Test: `test_design_for_lift_uses_the_maximum_over_er`. |
| F4 mixed operating points | Critical: outlet re-flashed at the specified P_out, eta_ej there, P_b_critical/ER_critical reported separately. Backflow: outlet = motive stream at P_out, eta_ej = 0. Test: `test_critical_and_backflow_results_are_consistent`. |
| F5 solid guard | `check_solid` raises SolidPhaseRisk when the solution sits on the 1.001·P_tp floor. Test: `test_solution_on_the_triple_point_floor_raises`. |
| F6 HNE | Documented as a heuristic density correction. Consistent-form numbers quoted (+28 % vs +44 % at phi = 0.5; 4–13 % overstatement, re-run and confirmed). Conclusion kept. Not reimplemented. |
| F7 regime/lift | Text rewritten: z = 3.0; regime and lift confounded; 4.6 % MAE within the sub/subcooled lift range (recomputed). |
| F8 flat objective | Claim removed. |
| F9 duty uncertainty | eta_m sensitivity (22.8 / 24.0 / 25.3 bar, recomputed), lift bias (r = -0.43) and the under-/ideal-expansion difference added. Verdict now "probably short of 28 bar". |
| F10 Goncalves | Stated as inconclusive for Cd transfer; calibrated set +18–21 %; "not independently validated" in the coverage table; (P,T) inlet gives backflow (mentioned). |
| F11 Huang | Rephrased: suction-choke ER (eta_s only) and P_b* at imposed ER; Ma's model and its use of the measured ER described; coverage row changed to "critical mode only". |
| F12 march step | Residual maximum refined between grid points; P_b* for case 1 is now 40.79 bar. |
| F13 test table | Updated (25 Python tests; Cd and Goncalves rows corrected). |
| F14 off-design figure | P_b* line at the bisected value (exported to the CSV); caption corrected. |
| F15 regime labels | Labels defined; Ringstad comparison marked qualitative. |
| F16 schematic | Caption states that the model puts the nozzle exit plane at y. |
| F17 pq quality | Molar q is no longer written into the mass-fraction field except at 0/1 (Python and C#). |
| F18 stale text | Docstring, Liu & Groll year, the fallback wording and the flash-count table updated (re-measured). |
| F19 error type | `design_for_lift` rewritten; failures are caught and reported. |
| F20 overstatements | Design optimality, Cd effect (design vs rating) and the Goncalves backflow corrected. |

The C# engine received the same fixes. Nine Python reference cases were added for these paths, including three that must raise. C# matches Python on all 411 compared quantities, with zero difference.
