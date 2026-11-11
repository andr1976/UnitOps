"""Calibrate ejector efficiencies on Giacomelli et al. (2019) R744 ejector data.

42 measured points (data/giacomelli2019.csv; motive 71-104 bar, supercritical to
subcooled liquid) on one fully specified geometry (Fig. 2 of the paper):
motive throat 0.9 mm, exit 1.03 mm, mixing section 2 mm x 16.9 mm, diffuser 5 deg to 10 mm.
Outlet pressure = P_EJ5 (wall tap at the end of the diffuser).

Stage 1: motive discharge coefficient Cd_m on measured motive mass flow (eta_m fixed).
Stage 2: (eta_s, eta_mix, eta_d) on measured suction mass flow, rating mode at P_EJ5.
Cross-check: fit on the supercritical-inlet points, predict the subcritical ones and
vice versa.

Usage:  python calibrate_r744.py [--quick]
"""
import argparse
import csv
import os

import numpy as np
from scipy.optimize import least_squares

from ejector1d import Ejector, Thermo, area

HERE = os.path.dirname(os.path.abspath(__file__))
GEOM = dict(A_t=area(0.9e-3), A_e=area(1.03e-3), A_mix=area(2.0e-3))


def load_points(path=os.path.join(HERE, "data", "giacomelli2019.csv")):
    with open(path) as f:
        rows = list(csv.DictReader(line for line in f if not line.startswith("#")))
    return [dict(case=int(r["case"]), inlet=r["inlet"],
                 PM=float(r["PMN"]) * 1e5, TM=float(r["TMN"]) + 273.15,
                 mm=float(r["MMN"]) / 60, PS=float(r["PSN"]) * 1e5,
                 TS=float(r["TSN"]) + 273.15, ms=float(r["MSN"]) / 60,
                 PO=float(r["PEJ5"]) * 1e5, ER=float(r["ER"])) for r in rows]


TH = Thermo("CO2")


def simulate(points, eta):
    ej = Ejector(TH, **eta)
    out = []
    for p in points:
        m0 = ej.inlet(p["PM"], T=p["TM"])
        s0 = ej.inlet(p["PS"], T=p["TS"])
        try:
            r = ej.rate(m0, s0, GEOM["A_t"], GEOM["A_e"], GEOM["A_mix"], p["PO"], n_scan=25)
            out.append((r.m_m, r.m_s, r.regime))
        except RuntimeError:
            out.append((np.nan, np.nan, "fail"))
    return out


def motive_only(points, eta):
    """Motive mass flow: choked flux x A_t (independent of suction side)."""
    ej = Ejector(TH, **eta)
    res = []
    for p in points:
        m0 = ej.inlet(p["PM"], T=p["TM"])
        _, _, G = ej._motive(m0, 0.2 * p["PS"])
        res.append(G * GEOM["A_t"])
    return np.array(res)


def fit_cd(points, eta):
    mm = np.array([p["mm"] for p in points])
    G1 = motive_only(points, dict(eta, Cd_m=1.0))
    cd = float(np.sum(G1 * mm) / np.sum(G1 * G1))      # linear LSQ on m = Cd * m_HEM
    return cd, (cd * G1 - mm) / mm * 100


def fit_suction(points, eta0, names=("eta_s", "eta_mix", "eta_d")):
    ms = np.array([p["ms"] for p in points])
    scale = ms.mean()

    def resid(x):
        eta = dict(eta0, **dict(zip(names, x)))
        sim = simulate(points, eta)
        return np.array([np.nan_to_num((s[1] - m) / scale, nan=2.0) for s, m in zip(sim, ms)])

    r = least_squares(resid, [eta0[n] for n in names], bounds=([0.3] * len(names), [1.0] * len(names)),
                      diff_step=0.01, xtol=1e-4, ftol=1e-4)
    return dict(eta0, **dict(zip(names, r.x)))


def stats(points, sim, label, verbose=False):
    em = np.array([(s[0] - p["mm"]) / p["mm"] * 100 for p, s in zip(points, sim)])
    es = np.array([(s[1] - p["ms"]) / p["ms"] * 100 for p, s in zip(points, sim)])
    eER = np.array([s[1] / s[0] - p["ER"] for p, s in zip(points, sim)])
    if verbose:
        for p, s, a, b in zip(points, sim, em, es):
            print(f"   case {p['case']:2d} {p['inlet']:13s} PM={p['PM']/1e5:5.1f} PS={p['PS']/1e5:5.2f} "
                  f"PO={p['PO']/1e5:5.2f} | m_m {a:+5.1f}%  m_s exp {p['ms']*1e3:5.2f} sim {s[1]*1e3:5.2f} g/s "
                  f"({b:+6.1f}%)  ER exp {p['ER']:.3f} sim {s[1]/s[0]:.3f} {s[2]}")
    print(f"{label}: m_m MAE {np.nanmean(np.abs(em)):.1f}% | m_s MAE {np.nanmean(np.abs(es)):.1f}% "
          f"(bias {np.nanmean(es):+.1f}%) | ER MAE {np.nanmean(np.abs(eER)):.3f}  max {np.nanmax(np.abs(eER)):.3f}")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--quick", action="store_true", help="skip the regime cross-check")
    a = ap.parse_args()
    pts = load_points()
    eta0 = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80, Cd_m=1.0)

    stats(pts, simulate(pts, eta0), "uncalibrated (0.90/0.85/0.90/0.80, Cd=1)")
    cd, em = fit_cd(pts, eta0)
    print(f"\nstage 1: Cd_m = {cd:.4f}  motive-flow error after fit: MAE {np.abs(em).mean():.1f}% "
          f"max {np.abs(em).max():.1f}%")
    for reg in ("supercritical", "subcritical", "subcooled"):
        sel = [e for p, e in zip(pts, em) if p["inlet"] == reg]
        print(f"   {reg:13s} bias {np.mean(sel):+5.1f}%  (n={len(sel)})")
    eta1 = dict(eta0, Cd_m=cd)
    eta = fit_suction(pts, eta1)
    print("\nstage 2 fitted:", {k: round(v, 3) for k, v in eta.items()})
    stats(pts, simulate(pts, eta), "calibrated (all 42)", verbose=True)

    if not a.quick:
        sup = [p for p in pts if p["inlet"] == "supercritical"]
        sub = [p for p in pts if p["inlet"] != "supercritical"]
        e_sup = fit_suction(sup, eta1)
        e_sub = fit_suction(sub, eta1)
        print("\ncross-check")
        print("  fit on supercritical:", {k: round(v, 3) for k, v in e_sup.items()})
        stats(sub, simulate(sub, e_sup), "  -> predict subcritical/subcooled")
        print("  fit on subcritical/subcooled:", {k: round(v, 3) for k, v in e_sub.items()})
        stats(sup, simulate(sup, e_sub), "  -> predict supercritical")
