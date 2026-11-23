"""Dump every 'this work' number used in the ejector technical reference.

Runs the reference implementation (ejector/prototype/ejector1d.py) and writes
CSVs into figures/data/.  The make_*.py plotting scripts and the tables in the
.rst chapters read only these CSVs; no model result is hand-entered.

Usage (from this directory):
    python gen_data.py                 # everything (~5 min, calibration dominates)
    python gen_data.py nozzle fluids   # selected sections
Sections: nozzle fluids huang goncalves giacomelli offdesign duty
'offdesign' and 'duty' read the fitted efficiencies written by 'giacomelli'.
"""
import csv
import os
import sys
import time

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
PROTO = os.path.normpath(os.path.join(HERE, "..", "..", "prototype"))
sys.path.insert(0, PROTO)

import CoolProp.CoolProp as CP                        # noqa: E402
from ejector1d import Ejector, Expansion, Thermo, area  # noqa: E402

DATA = os.path.join(HERE, "data")


def write(name, header, rows):
    path = os.path.join(DATA, name)
    with open(path, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(header)
        for r in rows:
            w.writerow([f"{v:.6g}" if isinstance(v, float) else v for v in r])
    print(f"  wrote data/{name} ({len(rows)} rows)")


def read(name):
    with open(os.path.join(DATA, name)) as f:
        return list(csv.DictReader(f))


# ----------------------------------------------------------------------------- nozzle
def nozzle():
    """Mass flux along the expansion path: max(rho*u) = choking (Ch. nozzles)."""
    rows = []
    # (a) ideal-gas check: isentropic nitrogen, analytic curve alongside
    th = Thermo("Nitrogen")
    st = th.pt(10e5, 300.0)
    ex = Expansion(th, st, 1.0)
    k = 1.4
    R = 8.314462618 / 0.0280134
    for r in np.linspace(0.15, 0.999, 120):
        P = r * st.P
        G = ex.flux(P)
        G_ig = st.P / np.sqrt(R * 300.0) * np.sqrt(2 * k / (k - 1) * (r ** (2 / k) - r ** ((k + 1) / k)))
        rows.append(("N2_10bar_300K", P / 1e5, r, G, G_ig))
    Pt, Gt = ex.critical(1e5)
    print(f"  N2 critical ratio {Pt/st.P:.4f} (theory {(2/(k+1))**(k/(k-1)):.4f})")
    # (b) flashing CO2 motive: saturated liquid 47 bar (HEM, HNE phi=0.5) and dense 84.5 bar/32 C
    th = Thermo("CO2")
    cases = [("CO2_47bar_satliq_HEM", th.pq(47e5, 0.0), 1.0),
             ("CO2_47bar_satliq_HNE0.5", th.pq(47e5, 0.0), 0.5),
             ("CO2_84.5bar_32C_HEM", th.pt(84.5e5, 305.15), 1.0)]
    crit = []
    for name, s0, phi in cases:
        ex = Expansion(th, s0, 0.9, phi=phi)
        for P in np.linspace(8e5, s0.P * 0.999, 160):
            rows.append((name, P / 1e5, P / s0.P, ex.flux(P), float("nan")))
        Pt, Gt = ex.critical(6e5)
        crit.append((name, s0.P / 1e5, s0.T - 273.15, Pt / 1e5, Gt))
    write("nozzle_flux.csv", ["case", "P_bar", "P_over_P0", "G", "G_ideal_gas"], rows)
    write("nozzle_critical.csv", ["case", "P0_bar", "T0_C", "Pt_bar", "Gstar"], crit)


# ----------------------------------------------------------------------------- fluids
def fluids_limit():
    """Loss-free water jet pump vs fluids.liquid_jet_pump (incompressible, K = 0)."""
    from fluids.jet_pump import liquid_jet_pump
    th = Thermo("Water")
    ej = Ejector(th, eta_m=1, eta_s=1, eta_mix=1, eta_d=1)
    m0, s0 = ej.inlet(5e5, T=293.15), ej.inlet(1e5, T=293.15)
    Dn, Dm = 0.02, 0.045
    rows = []
    for P5 in np.linspace(1.6e5, 2.5e5, 19):
        r = ej.rate(m0, s0, area(Dn), area(Dn), area(Dm), P_out=P5)
        if r.regime != "subcritical":
            rows.append((P5 / 1e5, r.regime, r.m_m, r.m_s, float("nan"), float("nan")))
            continue
        f = liquid_jet_pump(rhop=m0.rho, rhos=s0.rho, Kp=0, Ks=0, Km=0, Kd=0, d_nozzle=Dn,
                            d_mixing=Dm, P1=5e5, P2=1e5, Qs=r.m_s / s0.rho, nozzle_retracted=False)
        rows.append((P5 / 1e5, r.regime, r.m_m, r.m_s, f["P5"] / 1e5, f["Qp"] * m0.rho))
    write("fluids_limit.csv", ["P5_bar", "regime", "m_m", "m_s", "P5_fluids_bar", "m_m_fluids"], rows)


# ----------------------------------------------------------------------------- huang
def huang():
    import validate_huang1999 as vh
    pts = np.loadtxt(vh.DATA.strip().splitlines())
    th = Thermo("R141b")
    rows = []
    for label, eff in (("Ma2017", dict(eta_m=0.95, eta_s=0.85, eta_mix=0.95, eta_d=0.85)),
                       ("eta_s0.7", dict(eta_m=0.95, eta_s=0.70, eta_mix=0.95, eta_d=0.85))):
        ej = Ejector(th, **eff)
        for Dt, De, D3, Pp, Ps, er_x, Td_x in pts:
            m0, s0 = ej.inlet(Pp * 1e5, q=1.0), ej.inlet(Ps * 1e5, q=1.0)
            r = ej.rate(m0, s0, area(Dt * 1e-3), area(De * 1e-3), area(D3 * 1e-3), P_out=s0.P)
            Td = CP.PropsSI("T", "P", r.out.P, "Q", 1, "R141b") - 273.15
            rows.append((label, Dt, De, D3, Pp, Ps, er_x, r.ER, Td_x, Td, r.m_m * 1e3))
    write("huang1999.csv", ["set", "Dt_mm", "De_mm", "D3_mm", "Pp_bar", "Ps_bar", "ER_exp", "ER_sim",
                            "Td_exp_C", "Td_sim_C", "m_m_sim_gs"], rows)


# ----------------------------------------------------------------------------- goncalves
def goncalves():
    import validate_goncalves2024 as vg
    th = Thermo("CO2")
    rows = []
    for inlet in ("p_rho", "p_T"):
        ej = Ejector(th, eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80)
        for k, (Pm, Tm, rhom, mm, Ps, Ts, ms, Po) in enumerate(vg.RUNS, 1):
            Tin = CP.PropsSI("T", "P", Pm * 1e3, "D", rhom, "CO2") - 273.15 if inlet == "p_rho" else Tm
            m0, s0 = ej.inlet(Pm * 1e3, T=Tin + 273.15), ej.inlet(Ps * 1e3, T=Ts + 273.15)
            r = ej.rate(m0, s0, area(vg.D_T), area(vg.D_E), area(vg.D_MIX), P_out=Po * 1e3)
            rows.append((inlet, k, Pm / 100, Tm, Tin, mm, r.m_m * 1e3, ms / mm, r.ER, r.regime,
                         r.P_t / 1e5))
    write("goncalves2024.csv", ["inlet", "run", "Pm_bar", "Tm_meas_C", "Tm_used_C", "mm_exp_gs",
                                "mm_sim_gs", "ER_exp", "ER_sim", "regime", "Pt_bar"], rows)


# ----------------------------------------------------------------------------- giacomelli
def giacomelli():
    import calibrate_r744 as c
    pts = c.load_points()
    eta0 = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80, Cd_m=1.0)
    t = time.time()
    sim0 = c.simulate(pts, eta0)
    cd, _ = c.fit_cd(pts, eta0)
    eta1 = dict(eta0, Cd_m=cd)
    sup = [p for p in pts if p["inlet"] == "supercritical"]
    sub = [p for p in pts if p["inlet"] != "supercritical"]
    fits, corr, se = {}, {}, {}
    for k, sel in (("all", pts), ("supercritical", sup), ("sub_subcooled", sub)):
        fits[k], corr[k], se[k] = c.fit_suction(sel, eta1, full=True)
    print(f"  fits done in {time.time()-t:.0f} s")
    write("giacomelli_fit.csv", ["fit_set", "n", "eta_m", "Cd_m", "eta_s", "eta_mix", "eta_d",
                                 "se_eta_s", "se_eta_mix", "se_eta_d",
                                 "corr_s_mix", "corr_s_d", "corr_mix_d"],
          [(k, {"all": len(pts), "supercritical": len(sup), "sub_subcooled": len(sub)}[k],
            e["eta_m"], e["Cd_m"], e["eta_s"], e["eta_mix"], e["eta_d"], *se[k],
            corr[k][0, 1], corr[k][0, 2], corr[k][1, 2]) for k, e in fits.items()])
    sim1 = c.simulate(pts, fits["all"])
    # cross-check: each regime predicted by the set fitted on the other one
    simx = {p["case"]: s for p, s in zip(sub, c.simulate(sub, fits["supercritical"]))}
    simx.update({p["case"]: s for p, s in zip(sup, c.simulate(sup, fits["sub_subcooled"]))})
    rows = []
    for p, s0, s1 in zip(pts, sim0, sim1):
        sx = simx[p["case"]]
        rows.append((p["case"], p["inlet"], p["PM"] / 1e5, p["TM"] - 273.15, p["PS"] / 1e5,
                     p["PO"] / 1e5, p["mm"] * 1e3, s0[0] * 1e3, s1[0] * 1e3, p["ms"] * 1e3,
                     s0[1] * 1e3, s1[1] * 1e3, sx[1] * 1e3, p["ER"], s1[1] / s1[0], s1[2]))
    write("giacomelli_sim.csv", ["case", "inlet", "PM_bar", "TM_C", "PS_bar", "PO_bar", "mm_exp_gs",
                                 "mm_uncal_gs", "mm_cal_gs", "ms_exp_gs", "ms_uncal_gs", "ms_cal_gs",
                                 "ms_cross_gs", "ER_exp", "ER_cal", "regime_cal"], rows)


def fitted_sets():
    return {r["fit_set"]: dict(eta_m=float(r["eta_m"]), Cd_m=float(r["Cd_m"]),
                               eta_s=float(r["eta_s"]), eta_mix=float(r["eta_mix"]),
                               eta_d=float(r["eta_d"])) for r in read("giacomelli_fit.csv")}


# ----------------------------------------------------------------------------- offdesign
def offdesign():
    """Characteristic of the Giacomelli geometry at the case-1 inlet states."""
    import calibrate_r744 as c
    p = [q for q in c.load_points() if q["case"] == 1][0]
    th = Thermo("CO2")
    ej = Ejector(th, **fitted_sets()["all"])
    m0, s0 = ej.inlet(p["PM"], T=p["TM"]), ej.inlet(p["PS"], T=p["TS"])
    rows = []
    for Po in np.arange(31.0, 54.01, 0.25):
        r = ej.rate(m0, s0, c.GEOM["A_t"], c.GEOM["A_e"], c.GEOM["A_mix"], Po * 1e5, n_scan=40)
        rows.append((Po, r.regime, r.m_m * 1e3, r.m_s * 1e3, r.ER, r.P_y / 1e5))
    write("offdesign_case1.csv", ["PO_bar", "regime", "mm_gs", "ms_gs", "ER", "Py_bar"], rows)


# ----------------------------------------------------------------------------- duty
def duty():
    """Worked example: 13.66 kg/s CO2 motive, 2 kg/s sat. vapour at 15 bar, 28 bar wanted."""
    th = Thermo("CO2")
    M_M, ER0, P_REQ = 13.66, 2.0 / 13.66, 28e5
    typical = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80)
    fits = fitted_sets()
    motives = {"47bar_satliq": lambda e: e.inlet(47e5, q=0.0),
               "84.5bar_32C": lambda e: e.inlet(84.5e5, T=305.15)}
    curves = [("47bar_satliq", "typical", typical), ("84.5bar_32C", "typical", typical)]
    curves += [("84.5bar_32C", f"fit_{k}", v) for k, v in fits.items()]
    s0 = Thermo("CO2").pq(15e5, 1.0)
    rows, pts = [], []
    for mot, lab, eff in curves:
        ej = Ejector(th, **eff)
        m0 = motives[mot](ej)
        for ER in np.r_[np.linspace(0.02, 0.5, 17), ER0]:
            r = ej.design(m0, s0, M_M, float(ER))
            rows.append((mot, lab, ER, r.P_out / 1e5, r.out.q, r.eta_ejector, r.solid_margin,
                         2e3 * np.sqrt(r.A_t / np.pi), 2e3 * np.sqrt(r.A_mix / np.pi)))
        r = ej.design(m0, s0, M_M, ER0)
        try:
            rl = ej.design_for_lift(m0, s0, M_M, P_REQ)
            ms28 = rl.m_s
            mm28 = M_M * ER0 / rl.ER          # motive needed for 2 kg/s at 28 bar (ER fixed)
        except RuntimeError:
            ms28, mm28 = 0.0, float("inf")
        pts.append((mot, lab, r.P_out / 1e5, r.ER, ms28, mm28, r.out.q * (M_M + 2.0),
                    2e3 * np.sqrt(r.A_t / np.pi), 2e3 * np.sqrt(r.A_mix / np.pi), r.eta_ejector,
                    r.solid_margin, m0.T - 273.15))
        print(f"  {mot:13s} {lab:18s} P_out(ER0)={r.P_out/1e5:6.2f} bar  m_s@28={ms28:5.2f}  "
              f"m_m for 2 kg/s@28={mm28:6.1f}")
    write("duty_curves.csv", ["motive", "eff_set", "ER", "Pout_bar", "x_out", "eta_ej", "solid_margin",
                              "Dt_mm", "Dmix_mm"], rows)
    write("duty_point.csv", ["motive", "eff_set", "Pout_bar_at_ER0", "ER0", "ms_at_28bar",
                             "mm_needed_2kgs_28bar", "vapour_out_kgs", "Dt_mm", "Dmix_mm", "eta_ej",
                             "solid_margin", "Tm_C"], pts)


SECTIONS = dict(nozzle=nozzle, fluids=fluids_limit, huang=huang, goncalves=goncalves,
                giacomelli=giacomelli, offdesign=offdesign, duty=duty)

if __name__ == "__main__":
    for name in (sys.argv[1:] or list(SECTIONS)):
        t = time.time()
        print(f"[{name}]")
        SECTIONS[name]()
        print(f"  ({time.time()-t:.1f} s)")
