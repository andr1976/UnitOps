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
    """Subcooled liquid CO2 motive (34.8 bar): HEM choked flow within 5 % of data."""
    import validate_goncalves2024
    for err_m, _, _ in validate_goncalves2024.run(eta_m=0.9):
        assert abs(err_m) < 5.0


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
    """Motive C_d fitted on 42 R744 points: ~1.2 (HEM under-predicts), MAE < 3 %."""
    import calibrate_r744 as c
    pts = c.load_points()
    assert len(pts) == 42
    cd, err = c.fit_cd(pts, dict(eta_m=0.90))
    assert 1.15 < cd < 1.25
    assert np.abs(err).mean() < 3.0
