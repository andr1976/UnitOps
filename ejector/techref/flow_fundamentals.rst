.. _flow_fundamentals:

===============================
One-Dimensional Real-Fluid Flow
===============================

Every part of the ejector model is built from one relation: the adiabatic
acceleration of a stream from a stagnation state. This chapter states that
relation for a real fluid and shows how choking follows from it without an
explicit speed of sound. It then covers the homogeneous treatment of flashing
flow, the delayed-flashing correction, and the triple-point limit.

.. _sec-expansion-path:

Adiabatic expansion from a stagnation state
===========================================

A stream with stagnation state :math:`(P_0, h_0, s_0, \vec z)` accelerates
adiabatically to static pressure :math:`P`. The steady energy equation gives
the velocity:

.. math::
   :label: eq-velocity

   h_0 = h + \tfrac{1}{2}u^2 \qquad\Longrightarrow\qquad u(P) = \sqrt{2\,\bigl(h_0 - h(P)\bigr)} .

Losses are expressed through an **isentropic efficiency** :math:`\eta`. The
enthalpy drop is a fraction :math:`\eta` of the isentropic one:

.. math::
   :label: eq-expansion-path

   h(P) = h_0 - \eta\,\bigl(h_0 - h_\mathrm{s}(P)\bigr), \qquad
   h_\mathrm{s}(P) = h(P, s_0, \vec z) ,

so one point on the path costs one PS flash (for :math:`h_\mathrm{s}`) and one
PH flash (for the state at :math:`(P, h)`). The PH flash returns the density
:math:`\rho(P)` and, when two-phase, the phase densities. The local **mass flux**
follows as

.. math::
   :label: eq-mass-flux

   G(P) = \rho(P)\,u(P) .

The same path describes the motive nozzle (:math:`\eta = \eta_\mathrm{m}`) and
the acceleration of the suction stream into the mixing section
(:math:`\eta = \eta_\mathrm{s}`) (:ref:`nozzles`).

.. _sec-choking:

Choking as a maximum of the mass flux
=====================================

On an isentropic path :math:`\dd h = \dd P/\rho`, so :math:`u\,\dd u = -\dd P/\rho`, and
:math:`(\partial\rho/\partial P)_s = 1/c^2` defines the speed of sound. Then

.. math::
   :label: eq-dGdP

   \tder{G}{P} = u\,\tder{\rho}{P} + \rho\,\tder{u}{P}
              = \frac{u}{c^2} - \frac{1}{u}
              = \frac{u^2 - c^2}{u\,c^2} .

As the pressure falls from :math:`P_0` the flow is subsonic (:math:`u < c`) and
:math:`G` *rises*. It peaks where :math:`u = c` and falls on the supersonic
branch. A converging–diverging nozzle therefore passes at most

.. math::
   :label: eq-critical-flux

   G^* = \max_{P < P_0} G(P), \qquad P_\mathrm{t} = \arg\max_{P < P_0} G(P),

per unit throat area, with :math:`P_\mathrm{t}` the throat pressure.
:eq:`eq-critical-flux` is used as the **choking criterion** for any fluid. It
needs no speed of sound, which is ill-defined for a two-phase mixture and
discontinuous at the phase boundary. Only flashes along the path are needed.
For :math:`\eta < 1` the path is not isentropic. The maximum of :eq:`eq-critical-flux`
then still marks choking, with :math:`c` read as the derivative
:math:`(\dd P/\dd\rho)^{1/2}` along the actual path. This maximum-flux
condition is the usual way 1D two-phase ejector models locate the motive
throat state.

For an ideal gas :eq:`eq-critical-flux` reproduces the textbook critical
pressure ratio :math:`P^*/P_0 = (2/(k+1))^{k/(k-1)} = 0.528` for
:math:`k = 1.4`. :numref:`fig-nozzle-flux` (a) shows the real-fluid path for
nitrogen on top of the analytic curve, and :ref:`sec-val-ideal-gas` gives the
numerical check.

.. _fig-nozzle-flux:

.. figure:: figures/nozzle_flux.png
   :width: 100%
   :align: center

   Mass flux along the expansion path, :eq:`eq-mass-flux`. (a) Isentropic
   nitrogen from 10 bar, 300 K: the real-fluid path coincides with the
   analytic ideal-gas curve and peaks at :math:`P^*/P_0 = 0.528`. (b)
   CO\ :sub:`2` motive states with :math:`\eta_\mathrm{m} = 0.9`. Saturated liquid at
   47 bar under HEM and with delayed flashing (HNE, :math:`\varphi = 0.5`), and
   dense supercritical fluid at 84.5 bar, 32 °C. Circles mark the throat state
   :math:`(P_\mathrm{t}, G^*)`. Data: ``figures/data/nozzle_flux.csv``.

.. _sec-hem:

Homogeneous equilibrium flashing flow
=====================================

When a liquid or a dense supercritical fluid expands into the two-phase region,
the model uses the **homogeneous equilibrium model (HEM)**: both phases move
at the same velocity, at the same temperature, and in phase equilibrium
:cite:`wallis1969`. The mixture then behaves as a pseudo-fluid whose density
comes from the equilibrium PH flash:

.. math::
   :label: eq-hem-density

   \frac{1}{\rho} = \frac{x_\mathrm{eq}}{\rho_\mathrm{v}} + \frac{1 - x_\mathrm{eq}}{\rho_\mathrm{l}} .

The HEM speed of sound is far below that of either phase, and it drops
discontinuously where the path crosses the saturation line. Two features of
:numref:`fig-nozzle-flux` (b) follow from this:

* A **single-phase liquid** barely changes density as it accelerates, so
  :math:`G` keeps rising and the flow cannot choke until it flashes. A
  liquid-driven jet pump whose motive does not flash therefore works unchoked
  (:ref:`sec-unchoked-motive`).
* A **dense supercritical** motive (84.5 bar, 32 °C) behaves like a stiff liquid
  until its path enters the two-phase region near 68 bar. :math:`G` then bends
  sharply, and the maximum lies inside the dome. The kink is why the choke
  search brackets the maximum with a scan before refining it
  (:ref:`sec-choke-search`).

Because every property comes from a flash, the same equations handle pure
fluids, mixtures, and fluids whose motive and suction are different phases of
the same substance. For mixtures the flash returns a molar vapour fraction. The
mass quality used in :eq:`eq-hem-density` is recovered from the phase
densities, :math:`x = (1/\rho - 1/\rho_\mathrm{l})/(1/\rho_\mathrm{v} - 1/\rho_\mathrm{l})`.

.. _sec-hne:

Delayed flashing: homogeneous non-equilibrium correction
========================================================

In short, fast nozzles the liquid stays superheated (metastable) and flashes
late. Less vapour forms, so the mixture is denser and the critical flux is
*higher* than HEM predicts. The classical corrections introduce a
boiling-delay factor on the vapour generated :cite:`henry1971,diener2004`. The
model offers the simplest form of this idea, a **homogeneous non-equilibrium
(HNE)** quality in the density only:

.. math::
   :label: eq-hne

   x = \varphi\,x_\mathrm{eq}, \qquad
   \frac{1}{\rho} = \frac{x}{\rho_\mathrm{v}} + \frac{1-x}{\rho_\mathrm{l}},
   \qquad 0 < \varphi \le 1 ,

while the enthalpy, and hence the velocity :eq:`eq-velocity`, stays on the
equilibrium path. :math:`\varphi = 1` recovers HEM. For saturated CO\ :sub:`2`
at 47 bar, :math:`\varphi = 0.5` lowers the throat pressure from 34.2 to 28.1
bar. It raises :math:`G^*` from 27.0 to 38.9 t m\ :sup:`-2` s\ :sup:`-1`
(+44 %) (``figures/data/nozzle_critical.csv``).

An equally simple alternative is a **discharge coefficient** :math:`C_d` on the
HEM flux, :math:`\dot m_\mathrm{m} = C_d\,G^*_\mathrm{HEM}\,A_\mathrm{t}`. The
calibration on 42 R744 points finds :math:`C_d = 1.20`: HEM under-predicts the
motive flow of that 0.9 mm nozzle by 17 % on average, with nearly the same bias
for supercritical, subcritical and subcooled inlets (:ref:`sec-val-giacomelli`).
:math:`C_d` and :math:`\varphi` change the **throat size** for a given flow. Their effect
on the outlet pressure is small, because the motive momentum per unit mass flow
is set by the enthalpy drop, which they do not change.

How well HEM predicts motive flow depends on the motive state. It works best
near and above the critical point, and deviations are larger and of less
consistent sign for subcritical motive states
:cite:`palacz2015,haida2018,bodys2020,goncalves2024,ringstad2020`. The motive
inlet state itself is a first-order uncertainty for subcooled liquids.
Gonçalves *et al.* :cite:`goncalves2024` found that the measured motive
temperature and the temperature implied by the measured pressure and density
differ by about 9 K. With the measured temperature this model under-predicts
the motive flow by 42 %; with the density-implied state the error is 2 % or
less (:ref:`sec-val-goncalves`).

.. _sec-solid:

The triple point and solid formation
====================================

Fluid equations of state, and the CoolProp backend of the reference
implementation :cite:`coolprop,span1996`, have no solid phase. For CO\ :sub:`2`
the triple point lies at 5.18 bar and 216.6 K. Below this pressure a two-phase
expansion would form dry ice in reality, but the EOS would quietly return a
metastable vapour–liquid state. The model therefore:

* raises ``SolidPhaseRisk`` for any pure-fluid flash below the triple-point
  pressure, so the solution can never pass through that region silently; and
* reports the **solid margin** :math:`\sigma = P_\mathrm{min}/P_\mathrm{tp}`, with
  :math:`P_\mathrm{min} = \min(P_\mathrm{e}, P_\mathrm{y}, P_3)` the lowest static
  pressure in the ejector. The motive exit and the suction at the mixing inlet
  are the coldest, lowest-pressure points.

For mixtures there is no single triple point. Freeze-out of CO\ :sub:`2` from a
mixture depends on composition and needs a solid–fluid equilibrium model.
In the CAPE-OPEN unit this check is delegated to the PME's property package:
the unit inspects the phases returned by its flashes (:ref:`architecture`).
