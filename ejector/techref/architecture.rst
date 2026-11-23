.. _architecture:

=====================
Software Architecture
=====================

.. admonition:: Planned, not yet built

   This chapter specifies the CAPE-OPEN implementation. Today the model exists
   as the Python reference implementation ``ejector/prototype/ejector1d.py``.
   It is the source of every number in this reference, and the regression
   baseline for the port.

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
     - saturated inlets (testing, design studies)
     - ``CalcEquilibrium`` with pressure + vapour phase fraction
   * - ``M(z)``
     - mass/molar conversion, :eq:`eq-mix-composition`
     - compound molar weights from the Material Object

Each call returns :math:`T`, :math:`h`, :math:`s`, the overall density and,
when two phases are present, the phase densities and the vapour mass fraction.
The adapter reads these as overall and per-phase properties after the flash.
PMEs work in molar units, so enthalpies and entropies are converted with
:math:`M(\vec z)` at the boundary.

Two robustness rules carry over from the reference implementation:

* **PS fallback.** Not every property package supports an entropy
  specification. If ``CalcEquilibrium`` declines it, the adapter solves
  :math:`s(P, T) = s` by a bracketed secant on :math:`T` with TP flashes.
* **Failed flashes are data, not crashes.** Far below the operating range a
  flash may fail to converge, as CoolProp mixture flashes do at a fraction of a
  bar. The choke search treats such points as missing
  (:ref:`sec-choke-search`). Only failures inside the converged solution are
  reported as CAPE-OPEN errors.

The **solid check** moves to the PME as well. When the property package
supports a solid phase, the adapter inspects the phases present after each
flash on the solution path. Otherwise it applies the pure-component
triple-point test of :ref:`sec-solid` to the dominant component and reports
the margin.

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
     - 2 410
     - 28 ms
   * - rating, subcritical regime
     - 2 665
     - 41 ms
   * - optimum design (worked example)
     - 1 419
     - 10 ms
   * - design for lift
     - 15 758
     - 120 ms
   * - optimum design, CO\ :sub:`2` + N\ :sub:`2` mixture
     - 1 431
     - 35 s (25 ms per flash)

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

The specification mode (:ref:`sec-flowsheet-specs`) flips each geometry and
pressure parameter between CAPE-OPEN ``input`` and ``output``, as the
membrane unit's ``SpecMode`` does for area and stage cut. The internal station
states (:math:`P, T, x, u` at m0, t, e, s0, y, 3, out) are exposed as array
output parameters for plotting and for the report. The parameter list is in
:ref:`appendix_capeopen`.

Errors map onto CAPE-OPEN exceptions with readable descriptions:

* an **infeasible duty**, either outside the zero-entrainment limit or with the
  mixing section unable to pass the flow;
* a **solid-phase risk** (:math:`\sigma \le 1`);
* a **motive capacity mismatch** in rating mode.

Each message gives the quantity that would make the duty feasible, such as the
reachable :math:`P_\mathrm{out}`, the critical ER or the required throat
diameter, so the user can act on it.
