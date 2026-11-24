import math

import numpy as np
import pytest
from fluids.jet_pump import liquid_jet_pump

from ejector1d import Ejector, Expansion, SolidPhaseRisk, Thermo, area
import validate_huang1999


@pytest.mark.parametrize("P5", [2.05e5, 2.15e5, 2.25e5, 2.35e5])
def test_incompressible_limit_matches_fluids(P5):
    """Ideal (loss-free) water jet pump reproduces fluids.liquid_jet_pump (K=0)."""
    th = Thermo("Water")
    ej = Ejector(th, eta_m=1, eta_s=1, eta_mix=1, eta_d=1)
    m0, s0 = ej.inlet(5e5, T=293.15), ej.inlet(1e5, T=293.15)
    Dn, Dm = 0.02, 0.045
    r = ej.rate(m0, s0, area(Dn), area(Dn), area(Dm), P_out=P5)
    assert r.regime == "subcritical"
    f = liquid_jet_pump(rhop=m0.rho, rhos=s0.rho, Kp=0, Ks=0, Km=0, Kd=0, d_nozzle=Dn,
                        d_mixing=Dm, P1=5e5, P2=1e5, Qs=r.m_s / s0.rho, nozzle_retracted=False)
    assert f["P5"] == pytest.approx(P5, rel=2e-4)
    assert f["Qp"] == pytest.approx(r.m_m / m0.rho, rel=5e-4)


def test_liquid_suction_limited_by_cavitation():
    th = Thermo("Water")
    ej = Ejector(th, eta_m=1, eta_s=1, eta_mix=1, eta_d=1)
    m0, s0 = ej.inlet(5e5, T=293.15), ej.inlet(1e5, T=293.15)
    r = ej.rate(m0, s0, area(0.02), area(0.02), area(0.045), P_out=1.2e5)
    assert r.regime == "critical"
    assert r.P_y == pytest.approx(2339, rel=0.02)   # P_sat(20 C)


def test_ideal_gas_nozzle_critical_pressure_ratio():
    """Isentropic nitrogen nozzle: P*/P0 = (2/(k+1))^(k/(k-1)) = 0.528."""
    th = Thermo("Nitrogen")
    st = th.pt(10e5, 300.0)
    P_t, G = Expansion(th, st, 1.0).critical(1e5)
    assert P_t / st.P == pytest.approx(0.5283, abs=2e-3)


def test_huang1999_r141b_regression():
    """Predictive accuracy on 39 critical-mode points (literature efficiencies)."""
    err_er, err_td = validate_huang1999.run(verbose=False)
    assert np.abs(err_td).mean() < 1.2          # K
    assert np.abs(err_er).mean() < 18.0         # %


def test_co2_design_conservation_and_solid_margin():
    th = Thermo("CO2")
    ej = Ejector(th, eta_m=0.9, eta_s=0.85, eta_mix=0.9, eta_d=0.8)
    m0, s0 = ej.inlet(47e5, q=0.0), ej.inlet(15e5, q=1.0)
    r = ej.design(m0, s0, 1.0, 0.2)
    h_out = (m0.h + 0.2 * s0.h) / 1.2
    assert r.out.h == pytest.approx(h_out, rel=1e-9)              # energy balance
    assert s0.P < r.P_out < m0.P
    assert r.solid_margin > 2.0
    # design geometry rated at its design back pressure returns the design ER
    rr = ej.rate(m0, s0, r.A_t, r.A_e, r.A_mix, r.P_out)
    assert rr.ER == pytest.approx(0.2, rel=0.02)


def test_triple_point_guard():
    th = Thermo("CO2")
    with pytest.raises(SolidPhaseRisk):
        th.ph(4e5, -2e5)


def test_goncalves2024_motive_flow():
    """Subcooled liquid CO2 motive (34.8 bar), (p, rho) inlet state.

    Isentropic converging section, Cd = 1: HEM over-predicts the motive flow by
    3-7 % (before the throat was made loss-free it was within +/-2 %).
    """
    import validate_goncalves2024
    errs = [e for e, _, _ in validate_goncalves2024.run(eta_m=0.9)]
    assert all(0.0 < e < 7.5 for e in errs)


def test_mixture_composition_bookkeeping():
    th = Thermo("CO2&Nitrogen")
    ej = Ejector(th)
    m0 = ej.inlet(47e5, T=278.15, z=(0.995, 0.005))
    s0 = ej.inlet(15e5, T=253.15, z=(0.95, 0.05))
    z = ej._mixed_z(m0, s0, 1.0, 0.2)
    n_m, n_s = 1.0 / th.molar_mass(m0.z), 0.2 / th.molar_mass(s0.z)
    n_n2 = n_m * 0.005 + n_s * 0.05
    assert z[1] == pytest.approx(n_n2 / (n_m + n_s), rel=1e-12)
    assert sum(z) == pytest.approx(1.0)


def test_giacomelli2019_motive_cd():
    """Motive C_d fitted on 42 R744 points: ~1.14 (isentropic HEM throat under-predicts), MAE < 3 %."""
    import calibrate_r744 as c
    pts = c.load_points()
    assert len(pts) == 42
    cd, err = c.fit_cd(pts, dict(eta_m=0.90))
    assert 1.10 < cd < 1.18
    assert np.abs(err).mean() < 3.0


def test_critical_er_independent_of_scan_resolution():
    """Critical regime: the feasibility boundary is bisected, so ER must not depend on n_scan."""
    import calibrate_r744 as c
    p = [q for q in c.load_points() if q["case"] == 1][0]
    ej = Ejector(Thermo("CO2"), eta_m=0.90, Cd_m=1.138, eta_s=0.61, eta_mix=0.825, eta_d=0.82)
    m0, s0 = ej.inlet(p["PM"], T=p["TM"]), ej.inlet(p["PS"], T=p["TS"])
    g = c.GEOM
    ers = [ej.rate(m0, s0, g["A_t"], g["A_e"], g["A_mix"], p["PO"], n_scan=n) for n in (10, 25, 80)]
    assert all(r.regime == "critical" for r in ers)
    assert max(r.ER for r in ers) - min(r.ER for r in ers) < 1e-4


def test_motive_throat_isentropic_eta_beyond():
    """G* is taken on the isentrope (independent of eta_m); eta_m acts only below P_t."""
    th = Thermo("Nitrogen")
    st = th.pt(10e5, 300.0)
    ej_a = Ejector(th, eta_m=0.80)
    ej_b = Ejector(th, eta_m=1.00)
    ea, Pta, Ga = ej_a._motive(st, 1e5)
    eb, Ptb, Gb = ej_b._motive(st, 1e5)
    assert Pta == pytest.approx(Ptb, rel=1e-6) and Ga == pytest.approx(Gb, rel=1e-9)
    # continuous at the throat, lossy (slower) beyond it, identical above it
    assert ea.at(Pta)[1] == pytest.approx(eb.at(Ptb)[1], rel=1e-9)
    assert ea.at(0.9 * Pta)[1] < eb.at(0.9 * Ptb)[1]
    assert ea.at(1.2 * Pta)[1] == pytest.approx(eb.at(1.2 * Ptb)[1], rel=1e-12)


def _case1():
    import calibrate_r744 as c
    p = [q for q in c.load_points() if q["case"] == 1][0]
    ej = Ejector(Thermo("CO2"), eta_m=0.90, Cd_m=1.138, eta_s=0.61, eta_mix=0.825, eta_d=0.82)
    return ej, ej.inlet(p["PM"], T=p["TM"]), ej.inlet(p["PS"], T=p["TS"]), c.GEOM


@pytest.mark.parametrize("P_out", [35e5, 44e5, 47e5])
def test_rate_flows_inverts_rate(P_out):
    """Both flows from a forward rating -> rate_flows returns the same P_out and A_t."""
    ej, m0, s0, g = _case1()
    f = ej.rate(m0, s0, g["A_t"], g["A_e"], g["A_mix"], P_out)
    r = ej.rate_flows(m0, s0, f.m_m, f.m_s, g["A_e"] / g["A_t"], g["A_mix"] / g["A_t"], A_t=g["A_t"])
    assert r.regime == f.regime
    assert r.A_t == pytest.approx(g["A_t"], rel=1e-9)
    assert abs(r.motive_mismatch) < 1e-9
    if f.regime == "subcritical":
        assert r.P_out == pytest.approx(P_out, rel=1e-6)
    else:                                    # critical: the highest back pressure on the plateau
        assert r.ER == pytest.approx(f.ER, rel=1e-9)
        assert r.P_out == pytest.approx(r.P_b_critical, rel=1e-12) and r.P_out > P_out


def test_rate_flows_excess_and_zero_entrainment():
    from ejector1d import EntrainmentExceeded
    ej, m0, s0, g = _case1()
    re_, rm = g["A_e"] / g["A_t"], g["A_mix"] / g["A_t"]
    r = ej.rate_flows(m0, s0, 0.04, 0.0, re_, rm, n_curve=12)
    assert r.ER == 0.0 and r.P_out > r.P_b_critical
    assert len(r.curve) >= 10 and r.curve[0][1] == pytest.approx(r.ER_critical, rel=1e-9)
    with pytest.raises(EntrainmentExceeded):
        ej.rate_flows(m0, s0, 0.04, 0.04 * r.ER_critical * 1.01, re_, rm)


def test_rate_flows_unchoked_jet_pump():
    """Unchoked (liquid) motive: throat sized at the solution P_y; inverse of rate()."""
    th = Thermo("Water")
    ej = Ejector(th, eta_m=1, eta_s=1, eta_mix=1, eta_d=1)
    m0, s0 = ej.inlet(5e5, T=293.15), ej.inlet(1e5, T=293.15)
    Dn, Dm = 0.02, 0.045
    f = ej.rate(m0, s0, area(Dn), area(Dn), area(Dm), P_out=2.15e5)
    r = ej.rate_flows(m0, s0, f.m_m, f.m_s, 1.0, (Dm / Dn) ** 2, A_t=area(Dn))
    assert r.P_out == pytest.approx(2.15e5, rel=1e-6)
    assert abs(r.motive_mismatch) < 1e-6


# ---------------------------------------------------------------- adversarial-review regressions
def _n2_cd():
    """N2 C-D nozzle, 10 -> 6 bar, A_e/A_t 1.5, A_mix/A_t 6 (review F1)."""
    th = Thermo("Nitrogen")
    ej = Ejector(th, eta_m=0.9, eta_s=0.85, eta_mix=0.9, eta_d=0.8)
    m0, s0 = ej.inlet(10e5, T=300.0), ej.inlet(6e5, T=300.0)
    A_t = area(0.01)
    return ej, m0, s0, A_t, 1.5 * A_t, 6.0 * A_t


def test_cd_nozzle_motive_state_continuous():
    """F1: motive flow and exit state are continuous at the throat pressure and at P_fc."""
    ej, m0, s0, A_t, A_e, A_mix = _n2_cd()
    P_low = max(ej.th.p_triple * 1.001, 0.01 * s0.P)
    mexp, P_t, G_t = ej._motive(m0, P_low)
    cache = {}
    ex = ej._cd_exit(mexp, P_t, G_t, A_e / A_t, P_low, cache)
    assert ex["P_sup"] < P_t < ex["P_se"] < ex["P_fc"] < m0.P
    for P in (P_t, ex["P_fc"]):
        a = ej._motive_state(mexp, P_t, G_t, A_t, A_e, P * (1 - 1e-7), P_low, cache)
        b = ej._motive_state(mexp, P_t, G_t, A_t, A_e, P * (1 + 1e-7), P_low, cache)
        assert a[0] == pytest.approx(b[0], rel=1e-5)               # motive flow
    a = ej._motive_state(mexp, P_t, G_t, A_t, A_e, ex["P_fc"] * (1 - 1e-7), P_low, cache)
    b = ej._motive_state(mexp, P_t, G_t, A_t, A_e, ex["P_fc"] * (1 + 1e-7), P_low, cache)
    assert a[4] == pytest.approx(b[4], rel=1e-3)                   # jet velocity at P_fc
    # exit mass flux equals the motive flow in every regime (continuity at the exit)
    for P in np.linspace(ex["P_sup"] * 1.01, ex["P_fc"] * 1.05, 9):
        m, _, _, st, u = ej._motive_state(mexp, P_t, G_t, A_t, A_e, P, P_low, cache)
        assert st.rho * u * A_e == pytest.approx(m, rel=1e-5)


def test_rate_never_returns_an_unspecified_outlet_pressure():
    """F1: a specified P_out is met (or the call fails loudly), never silently moved."""
    ej, m0, s0, A_t, A_e, A_mix = _n2_cd()
    for P_out in np.linspace(5.8e5, 6.6e5, 9):
        try:
            r = ej.rate(m0, s0, A_t, A_e, A_mix, P_out)
        except RuntimeError as e:
            assert "discontinuity" in str(e)
            continue
        assert r.P_out == pytest.approx(P_out, rel=1e-4)


def test_exit_area_below_cd_raises_instead_of_clamping():
    """F2: 1 < A_e/A_t < Cd cannot pass the corrected flow: explicit error, no clamp."""
    ej, m0, s0, g = _case1()
    with pytest.raises(ValueError, match="below the discharge coefficient"):
        ej.rate(m0, s0, g["A_t"], 1.05 * g["A_t"], g["A_mix"], 40e5)
    r = ej.rate(m0, s0, g["A_t"], g["A_t"], g["A_mix"], 40e5)      # converging nozzle is fine
    assert r.P_e == pytest.approx(r.P_t)


def test_design_for_lift_uses_the_maximum_over_er():
    """F3: at small ER the feasible design window is a narrow band near P_s0."""
    ej = Ejector(Thermo("CO2"), eta_m=0.90, Cd_m=1.138, eta_s=0.61, eta_mix=0.825, eta_d=0.82)
    m0, s0 = ej.inlet(84.5e5, T=305.15), ej.inlet(15e5, q=1.0)
    d = ej.design(m0, s0, 13.66, 0.005)
    assert d.P_out > 32e5
    r = ej.design_for_lift(m0, s0, 13.66, 32e5)
    assert r.P_out == pytest.approx(32e5, rel=1e-3) and r.ER > 0.005


def test_critical_and_backflow_results_are_consistent():
    """F4: outlet state and eta_ej at the specified P_out; backflow outlet = motive stream."""
    ej, m0, s0, g = _case1()
    c = ej.rate(m0, s0, g["A_t"], g["A_e"], g["A_mix"], 31e5)
    assert c.regime == "critical" and c.out.P == pytest.approx(31e5)
    assert c.P_b_critical > 40e5 and c.eta_ejector < 0.05
    b = ej.rate(m0, s0, g["A_t"], g["A_e"], g["A_mix"], 52e5)
    assert b.regime == "backflow" and b.out.h == pytest.approx(m0.h) and b.eta_ejector == 0.0


def test_solution_on_the_triple_point_floor_raises():
    """F5: a design whose suction expansion is truncated at the triple point is not valid."""
    from ejector1d import SolidPhaseRisk
    th = Thermo("CO2")
    ej = Ejector(th, eta_m=0.9, eta_s=0.85, eta_mix=0.9, eta_d=0.8)
    m0 = ej.inlet(47e5, q=0.0)
    with pytest.raises(SolidPhaseRisk):
        ej.design(m0, ej.inlet(5.3e5, q=1.0), 1.0, 0.2)
