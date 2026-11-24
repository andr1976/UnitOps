"""
ejector1d -- fast 0D/1D real-fluid ejector (jet pump / eductor) model.

One model covers gas-gas, liquid-liquid, liquid-motive/gas-suction and flashing
two-phase ejectors, because every stream is described only by (P, h) and all
properties come from a homogeneous-equilibrium (HEM) flash.  The thermodynamics
are reached through four flashes (`Thermo.ph`, `Thermo.ps`, `Thermo.pt`,
`Thermo.pq`) plus the molar mass, so the CAPE-OPEN version can swap CoolProp
for PME flashes.

Model (classical constant-area-mixing 1D ejector, cf. Keenan et al. 1950,
Huang et al. 1999, Liu & Groll 2013, Banasiak & Hafner 2011, Ringstad et al. 2020):

  stations   m0 motive inlet (stagnation)   t motive throat   e motive exit
             s0 suction inlet (stagnation)  y suction at mixing-section inlet
             3  end of constant-area mixing section            out diffuser outlet

  1. Motive nozzle: isentropic to the throat, isentropic efficiency eta_m on
     the expansion beyond it.  Choked mass flux G* = max_P rho*u on the
     isentrope (HEM critical flow), times a discharge coefficient Cd_m.
     Optional homogeneous non-equilibrium (HNE) delayed-flashing factor.
  2. Suction: expansion s0 -> y with eta_s, capped at its own choked flux.
  3. Constant-area mixing (area A_mix): mass, momentum (mixing efficiency
     eta_mix on the inlet momentum flux) and energy -> subsonic outlet state 3.
  4. Diffuser: isentropic efficiency eta_d, outlet velocity -> 0.

Two modes:
  rate()   geometry known (A_t, A_e, A_mix) + inlet states + outlet pressure
           -> motive flow, suction flow, entrainment ratio, operating regime.
  design() inlet states + motive flow + ER -> best achievable outlet pressure
           and the geometry that delivers it (non-dimensional area ratios too).

SI units throughout (Pa, K, J/kg, kg/s, m, m2).
"""
from __future__ import annotations

import math
from dataclasses import dataclass, field

import numpy as np
from scipy.optimize import brentq, minimize_scalar

import CoolProp.CoolProp as CP


class SolidPhaseRisk(RuntimeError):
    """A state falls below the triple-point pressure (e.g. dry ice for CO2)."""


class EntrainmentExceeded(RuntimeError):
    """rate_flows(): the given suction flow exceeds what the geometry can entrain."""

    def __init__(self, msg, ER_critical, P_b_critical):
        super().__init__(msg)
        self.ER_critical, self.P_b_critical = ER_critical, P_b_critical


@dataclass
class State:
    P: float
    h: float
    T: float = float("nan")
    s: float = float("nan")
    rho: float = float("nan")
    q: float = float("nan")      # vapour MASS fraction (HEM); -1 liquid, 2 vapour/supercritical
    z: tuple | None = None       # overall mole fractions (None = pure fluid)
    rho_l: float = float("nan")  # phase densities when two-phase
    rho_v: float = float("nan")


# --------------------------------------------------------------------------- thermo
class Thermo:
    """Minimal flash interface the ejector model needs (CoolProp backend).

    ph / ps / pt / pq flashes at a given overall composition z, plus the
    molar mass.  The CAPE-OPEN adapter implements the same calls with the PME
    property package (PH, PS, TP, PVF flashes on a scratch material object),
    which is where general mixtures and solid phases belong.

    fluid: CoolProp name, e.g. "CO2", or a mixture "CO2&Nitrogen" (then pass z).
    """

    def __init__(self, fluid: str = "CO2", backend: str = "HEOS",
                 solid_check: bool = True):
        self.AS = CP.AbstractState(backend, fluid)
        self.fluid = fluid
        self.components = fluid.split("&")
        self.is_mixture = len(self.components) > 1
        self.solid_check = solid_check and not self.is_mixture
        self._z = None
        if self.is_mixture:
            # no single triple point: freeze-out needs a solid-fluid model (PME)
            self.p_triple, self.T_triple = 0.0, 0.0
        else:
            self.p_triple = self.AS.trivial_keyed_output(CP.iP_triple)
            self.T_triple = self.AS.trivial_keyed_output(CP.iT_triple)
        self.n_flash = 0

    def _set(self, z):
        if not self.is_mixture:
            return None
        if z is None:
            raise ValueError("mixture flash needs a composition z")
        z = tuple(float(v) for v in z)
        if z != self._z:
            self.AS.set_mole_fractions(list(z))
            self._z = z
        return z

    def molar_mass(self, z=None) -> float:
        self._set(z)
        return self.AS.molar_mass()

    def _guard(self, P):
        if self.solid_check and P < self.p_triple:
            raise SolidPhaseRisk(
                f"P = {P/1e5:.3f} bar below triple point {self.p_triple/1e5:.3f} bar "
                f"of {self.fluid}: solid formation (dry ice) possible; HEM/EOS invalid")

    def _out(self, P, h, z):
        a = self.AS
        st = State(P=P, h=h, T=a.T(), s=a.smass(), rho=a.rhomass(), z=z)
        Qm = a.Q()                                   # molar for mixtures
        if 0.0 < Qm < 1.0:
            st.rho_l = a.saturated_liquid_keyed_output(CP.iDmass)
            st.rho_v = a.saturated_vapor_keyed_output(CP.iDmass)
            v, vl, vv = 1 / st.rho, 1 / st.rho_l, 1 / st.rho_v
            st.q = (v - vl) / (vv - vl)              # mass vapour fraction
        elif Qm == 0.0 or Qm == 1.0:
            st.q = float(Qm)
        else:
            st.q = -1.0 if a.phase() == CP.iphase_liquid else 2.0
        return st

    def ph(self, P, h, z=None) -> State:
        self._guard(P)
        z = self._set(z)
        self.n_flash += 1
        self.AS.update(CP.HmassP_INPUTS, h, P)
        return self._out(P, h, z)

    def ps(self, P, s, z=None) -> State:
        self._guard(P)
        z = self._set(z)
        self.n_flash += 1
        self.AS.update(CP.PSmass_INPUTS, P, s)
        return self._out(P, self.AS.hmass(), z)

    def pt(self, P, T, z=None) -> State:
        self._guard(P)
        z = self._set(z)
        self.n_flash += 1
        self.AS.update(CP.PT_INPUTS, P, T)
        return self._out(P, self.AS.hmass(), z)

    def pq(self, P, q, z=None) -> State:
        """Bubble (q=0) / dew (q=1) point at P (molar vapour fraction)."""
        self._guard(P)
        z = self._set(z)
        self.n_flash += 1
        self.AS.update(CP.PQ_INPUTS, P, q)
        st = self._out(P, self.AS.hmass(), z)
        if q in (0.0, 1.0):          # exact end points (molar = mass there)
            st.q = float(q)
        return st


# --------------------------------------------------------------------------- nozzle
@dataclass
class Expansion:
    """Adiabatic expansion from a stagnation state with isentropic efficiency."""
    th: Thermo
    inlet: State
    eta: float = 1.0
    phi: float = 1.0   # vapour-generation fraction: 1 = HEM, <1 delayed flashing (HNE)
    P_loss: float = math.inf   # eta acts only below this pressure (isentropic above it)
    _h_loss: float = field(default=math.nan, init=False, repr=False)

    def at(self, P) -> tuple[State, float]:
        """Static state and velocity after expanding to P.

        Energy follows the (equilibrium) isentrope with efficiency eta.  With
        P_loss set (the motive throat pressure) the path is isentropic down to
        P_loss and eta acts only on the enthalpy drop below it:
        h = h_s(P_loss) - eta * (h_s(P_loss) - h_s(P)).  With phi < 1 the
        density of a flashing mixture uses the non-equilibrium quality
        x = phi * x_eq (homogeneous non-equilibrium, cf. the boiling delay
        factor N of Henry & Fauske 1971 / Diener & Schmidt 2004): the
        metastable liquid carries less vapour, so the critical flux rises.
        """
        i = self.inlet
        if P >= i.P:
            return i, 0.0
        h_is = self.th.ps(P, i.s, i.z).h
        if P >= self.P_loss:
            h = h_is
        else:
            if math.isinf(self.P_loss):
                h_ref = i.h
            else:
                if math.isnan(self._h_loss):
                    self._h_loss = self.th.ps(self.P_loss, i.s, i.z).h
                h_ref = self._h_loss
            h = h_ref - self.eta * (h_ref - h_is)
        st = self.th.ph(P, h, i.z)
        if self.phi < 1.0 and 0.0 < st.q < 1.0:
            x = self.phi * st.q
            st.rho = 1.0 / (x / st.rho_v + (1.0 - x) / st.rho_l)
        return st, math.sqrt(max(2.0 * (i.h - h), 0.0))

    def flux(self, P) -> float:
        st, u = self.at(P)
        return st.rho * u

    def _safe_flux(self, P) -> float:
        """Flux, or NaN where the flash fails (e.g. mixture flash far off-design)."""
        try:
            return self.flux(P)
        except (ValueError, SolidPhaseRisk):
            return float("nan")

    def critical(self, P_min, n=60) -> tuple[float, float]:
        """(P*, G*) maximising the mass flux between P_min and the inlet pressure.

        A log-spaced scan finds the bracket (the HEM flux has a kink at the
        saturation pressure for subcooled liquid), then a bounded Brent refine.
        If G keeps rising down to P_min the flow is not choked there and
        (P_min, G(P_min)) is returned.
        """
        P0 = self.inlet.P
        Ps = np.geomspace(P_min, P0 * 0.9999, n)
        G = np.array([self._safe_flux(p) for p in Ps])
        if not np.isfinite(G).any():
            raise RuntimeError("no converged flash along the expansion path")
        k = int(np.nanargmax(G))
        if k == 0:
            return Ps[0], G[0]
        lo, hi = Ps[k - 1], Ps[min(k + 1, n - 1)]
        r = minimize_scalar(lambda p: -np.nan_to_num(self._safe_flux(p)), bounds=(lo, hi),
                            method="bounded", options={"xatol": 1e-6 * P0})
        return (r.x, -r.fun) if -r.fun > G[k] else (Ps[k], G[k])


# --------------------------------------------------------------------------- results
@dataclass
class Result:
    regime: str                     # 'critical', 'subcritical', 'backflow', 'design'
    m_m: float
    m_s: float
    ER: float
    P_out: float
    P_t: float = float("nan")
    P_e: float = float("nan")
    P_y: float = float("nan")
    P_3: float = float("nan")
    A_t: float = float("nan")
    A_e: float = float("nan")
    A_mix: float = float("nan")
    u_e: float = float("nan")
    u_y: float = float("nan")
    u_3: float = float("nan")
    out: State | None = None
    eta_ejector: float = float("nan")   # Koehler/Elbel expansion-work recovery efficiency
    states: dict = field(default_factory=dict)
    solid_margin: float = float("inf")  # min static pressure / triple-point pressure
    # rate_flows() only
    ER_critical: float = float("nan")      # most-entraining feasible ER of the sized geometry
    P_b_critical: float = float("nan")     # outlet pressure at ER_critical (critical back pressure)
    motive_capacity: float = float("nan")  # Cd*G*A_t of a fixed-size throat [kg/s]
    motive_mismatch: float = float("nan")  # motive_capacity / m_m - 1
    curve: list = field(default_factory=list)  # rating characteristic [(P_out, ER), ...]

    def check_solid(self, p_triple: float):
        """Solid margin; raises SolidPhaseRisk if the solution sits on the search floor
        (1.001 x the triple-point pressure): every search stops there, so such a
        result is an expansion truncated at the triple point, not a valid state."""
        Pmin = min(self.P_e, self.P_y, self.P_3)
        self.solid_margin = Pmin / p_triple if p_triple > 0 else float("inf")
        if p_triple > 0 and Pmin <= 1.0011 * p_triple:
            raise SolidPhaseRisk(
                f"the solution reaches the triple-point pressure (lowest static pressure "
                f"{Pmin/1e5:.3f} bar, triple point {p_triple/1e5:.3f} bar): solid formation "
                f"would occur; the equation of state has no solid phase")
        return self

    def summary(self) -> str:
        d = lambda A: math.sqrt(4 * A / math.pi) * 1e3
        return (f"[{self.regime}] m_m={self.m_m:.4g} kg/s  m_s={self.m_s:.4g} kg/s  "
                f"ER={self.ER:.4f}  P_out={self.P_out/1e5:.3f} bar  "
                f"P_t={self.P_t/1e5:.2f}  P_e={self.P_e/1e5:.2f}  P_y={self.P_y/1e5:.2f}  "
                f"P_3={self.P_3/1e5:.2f} bar | D_t={d(self.A_t):.3f}  D_e={d(self.A_e):.3f}  "
                f"D_mix={d(self.A_mix):.3f} mm  A_mix/A_t={self.A_mix/self.A_t:.2f}  "
                f"eta_ej={self.eta_ejector:.3f}  P_min/P_triple={self.solid_margin:.2f}")


# --------------------------------------------------------------------------- ejector
@dataclass
class Ejector:
    th: Thermo
    eta_m: float = 0.90      # motive nozzle isentropic efficiency
    eta_s: float = 0.85      # suction nozzle isentropic efficiency
    eta_mix: float = 0.90    # momentum (mixing) efficiency
    eta_d: float = 0.80      # diffuser isentropic efficiency
    motive_model: str = "HEM"   # 'HEM' | 'HNE' (delayed flashing in motive nozzle)
    phi_ne: float = 1.0         # 'HNE' vapour-generation fraction (1 = HEM)
    Cd_m: float = 1.0           # motive discharge coefficient on G*
    mixing: str = "CAM"         # 'CAM' constant-area | 'CPM' constant-pressure (Huang 1999)
    phi_p: float = 1.0          # CPM: primary-core area coefficient at the hypothetical throat

    def __post_init__(self):
        if self.mixing not in ("CAM", "CPM"):
            raise ValueError(f"mixing must be 'CAM' or 'CPM', not {self.mixing!r}")

    # ---- inlet helpers
    def inlet(self, P, T=None, q=None, z=None) -> State:
        """Stagnation inlet from (P, T) or (P, molar vapour fraction q)."""
        return self.th.pq(P, q, z) if q is not None else self.th.pt(P, T, z)

    def _motive(self, m0: State, P_low: float):
        """Motive nozzle: isentropic to the throat, eta_m on the expansion beyond it.

        The choked flux G* is the maximum of rho*u on the isentrope; losses in
        the converging section are left to Cd_m.  Below the throat pressure the
        path carries eta_m (diverging section).  An unchoked nozzle (no maximum
        above P_low) is therefore loss-free apart from Cd_m.
        Returns (exit-path expansion, throat pressure, Cd_m * G*).
        """
        phi = 1.0 if self.motive_model == "HEM" else self.phi_ne
        P_t, G = Expansion(self.th, m0, 1.0, phi=phi).critical(P_low)
        exp = Expansion(self.th, m0, self.eta_m, phi=phi, P_loss=P_t)
        return exp, P_t, G * self.Cd_m

    # ---- constant-area mixing
    def _mix(self, A_mix, streams, m_t, h0_t, z_t, eta=None):
        """streams: list of (pressure*area, momentum flux).  Returns (P3, u3, st3).

        Mass, momentum and energy over the constant-area section give one
        residual in P3.  The subsonic (post-shock) root is the highest-pressure
        one, so march down from the u3 -> 0 end until the residual turns positive.
        If the residual starts to fall without turning positive, its maximum is
        refined between the grid points before the section is declared choked
        (a coarse march step would otherwise miss a narrow positive peak).
        eta overrides eta_mix (eta=1 gives a loss-free normal shock).
        """
        eta = self.eta_mix if eta is None else eta
        F_in = sum(pa for pa, _ in streams) + eta * sum(mu for _, mu in streams)

        def resid(P3):
            u3 = (F_in - P3 * A_mix) / m_t
            if u3 <= 0:
                return -1.0, u3, None
            h3 = h0_t - 0.5 * u3 * u3
            st = self.th.ph(P3, h3, z_t)
            return st.rho * u3 * A_mix / m_t - 1.0, u3, st

        def root(lo, hi):
            P3 = brentq(lambda x: resid(x)[0], lo, hi, xtol=1e-6 * lo)
            _, u3, st = resid(P3)
            return P3, u3, st

        P_hi = F_in / A_mix * (1 - 1e-9)          # u3 -> 0: residual = -1
        P_lo = max(self.th.p_triple * 1.0001, 1e3)
        p_prev2, p_prev, r_prev, p = P_hi, P_hi, -1.0, P_hi
        while p > P_lo:
            p = max(p * 0.96, P_lo)
            try:
                r = resid(p)[0]
            except (ValueError, SolidPhaseRisk):
                return None
            if r > 0:
                return root(p, p_prev)
            if r < r_prev and r_prev > -0.999:   # past the residual maximum on the grid
                def neg(x):
                    try:
                        return -resid(x)[0]
                    except (ValueError, SolidPhaseRisk):
                        return 1.0
                m = minimize_scalar(neg, bounds=(p, p_prev2), method="bounded",
                                    options={"xatol": 1e-7 * p})
                if -m.fun > 0:                    # narrow positive peak between grid points
                    return root(m.x, p_prev2)
                return None
            p_prev2, p_prev, r_prev = p_prev, p, r
        return None   # mixing section cannot pass the flow (choked / infeasible)

    # ---- diffuser
    def _diffuser(self, st3: State, u3: float, h0: float) -> State:
        """Outlet stagnation state at velocity ~0."""
        h_is_target = st3.h + self.eta_d * 0.5 * u3 * u3
        f = lambda P: self.th.ps(P, st3.s, st3.z).h - h_is_target
        P_hi = st3.P * 1.5
        while f(P_hi) < 0:
            P_hi *= 1.5
        P_out = brentq(f, st3.P, P_hi, xtol=1e-7 * st3.P)
        return self.th.ph(P_out, h0, st3.z)

    def _eta_ejector(self, m0, s0, out, ER):
        """Expansion-work recovery efficiency (Koehler et al. / Elbel & Hrnjak)."""
        try:
            w_rec = self.th.ps(out.P, s0.s, s0.z).h - s0.h
            w_avail = m0.h - self.th.ps(out.P, m0.s, m0.z).h
            return ER * w_rec / w_avail
        except (ValueError, SolidPhaseRisk):
            return float("nan")

    def _mixed_z(self, m0, s0, m_m, m_s):
        """Overall mole fractions of the mixed stream (None for a pure fluid)."""
        if not self.th.is_mixture:
            return None
        n_m = m_m / self.th.molar_mass(m0.z)
        n_s = m_s / self.th.molar_mass(s0.z)
        return tuple((n_m * a + n_s * b) / (n_m + n_s) for a, b in zip(m0.z, s0.z))

    # ---- one pass for a given suction static pressure P_y and geometry
    def _pass(self, m0, s0, m_m, P_e, st_e, u_e, A_e, A_mix, P_y, sexp, G_s_max, mexp=None):
        if self.mixing == "CPM":
            return self._pass_cpm(m0, s0, m_m, P_e, st_e, u_e, A_mix, P_y, sexp, G_s_max, mexp)
        A_sy = A_mix - A_e
        if A_sy <= 0:
            raise ValueError("nozzle exit area >= mixing area")
        st_y, u_y = sexp.at(P_y)
        G_s = min(st_y.rho * u_y, G_s_max)
        m_s = G_s * A_sy
        m_t = m_m + m_s
        h0_t = (m_m * m0.h + m_s * s0.h) / m_t
        z_t = self._mixed_z(m0, s0, m_m, m_s)
        mix = self._mix(A_mix, [(P_e * A_e, m_m * u_e), (P_y * A_sy, m_s * u_y)],
                        m_t, h0_t, z_t)
        if mix is None:
            return None
        P3, u3, st3 = mix
        out = self._diffuser(st3, u3, h0_t)
        return dict(m_s=m_s, st_y=st_y, u_y=u_y, P3=P3, u3=u3, st3=st3, out=out)

    def _pass_cpm(self, m0, s0, m_m, P_e, st_e, u_e, A_mix, P_y, sexp, G_s_max, mexp):
        """Constant-pressure mixing (Huang et al. 1999, real fluid) at mixing pressure P_y.

        Hypothetical throat y-y inside the constant-area section A_mix: the
        motive core, carried isentropically (motive path) from the nozzle exit
        to P_y, takes phi_p * m_m / (rho u) of the section; the suction stream,
        expanded to P_y, passes the rest.  The streams then mix at constant
        pressure P_y, with eta_mix on the momentum flux, and the mixed stream
        passes a loss-free normal shock (real fluid; none if it is subsonic)
        before the diffuser.  Continuity of the mixed stream in A_mix is not
        imposed (as in Huang's model); P_out does not depend on A_mix.
        """
        if abs(P_e - P_y) <= 1e-9 * P_y or mexp is None:
            st_p, u_p = st_e, u_e
        else:
            st_p, u_p = mexp.at(P_y)
        A_sy = A_mix - self.phi_p * m_m / (st_p.rho * u_p)
        if A_sy <= 0.0:
            return None                      # the motive core fills the section
        st_y, u_y = sexp.at(P_y)
        G_s = min(st_y.rho * u_y, G_s_max)
        m_s = G_s * A_sy
        m_t = m_m + m_s
        h0_t = (m_m * m0.h + m_s * s0.h) / m_t
        z_t = self._mixed_z(m0, s0, m_m, m_s)
        u_m = self.eta_mix * (m_m * u_p + m_s * u_y) / m_t
        st_m = self.th.ph(P_y, h0_t - 0.5 * u_m * u_m, z_t)
        A_m = m_t / (st_m.rho * u_m)
        shock = self._mix(A_m, [(P_y * A_m, m_t * u_m)], m_t, h0_t, z_t, eta=1.0)
        if shock is None:
            return None
        P3, u3, st3 = shock
        out = self._diffuser(st3, u3, h0_t)
        return dict(m_s=m_s, st_y=st_y, u_y=u_y, P3=P3, u3=u3, st3=st3, out=out)

    def _critical_point(self, Pys, rows, run, s0):
        """Index of the most-entraining feasible scan point (made exact in place).

        CAM: entrainment rises monotonically towards low P_y; the most-entraining
        point is the feasibility boundary (mixed flow sonic at the end of the
        mixing section), which is bisected.  CPM: the suction passage at the
        hypothetical throat shrinks as P_y falls (the motive core expands), so
        m_s(P_y) has an interior maximum near the suction choke; the critical
        point is that maximum (compound choking), refined with a bounded search.
        Points below it are not reached in operation and are dropped (None).
        """
        ok = [r is not None for r in rows]
        if not any(ok):
            raise RuntimeError("no feasible mixing solution (mixing section too small?)")
        if self.mixing == "CPM":
            er = [r["m_s"] / r["m_m"] if r else -math.inf for r in rows]
            k = int(np.argmax(er))
            lo, hi = Pys[max(k - 1, 0)], Pys[min(k + 1, len(Pys) - 1)]

            def neg(p):
                r = run(p)
                return -r["m_s"] / r["m_m"] if r else 1.0
            m = minimize_scalar(neg, bounds=(lo, hi), method="bounded",
                                options={"xatol": 1e-7 * s0.P})
            if -m.fun > er[k]:
                Pys[k], rows[k] = m.x, run(m.x)
            for j in range(k):
                rows[j] = None
            return k
        k0 = ok.index(True)
        if k0 > 0:
            # The feasibility boundary lies between grid points k0-1 and k0: bisect
            # it, otherwise the critical ER depends on the scan resolution.
            lo, hi, r_hi = Pys[k0 - 1], Pys[k0], rows[k0]
            while hi - lo > 1e-6 * s0.P:
                mid = 0.5 * (lo + hi)
                r = run(mid)
                if r:
                    hi, r_hi = mid, r
                else:
                    lo = mid
            Pys[k0], rows[k0] = hi, r_hi
        return k0

    # ==================================================================== RATING
    def _cd_exit(self, mexp, P_t, G_t, r_e, P_low, cache):
        """Exit conditions of a converging-diverging motive nozzle, A_e/A_t = r_e > 1.

        Cd acts at the throat only; the diverging section carries the HEM flux,
        so a choked nozzle has the exit mass flux Ge = Cd*G*/r_e. That requires
        Ge <= G* (r_e >= Cd), otherwise the corrected flow cannot pass the exit
        and a ValueError is raised (no silent clamp).
          P_sup  supersonic exit pressure, G(P_sup) = Ge (design expansion)
          P_fc   first-critical back pressure, subsonic branch G(P_fc) = Ge:
                 the nozzle is choked for P_y < P_fc, exit-controlled above it
          P_se   back pressure with a normal shock standing at the exit (the
                 post-shock pressure of the supersonic exit state); between P_se
                 and P_fc the shock sits inside and the exit is subsonic at P_y.
        Depends on r_e only (fluxes), so it is cached per r_e.
        """
        key = round(r_e, 12)
        if key in cache:
            return cache[key]
        G_star = mexp.flux(P_t)                       # isentropic G* (G_t = Cd * G*)
        Ge = G_t / r_e
        if Ge > G_star * (1 + 1e-9):
            raise ValueError(
                f"nozzle exit/throat area ratio {r_e:.4f} is below the discharge coefficient "
                f"{self.Cd_m:.4f}: with Cd applied at the throat, the equilibrium flow in the "
                f"diverging section cannot pass the corrected motive flow")
        try:
            P_sup = brentq(lambda P: mexp.flux(P) - Ge, P_low, P_t, xtol=1e-6 * P_t)
        except ValueError:
            raise ValueError(f"motive nozzle over-expands below the search floor "
                             f"({P_low / 1e5:.3f} bar) at A_e/A_t = {r_e:.3f}")
        P_fc = brentq(lambda P: mexp.flux(P) - Ge, P_t, mexp.inlet.P * (1 - 1e-12),
                      xtol=1e-7 * P_t)
        st_sup, u_sup = mexp.at(P_sup)
        shock = self._mix(1.0, [(P_sup, Ge * u_sup)], Ge, mexp.inlet.h, mexp.inlet.z, eta=1.0)
        P_se = min(shock[0], P_fc) if shock else P_fc
        ex = dict(Ge=Ge, P_sup=P_sup, st_sup=st_sup, u_sup=u_sup, P_fc=P_fc, P_se=P_se)
        cache[key] = ex
        return ex

    def _motive_state(self, mexp, P_t, G_t, A_t, A_e, P_y, P_low, cache=None):
        """Motive flow, throat pressure, exit pressure, exit state and jet velocity.

        Converging nozzle (A_e <= 1.0001 A_t): choked below P_t (exit = throat),
        otherwise subsonic and exhausting at P_y.
        Converging-diverging nozzle (see _cd_exit): choked below P_fc with the
        supersonic exit state below P_se, a subsonic post-shock exit at P_y
        between P_se and P_fc; exit-controlled subsonic flow G(P_y)*A_e above
        P_fc. Mass flow and exit state are continuous at P_fc and the motive
        flow is continuous everywhere.
        """
        r_e = A_e / A_t
        if r_e <= 1.0001:
            if P_y >= P_t:
                G = mexp.flux(P_y) * self.Cd_m
                st_e, u_e = mexp.at(P_y)
                return G * A_t, P_y, P_y, st_e, u_e
            st_e, u_e = mexp.at(P_t)
            return G_t * A_t, P_t, P_t, st_e, u_e
        ex = self._cd_exit(mexp, P_t, G_t, r_e, P_low, {} if cache is None else cache)
        if P_y >= ex["P_fc"]:                          # unchoked: the exit controls the flow
            st_e, u_e = mexp.at(P_y)
            return st_e.rho * u_e * A_e, P_y, P_y, st_e, u_e
        m_m = G_t * A_t
        if P_y < ex["P_se"]:                           # supersonic exit (over- or under-expanded jet)
            return m_m, P_t, ex["P_sup"], ex["st_sup"], ex["u_sup"]
        # normal shock inside the diverging section: subsonic exit at P_y, same h0 and mass flux
        h0, z = mexp.inlet.h, mexp.inlet.z
        f = lambda h: self.th.ph(P_y, h, z).rho * math.sqrt(max(2.0 * (h0 - h), 0.0)) - ex["Ge"]
        h_isen = mexp.at(P_y)[0].h                     # isentropic subsonic state: flux > Ge
        h = brentq(f, h_isen, h0 - 1e-9 * max(abs(h0), 1.0), xtol=1e-9 * max(abs(h0), 1.0))
        st_e = self.th.ph(P_y, h, z)
        return m_m, P_t, P_y, st_e, math.sqrt(2.0 * (h0 - h))

    def _throat_flux(self, mexp, P_t, G_t, r_e, P_y, P_low, cache):
        """Motive flow per unit throat area at P_y (sizing a fixed shape to a given flow)."""
        if r_e <= 1.0001:
            return G_t if P_y < P_t else mexp.flux(P_y) * self.Cd_m
        ex = self._cd_exit(mexp, P_t, G_t, r_e, P_low, cache)
        return G_t if P_y < ex["P_fc"] else mexp.flux(P_y) * r_e

    def rate(self, m0: State, s0: State, A_t: float, A_e: float, A_mix: float,
             P_out: float, n_scan: int = 40) -> Result:
        """Off-design performance of a given geometry at a given outlet pressure."""
        P_low = max(self.th.p_triple * 1.001, 0.01 * min(s0.P, m0.P))
        mexp, P_t, G_t = self._motive(m0, P_low)
        sexp = Expansion(self.th, s0, self.eta_s)
        P_ys, G_s_max = sexp.critical(P_low)
        cache = {}

        def run(py):
            m_m, pt, pe, st_e, u_e = self._motive_state(mexp, P_t, G_t, A_t, A_e, py, P_low, cache)
            r = self._pass(m0, s0, m_m, pe, st_e, u_e, A_e, A_mix, py, sexp, G_s_max, mexp)
            if r:
                r.update(m_m=m_m, P_t=pt, P_e=pe, st_e=st_e, u_e=u_e)
            return r

        def make(regime, py, r, out=None, ER=None):
            ER = r["m_s"] / r["m_m"] if ER is None else ER
            out = r["out"] if out is None else out
            return Result(regime, r["m_m"], r["m_s"], ER, out.P, P_t=r["P_t"],
                          P_e=r["P_e"], P_y=py, P_3=r["P3"], A_t=A_t, A_e=A_e,
                          A_mix=A_mix, u_e=r["u_e"], u_y=r["u_y"], u_3=r["u3"],
                          out=out, eta_ejector=self._eta_ejector(m0, s0, out, ER),
                          states=dict(m0=m0, s0=s0, e=r["st_e"], y=r["st_y"], mix=r["st3"])
                          ).check_solid(self.th.p_triple)

        # scan suction static pressure from its choke to the inlet pressure
        Pys = np.linspace(P_ys, s0.P * 0.99999, n_scan)
        rows = [run(py) for py in Pys]
        k0 = self._critical_point(Pys, rows, run, s0)   # most-entraining feasible point
        Pout = np.array([r["out"].P if r else np.nan for r in rows])
        ok = np.isfinite(Pout)
        ER_c, Pb_c = rows[k0]["m_s"] / rows[k0]["m_m"], Pout[k0]
        z0 = lambda r: self._mixed_z(m0, s0, r["m_m"], r["m_s"])
        if P_out <= Pout[k0]:
            # critical: flows as at the critical point; the excess lift is lost in shocks
            # downstream, so the outlet stream is the mixed stagnation enthalpy at P_out
            r = rows[k0]
            out = self.th.ph(P_out, r["out"].h, z0(r))
            res = make("critical", Pys[k0], r, out=out)
        else:
            res = None
            for k in range(k0, len(Pys) - 1):
                if ok[k] and ok[k + 1] and (Pout[k] - P_out) * (Pout[k + 1] - P_out) <= 0:
                    py = brentq(lambda p: run(p)["out"].P - P_out, Pys[k], Pys[k + 1],
                                xtol=1e-6 * s0.P)
                    r = run(py)
                    if abs(r["out"].P - P_out) > 1e-4 * P_out:
                        # Brent converged onto a jump of P_out(P_y) (nozzle regime change)
                        raise RuntimeError(
                            f"specified outlet pressure {P_out/1e5:.4f} bar falls in a discontinuity "
                            f"of the characteristic between {Pout[k]/1e5:.4f} and "
                            f"{Pout[k+1]/1e5:.4f} bar (motive nozzle regime change near "
                            f"P_y = {py/1e5:.4f} bar); computed {r['out'].P/1e5:.4f} bar")
                    res = make("subcritical", py, r)
                    break
            if res is None:
                # backflow: no entrainment; the outlet is the motive stream at P_out
                r = rows[-1]
                out = self.th.ph(P_out, m0.h, m0.z)
                res = make("backflow", Pys[-1], r, out=out, ER=0.0)
                res.m_s, res.u_y = 0.0, 0.0
        res.ER_critical, res.P_b_critical = ER_c, Pb_c
        return res

    # ======================================================= RATING, BOTH FLOWS GIVEN
    def rate_flows(self, m0: State, s0: State, m_m: float, m_s: float, r_e: float,
                   r_mix: float, A_t: float | None = None, n_scan: int = 40,
                   n_curve: int = 0) -> Result:
        """Inverse rating: both inlet flows given, outlet pressure computed.

        This is what a sequential-modular flowsheet needs: the ports fix both
        inlet flows.  The geometry is a fixed SHAPE, A_e/A_t = r_e and
        A_mix/A_t = r_mix, sized so that the nozzle passes m_m.  The model is
        scale-invariant, so the shape alone fixes ER(P_y) and P_out(P_y); P_y
        is found from ER(P_y) = m_s/m_m between the most-entraining feasible
        point (ER_critical, outlet pressure P_b_critical) and P_y -> P_s0
        (ER -> 0).  At ER = ER_critical the ejector delivers P_b_critical (the
        highest back pressure on the critical plateau).

        Fixed SIZE: pass the actual throat area A_t (the shape is then the
        caller's A_e/A_t and A_mix/A_t).  The solution still uses m_m, and the
        result reports the capacity Cd*G*A_t of that throat and its mismatch
        motive_capacity/m_m - 1, for the flowsheet to close (e.g. by adjusting
        the motive pressure upstream).

        result.curve: the rating characteristic of the sized geometry, (P_out,
        ER) along the P_y scan from the critical point to zero entrainment
        (re-sampled on n_curve points between the two if n_curve > 0).
        Raises EntrainmentExceeded if m_s/m_m > ER_critical.
        """
        if not (1.0 <= r_e < r_mix):
            raise ValueError("need 1 <= A_e/A_t < A_mix/A_t")
        ER_req = m_s / m_m
        P_low = max(self.th.p_triple * 1.001, 0.01 * min(s0.P, m0.P))
        mexp, P_t, G_t = self._motive(m0, P_low)
        sexp = Expansion(self.th, s0, self.eta_s)
        P_ys, G_s_max = sexp.critical(P_low)

        cache = {}

        def run(py):
            At = m_m / self._throat_flux(mexp, P_t, G_t, r_e, py, P_low, cache)
            Ae, Amix = r_e * At, r_mix * At
            mm, pt, pe, st_e, u_e = self._motive_state(mexp, P_t, G_t, At, Ae, py, P_low, cache)
            r = self._pass(m0, s0, mm, pe, st_e, u_e, Ae, Amix, py, sexp, G_s_max, mexp)
            if r:
                r.update(m_m=mm, P_t=pt, P_e=pe, st_e=st_e, u_e=u_e, A_t=At, A_e=Ae,
                         A_mix=Amix, ER=r["m_s"] / mm)
            return r

        Pys = list(np.linspace(P_ys, s0.P * 0.99999, n_scan))
        rows = [run(py) for py in Pys]
        k0 = self._critical_point(Pys, rows, run, s0)   # most-entraining feasible point
        ok = [r is not None for r in rows]
        ER_c, Pb_c = rows[k0]["ER"], rows[k0]["out"].P
        curve = [(r["out"].P, r["ER"]) for r, o in zip(rows[k0:], ok[k0:]) if o]
        if n_curve > 0:
            curve = []
            for py in np.linspace(Pys[k0], s0.P, n_curve):
                r = run(py)
                if r:
                    curve.append((r["out"].P, r["ER"]))

        def make(regime, py, r):
            res = Result(regime, r["m_m"], r["m_s"], r["ER"], r["out"].P, P_t=r["P_t"],
                         P_e=r["P_e"], P_y=py, P_3=r["P3"], A_t=r["A_t"], A_e=r["A_e"],
                         A_mix=r["A_mix"], u_e=r["u_e"], u_y=r["u_y"], u_3=r["u3"], out=r["out"],
                         eta_ejector=self._eta_ejector(m0, s0, r["out"], r["ER"]),
                         states=dict(m0=m0, s0=s0, e=r["st_e"], y=r["st_y"], mix=r["st3"]),
                         ER_critical=ER_c, P_b_critical=Pb_c, curve=curve)
            if A_t is not None:
                res.motive_capacity = m_m * A_t / r["A_t"]
                res.motive_mismatch = A_t / r["A_t"] - 1.0
            return res.check_solid(self.th.p_triple)

        tol = 1e-9
        if ER_req > ER_c * (1 + 1e-6):
            raise EntrainmentExceeded(
                f"suction flow {m_s:.4g} kg/s (ER {ER_req:.4f}) exceeds the critical "
                f"entrainment of this geometry, ER* = {ER_c:.4f}", ER_c, Pb_c)
        if ER_req >= ER_c * (1 - 1e-6):
            return make("critical", Pys[k0], rows[k0])
        # subcritical branch: ER falls from ER_c (k0) towards 0 at P_y -> P_s0
        ERs = [r["ER"] if r else math.nan for r in rows]
        Pys.append(s0.P)                 # zero entrainment at P_y = P_s0
        ERs.append(0.0)
        for k in range(k0, len(Pys) - 1):
            a, b = ERs[k], ERs[k + 1]
            if np.isfinite(a) and np.isfinite(b) and (a - ER_req) * (b - ER_req) <= 0:
                if ER_req <= tol:
                    py = s0.P
                else:
                    py = brentq(lambda p: run(p)["ER"] - ER_req, Pys[k], Pys[k + 1],
                                xtol=1e-7 * s0.P)
                return make("subcritical", py, run(py))
        raise RuntimeError("rate_flows: no bracket for the requested entrainment")

    # ==================================================================== DESIGN
    def design(self, m0: State, s0: State, m_m: float, ER: float,
               P_y: float | None = None) -> Result:
        """Size an ejector for motive flow m_m and entrainment ratio ER.

        Motive nozzle is ideally expanded to the mixing-inlet pressure P_y
        (P_e = P_y).  If P_y is None, it is chosen to maximise the outlet
        pressure (the optimum-design ejector for this ER).
        """
        P_low = max(self.th.p_triple * 1.001, 0.01 * min(s0.P, m0.P))
        mexp, P_t, G_t = self._motive(m0, P_low)
        A_t = float("nan")
        sexp = Expansion(self.th, s0, self.eta_s)
        P_ys, G_s_max = sexp.critical(P_low)
        m_s = ER * m_m

        def build(py):
            # throat: choked flux, or (unchoked, converging nozzle) the flux at py
            G = G_t if py < P_t else mexp.flux(py) * self.Cd_m
            nonlocal A_t
            A_t = m_m / G
            st_e, u_e = mexp.at(py)                 # ideally expanded to P_y
            A_e = max(m_m / (st_e.rho * u_e), A_t)
            st_y, u_y = sexp.at(py)
            A_sy = m_s / max(st_y.rho * u_y, 1e-12) if m_s > 0 else 0.0
            if self.mixing == "CPM":         # hypothetical throat: phi_p * core + suction
                A_mix = self.phi_p * m_m / (st_e.rho * u_e) + A_sy
            else:
                A_mix = A_e + A_sy
            r = self._pass(m0, s0, m_m, py, st_e, u_e, A_e, A_mix, py, sexp, G_s_max, mexp)
            return r, A_e, A_mix, u_e, st_e

        def neg_pout(py):
            try:
                r = build(py)[0]
                return -r["out"].P if r else 0.0
            except (ValueError, SolidPhaseRisk):
                return 0.0

        if P_y is None:
            # 30 linear points, plus a log-spaced cluster just below P_s0: at small ER the
            # feasible window is a narrow band there (the annulus is otherwise too small)
            Pys = np.unique(np.r_[np.linspace(max(P_ys, P_low), s0.P * 0.995, 30),
                                  s0.P * (1.0 - np.geomspace(4e-3, 1e-4, 10))])
            vals = [neg_pout(p) for p in Pys]
            k = int(np.argmin(vals))
            lo, hi = Pys[max(k - 1, 0)], Pys[min(k + 1, len(Pys) - 1)]
            P_y = minimize_scalar(neg_pout, bounds=(lo, hi), method="bounded",
                                  options={"xatol": 1e-5 * s0.P}).x
        r, A_e, A_mix, u_e, st_e = build(P_y)
        if r is None:
            raise RuntimeError("design infeasible: mixing cannot pass the flow")
        return Result("design", m_m, m_s, ER, r["out"].P, P_t=max(P_t, P_y), P_e=P_y,
                      P_y=P_y, P_3=r["P3"], A_t=A_t, A_e=A_e, A_mix=A_mix, u_e=u_e,
                      u_y=r["u_y"], u_3=r["u3"], out=r["out"],
                      eta_ejector=self._eta_ejector(m0, s0, r["out"], ER),
                      states=dict(m0=m0, s0=s0, e=st_e, y=r["st_y"], mix=r["st3"])
                      ).check_solid(self.th.p_triple)

    def design_for_lift(self, m0, s0, m_m, P_out, ER_max=20.0) -> Result:
        """Largest ER whose optimum design still reaches P_out.

        The optimum-design outlet pressure is not monotone in ER: at very small
        ER the constant-area section can hardly pass the flow and P_out_opt
        rises with ER before it falls. So the maximum is located first (grid in
        ER, then a bounded refinement); P_out is unreachable only if it exceeds
        that maximum. The answer is the root on the falling branch.
        """
        def p_opt(er):
            try:
                return self.design(m0, s0, m_m, er).P_out
            except (RuntimeError, ValueError, SolidPhaseRisk):
                return -math.inf

        grid = [1e-4, 3e-4, 1e-3, 2e-3, 5e-3, 0.01, 0.02, 0.05, 0.1, 0.2, 0.4, 0.8, 1.6, 3.2,
                6.4, 12.8, ER_max]
        vals, kbest = [], 0
        for k, er in enumerate(grid):
            vals.append(p_opt(er))
            if vals[k] > vals[kbest]:
                kbest = k
            if k > kbest and vals[k] < P_out <= vals[kbest]:
                break
        lo, hi = grid[max(kbest - 1, 0)], grid[min(kbest + 1, len(vals) - 1)]
        if lo < hi:
            m = minimize_scalar(lambda e: -p_opt(e), bounds=(lo, hi), method="bounded",
                                options={"xatol": 1e-6})
            er_best, p_best = (m.x, -m.fun) if -m.fun > vals[kbest] else (grid[kbest], vals[kbest])
        else:
            er_best, p_best = grid[kbest], vals[kbest]
        if p_best < P_out:
            raise RuntimeError(f"required outlet pressure {P_out/1e5:.3f} bar not reachable: the "
                               f"highest optimum-design outlet pressure is {p_best/1e5:.3f} bar "
                               f"(at ER {er_best:.4g})")
        k_hi = next((k for k in range(kbest + 1, len(vals)) if vals[k] < P_out), None)
        if k_hi is None:
            raise RuntimeError(f"required outlet pressure {P_out/1e5:.3f} bar is reached up to "
                               f"ER_max = {ER_max}")
        er = brentq(lambda e: p_opt(e) - P_out, max(er_best, grid[k_hi - 1]), grid[k_hi],
                    xtol=1e-5)
        return self.design(m0, s0, m_m, er)


def area(D):
    return math.pi * D * D / 4.0
