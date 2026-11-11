"""Validation: Goncalves et al. (2024) subcooled-liquid CO2 motive ejector.

Goncalves, Varga, Butrymowicz, Smierciew, "Analysis of HEM applicability for a
subcritical flashing ejector at low motive pressure", Front. Mech. Eng. 10
(2024) 1410743, doi:10.3389/fmech.2024.1410743 (open access).

Operating data: Table 1 (rows taken as Runs 1-3 in order).  Geometry: Fig. 5
(dimensioned drawing, mm): motive throat 1.5, motive exit 1.9 (assignment read
from the drawing), constant-area section 4 x 40, diffuser 5 deg to 15.
Measured: motive mass flow and outlet pressure; entrainment is tiny
(ER 0.6-1.7 %), the authors' HEM CFD gave ER ~16 % with double choking.
"""
import numpy as np
import CoolProp.CoolProp as CP
from ejector1d import Thermo, Ejector, area

# Motive inlet: the authors found measured T ~9 K above the EOS value implied by
# measured (p, rho) and used (p, rho) as the CFD boundary condition (Sec. 2,
# Table 1: T*(p, rho) = -10.7/-10.5/-11.3 C).  Both options are kept.
#        P_m [kPa] T_m [C] rho_m   m_m [g/s]  P_s [kPa] T_s [C] m_s [g/s]  P_out [kPa]
RUNS = [(3483, -1.4, 992, 71.1, 2012, -4.9, 0.522, 2233),
        (3474, -1.3, 991, 70.8, 2013, -4.3, 0.409, 2227),
        (3451, -2.1, 995, 74.0, 1732, -6.6, 1.23, 1961)]
D_T, D_E, D_MIX = 1.5e-3, 1.9e-3, 4.0e-3


def run(eta_m=0.95, eta_s=0.85, eta_mix=0.90, eta_d=0.80, motive_model="HEM", phi_ne=1.0,
        inlet="p_rho"):
    th = Thermo("CO2")
    ej = Ejector(th, eta_m=eta_m, eta_s=eta_s, eta_mix=eta_mix, eta_d=eta_d,
                 motive_model=motive_model, phi_ne=phi_ne)
    out = []
    for k, (Pm, Tm, rhom, mm, Ps, Ts, ms, Po) in enumerate(RUNS, 1):
        if inlet == "p_rho":
            Tm = CP.PropsSI("T", "P", Pm * 1e3, "D", rhom, "CO2") - 273.15
        m0 = ej.inlet(Pm * 1e3, T=Tm + 273.15)
        s0 = ej.inlet(Ps * 1e3, T=Ts + 273.15)
        r = ej.rate(m0, s0, area(D_T), area(D_E), area(D_MIX), P_out=Po * 1e3)
        err_m = (r.m_m * 1e3 - mm) / mm * 100
        print(f"Run {k}: T_m={Tm:6.2f} C rho={m0.rho:6.1f}  m_m sim {r.m_m*1e3:5.1f} "
              f"exp {mm:5.1f} g/s ({err_m:+5.1f}%) P_t={r.P_t/1e5:5.2f} bar | {r.regime:11s} "
              f"ER sim {r.ER*100:5.2f}% exp {ms/mm*100:4.2f}%  P_y={r.P_y/1e5:5.2f}")
        out.append((err_m, r.ER, ms / mm))
    return out


if __name__ == "__main__":
    for kw in (dict(inlet="p_T"), dict(eta_m=1.0), dict(eta_m=0.95), dict(eta_m=0.90),
               dict(eta_m=0.95, eta_s=0.5)):
        print(kw)
        run(**kw)
