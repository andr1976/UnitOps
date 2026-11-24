.. _architecture:

=====================
Software Architecture
=====================

.. admonition:: Implementation status

   The CAPE-OPEN implementation described here is in ``ejector/src``:
   ``EjectorCore`` (engine), ``Ejector.CapeOpen`` (adapter) and their test
   projects. The Python reference implementation
   ``ejector/prototype/ejector1d.py`` remains the source of every number in this
   reference and the regression baseline of the port (:ref:`sec-val-port`).
   Path tabulation (:ref:`sec-flash-cost`) is not implemented yet.

Design principle: separate the physics from the plumbing
=========================================================

The ejector unit follows the ports-and-adapters split that the membrane unit
operation in this repository already uses. It is built as **its own
assemblies**, with no build-time dependency on the membrane unit:

.. only:: html

   .. figure:: figures/tikz_arch.png
      :width: 100%
      :align: center

.. only:: latex

   .. raw:: latex

      \begin{center}
      \resizebox{\linewidth}{!}{\input{arch_body.tex}}
      \end{center}

* **EjectorCore** holds *only* the numerical model: expansion paths and choke
  search, mixing, diffuser, and the rating and design drivers. It has no COM,
  no CAPE-OPEN and no thermodynamics of its own. It reaches fluid properties
  through one narrow port, :class:`IFlash` (:ref:`sec-iflash`), so the physics
  can be unit-tested headlessly against a CoolProp-backed or synthetic flash.
* **Ejector.CapeOpen** is the adapter: a ``net8.0-windows``, x64, COM-visible
  assembly activated through the generated ``comhost`` shim. It implements the
  CAPE-OPEN unit interfaces, converts between Material Objects and the core's
  mass-based API, and implements :class:`IFlash` with the PME's property
  package.

Proven pieces of the membrane adapter are reused **by copy**, not by reference:
the real/option/array parameter classes, the persistence pattern, the
registration script and the report scaffolding. Their CAPE-OPEN quirks
(array-parameter marshalling, the solve-instance hop, always-dirty persistence)
apply unchanged. The two units can then be versioned and released
independently.

.. _sec-iflash:

The thermodynamic port
======================

The whole model needs four flashes and a molar mass, all at a given overall
composition :math:`\vec z`:

.. list-table::
   :header-rows: 1
   :widths: 22 30 48

   * - Call
     - Used for
     - CAPE-OPEN 1.1 realisation (scratch Material Object)
   * - ``PH(P, h, z)``
     - every state on a path, mixed state, outlet
     - ``CalcEquilibrium`` with specifications pressure + enthalpy
   * - ``PS(P, s, z)``
     - isentropic reference enthalpy :eq:`eq-expansion-path`, diffuser :eq:`eq-diffuser`
     - ``CalcEquilibrium`` with pressure + entropy (fallback below)
   * - ``PT(P, T, z)``
     - inlet states from port :math:`(T, P)`
     - ``CalcEquilibrium`` with temperature + pressure
   * - ``PQ(P, q, z)``
     - inlets the stream reports as two-phase or saturated
     - ``CalcEquilibrium`` with pressure + vapour phase fraction
   * - ``M(z)``
     - mass/molar conversion, :eq:`eq-mix-composition`
     - compound molar weights from the Material Object

Each call returns :math:`T`, :math:`h`, :math:`s`, the overall density and,
when two phases are present, the phase densities and the vapour mass fraction.
The adapter reads these as overall and per-phase properties after the flash.
PMEs work in molar units, so enthalpies and entropies are converted with
:math:`M(\vec z)` at the boundary.

Two robustness rules apply in the adapter (the first is new there; the reference implementation calls CoolProp directly):

* **PS and PH fallback.** Not every property package supports an entropy (or
  enthalpy) specification. If ``CalcEquilibrium`` declines it, the adapter
  solves :math:`s(P, T) = s` (or :math:`h`) by Brent on :math:`T` with TP
  flashes. For a pure fluid, a target between the saturated liquid and vapour
  values is a two-phase state: it is built from the two saturated end points
  (PQ flashes) on a mass basis, because :math:`s(T)` jumps at the saturation
  temperature. After three such rescues the adapter stops asking for the
  specification.
* **Failed flashes are data, not crashes.** Far below the operating range a
  flash may fail to converge, as CoolProp mixture flashes do at a fraction of a
  bar. The choke search treats such points as missing
  (:ref:`sec-choke-search`). Only failures inside the converged solution are
  reported as CAPE-OPEN errors.

The **solid check** uses the package's data. For a pure compound the adapter
reads the triple-point pressure from the compound constants
(``triplePointPressure``) and the engine applies the test of :ref:`sec-solid`.
For mixtures, and for packages without that constant, no check is made yet;
inspecting the phases present for a solid phase is a planned extension.

.. _sec-flash-cost:

Flash cost and tabulation
=========================

The reference implementation calls flashes freely. Measured counts for pure
CO\ :sub:`2` (CoolProp HEOS, about 7–15 µs per flash including Python overhead)
are:

.. list-table::
   :header-rows: 1
   :widths: 46 18 36

   * - Call
     - Flashes
     - Time, reference implementation
   * - rating, critical regime (Giacomelli case 1)
     - 1 943
     - 32 ms
   * - rating, subcritical regime
     - 2 111
     - 34 ms
   * - inverse rating (both flows given)
     - 2 127
     - 31 ms
   * - optimum design (worked example)
     - 1 684
     - 12 ms
   * - design for lift (maximum over ER first, then the root)
     - 57 816
     - 450 ms
   * - optimum design, CO\ :sub:`2` + N\ :sub:`2` mixture
     - 1 717
     - 38 s (22 ms per flash)

A PME flash through COM typically costs a millisecond or more, and mixture
flashes cost more. Run naively, one Calculate would take seconds, and design
for lift tens of seconds. The core therefore separates the flashes into two
kinds:

* **Path flashes** depend only on an inlet state and its efficiency: the
  motive and suction expansions, :math:`G(P)`, :math:`u(P)`, :math:`\rho(P)`.
  They are tabulated **once per Calculate** on a pressure grid, refined
  around the choke and the phase boundary. Monotone interpolation is used
  between grid points, and the choke is re-polished with real flashes. Every
  trial :math:`P_\mathrm{y}` then reads the tables without flashing.
* **Mixing and diffuser flashes** depend on :math:`P_\mathrm{y}` and cannot be
  tabulated in advance. Measured for Giacomelli case 1, one trial
  :math:`P_\mathrm{y}` costs 10–20 PH flashes for the mixing root and 9 flashes
  for the diffuser.

The two choke searches cost about 290 flashes together (154 motive, 134
suction for case 1), and a 100-point table per path about 200 more. The
per-trial cost of about 25 flashes therefore dominates. A rating visits about
65 trials: 40 scan points, about 15 bisection steps and about 10 Brent steps.
With the critical point bisected, the scan can drop to 10 points without
changing the result in the cases tested (:ref:`sec-rating-algorithm`). That
gives about 35 trials, or roughly 1 100 PME flashes per rating, about 1 s at
1 ms per flash. Further savings are possible, e.g. warm-starting the mixing
march from the neighbouring trial's root. They will be measured on the port.
The Python reference implementation gets the same tabulation first, so its
results can be checked against the un-tabulated ones.

Ports, parameters and lifecycle
===============================

The unit exposes three material ports: **Motive** and **Suction** (inlets) and
**Outlet**. It follows the same lifecycle as the membrane unit. ``Validate``
reads only the compound lists and checks that the parameters are consistent
with the selected specification mode. ``Calculate`` reads the inlet states,
runs the selected mode and writes the outlet by a PH flash at
:math:`(P_\mathrm{out}, h_\mathrm{out})`. Writing the outlet by enthalpy rather
than temperature preserves the exact energy balance across the
unit (:ref:`sec-diffuser`).

``Mode`` (Design / Rating), ``GeometrySpec`` (FixedShape / FixedSize) and
``EfficiencySet`` flip the geometry and efficiency parameters between CAPE-OPEN
``input`` and ``output`` (:ref:`sec-flowsheet-specs`), as the membrane unit's
``SpecMode`` does for area and stage cut. The pressures and velocities at the
stations are scalar outputs. The operating curve (outlet pressure against ER,
from the critical point to zero entrainment) is a pair of array outputs, and is
drawn in the HTML report together with the operating point. The parameter
list is in :ref:`appendix_capeopen`.

Errors map onto CAPE-OPEN exceptions with readable descriptions, also exposed
through ``ECapeUser`` on the unit. Examples are a suction flow above the
critical entrainment (the message gives the critical ER and the outlet pressure
where it is reached), a mixing section that cannot pass the flow, and a state
below the triple point. A motive capacity mismatch in ``FixedSize`` is reported
as an output, not raised.
