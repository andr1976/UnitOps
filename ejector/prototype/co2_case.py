"""Target application: liquid CO2 motive (47 bar) entraining CO2 vapour (15 bar).

1. Design curve: optimum-design outlet pressure vs entrainment ratio.
2. Off-design rating of one designed geometry vs back pressure.
3. Motive-nozzle sensitivity: HEM vs delayed flashing (HNE), saturated vs subcooled.
4. Dry-ice guard: lowering the suction pressure towards the triple point.
"""
import numpy as np
from ejector1d import Thermo, Ejector, SolidPhaseRisk

th = Thermo("CO2")
P_M, P_S, M_M = 47e5, 15e5, 1.0          # motive, suction [Pa]; motive flow [kg/s]


def ejector(**kw):
    eff = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80)
    eff.update(kw)
    return Ejector(th, **eff)


ej = ejector()
m0 = ej.inlet(P_M, q=0.0)                 # saturated liquid, T = 11.7 C
s0 = ej.inlet(P_S, q=1.0)                 # saturated vapour, T = -28.5 C
print(f"motive  {P_M/1e5:.0f} bar sat. liquid  T={m0.T-273.15:.2f} C")
print(f"suction {P_S/1e5:.0f} bar sat. vapour  T={s0.T-273.15:.2f} C  "
      f"(triple point {th.p_triple/1e5:.2f} bar, {th.T_triple-273.15:.1f} C)\n")

print("1) optimum design, m_m = 1 kg/s")
for ER in (0.02, 0.05, 0.1, 0.2, 0.3, 0.5, 0.75, 1.0):
    r = ej.design(m0, s0, M_M, ER)
    print(f"   ER={ER:4.2f}  P_out={r.P_out/1e5:6.2f} bar  lift={r.P_out/P_S:5.3f}  "
          f"x_out={r.out.q:5.3f}  T_y={r.states['y'].T-273.15:6.1f} C  "
          f"A_mix/A_t={r.A_mix/r.A_t:5.2f}  eta_ej={r.eta_ejector:5.3f}")

print("\n   design for a required outlet pressure:")
for P_out in (18e5, 20e5, 22e5):
    r = ej.design_for_lift(m0, s0, M_M, P_out)
    print(f"   P_out={P_out/1e5:.0f} bar -> max ER={r.ER:.3f}   {r.summary()}")

print("\n2) off-design rating of the ER=0.2 design geometry")
g = ej.design(m0, s0, M_M, 0.2)
for P_out in np.arange(16e5, 24.1e5, 1e5):
    r = ej.rate(m0, s0, g.A_t, g.A_e, g.A_mix, P_out)
    print(f"   P_out={P_out/1e5:5.1f} bar  {r.regime:11s} ER={r.ER:.3f}  m_m={r.m_m:.3f}  "
          f"P_y={r.P_y/1e5:5.2f}")

print("\n3) motive nozzle model sensitivity (design ER=0.2)")
for label, T_sub, kw in (("HEM, saturated", 0, {}),
                         ("HNE phi=0.5, saturated", 0, dict(motive_model="HNE", phi_ne=0.5)),
                         ("HNE phi=0.2, saturated", 0, dict(motive_model="HNE", phi_ne=0.2)),
                         ("HEM, 10 K subcooled", 10, {}),
                         ("HNE phi=0.5, 10 K subcooled", 10, dict(motive_model="HNE", phi_ne=0.5))):
    e = ejector(**kw)
    mi = e.inlet(P_M, q=0.0) if T_sub == 0 else e.inlet(P_M, T=m0.T - T_sub)
    r = e.design(mi, s0, M_M, 0.2)
    print(f"   {label:30s} P_t={r.P_t/1e5:5.2f} bar  G*={M_M/r.A_t/1e3:6.1f} t/m2s  "
          f"D_t={2e3*np.sqrt(r.A_t/np.pi):.3f} mm  P_out={r.P_out/1e5:5.2f} bar")

print("\n4) dry-ice guard: lower suction pressure, ER = 0.2")
for Ps in (10e5, 8e5, 6.5e5, 5.6e5):
    try:
        si = ej.inlet(Ps, q=1.0)
        r = ej.design(m0, si, M_M, 0.2)
        print(f"   P_s={Ps/1e5:4.1f} bar  P_y={r.P_y/1e5:5.2f} bar  T_y={r.states['y'].T-273.15:6.1f} C"
              f"  P_out={r.P_out/1e5:5.2f} bar  P_min/P_triple={r.solid_margin:.3f}")
    except (SolidPhaseRisk, RuntimeError) as exc:
        print(f"   P_s={Ps/1e5:4.1f} bar  -> {type(exc).__name__}: {exc}")
