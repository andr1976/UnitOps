.. _appendix_capeopen:

========================================
Appendix: CAPE-OPEN Interface (Proposed)
========================================

.. admonition:: Proposal

   This appendix specifies the interface of the planned unit, for review
   before implementation. Names and defaults may change.

Component identity
==================

.. list-table::
   :header-rows: 0
   :widths: 26 74

   * - Name
     - Ejector Unit Operation
   * - ProgID
     - ``Ejector.EjectorUnitOperation.1``
   * - Category
     - CAPE-OPEN Unit Operation
   * - Runtime
     - ``net8.0-windows``, x64, COM in-process server via the comhost shim (needs the .NET 8 Desktop runtime)
   * - Registration
     - per-user (``HKCU``) via ``register-user.ps1`` (no admin); ``-Machine`` for ``HKLM``
   * - Thermodynamics
     - delegated to the PME through scratch Material Objects (:ref:`sec-iflash`)

Ports
=====

.. list-table::
   :header-rows: 1
   :widths: 18 14 68

   * - Port
     - Direction
     - Role
   * - ``Motive``
     - inlet
     - High-pressure motive stream: stagnation state m0 and :math:`\dot m_\mathrm{m}`
   * - ``Suction``
     - inlet
     - Low-pressure entrained stream: stagnation state s0 and :math:`\dot m_\mathrm{s}`
   * - ``Outlet``
     - outlet
     - Mixed stream at :math:`(P_\mathrm{out}, h_\mathrm{out})`, written by a PH flash

Specification modes
===================

The ports fix both inlet flows, so ER is known on entry (:ref:`sec-flowsheet-specs`).

.. list-table::
   :header-rows: 1
   :widths: 20 40 40

   * - ``SpecMode``
     - Inputs (besides the ports)
     - Outputs
   * - ``Design`` (default)
     - efficiencies
     - maximum :math:`P_\mathrm{out}`, geometry, area ratios
   * - ``DesignForPressure``
     - efficiencies, ``OutletPressure``
     - geometry. If ``OutletPressure`` exceeds the maximum: error, reporting the
       maximum ER at that pressure and the motive flow needed
   * - ``Rating``
     - efficiencies, diameters
     - :math:`P_\mathrm{out}` from the port suction flow (inverse rating), regime,
       motive capacity and its mismatch with the port motive flow

Parameters
==========

.. list-table:: Input parameters
   :header-rows: 1
   :widths: 30 10 10 14 36

   * - Name
     - Type
     - Unit
     - Default
     - Meaning
   * - ``SpecMode``
     - option
     - --
     - ``Design``
     - see above
   * - ``OutletPressure``
     - real
     - Pa
     - --
     - required :math:`P_\mathrm{out}`; input only in ``DesignForPressure``
   * - ``ThroatDiameter``, ``ExitDiameter``, ``MixingDiameter``
     - real
     - m
     - --
     - :math:`D_\mathrm{t}, D_\mathrm{e}, D_\mathrm{mix}`: input in ``Rating``, output otherwise
   * - ``MotiveEfficiency``
     - real
     - --
     - 0.90
     - :math:`\eta_\mathrm{m}`
   * - ``SuctionEfficiency``
     - real
     - --
     - 0.85
     - :math:`\eta_\mathrm{s}`
   * - ``MixingEfficiency``
     - real
     - --
     - 0.90
     - :math:`\eta_\mathrm{mix}`
   * - ``DiffuserEfficiency``
     - real
     - --
     - 0.80
     - :math:`\eta_\mathrm{d}`
   * - ``DischargeCoefficient``
     - real
     - --
     - 1.0
     - motive :math:`C_d`
   * - ``MotiveModel``
     - option
     - --
     - ``HEM``
     - ``HEM`` / ``HNE``
   * - ``FlashingFactor``
     - real
     - --
     - 1.0
     - :math:`\varphi` for ``HNE`` (:ref:`sec-hne`)

The default efficiencies are the screening set of :ref:`data_library`. For
R744 the calibrated sets are recommended, and the unit could offer them as
named presets.

.. list-table:: Output parameters
   :header-rows: 1
   :widths: 34 12 54

   * - Name
     - Unit
     - Meaning
   * - ``EntrainmentRatio``
     - --
     - ER from the ports (Design) or computed (Rating)
   * - ``Regime``
     - --
     - ``critical`` / ``subcritical`` / ``backflow`` / ``design``
   * - ``CriticalBackPressure``
     - Pa
     - :math:`P_\mathrm{b}^*` (Rating)
   * - ``MaxEntrainmentAtOutletPressure``
     - --
     - largest ER that reaches ``OutletPressure`` (DesignForPressure)
   * - ``ThroatPressure``, ``MixingInletPressure``
     - Pa
     - :math:`P_\mathrm{t}`, :math:`P_\mathrm{y}`
   * - ``MotiveCapacity``
     - kg s\ :sup:`-1`
     - :math:`C_d G^* A_\mathrm{t}` (Rating), to compare with the port flow
   * - ``AreaRatio``
     - --
     - :math:`A_\mathrm{mix}/A_\mathrm{t}`
   * - ``EjectorEfficiency``
     - --
     - :math:`\eta_\mathrm{ej}`, :eq:`eq-eta-ej`
   * - ``SolidMargin``
     - --
     - :math:`\sigma` (:ref:`sec-solid`)
   * - ``Station_Pressure``, ``Station_Temperature``, ``Station_VapourFraction``, ``Station_Velocity``
     - SI
     - array parameters over stations m0, t, e, s0, y, 3, out

Errors
======

``Calculate`` raises a CAPE-OPEN error with a readable description, and with
the value that would make the duty feasible, when:

* the required outlet pressure is above the zero-entrainment limit;
* the port suction flow exceeds the critical entrainment of the given geometry;
* the mixing section cannot pass the flow at any :math:`P_\mathrm{y}`;
* a state on the solution path falls below the triple point, or the PME
  reports a solid phase.
