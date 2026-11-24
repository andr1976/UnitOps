.. _appendix_capeopen:

==============================
Appendix: CAPE-OPEN Interface
==============================

.. admonition:: Implementation status

   The interface below is implemented in ``ejector/src/Ejector.CapeOpen`` and
   tested headless against mock material objects (:ref:`sec-val-port`). It has
   not yet been run inside a process modelling environment. Names may still
   change before the first release.

Component identity
==================

.. list-table::
   :header-rows: 0
   :widths: 26 74

   * - Name
     - Ejector (1D real-fluid)
   * - ProgID
     - ``Ejector.EjectorUnitOperation.1``
   * - CLSID
     - ``{661320FB-F98D-47CB-9DFD-6A84D72680A9}``
   * - Category
     - CAPE-OPEN 1.0 Unit Operation; consumes thermodynamics (CO 1.0 and 1.1 CATIDs)
   * - Runtime
     - ``net8.0-windows``, x64, COM in-process server via the comhost shim (needs the .NET 8 Desktop runtime)
   * - Registration
     - per-user (``HKCU``) via ``register-user.ps1`` (no admin); ``-Machine`` or ``register.bat`` for ``HKLM``
   * - Thermodynamics
     - CAPE-OPEN 1.1 property package through a scratch material object (:ref:`sec-iflash`)

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
     - Mixed stream at :math:`(P_\mathrm{out}, h_\mathrm{out}, \vec z_\mathrm{out})`, written by a PH flash

Both inlets must use the same compound list. Their state is read as follows:
the stream's own overall enthalpy if it exposes one (PH flash); a two-phase
stream by its vapour fraction (PQ flash); otherwise a TP flash, replaced by the
saturated end point when the stream reports a single saturated phase and the
TP flash lands on the other side of the saturation line.

Modes
=====

The ports fix both inlet flows, so the entrainment ratio is known on entry and
the outlet pressure is what the unit computes (:ref:`sec-flowsheet-specs`).

.. list-table::
   :header-rows: 1
   :widths: 18 18 32 32

   * - ``Mode``
     - ``GeometrySpec``
     - Inputs (besides the ports)
     - Computed
   * - ``Design`` (default)
     - --
     - efficiencies
     - the optimum ("ideal") ejector: highest :math:`P_\mathrm{out}`, diameters and area ratios
   * - ``Rating``
     - ``FixedShape`` (default)
     - ``AreaRatioExit``, ``AreaRatioMixing``, efficiencies
     - :math:`P_\mathrm{out}` by inverse rating; throat sized for the motive port flow
   * - ``Rating``
     - ``FixedSize``
     - ``ThroatDiameter``, ``ExitDiameter``, ``MixingDiameter``, efficiencies
     - :math:`P_\mathrm{out}` by inverse rating; motive capacity of the given throat and its mismatch with the port flow

Parameters
==========

Parameters switch between CAPE-OPEN *input* and *output* with ``Mode``,
``GeometrySpec`` and ``EfficiencySet``, so the PME greys out whatever the unit
computes. An efficiency set fixes the mixing model it was fitted with, and
``Custom`` starts from the values of the set shown last.

.. list-table:: Input parameters
   :header-rows: 1
   :widths: 30 10 8 14 38

   * - Name
     - Type
     - Unit
     - Default
     - Meaning
   * - ``Mode``
     - option
     - --
     - ``Design``
     - ``Design`` / ``Rating``
   * - ``GeometrySpec``
     - option
     - --
     - ``FixedShape``
     - ``FixedShape`` / ``FixedSize`` (Rating only)
   * - ``AreaRatioExit``, ``AreaRatioMixing``
     - real
     - --
     - 1.31, 4.94
     - :math:`A_\mathrm{e}/A_\mathrm{t}`, :math:`A_\mathrm{mix}/A_\mathrm{t}`: input in FixedShape, output otherwise
   * - ``ThroatDiameter``, ``ExitDiameter``, ``MixingDiameter``
     - real
     - m
     - 0.9, 1.03, 2.0 mm
     - :math:`D_\mathrm{t}, D_\mathrm{e}, D_\mathrm{mix}`: input in FixedSize, output otherwise
   * - ``EfficiencySet``
     - option
     - --
     - ``Screening``
     - named set of :numref:`tab-presets`, or ``Custom``
   * - ``MotiveEfficiency``, ``SuctionEfficiency``, ``MixingEfficiency``, ``DiffuserEfficiency``
     - real
     - --
     - 0.90, 0.85, 0.90, 0.80
     - :math:`\eta_\mathrm{m}, \eta_\mathrm{s}, \eta_\mathrm{mix}, \eta_\mathrm{d}`: input with ``Custom``, else show the set's values
   * - ``DischargeCoefficient``
     - real
     - --
     - 1.0
     - motive :math:`C_d`: input with ``Custom``
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
   * - ``MixingModel``
     - option
     - --
     - ``ConstantPressure``
     - ``ConstantPressure`` / ``ConstantArea`` (:ref:`sec-mixing-cpm`): input with ``Custom``, else the set's model
   * - ``PrimaryCoreCoefficient``
     - real
     - --
     - 1.0
     - :math:`\varphi_\mathrm{p}` of constant-pressure mixing (:eq:`eq-cpm-area`): input with ``Custom``
   * - ``CurvePoints``
     - real
     - --
     - 0
     - operating curve: 0 = the points of the rating scan; :math:`n > 0` = :math:`n` re-sampled points (Design: of the designed geometry)

.. _tab-presets:

.. list-table:: Efficiency sets (``EfficiencySet``). Values from :ref:`data_library`.
   :header-rows: 1
   :widths: 30 36 10 24

   * - Name
     - :math:`\eta_\mathrm{m}/\eta_\mathrm{s}/\eta_\mathrm{mix}/\eta_\mathrm{d}`
     - :math:`C_d`
     - Basis
   * - ``Screening``
     - 0.90 / 0.85 / 0.90 / 0.80
     - 1.0
     - assumption; **constant-pressure mixing**, :math:`\varphi_\mathrm{p}` 1
   * - ``R744_AllPoints``
     - 0.90 / 0.610 / 0.825 / 0.820
     - 1.138
     - 42 R744 points, 2 mm ejector
   * - ``R744_Supercritical``
     - 0.90 / 0.638 / 0.829 / 0.814
     - 1.138
     - 27 of those points
   * - ``R744_SubcriticalSubcooled``
     - 0.90 / 0.872 / 0.767 / 0.790
     - 1.138
     - 15 of those points
   * - ``GasEjector_Ma2017``
     - 0.95 / 0.85 / 0.95 / 0.85
     - 1.0
     - single-phase gas ejectors
   * - ``Vapour_CPM``
     - 0.90 / 0.85 / 0.908 / 0.85
     - 1.0
     - **constant-pressure mixing**, :math:`\varphi_\mathrm{p}` 0.869; three single-phase vapour data sets (:ref:`parameter_guide`)

``Screening`` (the default) and ``Vapour_CPM`` use constant-pressure mixing, which predicts better than
constant-area mixing when no calibration is available (:ref:`sec-guide-mixing`). The fitted R744 and gas
sets keep the constant-area mixing they were fitted or validated with.

.. list-table:: Output parameters
   :header-rows: 1
   :widths: 34 12 54

   * - Name
     - Unit
     - Meaning
   * - ``OutletPressure``
     - Pa
     - :math:`P_\mathrm{out}`
   * - ``EntrainmentRatio``, ``PressureLift``
     - --
     - ER from the ports; :math:`P_\mathrm{out}/P_\mathrm{s0}`
   * - ``Regime``
     - --
     - ``Design`` / ``Critical`` / ``Subcritical``
   * - ``ThroatPressure``, ``NozzleExitPressure``, ``SuctionStaticPressure``, ``MixingOutletPressure``
     - Pa
     - :math:`P_\mathrm{t}, P_\mathrm{e}, P_\mathrm{y}, P_3`
   * - ``NozzleExitVelocity``, ``MixingOutletVelocity``
     - m s\ :sup:`-1`
     - :math:`u_\mathrm{e}, u_3`
   * - ``CriticalEntrainmentRatio``, ``CriticalBackPressure``
     - --, Pa
     - Rating: most-entraining ER of the sized geometry and the outlet pressure there
   * - ``MotiveCapacity``, ``MotiveFlowMismatch``
     - kg s\ :sup:`-1`, --
     - FixedSize: :math:`C_d G^* A_\mathrm{t}` of the given throat, and capacity / port flow :math:`- 1`
   * - ``EjectorEfficiency``, ``SolidMargin``, ``OutletVapourFraction``
     - --
     - :math:`\eta_\mathrm{ej}` (:eq:`eq-eta-ej`); :math:`\sigma` (:ref:`sec-solid`; 0 if no triple point); outlet vapour mass fraction
   * - ``FlashCount``, ``CalculationTime``
     - --, s
     - property-package flashes and wall time of the last calculation
   * - ``Curve_OutletPressure``, ``Curve_EntrainmentRatio``
     - Pa, --
     - array parameters: operating curve from the critical point to zero entrainment

The unit also produces a report (``ICapeUnitReport``): plain text, or HTML
with a chart of the operating curve when the PME supports HTML reports.

Errors
======

``Calculate`` raises a CAPE-OPEN error with a readable description, which is
also exposed through ``ECapeUser`` on the unit for PMEs that read it there,
when:

* the port suction flow exceeds the critical entrainment of the geometry
  (the message gives the critical ER and the outlet pressure at which it is
  reached);
* the mixing section cannot pass the flow at any :math:`P_\mathrm{y}`;
* the geometry is inconsistent (:math:`1 \le A_\mathrm{e}/A_\mathrm{t} < A_\mathrm{mix}/A_\mathrm{t}`
  violated, or :math:`1 < A_\mathrm{e}/A_\mathrm{t} < C_d`, which the diverging
  section cannot pass; :ref:`sec-motive-nozzle`), or the motive pressure is not
  above the suction pressure;
* a state on the solution path falls below the triple point of a pure fluid;
* a flash fails and the temperature-search fallback cannot recover it.

A motive capacity mismatch in ``FixedSize`` is reported, not raised: the
flowsheet closes it, for example with an adjust on the motive pressure.
