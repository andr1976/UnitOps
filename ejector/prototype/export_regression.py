"""Export reference results of the Python model for the C# port (EjectorCore).

Runs a fixed set of cases with ejector1d.py (CoolProp HEOS) and writes them,
inputs and outputs, to ejector/src/EjectorCore.Tests/regression_python.json.
EjectorCore.Tests re-runs every case through the C# engine with the same
CoolProp build (P/Invoke) and compares.  Re-run this script whenever the Python
model changes:

    python export_regression.py
"""
import json
import math
import os

from ejector1d import Ejector, Thermo, area
import calibrate_r744 as c

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "src", "EjectorCore.Tests", "regression_python.json"))

CAL = dict(eta_m=0.90, Cd_m=1.138, eta_s=0.61, eta_mix=0.825, eta_d=0.82)
TYP = dict(eta_m=0.90, eta_s=0.85, eta_mix=0.90, eta_d=0.80)


def res(r):
    f = lambda v: None if v is None or (isinstance(v, float) and not math.isfinite(v)) else v
    return dict(regime=r.regime, m_m=r.m_m, m_s=r.m_s, ER=r.ER, P_out=r.P_out, P_t=f(r.P_t),
                P_e=f(r.P_e), P_y=f(r.P_y), P_3=f(r.P_3), A_t=f(r.A_t), A_e=f(r.A_e),
                A_mix=f(r.A_mix), u_e=f(r.u_e), u_y=f(r.u_y), u_3=f(r.u_3), h_out=r.out.h,
                eta_ejector=f(r.eta_ejector), solid_margin=f(r.solid_margin),
                ER_critical=f(r.ER_critical), P_b_critical=f(r.P_b_critical),
                motive_mismatch=f(r.motive_mismatch), curve=[list(p) for p in r.curve])


def inlet(kind, fluid, P, X, z=None):
    return dict(fluid=fluid, P=P, kind=kind, X=X, z=z)


def make_inlet(ej, d):
    return ej.inlet(d["P"], T=d["X"], z=d["z"]) if d["kind"] == "PT" else ej.inlet(d["P"], q=d["X"], z=d["z"])


def main():
    cases = []
    p1 = [q for q in c.load_points() if q["case"] == 1][0]
    g = c.GEOM
    m1, s1 = inlet("PT", "CO2", p1["PM"], p1["TM"]), inlet("PT", "CO2", p1["PS"], p1["TS"])
    for Po in (35e5, 44e5, 47e5):
        cases.append(dict(name=f"giacomelli1_rate_{Po/1e5:.0f}bar", op="rate", eff=CAL, motive=m1,
                          suction=s1, A_t=g["A_t"], A_e=g["A_e"], A_mix=g["A_mix"], P_out=Po))
    cases.append(dict(name="giacomelli1_rate_backflow", op="rate", eff=CAL, motive=m1, suction=s1,
                      A_t=g["A_t"], A_e=g["A_e"], A_mix=g["A_mix"], P_out=52e5))
    # inverse rating at the forward flows of the 44 bar case (fixed size) and zero entrainment
    cases.append(dict(name="giacomelli1_rate_flows_44bar", op="rate_flows", eff=CAL, motive=m1,
                      suction=s1, m_m=None, m_s=None, from_case="giacomelli1_rate_44bar",
                      r_e=g["A_e"] / g["A_t"], r_mix=g["A_mix"] / g["A_t"], A_t=g["A_t"], n_curve=0))
    cases.append(dict(name="giacomelli1_rate_flows_zero", op="rate_flows", eff=CAL, motive=m1,
                      suction=s1, m_m=0.04, m_s=0.0, r_e=g["A_e"] / g["A_t"],
                      r_mix=g["A_mix"] / g["A_t"], A_t=None, n_curve=12))
    # optimum designs: user's duty, liquid and dense motive, HEM and HNE
    s15 = inlet("PQ", "CO2", 15e5, 1.0)
    cases.append(dict(name="duty_47bar_design", op="design", eff=TYP, motive=inlet("PQ", "CO2", 47e5, 0.0),
                      suction=s15, m_m=13.66, ER=2.0 / 13.66))
    cases.append(dict(name="duty_47bar_design_hne", op="design",
                      eff=dict(TYP, motive_model="HNE", phi_ne=0.5),
                      motive=inlet("PQ", "CO2", 47e5, 0.0), suction=s15, m_m=13.66, ER=0.2))
    cases.append(dict(name="duty_84bar_design_cal", op="design", eff=CAL,
                      motive=inlet("PT", "CO2", 84.5e5, 305.15), suction=s15, m_m=13.66, ER=2.0 / 13.66))
    cases.append(dict(name="duty_84bar_design_for_lift", op="design_for_lift", eff=CAL,
                      motive=inlet("PT", "CO2", 84.5e5, 305.15), suction=s15, m_m=13.66, P_out=25e5))
    # water jet pump: unchoked motive, subcritical and cavitation-limited (critical)
    w_m, w_s = inlet("PT", "Water", 5e5, 293.15), inlet("PT", "Water", 1e5, 293.15)
    ideal = dict(eta_m=1, eta_s=1, eta_mix=1, eta_d=1)
    for Po in (2.15e5, 1.2e5):
        cases.append(dict(name=f"water_jet_pump_{Po/1e5:.2f}bar", op="rate", eff=ideal, motive=w_m,
                          suction=w_s, A_t=area(0.02), A_e=area(0.02), A_mix=area(0.045), P_out=Po))
    # R141b gas ejector (Huang 1999 geometry AA, first row of validate_huang1999.DATA)
    import numpy as np
    import validate_huang1999 as vh
    Dt, De, D3, Pp, Ps, _, _ = np.loadtxt(vh.DATA.strip().splitlines())[0]
    cases.append(dict(name="huang_r141b_rate", op="rate",
                      eff=dict(eta_m=0.95, eta_s=0.85, eta_mix=0.95, eta_d=0.85),
                      motive=inlet("PQ", "R141b", Pp * 1e5, 1.0), suction=inlet("PQ", "R141b", Ps * 1e5, 1.0),
                      A_t=area(Dt * 1e-3), A_e=area(De * 1e-3), A_mix=area(D3 * 1e-3), P_out=Ps * 1e5))

    # paths found by the adversarial review: converging-diverging nozzle regimes (incl. the
    # normal shock inside the nozzle), exit area below Cd, small-ER design, design for lift
    # above the ER -> 0 limit, solution on the triple-point floor
    n2m, n2s = inlet("PT", "Nitrogen", 10e5, 300.0), inlet("PT", "Nitrogen", 6e5, 300.0)
    n2eff = dict(eta_m=0.9, eta_s=0.85, eta_mix=0.9, eta_d=0.8)
    At = area(0.01)
    for Po in (5.9e5, 6.2e5, 6.4e5, 6.6e5):
        cases.append(dict(name=f"n2_cd_rate_{Po/1e5:.1f}bar", op="rate", eff=n2eff, motive=n2m, suction=n2s,
                          A_t=At, A_e=1.5 * At, A_mix=6.0 * At, P_out=Po))
    cases.append(dict(name="n2_cd_rate_flows", op="rate_flows", eff=n2eff, motive=n2m, suction=n2s,
                      m_m=None, m_s=None, from_case="n2_cd_rate_6.6bar", r_e=1.5, r_mix=6.0, A_t=At,
                      n_curve=8))
    cases.append(dict(name="giacomelli1_exit_below_cd", op="rate", eff=CAL, motive=m1, suction=s1,
                      A_t=g["A_t"], A_e=1.05 * g["A_t"], A_mix=g["A_mix"], P_out=40e5))
    m84 = inlet("PT", "CO2", 84.5e5, 305.15)
    cases.append(dict(name="duty_84bar_design_small_er", op="design", eff=CAL, motive=m84, suction=s15,
                      m_m=13.66, ER=0.005))
    cases.append(dict(name="duty_84bar_lift_32bar", op="design_for_lift", eff=CAL, motive=m84, suction=s15,
                      m_m=13.66, P_out=32e5))
    cases.append(dict(name="co2_triple_point_floor", op="design", eff=TYP, motive=inlet("PQ", "CO2", 47e5, 0.0),
                      suction=inlet("PQ", "CO2", 5.3e5, 1.0), m_m=1.0, ER=0.2))

    # constant-pressure mixing (CPM): Huang's coefficients on Huang's geometry (critical and
    # subcritical), C-D nozzle regimes (core carried along the motive path), design, inverse
    # rating with curve, design for lift on the duty
    cpm_h = dict(eta_m=1.0, Cd_m=math.sqrt(0.95), eta_s=0.85, eta_mix=0.82, eta_d=1.0, mixing="CPM",
                 phi_p=0.88)
    for Po in (Ps * 1e5, 0.95e5, 1.05e5):
        cases.append(dict(name=f"cpm_huang_rate_{Po/1e5:.2f}bar", op="rate", eff=cpm_h,
                          motive=inlet("PQ", "R141b", Pp * 1e5, 1.0),
                          suction=inlet("PQ", "R141b", Ps * 1e5, 1.0),
                          A_t=area(Dt * 1e-3), A_e=area(De * 1e-3), A_mix=area(D3 * 1e-3), P_out=Po))
    cpm_n2 = dict(n2eff, mixing="CPM")
    for Po in (5.9e5, 6.4e5, 6.6e5):
        cases.append(dict(name=f"cpm_n2_cd_rate_{Po/1e5:.1f}bar", op="rate", eff=cpm_n2, motive=n2m,
                          suction=n2s, A_t=At, A_e=1.5 * At, A_mix=6.0 * At, P_out=Po))
    n2lo_m, n2lo_s = inlet("PT", "Nitrogen", 6e5, 300.0), inlet("PT", "Nitrogen", 1e5, 300.0)
    cpm_g = dict(eta_m=0.95, eta_s=0.85, eta_mix=0.95, eta_d=0.85, mixing="CPM", phi_p=0.88)
    cases.append(dict(name="cpm_n2_design", op="design", eff=cpm_g, motive=n2lo_m, suction=n2lo_s,
                      m_m=1.0, ER=0.5))
    cases.append(dict(name="cpm_n2_rate_flows", op="rate_flows", eff=cpm_g, motive=n2lo_m, suction=n2lo_s,
                      m_m=1.0, m_s=0.3, r_e=None, r_mix=None, from_design="cpm_n2_design", A_t=None,
                      n_curve=10))
    cases.append(dict(name="cpm_duty_84bar_design_for_lift", op="design_for_lift",
                      eff=dict(CAL, mixing="CPM"), motive=m84, suction=s15, m_m=13.66, P_out=25e5))

    done = {}
    for cs in cases:
        fluid = cs["motive"]["fluid"]
        eff = dict(cs["eff"])
        ej = Ejector(Thermo(fluid), **eff)
        m0, s0 = make_inlet(ej, cs["motive"]), make_inlet(ej, cs["suction"])
        cs["inlet_states"] = dict(m0=dict(P=m0.P, h=m0.h, s=m0.s, T=m0.T), s0=dict(P=s0.P, h=s0.h, s=s0.s, T=s0.T))
        try:
            if cs["op"] == "rate":
                r = ej.rate(m0, s0, cs["A_t"], cs["A_e"], cs["A_mix"], cs["P_out"])
            elif cs["op"] == "rate_flows":
                if cs.get("from_case"):
                    ref = done[cs["from_case"]]
                    cs["m_m"], cs["m_s"] = ref["m_m"], ref["m_s"]
                if cs.get("from_design"):                 # shape of a designed geometry
                    ref = done[cs["from_design"]]
                    cs["r_e"], cs["r_mix"] = ref["A_e"] / ref["A_t"], ref["A_mix"] / ref["A_t"]
                r = ej.rate_flows(m0, s0, cs["m_m"], cs["m_s"], cs["r_e"], cs["r_mix"], A_t=cs["A_t"],
                                  n_curve=cs["n_curve"])
            elif cs["op"] == "design":
                r = ej.design(m0, s0, cs["m_m"], cs["ER"])
            else:
                r = ej.design_for_lift(m0, s0, cs["m_m"], cs["P_out"])
        except Exception as e:                        # expected failures are part of the reference
            kind = ("solid" if type(e).__name__ == "SolidPhaseRisk" else
                    "discontinuity" if "discontinuity" in str(e) else
                    "exit_below_cd" if "below the discharge coefficient" in str(e) else type(e).__name__)
            cs["result"] = dict(error=kind, message=str(e))
            done[cs["name"]] = cs["result"]
            print(f"{cs['name']:32s} ERROR {kind}: {str(e)[:70]}")
            continue
        cs["result"] = res(r)
        done[cs["name"]] = cs["result"]
        print(f"{cs['name']:32s} {r.regime:11s} P_out={r.P_out/1e5:9.5f} bar  ER={r.ER:.6f}  "
              f"m_m={r.m_m:.6g}")
    with open(OUT, "w") as f:
        json.dump(dict(generator="ejector/prototype/export_regression.py", coolprop=__import__("CoolProp").__version__,
                       cases=cases), f, indent=1)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
