.. _mixing_diffuser:

=================================
Constant-Area Mixing and Diffuser
=================================

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
comparable. The optimum designs of the worked example reach 0.14–0.30
(:ref:`worked_example`). The **solid margin** :math:`\sigma` (:ref:`sec-solid`)
is reported with every result.
