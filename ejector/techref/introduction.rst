.. _introduction:

============
Introduction
============

Scope
=====

This reference documents the **ejector unit operation**, a steady-state model of
an ejector (also called a jet pump, eductor or thermocompressor), intended as a
CAPE-OPEN unit operation for COFE and other CAPE-OPEN process modelling
environments (PMEs). A high-pressure **motive** stream is expanded through a
nozzle. Its momentum entrains and compresses a low-pressure **suction** stream,
and the mixture leaves at an intermediate pressure. The reference covers:

* the **one-dimensional real-fluid flow** relations on which the model rests:
  stagnation states, choking as a maximum of the mass flux, the homogeneous
  equilibrium model (HEM) for flashing flow, delayed flashing, and the
  solid-phase (dry-ice) limit (:ref:`flow_fundamentals`);
* the **component models**: motive and suction nozzles (:ref:`nozzles`), and
  constant-area mixing and diffuser (:ref:`mixing_diffuser`);
* the three **operating modes**: rating a known geometry, optimum design for a
  given entrainment ratio, and design for a required outlet pressure
  (:ref:`operating_modes`);
* the **numerical methods** and the planned **software architecture** that keeps
  the physics independent of the CAPE-OPEN plumbing (:ref:`solution_methods`,
  :ref:`architecture`);
* **verification and validation** against an analytic jet-pump model, the
  ideal-gas critical-pressure ratio, two gas-ejector and CO\ :sub:`2`
  data sets, and a **calibration** on 42 measured R744 points
  (:ref:`validation`, :ref:`data_library`); and
* a **worked example**: liquid and dense-phase CO\ :sub:`2` motive entraining
  CO\ :sub:`2` vapour (:ref:`worked_example`).

.. _sec-unit-operation:

The unit operation
==================

An ejector has two inlets and one outlet. The schematic below also defines the
stations of the one-dimensional model used throughout this reference.

.. only:: html

   .. figure:: figures/tikz_ejector.png
      :width: 100%
      :align: center

      Ejector schematic with the stations of the one-dimensional model:
      motive inlet m0, throat t and exit e; suction inlet s0 and mixing-section
      inlet y; end of mixing 3; diffuser outlet. Each component carries one
      loss parameter. The drawing shows the nozzle exit upstream of y; the model
      places the exit plane at the mixing inlet (the nozzle exit position is not
      modelled).

.. only:: latex

   .. raw:: latex

      \begin{figure}[htbp]
      \centering
      \resizebox{\linewidth}{!}{\input{ejector_body.tex}}
      \caption{Ejector schematic with the stations of the one-dimensional model:
      motive inlet m0, throat t and exit e; suction inlet s0 and mixing-section
      inlet y; end of mixing 3; diffuser outlet. Each component carries one
      loss parameter. The drawing shows the nozzle exit upstream of y; the model
      places the exit plane at the mixing inlet (the nozzle exit position is not
      modelled).}
      \end{figure}

The **motive nozzle** converts motive pressure into velocity. For a gas or a
flashing liquid it chokes at the throat and may expand supersonically beyond
it. The **suction** stream is drawn into the annulus around the nozzle exit and
accelerated to the mixing-section inlet pressure :math:`P_\mathrm{y}`, which is
below the suction pressure. In the **constant-area mixing section** the two
streams exchange momentum. The mixed stream may pass through a shock, and leaves
subsonic. The **diffuser** then recovers its kinetic energy as pressure.

One model has to cover very different duties:

.. list-table::
   :header-rows: 1
   :widths: 24 24 52

   * - Motive
     - Suction
     - Typical application
   * - gas / steam
     - gas / vapour
     - vacuum ejectors, thermocompressors, ejector refrigeration (e.g. R141b)
   * - liquid
     - liquid
     - jet pumps for artificial lift and transfer (incompressible limit)
   * - liquid
     - non-condensable gas
     - eductors; gas compression by a liquid jet
   * - flashing liquid or dense supercritical fluid
     - vapour of the same fluid
     - expansion-work recovery in CO\ :sub:`2` (R744) refrigeration; CO\ :sub:`2` handling

Representing every stream only by :math:`(P, h, \vec z)` lets one set of
balance equations cover all of these. All properties then come from flashes of
the same equation of state that the flowsheet uses.

Why a constant-area real-fluid 1D model
=======================================

Ejector models range from empirical correlations to CFD. CFD of two-phase CO\ :sub:`2`
ejectors resolves non-equilibrium and turbulent mixing, but needs minutes to hours
per point :cite:`ringstad2020,giacomelli2019`. That is far too slow for a unit
that a flowsheet solver calls many times per iteration. Correlation-based models
:cite:`muhammad2020` are fast, but valid only for the geometry and fluid they
were fitted to. The design target here sits between the two. The model must be:

i. **thermodynamically general**: any fluid or mixture, single- or two-phase,
   with all properties delegated to the PME's property package;
ii. **geometric**: able to *rate* a given geometry, not only to report an
    ideal performance, so that off-design operation, choking and backflow are
    predicted;
iii. **fast and robust**: of the order of a thousand flashes per solve
     (:ref:`sec-flash-cost`), only bracketed scalar root-finds, and no initial
     guesses from the PME; and
iv. **calibratable**: few, physically interpretable loss parameters that can be
    fitted to data.

The classical **one-dimensional ejector model** of Keenan *et al.*
:cite:`keenan1950` meets these requirements. Huang *et al.*
:cite:`huang1999` made it a predictive design and rating tool for gas ejectors.
Kornhauser :cite:`kornhauser1990` applied it to two-phase (flashing) refrigerant
ejectors. Liu & Groll :cite:`liu2013` and Banasiak & Hafner :cite:`banasiak2011`
developed it for CO\ :sub:`2` with real-fluid properties. Two mixing assumptions exist:

* **Constant-pressure mixing** :cite:`keenan1950,munday1977,huang1999`. The
  streams mix at the suction-choke pressure in a "hypothetical throat". Its
  area is not a geometric quantity and needs an empirical correlation for
  rating :cite:`ma2017`.
* **Constant-area mixing** :cite:`keenan1950`. The streams mix in
  the real cylindrical section of area :math:`A_\mathrm{mix}`, whose
  momentum balance closes with geometry alone.

The **constant-area** formulation is adopted here because it rates a measured
geometry without auxiliary correlations. It also reduces to the classical
incompressible jet-pump equations in the liquid limit (:ref:`sec-val-fluids`).
Four efficiencies (:math:`\eta_\mathrm{m}, \eta_\mathrm{s}, \eta_\mathrm{mix},
\eta_\mathrm{d}`) and a motive discharge coefficient :math:`C_d` absorb what
1D theory cannot resolve. These are friction, incomplete mixing, shock losses
and thermodynamic non-equilibrium.

.. _sec-limitations:

Assumptions and limitations
===========================

The current model assumes:

* **Steady, adiabatic, one-dimensional flow** with cross-section-averaged
  velocities. Wall friction is not modelled explicitly. It is absorbed into the
  efficiencies, so the model has **no size (Reynolds-number) effect**: a
  geometry scaled by a factor in diameter performs identically. Real ejectors
  do not, so scale-up beyond the calibration data needs care
  (:ref:`worked_example`).
* **Homogeneous equilibrium** two-phase flow by default (equal phase velocities
  and temperatures, phase equilibrium). Delayed flashing in the motive nozzle is
  available as a one-parameter homogeneous non-equilibrium (HNE) correction, or
  absorbed into :math:`C_d` (:ref:`sec-hne`).
* **Complete mixing** at the end of the constant-area section, with
  :math:`\eta_\mathrm{mix}` acting on the inlet momentum flux. The mixing length is
  assumed sufficient. Guidance for choosing it in design :cite:`flugel1939` is
  not yet part of the model.
* **Constant, user-supplied (or calibrated) efficiencies**. They do not vary with
  the operating point. This is the main limitation for extrapolation, as the
  calibration shows (:ref:`sec-val-giacomelli`).
* **No solid phase.** Pure fluids raise an error below the triple-point pressure,
  where dry ice would form for CO\ :sub:`2`. For mixtures, freeze-out must come
  from the PME (:ref:`sec-solid`).

These assumptions are those of the 1D models used for comparison
:cite:`huang1999,liu2013,banasiak2011`. They suit screening, conceptual design
and flowsheet simulation. Detailed design of a specific ejector still calls for
CFD or test data.
