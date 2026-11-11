"""
ejector1d -- fast 0D/1D real-fluid ejector (jet pump / eductor) model.

One model covers gas-gas, liquid-liquid, liquid-motive/gas-suction and flashing
two-phase ejectors, because every stream is described only by (P, h) and all
properties come from a homogeneous-equilibrium (HEM) flash.  The thermodynamics
are reached through a three-call interface (`Thermo.ph`, `Thermo.ps`,
`Thermo.pt`) so the CAPE-OPEN version can swap CoolProp for PME flashes.

Model (classical constant-area-mixing 1D ejector, cf. Keenan et al. 1950,
Huang et al. 1999, Liu & Groll 2012, Banasiak & Hafner 2011, Ringstad et al. 2020):

  stations   m0 motive inlet (stagnation)   t motive throat   e motive exit
             s0 suction inlet (stagnation)  y suction at mixing-section inlet
             3  end of constant-area mixing section            out diffuser outlet

  1. Motive nozzle: polytropic expansion with isentropic efficiency eta_m.
     Choked mass flux G* = max_P rho(P,h(P)) * u(P) (HEM critical flow).
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

    def at(self, P) -> tuple[State, float]:
        """Static state and velocity after expanding to P.

        Energy follows the (equilibrium) isentrope with efficiency eta.  With
        phi < 1 the density of a flashing mixture uses the non-equilibrium
        quality x = phi * x_eq (homogeneous non-equilibrium, cf. the boiling
        delay factor N of Henry & Fauske 1971 / Diener & Schmidt 2004): the
        metastable liquid carries less vapour, so the critical flux rises.
        """
        i = self.inlet
        if P >= i.P:
            return i, 0.0
        h_is = self.th.ps(P, i.s, i.z).h
        h = i.h - self.eta * (i.h - h_is)
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

    def check_solid(self, p_triple: float):
        Pmin = min(self.P_e, self.P_y, self.P_3)
        self.solid_margin = Pmin / p_triple if p_triple > 0 else float("inf")
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
    eta_s: float = 0.90      # suction nozzle isentropic efficiency
    eta_mix: float = 0.90    # momentum (mixing) efficiency
    eta_d: float = 0.80      # diffuser isentropic efficiency
    motive_model: str = "HEM"   # 'HEM' | 'HNE' (delayed flashing in motive nozzle)
    phi_ne: float = 1.0         # 'HNE' vapour-generation fraction (1 = HEM)
    Cd_m: float = 1.0           # motive discharge coefficient on G*

    # ---- inlet helpers
    def inlet(self, P, T=None, q=None, z=None) -> State:
        """Stagnation inlet from (P, T) or (P, molar vapour fraction q)."""
        return self.th.pq(P, q, z) if q is not None else self.th.pt(P, T, z)

    def _motive(self, m0: State, P_low: float):
        phi = 1.0 if self.motive_model == "HEM" else self.phi_ne
        exp = Expansion(self.th, m0, self.eta_m, phi=phi)
        P_t, G = exp.critical(P_low)
        return exp, P_t, G * self.Cd_m

    # ---- constant-area mixing
    def _mix(self, A_mix, streams, m_t, h0_t, z_t):
        """streams: list of (pressure*area, momentum flux).  Returns (P3, u3, st3).

        Mass, momentum and energy over the constant-area section give one
        residual in P3.  The subsonic (post-shock) root is the highest-pressure
        one, so march down from the u3 -> 0 end until the residual turns positive.
        """
        F_in = sum(pa for pa, _ in streams) + self.eta_mix * sum(mu for _, mu in streams)

        def resid(P3):
            u3 = (F_in - P3 * A_mix) / m_t
            if u3 <= 0:
                return -1.0, u3, None
            h3 = h0_t - 0.5 * u3 * u3
            st = self.th.ph(P3, h3, z_t)
            return st.rho * u3 * A_mix / m_t - 1.0, u3, st

        P_hi = F_in / A_mix * (1 - 1e-9)          # u3 -> 0: residual = -1
        P_lo = max(self.th.p_triple * 1.0001, 1e3)
        p_prev, r_prev, p = P_hi, -1.0, P_hi
        while p > P_lo:
            p = max(p * 0.96, P_lo)
            try:
                r = resid(p)[0]
            except (ValueError, SolidPhaseRisk):
                return None
            if r > 0:
                P3 = brentq(lambda x: resid(x)[0], p, p_prev, xtol=1e-6 * p)
                _, u3, st = resid(P3)
                return P3, u3, st
            if r < r_prev and r_prev > -0.999:   # past the residual maximum: no root
                return None
            p_prev, r_prev = p, r
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
    def _pass(self, m0, s0, m_m, P_e, st_e, u_e, A_e, A_mix, P_y, sexp, G_s_max):
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

    # ==================================================================== RATING
    def _motive_state(self, mexp, P_t, G_t, A_t, A_e, P_y, P_low):
        """Motive flow, throat/exit pressures for mixing-inlet pressure P_y.

        Choked if P_y < P_t* (exit pressure then from the supersonic area-flux
        relation G(P_e) A_e = m_m); otherwise the nozzle runs subsonic and is
        treated as converging, exhausting at P_y (liquid jet pumps live here).
        """
        if P_y >= P_t:
            G = mexp.flux(P_y) * self.Cd_m
            st_e, u_e = mexp.at(P_y)
            return G * A_t, P_y, P_y, st_e, u_e
        m_m = G_t * A_t
        P_e = P_t
        if A_e > A_t * 1.0001:
            target = m_m / A_e
            try:
                P_e = brentq(lambda P: mexp.flux(P) - target, P_low, P_t, xtol=1e-6 * P_t)
            except ValueError:
                P_e = P_low   # over-expansion beyond P_low: clamp
        st_e, u_e = mexp.at(P_e)
        return m_m, P_t, P_e, st_e, u_e

    def rate(self, m0: State, s0: State, A_t: float, A_e: float, A_mix: float,
             P_out: float, n_scan: int = 40) -> Result:
        """Off-design performance of a given geometry at a given outlet pressure."""
        P_low = max(self.th.p_triple * 1.001, 0.01 * min(s0.P, m0.P))
        mexp, P_t, G_t = self._motive(m0, P_low)
        sexp = Expansion(self.th, s0, self.eta_s)
        P_ys, G_s_max = sexp.critical(P_low)

        def run(py):
            m_m, pt, pe, st_e, u_e = self._motive_state(mexp, P_t, G_t, A_t, A_e, py, P_low)
            r = self._pass(m0, s0, m_m, pe, st_e, u_e, A_e, A_mix, py, sexp, G_s_max)
            if r:
                r.update(m_m=m_m, P_t=pt, P_e=pe, st_e=st_e, u_e=u_e)
            return r

        def make(regime, py, r):
            ER = r["m_s"] / r["m_m"]
            return Result(regime, r["m_m"], r["m_s"], ER, r["out"].P, P_t=r["P_t"],
                          P_e=r["P_e"], P_y=py, P_3=r["P3"], A_t=A_t, A_e=A_e,
                          A_mix=A_mix, u_e=r["u_e"], u_y=r["u_y"], u_3=r["u3"],
                          out=r["out"], eta_ejector=self._eta_ejector(m0, s0, r["out"], ER),
                          states=dict(m0=m0, s0=s0, e=r["st_e"], y=r["st_y"], mix=r["st3"])
                          ).check_solid(self.th.p_triple)

        # scan suction static pressure from its choke to the inlet pressure
        Pys = np.linspace(P_ys, s0.P * 0.99999, n_scan)
        rows = [run(py) for py in Pys]
        Pout = np.array([r["out"].P if r else np.nan for r in rows])
        ok = np.isfinite(Pout)
        if not ok.any():
            raise RuntimeError("no feasible mixing solution (mixing section too small?)")
        k0 = int(np.argmax(ok))                     # most-entraining feasible point
        if P_out <= Pout[k0]:
            res = make("critical", Pys[k0], rows[k0])   # suction choked / max entrainment
            res.P_out = P_out                            # excess lift lost in shocks
            return res
        # subcritical: P_y with P_out_calc(P_y) = P_out
        for k in range(k0, len(Pys) - 1):
            if ok[k] and ok[k + 1] and (Pout[k] - P_out) * (Pout[k + 1] - P_out) <= 0:
                py = brentq(lambda p: run(p)["out"].P - P_out, Pys[k], Pys[k + 1],
                            xtol=1e-6 * s0.P)
                return make("subcritical", py, run(py))
        res = make("backflow", Pys[-1], rows[-1])
        res.m_s, res.ER, res.P_out = 0.0, 0.0, P_out
        return res

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
            A_mix = A_e + A_sy
            r = self._pass(m0, s0, m_m, py, st_e, u_e, A_e, A_mix, py, sexp, G_s_max)
            return r, A_e, A_mix, u_e, st_e

        def neg_pout(py):
            try:
                r = build(py)[0]
                return -r["out"].P if r else 0.0
            except (ValueError, SolidPhaseRisk):
                return 0.0

        if P_y is None:
            Pys = np.linspace(max(P_ys, P_low), s0.P * 0.995, 30)
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
        """Largest ER whose optimum design still reaches P_out."""
        f = lambda er: self.design(m0, s0, m_m, er).P_out - P_out
        lo = 1e-4
        if f(lo) < 0:
            raise RuntimeError("required lift not reachable even at zero entrainment")
        hi = 0.1
        while f(hi) > 0 and hi < ER_max:
            lo, hi = hi, hi * 2
        er = brentq(f, lo, hi, xtol=1e-5)
        return self.design(m0, s0, m_m, er)


def area(D):
    return math.pi * D * D / 4.0
