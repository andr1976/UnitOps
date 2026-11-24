"""Comparison with manufacturer design charts: GEA ejector catalogue (BTC0011-EN, 2018).

The catalogue charts are for tailor-made ("optimum efficiency") ejectors, so they are
compared with the model's DESIGN mode (optimum geometry for each duty), both mixing
models.  Digitised data in data/gea_*.csv (provenance in their headers).

  steam_vacuum : saturated steam p1 (gauge) -> air 20 C from p0, compressed to 1013 mbar;
                 chart: specific steam consumption mu [kg steam / kg air]  (p.39 Fig. 1)
  ventilator   : air p1 (gauge, 20 C) -> air at 1013 mbar; chart: compression dp [mbar]
                 against suction ratio m [kg air / kg motive air]          (p.60 Fig. 2)
  liquid_jet   : water 20 C at p1 (gauge) -> air 20 C from p0 to 1013 mbar; chart: air
                 flow M0 (Fig. 1) and water flow V1 (Fig. 2) of pump size 2 -> ER = M0/(rho V1)

    python validate_gea.py [steam|vent|liquid] ...
"""
import csv
import math
import os
import sys
from concurrent.futures import ProcessPoolExecutor

import numpy as np

from ejector1d import Ejector, Thermo
from water_air import WaterAirThermo

HERE = os.path.dirname(os.path.abspath(__file__))
P_ATM = 1.01325e5
T_AIR = 293.15
EFF = dict(Screening=(0.90, 0.85, 0.90, 0.80), GasEjector_Ma2017=(0.95, 0.85, 0.95, 0.85))


def _read(name):
    with open(os.path.join(HERE, "data", name), encoding="utf-8") as f:
        return list(csv.DictReader(line for line in f if not line.startswith("#")))


def _p1(label):
    """'p1=10 bar g' -> 10.0 (first number after 'p1=')."""
    s = label.split("p1=")[1]
    return float(s.split()[0].replace(",", "."))


def _thin(rows, xkey, n):
    """About n points spread along a curve (by index)."""
    idx = np.unique(np.linspace(0, len(rows) - 1, n).round().astype(int))
    return [rows[i] for i in idx]


_TH = {}


def _th(kind):
    if kind not in _TH:
        _TH[kind] = WaterAirThermo() if kind == "water_air" else Thermo("Air", solid_check=False)
    return _TH[kind]


def _task(t):
    """One design evaluation; returns the model value or NaN (with the reason)."""
    kind, mixing, eff, x = t["kind"], t["mixing"], t["eff_values"], t
    try:
        if kind == "steam":
            th = _th("water_air")
            ej = Ejector(th, *eff, mixing=mixing)
            m0 = th.pq((x["p1"] + 1.01325) * 1e5, 1.0, (1.0, 0.0))
            s0 = th.pt(x["p0"] * 100, T_AIR, (0.0, 1.0))
            r = ej.design_for_lift(m0, s0, 1.0, P_ATM)
            return dict(t, model=1.0 / r.ER, ER=r.ER, P_y=r.P_y)
        if kind == "vent":
            th = _th("air")
            ej = Ejector(th, *eff, mixing=mixing)
            m0 = th.pt((x["p1"] + 1.01325) * 1e5, T_AIR)
            s0 = th.pt(P_ATM, T_AIR)
            r = ej.design(m0, s0, 1.0, x["m"])
            return dict(t, model=(r.P_out - P_ATM) / 100, P_y=r.P_y)
        if kind == "liquid":
            th = _th("water_air")
            ej = Ejector(th, *eff, mixing=mixing)
            m0 = th.pt((x["p1"] + 1.01325) * 1e5, T_AIR, (1.0, 0.0))
            s0 = th.pt(x["p0"] * 100, T_AIR, (0.0, 1.0))
            r = ej.design_for_lift(m0, s0, 1.0, P_ATM)
            return dict(t, model=r.ER, P_y=r.P_y)
    except (RuntimeError, ValueError) as e:
        return dict(t, model=math.nan, error=str(e)[:100])
    raise ValueError(kind)


def steam_tasks(n=6):
    rows = _read("gea_steam_vacuum.csv")
    out = []
    for lab in dict.fromkeys(r["curve_label"] for r in rows):
        cur = [r for r in rows if r["curve_label"] == lab]
        for r in _thin(cur, "p0_mbar", n):
            out.append(dict(kind="steam", curve=lab, p1=_p1(lab), p0=float(r["p0_mbar"]),
                            chart=float(r["mu_kg_steam_per_kg_air"])))
    return out


def vent_tasks(n=6):
    rows = _read("gea_air_ventilator.csv")
    out = []
    for lab in dict.fromkeys(r["curve_label"] for r in rows):
        cur = [r for r in rows if r["curve_label"] == lab]
        p1s = [3.0, 5.0] if "3 or 5" in lab else [_p1(lab)]
        for p1 in p1s:
            for r in _thin(cur, "m", n):
                out.append(dict(kind="vent", curve=lab if len(p1s) == 1 else f"p1={p1:g} bar g (as '3 or 5')",
                                p1=p1, m=float(r["m_kg_air_per_kg_motive_air"]), chart=float(r["dp_mbar"])))
    return out


def liquid_tasks(size="size 2"):
    """ER at p0 = 100 mbar for each motive pressure, pump size 2 (Fig. 1 x Fig. 2)."""
    fig1 = [r for r in _read("gea_liquid_jet.csv") if r["curve_label"].startswith(size + " ")]
    fig2 = [r for r in _read("gea_liquid_jet_water.csv") if r["curve_label"].startswith(size + " ")]
    out = []
    for lab in dict.fromkeys(r["curve_label"] for r in fig1):
        cur = [r for r in fig1 if r["curve_label"] == lab and abs(float(r["p0_mbar_abs"]) - 100) < 1e-6]
        if not cur:
            continue
        M0 = float(cur[0]["M0_kg_per_h_air"])
        p1s = [3.0, 2.0] if "3 bar g (H) / 2 bar g (N)" in lab else [_p1(lab)]
        for p1 in p1s:
            cons = "(N)" if p1 <= 2.0 else "(H)"
            w = [r for r in fig2 if r["curve_label"] == f"{size} p0=100 mbar {cons}"]
            xs = np.array([float(r["p1_bar_g"]) for r in w])
            vs = np.array([float(r["V1_m3_per_h"]) for r in w])
            if not (xs.min() - 1e-9 <= p1 <= xs.max() + 1e-9):
                continue
            V1 = float(np.interp(p1, xs, vs))
            out.append(dict(kind="liquid", curve=f"{size} p1={p1:g} bar g {cons}", p1=p1, p0=100.0,
                            chart=M0 / (998.2 * V1), M0=M0, V1=V1))
    return out


def run(tasks, mixings=("CAM", "CPM"), effs=("Screening", "GasEjector_Ma2017"), workers=None):
    jobs = [dict(t, mixing=m, eff=e, eff_values=EFF[e]) for t in tasks for m in mixings for e in effs]
    with ProcessPoolExecutor(max_workers=workers or max(1, (os.cpu_count() or 2) - 2)) as ex:
        return list(ex.map(_task, jobs, chunksize=1))


def report(res, label):
    keys = sorted({(r["mixing"], r["eff"]) for r in res})
    for mixing, eff in keys:
        sel = [r for r in res if r["mixing"] == mixing and r["eff"] == eff]
        e = np.array([(r["model"] / r["chart"] - 1) * 100 for r in sel])
        nf = int(np.sum(~np.isfinite(e)))
        print(f"{label:6s} {mixing} {eff:18s}: model/chart - 1 mean {np.nanmean(e):+6.1f} %  "
              f"MAE {np.nanmean(np.abs(e)):5.1f} %  range {np.nanmin(e):+.0f}..{np.nanmax(e):+.0f} %  "
              f"n {len(e)} fails {nf}")


if __name__ == "__main__":
    which = sys.argv[1:] or ["steam", "vent", "liquid"]
    if "steam" in which:
        report(run(steam_tasks()), "steam")
    if "vent" in which:
        report(run(vent_tasks()), "vent")
    if "liquid" in which:
        res = run(liquid_tasks())
        for r in res:
            print(f"  {r['curve']:22s} {r['mixing']} {r['eff']:18s} ER chart {r['chart']:.3e} model "
                  f"{r['model']:.3e}  {r.get('error', '')}")
        report(res, "liquid")
