.. _architecture:

=====================
Software Architecture
=====================

Design principle: separate the physics from the plumbing
=========================================================

The code is split into two assemblies with a strict dependency direction:

.. only:: html

   .. figure:: figures/tikz_arch.png
      :width: 100%
      :align: center

.. only:: latex

   .. raw:: latex

      \begin{center}
      \input{arch_body.tex}
      \end{center}

* **MembraneCore** contains *only* the numerical models. It has no COM, no
  CAPE-OPEN, and no thermodynamics of its own; where it needs a fluid property
  (enthalpy, for the energy balance) it declares a narrow *port* interface
  (:class:`IEnthalpyProvider`) that the caller supplies. This lets the entire
  physics be unit-tested headlessly on modern .NET with synthetic inputs
  (:ref:`validation`).
* **Membrane.CapeOpen** is the adapter: a .NET Framework 4.8, x64,
  COM-visible assembly that implements the CAPE-OPEN interfaces, translates the
  PME's Material Objects to and from the core's plain-array API, and delegates
  all thermodynamics to the PME.

The adapter depends on the core; the core never depends on the adapter. This is
the same hexagonal / ports-and-adapters arrangement used in the sibling
thermodynamic packages, and it is what makes the model portable to a non-CAPE-OPEN
host (a CLI, a test harness, a different flowsheet API) by writing a new adapter
only.

CAPE-OPEN interfaces implemented
================================

The unit is a CAPE-OPEN **Unit Operation** :cite:`cocolan_uo`. The primary
class :class:`MembraneUnitOperation` implements:

.. list-table::
   :header-rows: 1
   :widths: 34 66

   * - Interface
     - Role
   * - ``ICapeUnit``
     - ``ports``, ``Validate``, ``Calculate``, ``ValStatus`` --- the solve lifecycle.
   * - ``ICapeIdentification``
     - Component name and description shown in the PME palette.
   * - ``ICapeUtilities``
     - ``Initialize``/``Terminate``, ``Edit``, the parameter collection, and the simulation context.
   * - ``ICapeUnitReport``
     - Named reports; produces the text/HTML summary (:ref:`sec-report`).
   * - ``IPersistStreamInit``
     - Save/Load of the unit's state into the flowsheet file (:ref:`sec-persistence`).
   * - ``ECapeRoot`` / ``ECapeUser``
     - Readable error name/description so the PME never reports "failed to get error name".

Ports and the solve lifecycle
=============================

Three material ports are exposed: **Feed** (inlet), **Retentate** and
**Permeate** (outlets), each a :class:`MaterialPort` in the ports collection.
The PME drives the standard lifecycle:

#. **Connect** a Material Object to each port.
#. **Validate** --- checks all ports are connected, discovers the feed's
   compound list, ensures a permeance parameter exists per compound, and checks
   the operating parameters. Validate reads *only* the compound list, never
   stream state (T/P/composition), because that state is undefined until the
   flowsheet is solved; reading it in ``Validate`` would raise ``ECapeUnknown``.
#. **Calculate** --- reads the feed state, solves the selected model, and writes
   + flashes the two product streams.

Compound discovery and per-compound permeances
==============================================

The set of components is not known until a feed is connected, so the permeance
parameters are created **dynamically**. On the first ``Validate`` (or ``Edit``,
or ``Calculate`` on a fresh instance) the adapter reads the feed's compound list
and adds one ``Permeance_<id>`` real parameter per compound to the parameter
collection, seeding it from any persisted value. This is why the parameter grid
is initially sparse and populates once the feed is wired.

Thermodynamic delegation
========================

All thermodynamics is delegated to the PME through :class:`MaterialObjectAdapter`.
COFE (and CAPE-OPEN 1.1 PMEs generally) present **CO 1.1** Material Objects
(``ICapeThermoMaterial``); the adapter prefers the 1.1 path and falls back to
CO 1.0 (``ICapeThermoMaterialObject``) if only that is offered:

* **Read feed** (at ``Calculate`` only): ``GetOverallTPFraction`` for
  :math:`T,p,\vec z` and ``GetOverallProp("totalFlow")`` for the molar flow.
* **Write outlet**: ``ClearAllProps`` → ``SetOverallProp`` for
  temperature/pressure/composition/total flow → ``CalcEquilibrium`` as a
  temperature--pressure flash. The flash is treated as **non-fatal**: if the
  property package declines it, the overall state is already set and the PME
  flashes the stream itself during the flowsheet solve.
* **Enthalpy** (non-isothermal mode): see :ref:`sec-enthalpy-provider`.

The CAPE-OPEN 1.1 interop exposes ``[out]`` arguments as ``ref`` parameters, so
every read pre-declares a local and passes it by reference.

.. _sec-persistence:

Persistence and the solve-instance hop
======================================

COFE performs a whole-flowsheet solve in a **separate worker instance** of the
unit, distinct from the instance shown in the GUI. Output parameters set during
``Calculate`` on the worker are therefore *not* visible in the displayed
instance unless they are persisted and reloaded. Two measures close this gap:

#. :class:`IPersistStreamInit` ``Save``/``Load`` serialise the full state ---
   operating parameters, per-compound permeances, the computed stage cut, and
   every position profile --- with an integer **version header** so older
   flowsheet files load into newer builds. The format has grown monotonically:
   v2 added the stage cut, v3 the profiles, v4 the collected-permeate profile,
   v5 the spec mode, v6 the outlet temperatures and temperature profiles.
#. ``IsDirty`` always reports **dirty**. Parameter values edited in the PME grid
   update the parameter objects directly without notifying the unit, so a
   change-tracking flag would miss them and a fresh solve instance would load
   stale or zero values. Reporting always-dirty forces the PME to persist the
   current configuration every time.

.. _sec-spec-mode:

Rating vs. design: the spec-mode toggle
=======================================

A ``SpecMode`` option parameter selects which variable is specified:

.. list-table::
   :header-rows: 1
   :widths: 20 40 40

   * - ``SpecMode``
     - ``MembraneArea``
     - ``StageCut``
   * - ``Area`` (default, *rating*)
     - **input** --- you set it
     - **output** --- computed :math:`\theta`
   * - ``StageCut`` (*design*)
     - **output** --- required area, back-solved
     - **input** --- target :math:`\theta`

The toggle works by flipping each parameter's CAPE-OPEN **mode**
(``CAPE_INPUT`` / ``CAPE_OUTPUT``) so the PME greys out the computed one. In
design mode ``Calculate`` root-finds the area that hits the target stage cut
(:ref:`sec-required-area`) and writes it into ``MembraneArea``. The mode change
is applied in ``Validate``, ``Edit``, and on ``Load`` so the grid reflects it.

.. _sec-profiles:

Position profiles as plottable array parameters
================================================

To let COFE's plotting utility draw concentration and temperature profiles
(as it does for the plug-flow reactor), the unit exposes the profiles as
**array output parameters** sampled at 100 points: ``Profile_Position``,
``Profile_StageCut``, ``Profile_Retentate_<id>``, ``Profile_Permeate_<id>``,
``Profile_PermeateCollected_<id>`` and, in non-isothermal mode, the temperature
profiles.

Array parameters have a specific COM contract that COFE requires (from the
Parameter Common Interface and its errata :cite:`cocolan_param,cocolan_param_errata`):

* the parameter ``Type`` is ``CAPE_ARRAY``;
* the ``value`` is a ``SAFEARRAY(VARIANT)`` --- in .NET an ``object[]`` of boxed
  doubles, **not** a ``double[]`` (which marshals as ``SAFEARRAY(R8)`` and COFE
  rejects); and
* ``ItemsSpecifications`` returns a per-element array of real-parameter specs.

Getting this contract exactly right is what makes the profiles appear (and
plot) rather than showing ``<invalid value>`` in the grid.

.. _sec-report:

Reporting
=========

``ProduceReport`` emits a brand-styled **HTML** report with an inline SVG
profile chart when the PME advertises HTML support (via the
``HTMLReportSupport`` named value on the simulation context), and a plain-text
summary otherwise. Both list the operating point, the per-compound
feed/retentate/permeate/recovery table, and a sampled position profile.

Registration and assembly resolution
====================================

The unit registers per-user (``HKCU``) via ``register-user.ps1`` --- no
administrator rights required --- writing the CLSID, the ``InprocServer32``
pointing ``mscoree.dll`` at the assembly with its ``CodeBase``, and the
CAPE-OPEN Unit Operation category. A static ``AssemblyResolver`` hooks
``AppDomain.AssemblyResolve`` so the sibling ``MembraneCore.dll`` resolves when
the unit is activated inside the PME's process.
