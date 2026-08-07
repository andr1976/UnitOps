.. _nonisothermal:

=========================================
Non-isothermal Operation (Adiabatic + JT)
=========================================

Motivation
==========

Gas permeation is accompanied by an expansion of the permeating gas from the
high (retentate) pressure to the low (permeate) pressure. For a real gas this
expansion is not isothermal: it carries a **Joule--Thomson** temperature change.
For CO\ :sub:`2`-rich separations at high pressure the permeate can cool by
several kelvin, which matters for downstream equipment and for the enthalpy
balance of the flowsheet. The unit therefore offers an optional **adiabatic
non-isothermal** mode that assigns physically consistent outlet temperatures.
A fully-resolved non-isothermal 2-D treatment for spiral-wound modules is given
by Fontoura *et al.* :cite:`fontoura2022`; the model here is a lumped adiabatic
reduction of the same physics.

The separation is decoupled from the temperature
=================================================

A deliberate and important property of the model is that **temperature does not
feed back into the separation**. The permeance is taken temperature-independent
(:ref:`sec-limitations`) and the driving force :eq:`eq-flux` uses partial
pressures :math:`p\,x_i`, which are independent of temperature at fixed pressure
and composition. Consequently the composition and stage-cut results of
:ref:`crossflow`/:ref:`flow_patterns` are unchanged by the energy balance; the
non-isothermal layer is a **post-step** that takes the solved separation and
assigns outlet temperatures. This keeps the validated separation numbers exactly
as they are and isolates the energy model for independent testing
(:ref:`sec-val-energy`).

Modelling choice
================

The energy model follows the "separate temperatures, adiabatic, Joule--Thomson
on the permeate" formulation. Its physical content:

* Crossing the membrane is a **throttling (isenthalpic) step**: the permeating
  gas retains the molar enthalpy it had on leaving the high-pressure side but is
  now at permeate pressure. Solving for the temperature that restores that
  enthalpy at the lower pressure yields the Joule--Thomson change.
* The retentate outlet temperature follows from the **overall adiabatic
  balance**, so total energy is conserved by construction.

All enthalpies are obtained from the **PME's property package** (i.e. from the
real equation of state), so the Joule--Thomson coefficient is whatever the
selected thermodynamic model implies --- the unit does not hard-code any caloric
correlation. This is the same delegation principle as the flash
(:ref:`architecture`).

Equations
=========

Let the feed be :math:`F` mol s\ :sup:`-1` at :math:`T_\mathrm{f}, p_\mathrm{r}`
with composition :math:`\{z_i\}`; the solved separation gives stage cut
:math:`\theta`, permeate composition :math:`\{y_i\}`, and retentate composition
:math:`\{x_i\}`, with permeate flow :math:`n_\mathrm{p} = F\theta` and retentate
flow :math:`n_\mathrm{r} = F(1-\theta)`. Writing :math:`h(T,p,\vec{x})` for the
PME molar enthalpy:

.. math::
   :label: eq-energy

   \begin{aligned}
   H_\mathrm{feed} &= F\,h(T_\mathrm{f}, p_\mathrm{r}, \vec{z}) &&\text{(feed enthalpy)}\\
   H_\mathrm{p}    &= n_\mathrm{p}\,h(T_\mathrm{f}, p_\mathrm{r}, \vec{y})
                    &&\text{(enthalpy carried by the gas leaving the high-P side)}\\
   T_\mathrm{p} &: \; h(T_\mathrm{p}, p_\mathrm{p}, \vec{y}) = H_\mathrm{p}/n_\mathrm{p}
                    &&\text{(isenthalpic expansion} \; p_\mathrm{r}\!\to\! p_\mathrm{p} \text{)}\\
   H_\mathrm{r} &= H_\mathrm{feed} - H_\mathrm{p} &&\text{(adiabatic overall balance)}\\
   T_\mathrm{r} &: \; h(T_\mathrm{r}, p_\mathrm{r}, \vec{x}) = H_\mathrm{r}/n_\mathrm{r}
                    &&\text{(retentate outlet)}
   \end{aligned}

The permeate temperature :math:`T_\mathrm{p}` is found by holding the molar
enthalpy fixed while dropping the pressure to :math:`p_\mathrm{p}` --- exactly a
Joule--Thomson throttle. Because :math:`h(T,p,\vec x)` is monotone in :math:`T`
(:math:`c_p>0`), each temperature solve is a bracketed root-find on the PME
enthalpy (:ref:`sec-temperature-solve`); crucially it needs only enthalpy
evaluations, not a PME "PH-flash" primitive, so it works with any property
package.

Analytic limits (used as tests)
===============================

Two limits pin the model and are asserted in the unit tests
(:ref:`sec-val-energy`):

#. **Ideal gas.** The enthalpy is pressure-independent, so the isenthalpic
   expansion produces no temperature change and both outlets return exactly to
   the feed temperature, :math:`T_\mathrm{p} = T_\mathrm{r} = T_\mathrm{f}`. This
   is the correct "no Joule--Thomson" limit.

#. **Linear Joule--Thomson gas.** With a molar enthalpy
   :math:`h = \sum_i x_i c_{p,i}\,[(T-T_\mathrm{ref}) - \mu_\mathrm{JT}(p-p_\mathrm{ref})]`
   (a uniform coefficient :math:`\mu_\mathrm{JT}`), the isenthalpic expansion
   gives the closed-form permeate drop

   .. math::
      :label: eq-jt-linear

      T_\mathrm{p} - T_\mathrm{f} \;=\; \mu_\mathrm{JT}\,\bigl(p_\mathrm{p} - p_\mathrm{r}\bigr) \;<\; 0 ,

   and, because that enthalpy is linear in composition, the constant-pressure
   retentate stays at the feed temperature, :math:`T_\mathrm{r} = T_\mathrm{f}`.
   All of the temperature change is carried by the throttled permeate, as the
   physical picture requires.

Temperature profiles
====================

When position profiles are requested (:ref:`sec-profiles`) the energy layer also
returns retentate and permeate **temperature profiles** along the module. At
position :math:`\xi` the retentate temperature follows from the adiabatic balance
over the sub-module :math:`[0,\xi]` (feed in, cumulative permeate
:math:`\theta(\xi)` out), and the permeate temperature from the isenthalpic
expansion of the cumulative collected permeate :eq:`eq-collected`. The endpoints
coincide with the overall outlet temperatures of :eq:`eq-energy`.

Delegation to the PME
=====================

The enthalpy provider (:ref:`sec-enthalpy-provider`) builds a private scratch
Material Object cloned from the feed, sets :math:`(T, p, \vec x)`, flashes at
:math:`T,p`, and sums the phase enthalpies weighted by phase fraction. If the
property package cannot deliver an enthalpy (a limited PME), the unit falls back
to an isothermal result rather than failing the solve, and records the reason in
the diagnostic log.
