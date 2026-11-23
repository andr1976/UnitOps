.. _notation:

========================
Notation and Conventions
========================

This chapter collects the symbols, units and conventions used throughout the
reference. Unlike the membrane model, the ejector physics core works on a
**mass basis** in SI units (Pa, K, J kg\ :sup:`-1`, kg s\ :sup:`-1`, m, m\ :sup:`2`),
because the momentum balance is naturally written per unit mass. The CAPE-OPEN
boundary is molar SI. The adapter converts at the ports with the molar mass of
each stream (:ref:`architecture`).

Stations
--------

The one-dimensional model tracks the flow through the stations shown in the
ejector schematic (:ref:`sec-unit-operation`).

.. list-table::
   :header-rows: 1
   :widths: 14 86

   * - Station
     - Meaning
   * - m0
     - Motive inlet, stagnation state (from the motive port)
   * - t
     - Motive nozzle throat (area :math:`A_\mathrm{t}`)
   * - e
     - Motive nozzle exit (area :math:`A_\mathrm{e}`)
   * - s0
     - Suction inlet, stagnation state (from the suction port)
   * - y
     - Suction flow at the inlet of the constant-area mixing section, annulus area :math:`A_\mathrm{sy} = A_\mathrm{mix}-A_\mathrm{e}`
   * - 3
     - End of the constant-area mixing section (area :math:`A_\mathrm{mix}`), fully mixed
   * - out
     - Diffuser outlet, velocity taken as zero (the outlet port)

Thermodynamic state and flow
----------------------------

.. list-table::
   :header-rows: 1
   :widths: 22 50 28

   * - Symbol
     - Meaning
     - Unit
   * - :math:`P,\,T`
     - Static pressure, temperature (absolute)
     - Pa, K
   * - :math:`h,\,s`
     - Specific enthalpy, specific entropy
     - J kg\ :sup:`-1`, J kg\ :sup:`-1` K\ :sup:`-1`
   * - :math:`h_0`
     - Stagnation enthalpy, :math:`h + u^2/2`
     - J kg\ :sup:`-1`
   * - :math:`\rho`
     - Mixture density, homogeneous (HEM) unless stated
     - kg m\ :sup:`-3`
   * - :math:`\rho_\mathrm{l},\,\rho_\mathrm{v}`
     - Saturated liquid and vapour phase densities from the flash
     - kg m\ :sup:`-3`
   * - :math:`x`
     - Vapour mass fraction (quality); equilibrium value :math:`x_\mathrm{eq}`
     - --
   * - :math:`\vec z`
     - Overall mole fractions of a stream (mixtures only)
     - --
   * - :math:`u`
     - Cross-section averaged velocity
     - m s\ :sup:`-1`
   * - :math:`c`
     - Speed of sound (HEM for two-phase flow)
     - m s\ :sup:`-1`
   * - :math:`G = \rho u`
     - Mass flux; :math:`G^*` its maximum (critical) value
     - kg m\ :sup:`-2` s\ :sup:`-1`
   * - :math:`\dot m_\mathrm{m},\,\dot m_\mathrm{s}`
     - Motive and suction (entrained) mass flow
     - kg s\ :sup:`-1`
   * - :math:`\dot m_\mathrm{t}`
     - Total flow, :math:`\dot m_\mathrm{m}+\dot m_\mathrm{s}`
     - kg s\ :sup:`-1`
   * - :math:`\mathrm{ER} = \dot m_\mathrm{s}/\dot m_\mathrm{m}`
     - Entrainment (mass) ratio
     - --

Geometry and model parameters
-----------------------------

.. list-table::
   :header-rows: 1
   :widths: 22 50 28

   * - Symbol
     - Meaning
     - Unit
   * - :math:`A_\mathrm{t},\,A_\mathrm{e},\,A_\mathrm{mix}`
     - Motive throat, motive exit, constant-area mixing section
     - m\ :sup:`2`
   * - :math:`D_\mathrm{t},\,D_\mathrm{e},\,D_\mathrm{mix}`
     - The corresponding diameters
     - m
   * - :math:`\eta_\mathrm{m},\,\eta_\mathrm{s}`
     - Isentropic efficiency of the motive nozzle and of the suction acceleration
     - --
   * - :math:`\eta_\mathrm{mix}`
     - Mixing (momentum) efficiency, applied to the inlet momentum flux
     - --
   * - :math:`\eta_\mathrm{d}`
     - Diffuser isentropic efficiency
     - --
   * - :math:`C_d`
     - Motive discharge coefficient on the critical mass flux
     - --
   * - :math:`\varphi`
     - Delayed-flashing (HNE) vapour-generation fraction, :math:`x = \varphi\,x_\mathrm{eq}`; :math:`\varphi = 1` is HEM
     - --

Performance quantities
----------------------

.. list-table::
   :header-rows: 1
   :widths: 22 50 28

   * - Symbol
     - Meaning
     - Unit
   * - :math:`P_\mathrm{out}`
     - Outlet (back) pressure
     - Pa
   * - :math:`P_\mathrm{b}^*`
     - Critical back pressure: the highest :math:`P_\mathrm{out}` at which the entrainment is still at its maximum
     - Pa
   * - :math:`\Pi = P_\mathrm{out}/P_\mathrm{s0}`
     - Pressure lift ratio
     - --
   * - :math:`\eta_\mathrm{ej}`
     - Ejector efficiency (expansion-work recovery) of Köhler / Elbel, :eq:`eq-eta-ej`
     - --
   * - :math:`\sigma = P_\mathrm{min}/P_\mathrm{tp}`
     - Solid margin: lowest static pressure in the ejector over the triple-point pressure
     - --

Conventions
-----------

* Inlet states are **stagnation** states. The port velocity is neglected, and so is
  the outlet velocity after the diffuser.
* The ejector is **adiabatic** and steady. The overall energy balance
  :math:`\dot m_\mathrm{t} h_\mathrm{out} = \dot m_\mathrm{m} h_\mathrm{m0} + \dot m_\mathrm{s} h_\mathrm{s0}`
  holds exactly by construction, whatever the efficiencies.
* Pressures in the text and figures are quoted in **bar (absolute)** for
  readability. The code uses Pa.
* The suffix in a symbol names a station (:math:`P_\mathrm{y}`, :math:`u_\mathrm{3}`).
  The "motive" and "suction" streams are also called primary and secondary in
  the refrigeration literature.
