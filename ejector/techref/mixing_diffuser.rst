.. _mixing_diffuser:

====================
Mixing and Diffuser
====================

The model offers two mixing models. **Constant-area mixing** (CAM, the default,
``mixing = "CAM"``) applies mass, momentum and energy balances over the
constant-area section (:ref:`sec-mixing`). **Constant-pressure mixing** (CPM,
``mixing = "CPM"``) mixes the streams at the pressure of a hypothetical throat
and then passes the mixed stream through a normal shock
(:ref:`sec-mixing-cpm`). The diffuser is common to both. Which one to use, and
with which parameters, is the subject of :ref:`parameter_guide`.

.. _sec-mixing:

Balances over the mixing section
================================

The control volume runs from the mixing-section inlet, where the motive jet
(area :math:`A_\mathrm{e}`) and the suction annulus (area :math:`A_\mathrm{sy}`)
enter side by side, to station 3 at the end of the constant-area section
(area :math:`A_\mathrm{mix}`). There the flow is taken as fully mixed and uniform.
Wall shear is not modelled explicitly. With :math:`\dot m_\mathrm{t} = \dot m_\mathrm{m} + \dot m_\mathrm{s}`
the balances are

.. math::
   :label: eq-mix-momentum

   \underbrace{P_\mathrm{e}A_\mathrm{e} + P_\mathrm{y}A_\mathrm{sy}
   + \eta_\mathrm{mix}\,\bigl(\dot m_\mathrm{m}u_\mathrm{e} + \dot m_\mathrm{s}u_\mathrm{y}\bigr)}_{F_\mathrm{in}}
   = P_3A_\mathrm{mix} + \dot m_\mathrm{t}u_3 ,

.. math::
   :label: eq-mix-energy

   h_3 + \tfrac{1}{2}u_3^2 = h_{0\mathrm{t}} \equiv
   \frac{\dot m_\mathrm{m}h_\mathrm{m0} + \dot m_\mathrm{s}h_\mathrm{s0}}{\dot m_\mathrm{t}} ,

.. math::
   :label: eq-mix-mass

   \rho(P_3, h_3, \vec z_\mathrm{t})\,u_3\,A_\mathrm{mix} = \dot m_\mathrm{t} .

The **mixing efficiency** :math:`\eta_\mathrm{mix}` multiplies only the momentum
flux of the incoming streams, not the pressure forces. It stands for wall
friction and the momentum lost in incomplete mixing. This momentum-efficiency
form is used, in variants, by several 1D models :cite:`huang1999,liu2013`.
:math:`\eta_\mathrm{mix} = 1` with all other efficiencies at one gives the loss-free
classical jet pump (:ref:`sec-val-fluids`).

For mixtures the outlet composition follows from the molar flows,

.. math::
   :label: eq-mix-composition

   \vec z_\mathrm{t} = \frac{n_\mathrm{m}\vec z_\mathrm{m} + n_\mathrm{s}\vec z_\mathrm{s}}{n_\mathrm{m}+n_\mathrm{s}},
   \qquad n_k = \dot m_k / M(\vec z_k) ,

and every flash downstream of the mixing inlet is made at :math:`\vec z_\mathrm{t}`.

The mixing residual and its two roots
=====================================

:eq:`eq-mix-momentum` gives :math:`u_3` explicitly as a function of :math:`P_3`, and
:eq:`eq-mix-energy` then gives :math:`h_3`. Mass conservation
:eq:`eq-mix-mass` is left as a single residual in :math:`P_3`:

.. math::
   :label: eq-mix-residual

   R(P_3) = \frac{\rho\bigl(P_3,\, h_{0\mathrm{t}} - \tfrac12 u_3^2,\, \vec z_\mathrm{t}\bigr)\,u_3\,A_\mathrm{mix}}{\dot m_\mathrm{t}} - 1,
   \qquad u_3 = \frac{F_\mathrm{in} - P_3A_\mathrm{mix}}{\dot m_\mathrm{t}} .

At :math:`P_3 = F_\mathrm{in}/A_\mathrm{mix}` the velocity is zero and :math:`R = -1`.
As :math:`P_3` falls, :math:`R` rises to a maximum and then falls again. For fixed
momentum and total enthalpy, the mass flux carried by a uniform state is greatest
when that state is sonic. Two outcomes are possible:

* **Two roots.** These are the supersonic and subsonic states that share the same
  mass, momentum and energy fluxes, i.e. the states on either side of a normal
  shock. The model takes the **subsonic (higher-pressure) root**. A supersonic
  mixed stream is assumed to shock before station 3, so the diffuser always
  receives subsonic flow.
* **No root.** :math:`\max R < 0`: the section cannot pass the combined flow at
  the available momentum, so the mixed flow is choked. This operating point
  (this :math:`P_\mathrm{y}`) is **infeasible**. In rating mode its boundary sets
  the maximum entrainment (:ref:`sec-regimes`).

The search for the subsonic root is described in :ref:`sec-mixing-root`.

.. _sec-mixing-cpm:

Constant-pressure mixing
========================

Keenan *et al.* :cite:`keenan1950` let the two streams mix at a uniform
pressure, which is the usual design of steam and refrigerant ejectors: the
nozzle exit sits in the converging suction chamber, upstream of the
constant-area section. Munday and Bagster :cite:`munday1977` added a
**hypothetical throat** y–y, where the motive jet has not yet mixed and the
suction stream flowing around it reaches its highest velocity. Huang *et al.*
:cite:`huang1999` placed that throat inside the constant-area section and made
the model predictive for the critical mode. The CPM option is Huang's model
with real-fluid states, extended to the subcritical regime. It is structured as
follows.

**Hypothetical throat.** The motive core at the mixing pressure
:math:`P_\mathrm{y}` is the state on the motive path
(:eq:`eq-motive-path`; the nozzle expansion continued from the exit state to
:math:`P_\mathrm{y}`). It occupies the fraction :math:`\varphi_\mathrm{p}` of its
isentropic area, and the suction stream passes the rest of the section:

.. math::
   :label: eq-cpm-area

   A_\mathrm{sy} = A_\mathrm{mix} - \varphi_\mathrm{p}\,
   \frac{\dot m_\mathrm{m}}{\rho_\mathrm{py}u_\mathrm{py}},
   \qquad \dot m_\mathrm{s} = \rho_\mathrm{y}u_\mathrm{y}A_\mathrm{sy} ,

with :math:`(\rho_\mathrm{y}, u_\mathrm{y})` the suction expansion to
:math:`P_\mathrm{y}` (:math:`\eta_\mathrm{s}`). :math:`\varphi_\mathrm{p}` is
Huang's coefficient for the loss of the motive jet between the nozzle exit and
the throat (0.88 in his calibration). :math:`\varphi_\mathrm{p} < 1` leaves more of
the section to the suction stream. :math:`A_\mathrm{sy} \le 0` (the core fills
the section) makes the point infeasible.

**Mixing at constant pressure.** With no pressure force on the control
volume, the momentum balance is

.. math::
   :label: eq-cpm-momentum

   \dot m_\mathrm{t}u_\mathrm{m} = \eta_\mathrm{mix}\,
   \bigl(\dot m_\mathrm{m}u_\mathrm{py} + \dot m_\mathrm{s}u_\mathrm{y}\bigr),
   \qquad h_\mathrm{m} = h_{0\mathrm{t}} - \tfrac12 u_\mathrm{m}^2 ,

and the mixed state is the flash at :math:`(P_\mathrm{y}, h_\mathrm{m}, \vec z_\mathrm{t})`.
:math:`\eta_\mathrm{mix}` has the same meaning as in CAM; it is Huang's
:math:`\varphi_\mathrm{m}`.

**Normal shock.** A supersonic mixed stream passes a loss-free normal shock in
the constant-area section. The post-shock state is the subsonic root of
:eq:`eq-mix-residual` for that single stream, with
:math:`A = \dot m_\mathrm{t}/(\rho_\mathrm{m}u_\mathrm{m})` and
:math:`\eta_\mathrm{mix} = 1`. A subsonic mixed stream passes unchanged. The
result is station 3, and the diffuser follows (:ref:`sec-diffuser`).

Three differences from CAM follow from this structure:

* :math:`P_\mathrm{out}` does not depend on :math:`A_\mathrm{mix}`. The section
  area only sets the suction flow through :eq:`eq-cpm-area`.
* Continuity of the mixed stream in :math:`A_\mathrm{mix}` is not imposed, as in
  Huang's model. Mixing-section choking therefore does not limit the
  entrainment.
* At the same efficiencies, CPM gives more lift than CAM. The constant-area
  momentum balance carries the wall pressure force of a rising static pressure;
  constant-pressure mixing followed by a shock loses less. That is the
  classical result of Keenan *et al.* (checked in the test suite).

**Critical point.** Huang fixes :math:`P_\mathrm{y}` at the suction choke
pressure (Mach 1 at y–y). On the real-fluid motive path the supersonic core
grows as :math:`P_\mathrm{y}` falls, so :math:`A_\mathrm{sy}` shrinks. As a result
:math:`\dot m_\mathrm{s}(P_\mathrm{y})` reaches its maximum slightly *above* the
suction choke. The model takes that maximum as the critical point. This is the
compound-choking condition of a two-stream flow at a common pressure (cf. the
"saturated supersonic" regime of Fabri and Paulon :cite:`fabri1958`). It
reduces to Huang's rule when the core area is fixed. Rating then scans
:math:`P_\mathrm{y}` from the critical point to :math:`P_\mathrm{s0}`, as for CAM
(:ref:`sec-rating-algorithm`).

**Design.** The optimum design (:ref:`sec-solution-design`) uses the same
balances. The section is sized at the hypothetical throat,
:math:`A_\mathrm{mix} = \varphi_\mathrm{p}\dot m_\mathrm{m}/(\rho_\mathrm{e}u_\mathrm{e}) + A_\mathrm{sy}`,
so that rating the designed geometry at the design flows returns the design
point.

Huang's own procedure takes the critical back pressure as input and iterates
:math:`A_\mathrm{mix}`, so it is a sizing calculation. His :math:`\eta_\mathrm{p}` and
:math:`\eta_\mathrm{s}` multiply the choked mass flows (as
:math:`\sqrt{\eta}`), so they act as discharge coefficients
(:math:`C_d = \sqrt{\eta_\mathrm{p}}`), not as isentropic efficiencies.

.. _sec-diffuser:

Diffuser
========

The diffuser decelerates the mixed stream from :math:`u_3` to rest with
isentropic efficiency :math:`\eta_\mathrm{d}`. The outlet pressure is the
pressure on the station-3 isentrope reached by a fraction :math:`\eta_\mathrm{d}`
of the kinetic energy:

.. math::
   :label: eq-diffuser

   h\bigl(P_\mathrm{out},\, s_3,\, \vec z_\mathrm{t}\bigr) = h_3 + \eta_\mathrm{d}\,\tfrac{1}{2}u_3^2 .

The outlet state is then the PH flash at :math:`(P_\mathrm{out}, h_{0\mathrm{t}})`.
The overall energy balance
:math:`\dot m_\mathrm{t}h_\mathrm{out} = \dot m_\mathrm{m}h_\mathrm{m0} + \dot m_\mathrm{s}h_\mathrm{s0}`
is therefore satisfied exactly, whatever the efficiencies. The test suite checks
this to machine precision (:ref:`sec-val-suite`). The outlet port receives this
state. A flashing motive generally gives a **two-phase outlet**.

.. _sec-performance:

Performance measures
====================

Three measures summarise an operating point:

* the **entrainment ratio** :math:`\mathrm{ER} = \dot m_\mathrm{s}/\dot m_\mathrm{m}`;
* the **pressure lift** :math:`\Pi = P_\mathrm{out}/P_\mathrm{s0}`; and
* the **ejector efficiency** of Köhler *et al.* and Elbel & Hrnjak
  :cite:`kohler2007,elbel2008,elbel2011`: the isentropic compression work
  recovered for the suction stream, divided by the isentropic expansion work
  available from the motive stream between the same pressures:

.. math::
   :label: eq-eta-ej

   \eta_\mathrm{ej} = \mathrm{ER}\,
   \frac{h(P_\mathrm{out}, s_\mathrm{s0}) - h_\mathrm{s0}}{h_\mathrm{m0} - h(P_\mathrm{out}, s_\mathrm{m0})} .

:math:`\eta_\mathrm{ej}` makes ejectors with different fluids and pressure levels
comparable. The optimum designs of the worked example reach 0.14–0.31
(:ref:`worked_example`). The **solid margin** :math:`\sigma` (:ref:`sec-solid`)
is reported with every result.
