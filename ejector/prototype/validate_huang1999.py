"""Validation: Huang et al. (1999) R141b gas ejector, critical-mode data.

Data transcribed from Ma, Bao & Roskilly, Int. J. Refrig. 75 (2017) 117-128,
Table 1 (which reproduces Huang et al., Int. J. Refrig. 22 (1999) 354-364).
Columns: Dt, Dp1 (nozzle exit), D3 (constant-area section) [mm], Pp, Ps [bar],
critical entrainment ratio ER*, critical back-pressure saturation temperature
Td* [degC].  Motive and suction are taken as saturated vapour (Huang et al.).
"""
import sys
import numpy as np
import CoolProp.CoolProp as CP
from ejector1d import Thermo, Ejector, area

DATA = """
2.82 5.10 9.20 6.04 0.40 0.438 31.30
2.82 5.10 8.84 6.04 0.40 0.394 33.00
2.64 4.50 8.10 6.04 0.40 0.346 33.60
2.82 5.10 8.54 6.04 0.40 0.351 34.20
2.64 4.50 7.60 6.04 0.40 0.281 36.30
2.82 5.10 8.10 6.04 0.40 0.290 37.10
2.82 5.10 7.60 6.04 0.40 0.227 38.80
2.64 4.50 7.34 6.04 0.40 0.255 38.60
2.82 5.10 7.34 6.04 0.40 0.204 41.00
2.64 4.50 6.70 6.04 0.40 0.186 42.10
2.64 4.50 8.10 5.38 0.40 0.445 31.50
2.64 4.50 7.60 5.38 0.40 0.349 33.80
2.64 4.50 7.34 5.38 0.40 0.304 36.70
2.64 4.50 6.98 5.38 0.40 0.272 37.50
2.64 4.50 6.70 5.38 0.40 0.225 38.90
2.64 4.50 8.10 4.65 0.40 0.539 28.00
2.64 4.50 7.60 4.65 0.40 0.424 30.50
2.64 4.50 7.34 4.65 0.40 0.388 32.30
2.64 4.50 6.98 4.65 0.40 0.312 33.60
2.64 4.50 6.70 4.65 0.40 0.288 35.50
2.64 4.50 8.10 4.00 0.40 0.623 24.40
2.64 4.50 7.60 4.00 0.40 0.489 26.90
2.64 4.50 7.34 4.00 0.40 0.439 29.10
2.64 4.50 6.98 4.00 0.40 0.392 29.50
2.64 4.50 6.70 4.00 0.40 0.326 32.50
2.82 5.10 8.84 6.04 0.47 0.499 33.10
2.82 5.10 8.54 6.04 0.47 0.405 34.20
2.64 4.50 8.10 6.04 0.47 0.454 34.50
2.64 4.50 7.34 6.04 0.47 0.350 38.70
2.82 5.10 7.60 6.04 0.47 0.304 39.30
2.64 4.50 6.70 6.04 0.47 0.235 42.50
2.64 4.50 8.10 5.38 0.47 0.542 32.00
2.64 4.50 7.34 5.38 0.47 0.403 36.00
2.64 4.50 6.70 5.38 0.47 0.295 39.50
2.64 4.50 8.10 4.65 0.47 0.635 28.90
2.64 4.50 7.34 4.65 0.47 0.479 32.40
2.64 4.50 6.70 4.65 0.47 0.340 36.00
2.64 4.50 8.10 4.00 0.47 0.741 25.70
2.64 4.50 7.34 4.00 0.47 0.613 29.20
"""


def run(eta_m=0.95, eta_s=0.85, eta_mix=0.95, eta_d=0.85, verbose=True):
    th = Thermo("R141b")
    ej = Ejector(th, eta_m=eta_m, eta_s=eta_s, eta_mix=eta_mix, eta_d=eta_d)
    rows = np.loadtxt(DATA.strip().splitlines())
    err_er, err_pb = [], []
    for Dt, De, D3, Pp, Ps, er_x, Td_x in rows:
        m0 = ej.inlet(Pp * 1e5, q=1.0)
        s0 = ej.inlet(Ps * 1e5, q=1.0)
        r = ej.rate(m0, s0, area(Dt * 1e-3), area(De * 1e-3), area(D3 * 1e-3), P_out=s0.P)
        # critical mode: r.out is the diffuser outlet at maximum (choked)
        # entrainment, i.e. its pressure is the critical back pressure
        Pb_crit = r.out.P
        Td = CP.PropsSI("T", "P", Pb_crit, "Q", 1, "R141b") - 273.15
        err_er.append((r.ER - er_x) / er_x * 100)
        err_pb.append(Td - Td_x)
        if verbose:
            print(f"{Dt:4.2f} {De:4.2f} {D3:4.2f} {Pp:4.2f} {Ps:4.2f} | ER exp {er_x:.3f} "
                  f"sim {r.ER:.3f} ({err_er[-1]:+6.1f}%) | Td* exp {Td_x:5.2f} sim {Td:5.2f} "
                  f"({err_pb[-1]:+5.2f} K)  m_m={r.m_m*1e3:.2f} g/s")
    err_er, err_pb = np.array(err_er), np.array(err_pb)
    print(f"ER*:  mean {err_er.mean():+.1f}%  MAE {np.abs(err_er).mean():.1f}%  max {np.abs(err_er).max():.1f}%")
    print(f"Td*:  mean {err_pb.mean():+.2f} K  MAE {np.abs(err_pb).mean():.2f} K")
    return err_er, err_pb


if __name__ == "__main__":
    run()
