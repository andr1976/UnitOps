"""Calibrate ejector efficiencies on R744 multi-ejector data (Giacomelli et al. 2019,
as compiled in Ringstad et al. 2020 Appendix A).

Each point: motive (PM, TM), suction (PS, TS), outlet PO, measured ER.  The model is
run in RATING mode on the published geometry (data/giacomelli2019_geometry.json),
and (eta_s, eta_mix, eta_d) are fitted to the measured ER by least squares.
eta_m only scales the motive flow, which ER is insensitive to; it is fixed
unless absolute motive mass flows are available (then fitted separately).

Usage:  python calibrate_r744.py [--fit eta_s,eta_mix,eta_d] [--loo]
"""
import argparse
import csv
import json
import os

import numpy as np
from scipy.optimize import least_squares

from ejector1d import Ejector, Thermo, area

HERE = os.path.dirname(os.path.abspath(__file__))


def load_points(path=os.path.join(HERE, "data", "ringstad2020_giacomelli.csv")):
    with open(path) as f:
        rows = [r for r in csv.DictReader(line for line in f if not line.startswith("#"))]
    return [dict(PM=float(r["PM"]) * 1e6, TM=float(r["TM"]), PS=float(r["PS"]) * 1e6,
                 TS=float(r["TS"]), PO=float(r["PO"]) * 1e6, ER=float(r["ER"]),
                 flag=r["flag"]) for r in rows]


def load_geometry(path=os.path.join(HERE, "data", "giacomelli2019_geometry.json")):
    with open(path) as f:
        g = json.load(f)
    return area(g["D_throat_mm"] * 1e-3), area(g["D_exit_mm"] * 1e-3), area(g["D_mix_mm"] * 1e-3), g


def simulate(points, geom, eta, th=None):
    th = th or Thermo("CO2")
    A_t, A_e, A_mix = geom
    ej = Ejector(th, **eta)
    out = []
    for p in points:
        m0 = ej.inlet(p["PM"], T=p["TM"])
        s0 = ej.inlet(p["PS"], T=p["TS"])
        try:
            r = ej.rate(m0, s0, A_t, A_e, A_mix, p["PO"])
            out.append((r.ER, r.m_m, r.regime))
        except RuntimeError:
            out.append((np.nan, np.nan, "fail"))
    return out


def fit(points, geom, names, eta0):
    def resid(x):
        eta = dict(eta0, **dict(zip(names, x)))
        sim = simulate(points, geom, eta)
        return np.array([np.nan_to_num(s[0] - p["ER"], nan=1.0) for s, p in zip(sim, points)])

    x0 = [eta0[n] for n in names]
    r = least_squares(resid, x0, bounds=([0.3] * len(names), [1.0] * len(names)),
                      diff_step=0.02)
    return dict(eta0, **dict(zip(names, r.x))), r


def report(points, sim, label):
    print(f"\n{label}")
    e = []
    for p, (er, mm, reg) in zip(points, sim):
        e.append(er - p["ER"])
        print(f"  PM={p['PM']/1e6:5.2f} TM={p['TM']:6.1f} PS={p['PS']/1e6:5.3f} PO={p['PO']/1e6:5.3f} "
              f"ER exp {p['ER']:.3f} sim {er:.3f} ({reg})  m_m={mm*1e3:6.1f} g/s {p['flag']}")
    e = np.array(e)
    print(f"  ER abs. error: mean {np.nanmean(e):+.3f}  MAE {np.nanmean(np.abs(e)):.3f}  "
          f"max {np.nanmax(np.abs(e)):.3f}")
    return e


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--fit", default="eta_s,eta_mix,eta_d")
    ap.add_argument("--loo", action="store_true", help="leave-one-out cross-validation")
    a = ap.parse_args()
    pts = load_points()
    A_t, A_e, A_mix, g = load_geometry()
    geom = (A_t, A_e, A_mix)
    print(f"geometry: {g}")
    eta0 = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80)
    report(pts, simulate(pts, geom, eta0), f"uncalibrated {eta0}")
    names = a.fit.split(",")
    eta, r = fit(pts, geom, names, eta0)
    print("\nfitted:", {k: round(v, 3) for k, v in eta.items()})
    report(pts, simulate(pts, geom, eta), "calibrated")
    if a.loo:
        errs = []
        for i in range(len(pts)):
            train = pts[:i] + pts[i + 1:]
            e_i, _ = fit(train, geom, names, eta0)
            er = simulate([pts[i]], geom, e_i)[0][0]
            errs.append(er - pts[i]["ER"])
        errs = np.array(errs)
        print(f"\nleave-one-out ER MAE {np.nanmean(np.abs(errs)):.3f}  max {np.nanmax(np.abs(errs)):.3f}")
