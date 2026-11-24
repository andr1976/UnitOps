"""Experimental validation sets as model cases, evaluated with either mixing model.

Every set is turned into a list of *cases* (plain dicts, picklable):

  rating   : inlet states, geometry and the measured outlet pressure; the model
             predicts motive and suction mass flow (Giacomelli, Falat, Lawrence)
  critical : inlet states and geometry; the model predicts the critical
             entrainment ratio and critical back pressure (Huang, Shestopalov)

Sources and provenance are in the headers of the data/*.csv files and in
validate_huang1999.py.  evaluate() runs the cases in parallel processes;
fit_cd() and fit() calibrate Cd_m (motive flow) and the efficiencies the same
way as calibrate_r744.py (stage 1 / stage 2).

    python validation_sets.py              # all sets, both mixing models, summary
"""
import csv
import math
import os
from concurrent.futures import ProcessPoolExecutor

import numpy as np
import CoolProp.CoolProp as CP
from scipy.optimize import least_squares

from ejector1d import Ejector, Thermo, area

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "data")


def _read(name):
    with open(os.path.join(DATA, name), encoding="utf-8") as f:
        return list(csv.DictReader(line for line in f if not line.startswith("#")))


def _psat(fluid, T):
    return CP.PropsSI("P", "T", T, "Q", 1, fluid)


# ------------------------------------------------------------------------- data sets
def giacomelli():
    """R744, 42 points, one geometry (Giacomelli et al. 2019); rating at P_EJ5."""
    geo = dict(A_t=area(0.9e-3), A_e=area(1.03e-3), A_mix=area(2.0e-3))
    out = []
    for r in _read("giacomelli2019.csv"):
        out.append(dict(set="Giacomelli2019", id=r["case"], group=r["inlet"], fluid="CO2",
                        kind="rating", m0=("PT", float(r["PMN"]) * 1e5, float(r["TMN"]) + 273.15),
                        s0=("PT", float(r["PSN"]) * 1e5, float(r["TSN"]) + 273.15),
                        P_out=float(r["PEJ5"]) * 1e5, m_m=float(r["MMN"]) / 60,
                        m_s=float(r["MSN"]) / 60, **geo))
    return out


def huang():
    """R141b, 39 critical-mode points, 11 ejectors (Huang et al. 1999, Tables 3-4)."""
    import validate_huang1999 as vh
    out = []
    for k, (Dt, De, D3, Pp, Ps, er, Td) in enumerate(np.loadtxt(vh.DATA.strip().splitlines())):
        out.append(dict(set="Huang1999", id=str(k + 1), group=f"A3/At {(D3 / Dt) ** 2:.2f}",
                        fluid="R141b", kind="critical", m0=("PQ", Pp * 1e5, 1.0),
                        s0=("PQ", Ps * 1e5, 1.0), A_t=area(Dt * 1e-3), A_e=area(De * 1e-3),
                        A_mix=area(D3 * 1e-3), ER=er, P_b=_psat("R141b", Td + 273.15)))
    return out


def falat():
    """R134a vapour ejector, 102 points in 5 back-pressure sweeps (Falat 2018, App. A)."""
    geo = dict(A_t=area(7.09e-3), A_e=area(9.70e-3), A_mix=area(16.4e-3))
    out = []
    for r in _read("falat2018.csv"):
        out.append(dict(set="Falat2018", id=f"{r['series']}.{r['row']}", group=f"series {r['series']}",
                        fluid="R134a", kind="rating",
                        m0=("PT", float(r["P2"]) * 1e3, float(r["T2"]) + 273.15),
                        s0=("PT", float(r["P1"]) * 1e3, float(r["T1"]) + 273.15),
                        P_out=float(r["P4"]) * 1e3, m_m=float(r["FT5"]) / 1e3,
                        m_s=(float(r["FT3"]) + float(r["FT4"])) / 1e3, **geo))
    return out


def lawrence():
    """R134a and R1234yf two-phase ejector, 10 points (Lawrence 2012, Table B.1).
    Motive: condenser-outlet liquid (Pcro, Tcro) throttled isenthalpically to Pmnri."""
    geo = dict(A_t=area(1.0e-3), A_e=area(2.7e-3), A_mix=area(5.0e-3))
    out = []
    for r in _read("lawrence2012.csv"):
        fl = r["fluid"]
        h = CP.PropsSI("H", "P", float(r["Pcro"]) * 1e3, "T", float(r["Tcro"]) + 273.15, fl)
        out.append(dict(set="Lawrence2012", id=r["row"], group=fl, fluid=fl, kind="rating",
                        m0=("PH", float(r["Pmnri"]) * 1e3, h),
                        s0=("PT", float(r["Pelro"]) * 1e3, float(r["Telro"]) + 273.15),
                        P_out=float(r["Pdro"]) * 1e3, m_m=float(r["Mmnri"]) / 1e3,
                        m_s=float(r["Melri"]) / 1e3, **geo))
    return out


def shestopalov(superheat=6.0):
    """R245fa, 9 cylindrical-mixer ejectors, critical mode (Shestopalov et al. 2015, Table 7).
    Motive saturated vapour at Tg; suction at p_sat(Te) with 5-7 K superheat (midpoint);
    critical back pressure = p_sat(Tc*)."""
    g = {}
    for r in _read("shestopalov2015_geometry.csv"):
        try:
            g[(r["component"], r["id"], r["symbol"])] = float(r["value"])
        except ValueError:
            pass                         # descriptive rows without a number
    out = []
    for r in _read("shestopalov2015.csv"):
        if r["mixer"] != "CMC":
            continue
        nz, ch = r["nozzle"], r["chamber"]
        Tg, Te = float(r["Tg_C"]) + 273.15, float(r["Te_C"]) + 273.15
        out.append(dict(set="Shestopalov2015", id=r["ejector"], group=f"Tg {r['Tg_C']}",
                        fluid="R245fa", kind="critical",
                        m0=("PQ", _psat("R245fa", Tg), 1.0),
                        s0=("PT", _psat("R245fa", Te), Te + superheat),
                        A_t=area(g[("nozzle", nz, "dt")] * 1e-3),
                        A_e=area(g[("nozzle", nz, "d1")] * 1e-3),
                        A_mix=area(g[("cyl_chamber", ch, "d3")] * 1e-3),
                        ER=float(r["omega_crit"]),
                        P_b=_psat("R245fa", float(r["Tc_crit_C"]) + 273.15)))
    return out


SETS = dict(Giacomelli2019=giacomelli, Huang1999=huang, Falat2018=falat,
            Lawrence2012=lawrence, Shestopalov2015=shestopalov)


# ------------------------------------------------------------------------- evaluation
_TH = {}


def _thermo(fluid):
    if fluid not in _TH:
        _TH[fluid] = Thermo(fluid)
    return _TH[fluid]


def _state(ej, spec):
    kind, P, x = spec
    th = ej.th
    if kind == "PT":
        return th.pt(P, x)
    if kind == "PQ":
        return th.pq(P, x)
    return th.ph(P, x)


def run_case(args):
    """One case -> dict of predictions (NaN on failure, with the reason)."""
    case, params = args
    ej = Ejector(_thermo(case["fluid"]), **params)
    try:
        m0, s0 = _state(ej, case["m0"]), _state(ej, case["s0"])
        if case["kind"] == "rating":
            r = ej.rate(m0, s0, case["A_t"], case["A_e"], case["A_mix"], case["P_out"], n_scan=25)
            return dict(m_m=r.m_m, m_s=r.m_s, regime=r.regime, ER_c=r.ER_critical,
                        P_b=r.P_b_critical)
        r = ej.rate(m0, s0, case["A_t"], case["A_e"], case["A_mix"], P_out=s0.P, n_scan=25)
        return dict(m_m=r.m_m, ER=r.ER_critical, P_b=r.P_b_critical, regime="critical")
    except (RuntimeError, ValueError) as e:
        return dict(m_m=math.nan, m_s=math.nan, ER=math.nan, P_b=math.nan, regime="fail",
                    error=str(e)[:120])


_POOL = None


def evaluate(cases, params, workers=None):
    global _POOL
    if _POOL is None:
        _POOL = ProcessPoolExecutor(max_workers=workers or max(1, (os.cpu_count() or 2) - 2))
    return list(_POOL.map(run_case, [(c, params) for c in cases], chunksize=1))


def motive_flow(cases, params):
    """Choked motive flow Cd*G*A_t (stage 1), evaluated serially (cheap)."""
    out = []
    for c in cases:
        ej = Ejector(_thermo(c["fluid"]), **params)
        m0 = _state(ej, c["m0"])
        s0P = c["s0"][1]
        _, _, G = ej._motive(m0, max(ej.th.p_triple * 1.001, 0.2 * s0P))
        out.append(G * c["A_t"])
    return np.array(out)


def errors(cases, preds):
    """Relative errors [%]: rating -> m_m, m_s; critical -> ER*, P_b* (and lift error)."""
    e = {}
    if cases[0]["kind"] == "rating":
        e["m_m"] = np.array([(p["m_m"] / c["m_m"] - 1) * 100 for c, p in zip(cases, preds)])
        e["m_s"] = np.array([(p["m_s"] / c["m_s"] - 1) * 100 if c["m_s"] > 0 else math.nan
                             for c, p in zip(cases, preds)])
        e["m_s_scaled"] = np.array([(p["m_s"] - c["m_s"]) / np.mean([d["m_s"] for d in cases]) * 100
                                    for c, p in zip(cases, preds)])
    else:
        e["ER"] = np.array([(p["ER"] / c["ER"] - 1) * 100 for c, p in zip(cases, preds)])
        e["P_b"] = np.array([(p["P_b"] / c["P_b"] - 1) * 100 for c, p in zip(cases, preds)])
        e["lift"] = np.array([((p["P_b"] - c["s0"][1]) / (c["P_b"] - c["s0"][1]) - 1) * 100
                              for c, p in zip(cases, preds)])
    return e


def fit_cd(cases, params):
    mm = np.array([c["m_m"] for c in cases])
    g1 = motive_flow(cases, dict(params, Cd_m=1.0))
    cd = float(np.sum(g1 * mm) / np.sum(g1 * g1))
    return cd, (cd * g1 / mm - 1) * 100


def fit(cases, params, names=("eta_s", "eta_mix", "eta_d"), lo=0.3, hi=1.0):
    """Least squares on suction flow (rating, scaled by the mean) or on ER* and the
    critical lift (critical sets).  Returns (params, standard errors)."""
    rating = cases[0]["kind"] == "rating"
    if rating:
        scale = np.mean([c["m_s"] for c in cases])
    else:
        er_scale = np.mean([c["ER"] for c in cases])
        lift_scale = np.mean([c["P_b"] - c["s0"][1] for c in cases])

    def resid(x):
        p = dict(params, **dict(zip(names, x)))
        pr = evaluate(cases, p)
        if rating:
            return np.array([np.nan_to_num((q["m_s"] - c["m_s"]) / scale, nan=2.0)
                             for c, q in zip(cases, pr)])
        return np.r_[[np.nan_to_num((q["ER"] - c["ER"]) / er_scale, nan=2.0) for c, q in zip(cases, pr)],
                     [np.nan_to_num((q["P_b"] - c["P_b"]) / lift_scale, nan=2.0)
                      for c, q in zip(cases, pr)]]

    r = least_squares(resid, [params[n] for n in names], bounds=([lo] * len(names), [hi] * len(names)),
                      diff_step=0.01, xtol=1e-4, ftol=1e-4)
    dof = max(len(r.fun) - len(names), 1)
    se = np.sqrt(np.diag(np.linalg.pinv(r.jac.T @ r.jac) * (2 * r.cost / dof)))
    return dict(params, **dict(zip(names, r.x))), dict(zip(names, se))


def summary(cases, preds):
    e = errors(cases, preds)
    nfail = sum(p["regime"] == "fail" for p in preds)
    if "m_s" in e:
        return (f"m_m MAE {np.nanmean(np.abs(e['m_m'])):5.1f}%  m_s MAE {np.nanmean(np.abs(e['m_s'])):6.1f}% "
                f"(scaled {np.nanmean(np.abs(e['m_s_scaled'])):5.1f}%, bias {np.nanmean(e['m_s_scaled']):+5.1f}%)"
                f"  fails {nfail}")
    return (f"ER* MAE {np.nanmean(np.abs(e['ER'])):5.1f}% (bias {np.nanmean(e['ER']):+5.1f}%)  "
            f"P_b* MAE {np.nanmean(np.abs(e['P_b'])):4.1f}%  lift MAE {np.nanmean(np.abs(e['lift'])):5.1f}% "
            f"(bias {np.nanmean(e['lift']):+5.1f}%)  fails {nfail}")


if __name__ == "__main__":
    import time
    base = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80, Cd_m=1.0)   # Screening set
    for name, loader in SETS.items():
        cases = loader()
        for mixing in ("CAM", "CPM"):
            t = time.time()
            pr = evaluate(cases, dict(base, mixing=mixing))
            print(f"{name:16s} n={len(cases):3d} {mixing} Screening: {summary(cases, pr)}  "
                  f"[{time.time() - t:.0f} s]")
