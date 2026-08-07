.. _notation:

========================
Notation and Conventions
========================

This chapter collects the symbols, units, and sign conventions used throughout
the reference. The physics core works internally in **SI molar units**
(:math:`\si{\kelvin}`, :math:`\si{\pascal}`, :math:`\si{\mol\per\second}`); the
CAPE-OPEN boundary is likewise SI (:ref:`appendix_capeopen`). Where the membrane
literature and datasheets use Barrer or GPU, the conversions are given in
:ref:`data_library`.

Operating state
---------------

.. list-table::
   :header-rows: 1
   :widths: 20 50 30

   * - Symbol
     - Meaning
     - Unit
   * - :math:`T`
     - Temperature
     - K
   * - :math:`p_\mathrm{f},\,p_\mathrm{r}`
     - Feed / retentate-side (high) pressure
     - Pa
   * - :math:`p_\mathrm{p}`
     - Permeate-side (low) pressure
     - Pa
   * - :math:`\gamma = p_\mathrm{p}/p_\mathrm{r}`
     - Pressure ratio
     - --
   * - :math:`x_i`
     - Retentate-side (bulk feed) mole fraction of component :math:`i`
     - --
   * - :math:`y_i`
     - Permeate-side mole fraction of component :math:`i`
     - --
   * - :math:`z_i`
     - Feed mole fraction of component :math:`i`
     - --
   * - :math:`n`
     - Component index, :math:`i = 1 \dots n`
     - --

Flows, area, and cut
--------------------

.. list-table::
   :header-rows: 1
   :widths: 20 50 30

   * - Symbol
     - Meaning
     - Unit
   * - :math:`F`
     - Feed molar flow
     - mol s\ :sup:`-1`
   * - :math:`R,\,V`
     - Local retentate / permeate molar flow
     - mol s\ :sup:`-1`
   * - :math:`A`
     - Total membrane area
     - m\ :sup:`2`
   * - :math:`a`
     - Cumulative membrane area from the feed end (:math:`0 \le a \le A`)
     - m\ :sup:`2`
   * - :math:`\xi = a/A`
     - Dimensionless position along the module (0 feed end, 1 retentate outlet)
     - --
   * - :math:`\theta = V_\mathrm{out}/F`
     - Overall stage cut (total permeate / feed)
     - --
   * - :math:`\theta(\xi)`
     - Cumulative stage cut collected up to position :math:`\xi`
     - --

Transport properties
---------------------

.. list-table::
   :header-rows: 1
   :widths: 20 50 30

   * - Symbol
     - Meaning
     - Unit
   * - :math:`\mathcal{P}_i`
     - Permeability of component :math:`i` (material property)
     - mol m m\ :sup:`-2` s\ :sup:`-1` Pa\ :sup:`-1`
   * - :math:`Q_i = \mathcal{P}_i / \ell`
     - Permeance of component :math:`i` (membrane property; :math:`\ell` = active-layer thickness)
     - mol m\ :sup:`-2` s\ :sup:`-1` Pa\ :sup:`-1`
   * - :math:`\ell`
     - Active-layer thickness
     - m
   * - :math:`\alpha_{ij} = \mathcal{P}_i/\mathcal{P}_j`
     - Ideal selectivity of :math:`i` over :math:`j`
     - --
   * - :math:`J_i`
     - Local molar permeation flux of :math:`i`
     - mol m\ :sup:`-2` s\ :sup:`-1`
   * - :math:`S_i = Q_i / Q_\mathrm{ref}`
     - Permeance ratio to a reference component (dimensionless solver variable)
     - --

Energy quantities (non-isothermal model)
-----------------------------------------

.. list-table::
   :header-rows: 1
   :widths: 20 50 30

   * - Symbol
     - Meaning
     - Unit
   * - :math:`h`
     - Molar enthalpy of a stream (from the PME's equation of state)
     - J mol\ :sup:`-1`
   * - :math:`H`
     - Total enthalpy flow (:math:`H = \dot n\, h`)
     - W
   * - :math:`c_p`
     - Molar isobaric heat capacity
     - J mol\ :sup:`-1` K\ :sup:`-1`
   * - :math:`\mu_\mathrm{JT} = (\partial T/\partial p)_h`
     - Joule--Thomson coefficient
     - K Pa\ :sup:`-1`
   * - :math:`T_\mathrm{r},\,T_\mathrm{p}`
     - Retentate / permeate outlet temperature
     - K

Sign and indexing conventions
-----------------------------

* Position runs from the **feed end** (:math:`\xi = 0`) to the **retentate
  outlet** (:math:`\xi = 1`). "Local permeate" means the permeate freshly formed
  at a position; "collected permeate" is the cumulative mixed-cup permeate
  gathered from the feed end up to that position.
* The **driving force** for component :math:`i` is the partial-pressure
  difference across the membrane, :math:`p_\mathrm{r} x_i - p_\mathrm{p} y_i`
  (:ref:`solution_diffusion`).
* The unit is **isothermal** in the separation calculation; the non-isothermal
  layer (:ref:`nonisothermal`) is an optional adiabatic post-step that assigns
  outlet temperatures without changing the separation.
