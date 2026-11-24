"""Validation: motive mass flow of the SINTEF R744 ejectors, Bodys et al. (2020).

150 operating points with subcooled or near-saturated liquid motive at 47-70 bar
(data/bodys2020.csv, from the paper's supplementary Tables 3-4).  Only the motive
nozzle is fully dimensioned (throat 1.41 / 2.00 mm), so this tests the choked
motive flux G* alone: Cd_req = m_meas / (G*_HEM A_t), and the HNE vapour-generation
fraction phi that reproduces m_meas with Cd = 1.

    python validate_bodys2020.py            # summary
    python validate_bodys2020.py out.csv    # also write the per-point table
"""
import csv
import math
import os
import sys

import numpy as np
from scipy.optimize import brentq

from ejector1d import Thermo, Expansion

HERE = os.path.dirname(os.path.abspath(__file__))
D_T = {"A": 1.41e-3, "B": 2.00e-3}
P_MIN = 10e5          # lower bound of the critical-flux search (above the triple point)
CD_R744 = 1.13759     # Giacomelli 2019 all-points calibration (techref giacomelli_fit.csv)
PHI_R744 = 0.628      # best single HNE phi on the same 42 points (techref giacomelli_hne_summary.csv)


def load(path=os.path.join(HERE, "data", "bodys2020.csv")):
    with open(path, encoding="utf-8") as f:
        rows = list(csv.DictReader(line for line in f if not line.startswith("#")))
    for r in rows:
        for k in r:
            if k not in ("nozzle", "oc"):
                r[k] = float(r[k])
    return rows


def g_star(th, m0, phi=1.0):
    """(P*, G*, choked) on the isentrope; choked=False if the flux still rises at P_MIN."""
    P_t, G = Expansion(th, m0, 1.0, phi=phi).critical(P_MIN)
    return P_t, G, P_t > 1.0001 * P_MIN


def analyse(th=None):
    th = th or Thermo("CO2")
    out = []
    for r in load():
        P, T = r["P_mn_bar"] * 1e5, r["T_mn_C"] + 273.15
        m0 = th.pt(P, T)
        sat = th.pq(P, 0.0)
        A_t = math.pi / 4 * D_T[r["nozzle"]] ** 2
        P_t, G, choked = g_star(th, m0)
        m_hem = G * A_t

        def resid(phi):
            return g_star(th, m0, phi)[1] * A_t - r["m_mn_kgs"]

        phi_req = math.nan
        if m_hem < r["m_mn_kgs"]:
            try:
                if resid(0.05) > 0:
                    phi_req = brentq(resid, 0.05, 1.0, xtol=1e-4)
            except ValueError:
                pass
        out.append(dict(nozzle=r["nozzle"], oc=r["oc"], P_mn_bar=r["P_mn_bar"], T_mn_C=r["T_mn_C"],
                        subcool_K=sat.T - T, P_sn_bar=r["P_sn_bar"], m_meas=r["m_mn_kgs"],
                        m_hem=m_hem, cd_req=r["m_mn_kgs"] / m_hem, P_t_bar=P_t / 1e5,
                        choked=choked, phi_req=phi_req))
    return out, th


def summary(out):
    a = {k: np.array([o[k] for o in out], dtype=float) for k in ("P_mn_bar", "subcool_K", "m_meas",
                                                                  "m_hem", "cd_req", "phi_req")}
    nz = np.array([o["nozzle"] for o in out])

    def mae(pred):
        return 100 * np.mean(np.abs(pred / a["m_meas"] - 1))

    # best single Cd in the MAE sense (scan)
    cds = np.linspace(0.9, 1.6, 701)
    best_cd = cds[np.argmin([mae(c * a["m_hem"]) for c in cds])]
    print(f"n = {len(out)}  (A {np.sum(nz == 'A')}, B {np.sum(nz == 'B')})")
    print(f"HEM, Cd 1          : MAE {mae(a['m_hem']):5.1f} %   bias {100*np.mean(a['m_hem']/a['m_meas']-1):+5.1f} %")
    print(f"HEM, Cd {CD_R744:.3f} (R744): MAE {mae(CD_R744*a['m_hem']):5.1f} %")
    print(f"HEM, best Cd {best_cd:.3f}  : MAE {mae(best_cd*a['m_hem']):5.1f} %")
    print("Cd_req by motive-pressure band and nozzle:")
    for lo, hi in [(45, 52), (52, 56), (56, 60), (60, 64), (64, 67), (67, 71)]:
        for n in "AB":
            s = (nz == n) & (a["P_mn_bar"] >= lo) & (a["P_mn_bar"] < hi)
            if s.any():
                c = a["cd_req"][s]
                print(f"  {lo}-{hi} bar {n}: n {s.sum():2d}  mean {c.mean():.3f}  sd {c.std():.3f}")
    ok = np.isfinite(a["phi_req"])
    print(f"phi_req (Cd 1) found for {ok.sum()} points: range {np.nanmin(a['phi_req']):.2f}-"
          f"{np.nanmax(a['phi_req']):.2f}, median {np.nanmedian(a['phi_req']):.2f}; "
          f"{np.sum(a['cd_req'] < 1)} points need Cd < 1 (phi > 1)")
    print("corr(Cd_req, subcooling) %.2f   corr(Cd_req, P) %.2f" % (
        np.corrcoef(a["subcool_K"], a["cd_req"])[0, 1], np.corrcoef(a["P_mn_bar"], a["cd_req"])[0, 1]))
    return best_cd


if __name__ == "__main__":
    res, th = analyse()
    summary(res)
    print("flashes", th.n_flash)
    if len(sys.argv) > 1:
        with open(sys.argv[1], "w", newline="") as f:
            w = csv.DictWriter(f, fieldnames=list(res[0]))
            w.writeheader()
            w.writerows(res)
