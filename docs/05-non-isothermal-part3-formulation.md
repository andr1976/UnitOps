# Non-Isothermal Model (Dias/Fontoura/Pinto Part III) — Recovered Formulation

**Purpose:** implementation-ready spec for the non-isothermal extension (task #11). Recovered from
**open-access Part IV** (Processes 2024, 12, 2597, DOI 10.3390/pr12112597), which restates Part III's
energy balance verbatim and cites it (*"the energy balance … described previously by … Fontoura et al.
[Part III]"*). Part III itself (Chem. Eng. Res. Des. 177 (2022) 376–393, DOI 10.1016/j.cherd.2021.10.036)
is paywalled but **not required** to implement. Equations transcribed from MathML (not images).

## Key clarifications (correct earlier assumptions)
- **Driving force is partial-pressure across the WHOLE series, including Part III.** Real-gas behaviour
  enters only via thermodynamic **properties** (the authors use **virial** correlations), never a
  fugacity-corrected flux. So `CrossFlowModel`'s flux law already matches; the non-isothermal step adds
  energy balances + Joule–Thomson + property calls, not a new driving force. (Retire the "fugacity seam" note.)
- **Permeance is constant** — no Arrhenius/T-dependence survives in the open restatement (one-way T coupling:
  mass balance → flux → temperature field → properties; no T→permeance feedback). Confirm against Part III
  only if T-dependent permeance is ever required.
- **Authorship:** Parts I–IV = COPPE-UFRJ + Petrobras. *Membranes* 2021, 11, 654 (CO₂/NG) is a **different
  group** (UTP Malaysia) — an independent re-implementation, useful as a fully-specified fixture, not the
  Petrobras case.

## Geometry & coordinates
2-D grid over one spiral-wound **leaf**, `z₁ = x/L` (axial, residue/retentate flow), `z₂ = y/W` (transverse,
permeate flow). Cross-flow at leaf scale; counter-current at module scale (elements in series). Scale-up
hierarchy leaf → element → tube → bank → train → stage. Upwind + semi-implicit discretisation; local
equilibrium (Eqs 17–19) initialised by modified-Powell (`scipy hybr`). Reference impl is Python; whole-unit
solve ≈ 13 s.

## Mass balance + flux (isothermal base — same as CrossFlowModel)
```
(1) ∂f_i/∂z₁ = − β_i (x_i − y_i·γ)·s_x          (residue)
(2) ∂g_i/∂z₂ = + β_i (x_i − y_i·γ)·s_y          (permeate)
(3) f_i=F_i/F_f  (4) g_i=G_i/F_f  (5) γ=P_p/P_r  (6) β_i=S_i/S_m
(7) z₁=x/L  (8) z₂=y/W  (9) s_x=S_m·P_r·L/(F_f·h_x·l)  (10) s_y=S_m·P_r·W/(F_f·h_y·l)
(11) f=Σf_i (12) g=Σg_i (13) x_i=f_i/f (14) y_i=g_i/g
BCs (15)–(16); local phase-equilibrium closure (17)–(19) [Shindo].
```
Units: P [Pa]; F,G [kmol·s⁻¹·m⁻²]; S [kmol·m⁻²·s⁻¹·Pa⁻¹]; h_x,h_y,W,L,l [m]; subscript m = base (most
permeable) component.

## Energy balances (Part III contribution)
```
(20) Σ_i f_i·ω_i·(∂θ_r/∂z₁) = − q_x·(θ_r − θ_p)                              (residue)
(21) Σ_i g_i·ω_i·(∂θ_p/∂z₂) = Σ_i ω_i·Γ·β_i·(x_i − y_i·γ)·D + q_y·(θ_r − θ_p) (permeate)
```
Dimensionless groups / variables:
```
(22) θ_r=T_r/T_f  (23) θ_p=T_p/T_f  (24) ω_i=c_{p,i}/c_{p,m}  (25) Γ=μ/μ_f (JT coeff, dimensionless)
(26) q_x=U·L/(h_x·F_f·c_{p,m})  (27) q_y=U·W/(h_y·F_f·c_{p,m})
(28) D=ΔP·P_r·μ_m·S_m·W/(h_y·l·F_f·T_f)    (ΔP = P_r − P_p, transmembrane)
```
Temperature BCs:
```
(29) θ_r(z₁=0,z₂)=1 ; ∂θ_p/∂z₂|_{z₁=0}=0
(30) ∂θ_r/∂z₁|_{z₂=0} = −q_x(θ_r−θ_p)/Σ_i f_i·ω_i ; θ_p(z₁,z₂=1)=θ_p(z₁,z₂=0)
```
Reading: retentate changes T **only** by heat exchange with permeate (no JT on the high-pressure side).
Permeate gets (i) enthalpy + **Joule–Thomson expansion cooling** of the permeating flux (the `Γ…D` term,
scaling with ΔP·P_r), and (ii) stream-stream heat exchange. Heat loss to surroundings neglected (adiabatic
module). `U` = lumped overall heat-transfer coefficient through the membrane [W·m⁻²·K⁻¹]. `μ` = Joule–Thomson
coefficient [K·Pa⁻¹].

## Thermo-vs-membrane split (CAPE-OPEN)
**PME supplies** (per grid cell): `c_{p,i}` (→ ω, q_x, q_y); Joule–Thomson coeff `μ` (→ Γ, μ_m in D) — most
CO packages don't expose μ directly, compute `μ = −(1/c_p)(∂H/∂P)_T` by finite-diff on PME enthalpy calls;
enthalpy; density/molar volume (virial); dew-point flash (T·VF=1) + fugacity coefficients (for the dew-point
/ condensation check). **Unit op owns:** permeances S_i, selectivity, geometry (L,W,h_x,h_y,l, element/bank/
tube counts), overall `U`. **Streams/ports:** P_f,P_r,P_p,ΔP, T_f,T_r,T_p; add an **energy port** if duty is
tracked. Note: axial ΔP is neglected in Part IV (matches the real site); the independent Membranes-2021 model
adds Hagen–Poiseuille (`λ=44·Re^−0.55`, needs viscosity) if pressure drop is wanted.

## Validation fixtures
### Petrobras offshore base case (Part IV Table 1) — the on-target case, partly confidential
12 components (C1–C8, CO₂, N₂). Base-case permeances [×10⁻¹⁰ MNm³·day⁻¹·m⁻²·bar⁻¹]: **CO₂ 102.91, CH₄ 7.24,
C₂H₆ 2.88** → CO₂/CH₄ selectivity **14.2**. Outputs: **permeate T 39.64 °C, retentate T 38.11 °C, retentate
dew point 19.64 °C, retentate CO₂ 4.38 %, permeate CO₂ 79.52 %, stage-cut 0.3097.** S_max normaliser
`3×10⁻⁷ MSm³·day⁻¹·m⁻²·bar⁻¹`. Dew-point safety margin monitored at 10 °C; HC-loss band 18–23 %; elements/tube
9–12. (Actual feed composition/pressures/flows withheld for industrial confidentiality — not in any source.)

### Independent fully-specified fixture (Membranes 2021, 11, 654 — different group, isothermal)
Permeances (GPU): **CO₂ 90, CH₄ 4.5, C₂H₆ 1.8, C₃H₈ 1.8, H₂S 87.3** (CO₂/CH₄ = 20). Feeds: binary
CO₂0.40/CH₄0.60; multicomp CO₂0.40/CH₄0.50/C₂H₆0.08/C₃H₈0.02; +H₂S case CO₂0.30/CH₄0.50/C₂0.08/C₃0.02/H₂S0.10.
Conditions: feed **40 °C, 35 bar**, permeate **1.05 bar**. Geometry: feed spacer 9.0×10⁻² cm, permeate
4.0×10⁻² cm; porosity 0.846/0.616; 30 envelopes; 8-inch (20.32 cm) module; 1 m length. Validated vs Baker
CO₂/H₂ retentate data, MAPE 1.23 %.

## Not recoverable from open access
Part III's exact FD stencil/derivation of s_x,s_y,D,q_x,q_y (Part IV summarises, cites Part III); whether
Part III has Arrhenius permeance-T (absent in Part IV → treat permeance as constant); Part III's own
sensitivity numbers; Part I's four validation tables; the confidential offshore feed data.

## Citations
Part I: J. Membr. Sci. 612 (2020) 118278, 10.1016/j.memsci.2020.118278 (paywalled).
Part II: Processes 2020, 8, 1035, 10.3390/pr8091035 (OA).
Part III: Chem. Eng. Res. Des. 177 (2022) 376–393, 10.1016/j.cherd.2021.10.036 (paywalled; restated via IV).
Part IV: Processes 2024, 12, 2597, 10.3390/pr12112597 (OA — primary recovery source).
Membranes 2021, 11, 654, 10.3390/membranes11090654 (OA; independent group).
