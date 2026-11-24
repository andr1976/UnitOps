"""Water/steam + dry air thermodynamics for validating steam-jet and liquid-jet ejectors.

The ejector model only needs ph / ps / pt / pq flashes, a molar mass and a triple-point
pressure (see ``ejector1d.Thermo``).  CoolProp's HEOS mixtures do not flash water with a
non-condensable gas reliably, so this class uses the psychrometric model instead:

* water from IAPWS-95 (CoolProp "Water"), air from CoolProp's pseudo-pure "Air";
* the gas phase is an ideal mixture (Dalton): each gas at its partial pressure, so the
  entropy of mixing is included and rho_gas = rho_w(T, p_w) + rho_a(T, p_a);
* condensed water is pure liquid at (T, P), no dissolved air; water condenses when its
  partial pressure would exceed p_sat(T), and the gas then holds y_w = p_sat(T) / P;
* below the triple-point temperature condensed water would be ice: SolidPhaseRisk.

Pure water (z = (1, 0)) and pure air (z = (0, 1)) are delegated to ``ejector1d.Thermo``,
so a saturated-steam motive and an air suction behave exactly as in the pure-fluid model.
Valid where the gas phase is close to ideal (pressures up to a few bar in the mixture).
Composition z = (water, air) mole fractions.
"""
import math

import CoolProp.CoolProp as CP
from scipy.optimize import brentq

from ejector1d import State, SolidPhaseRisk, Thermo

T_TRIPLE, P_TRIPLE = 273.16, 611.657


def p_sublimation(T):
    """IAPWS R14-08 sublimation pressure of ice Ih [Pa], 50 K < T <= 273.16 K."""
    th = T / T_TRIPLE
    a = (-0.212144006e2, 0.273203819e2, -0.610598130e1)
    b = (0.333333333e-2, 0.120666667e1, 0.170333333e1)
    return P_TRIPLE * math.exp(sum(ai * th ** bi for ai, bi in zip(a, b)) / th)


class WaterAirThermo:
    components = ["Water", "Air"]
    is_mixture = True
    fluid = "Water&Air (psychrometric)"

    def __init__(self, T_min=180.0, T_max=1000.0):
        self.water = Thermo("Water")
        self.air = Thermo("Air", solid_check=False)
        self.W = CP.AbstractState("HEOS", "Water")
        self.A = CP.AbstractState("HEOS", "Air")
        self.Mw = self.W.molar_mass()
        self.Ma = self.A.molar_mass()
        self.p_triple, self.T_triple = P_TRIPLE, T_TRIPLE   # water: sets the search floor
        self.T_min, self.T_max = T_min, T_max
        self.n_flash = 0

    # ---- helpers
    @staticmethod
    def _z(z):
        if z is None:
            raise ValueError("water/air flash needs a composition z = (x_water, x_air)")
        zw, za = float(z[0]), float(z[1])
        s = zw + za
        return (zw / s, za / s)

    def molar_mass(self, z=None) -> float:
        zw, za = self._z(z)
        return zw * self.Mw + za * self.Ma

    def _pure(self, z):
        zw, za = z
        if za == 0.0:
            return self.water
        if zw == 0.0:
            return self.air
        return None

    def _gas(self, AS, T, p):
        AS.specify_phase(CP.iphase_gas)
        try:
            AS.update(CP.PT_INPUTS, p, T)
            return AS.hmass(), AS.smass(), AS.rhomass()
        finally:
            AS.unspecify_phase()

    def _props(self, T, P, z):
        """Mixture state at (T, P): (h, s, rho, q_mass_gas, rho_liq, rho_gas, ice)."""
        zw, za = z
        if T >= T_TRIPLE:
            self.W.update(CP.QT_INPUTS, 0.0, min(T, 647.0))
            psat = self.W.p() if T < 647.0 else math.inf
        else:
            psat = p_sublimation(T)
        pw_free = zw * P
        condensed = pw_free > psat
        if condensed and T < T_TRIPLE:
            return None                      # condensate would be ice
        if condensed:
            yw = psat / P
            beta = za / (1.0 - yw)          # moles of gas per mole of mixture
        else:
            yw, beta = zw, 1.0
        n_wg, n_l = yw * beta, zw - yw * beta
        p_w, p_a = yw * P, (1.0 - yw) * P
        m_wg, m_a, m_l = n_wg * self.Mw, za * self.Ma, max(n_l, 0.0) * self.Mw
        M = zw * self.Mw + za * self.Ma
        h = s = 0.0
        rho_g = 0.0
        if m_wg > 0:
            if condensed:
                self.W.update(CP.QT_INPUTS, 1.0, T)
                hw, sw, rw = self.W.hmass(), self.W.smass(), self.W.rhomass()
            elif T >= T_TRIPLE:
                hw, sw, rw = self._gas(self.W, T, p_w)
            else:
                # vapour below IAPWS-95's T_min: ideal gas from 273.16 K at the same p_w
                h0, s0, _ = self._gas(self.W, T_TRIPLE, p_w)
                R = 8.314462618 / self.Mw
                cp = 1859.0                  # ideal-gas cp of steam near 0 C [J/kg K]
                hw = h0 - cp * (T_TRIPLE - T)
                sw = s0 - cp * math.log(T_TRIPLE / T)
                rw = p_w / (R * T)
            h += m_wg * hw
            s += m_wg * sw
            rho_g += rw
        ha, sa, ra = self._gas(self.A, T, p_a)
        h += m_a * ha
        s += m_a * sa
        rho_g += ra
        rho_l = math.nan
        vol = (m_wg + m_a) / rho_g
        if m_l > 0:
            self.W.specify_phase(CP.iphase_liquid)
            try:
                self.W.update(CP.PT_INPUTS, max(P, psat * 1.000001), T)
                hl, sl, rho_l = self.W.hmass(), self.W.smass(), self.W.rhomass()
            finally:
                self.W.unspecify_phase()
            h += m_l * hl
            s += m_l * sl
            vol += m_l / rho_l
        q = (m_wg + m_a) / M if m_l > 0 else 2.0
        return h / M, s / M, M / vol, q, rho_l, rho_g

    def _state(self, P, T, z, pr):
        h, s, rho, q, rho_l, rho_g = pr
        st = State(P=P, h=h, T=T, s=s, rho=rho, q=q, z=z)
        if 0.0 < q < 1.0:
            st.rho_l, st.rho_v = rho_l, rho_g
        return st

    def _solve(self, P, z, key, target):
        k = 0 if key == "h" else 1
        BIG = 1e12

        def f(T):
            pr = self._props(T, P, z)
            if pr is None:
                return -BIG + T              # ice region: below any physical target
            return pr[k] - target

        lo, hi = self.T_min, self.T_max
        flo, fhi = f(lo), f(hi)
        if fhi < 0:
            raise ValueError(f"water/air flash: {key} = {target:.6g} above T_max")
        if flo > 0:
            raise ValueError(f"water/air flash: {key} = {target:.6g} below T_min")
        T = brentq(f, lo, hi, xtol=1e-10, rtol=1e-15, maxiter=200)
        pr = self._props(T, P, z)
        if pr is None:
            raise SolidPhaseRisk(f"water/air at P = {P:.1f} Pa: condensate below 273.16 K (ice)")
        return self._state(P, T, z, pr)

    # ---- flash interface used by the ejector model
    def ph(self, P, h, z=None) -> State:
        z = self._z(z)
        pure = self._pure(z)
        if pure is not None:
            st = pure.ph(P, h)
            st.z = z
            return st
        self.n_flash += 1
        return self._solve(P, z, "h", h)

    def ps(self, P, s, z=None) -> State:
        z = self._z(z)
        pure = self._pure(z)
        if pure is not None:
            st = pure.ps(P, s)
            st.z = z
            return st
        self.n_flash += 1
        return self._solve(P, z, "s", s)

    def pt(self, P, T, z=None) -> State:
        z = self._z(z)
        pure = self._pure(z)
        if pure is not None:
            st = pure.pt(P, T)
            st.z = z
            return st
        self.n_flash += 1
        pr = self._props(T, P, z)
        if pr is None:
            raise SolidPhaseRisk(f"water/air at {T:.2f} K: condensate below 273.16 K (ice)")
        return self._state(P, T, z, pr)

    def pq(self, P, q, z=None) -> State:
        """Pure fluids: saturation; mixture: dew point (q = 1) only."""
        z = self._z(z)
        pure = self._pure(z)
        if pure is not None:
            st = pure.pq(P, q)
            st.z = z
            return st
        if q != 1.0:
            raise ValueError("water/air: only the dew point (q = 1) is defined")
        self.n_flash += 1
        pw = z[0] * P
        if pw < P_TRIPLE:
            raise SolidPhaseRisk("water/air dew point below the triple point (frost point)")
        T = brentq(lambda T: self._psat(T) - pw, T_TRIPLE, 647.0, xtol=1e-10)
        return self._state(P, T, z, self._props(T, P, z))

    def _psat(self, T):
        self.W.update(CP.QT_INPUTS, 0.0, T)
        return self.W.p()
