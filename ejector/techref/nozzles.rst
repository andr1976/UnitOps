.. _nozzles:

==========================
Motive and Suction Nozzles
==========================

Both inlet streams are accelerated along the expansion path of
:ref:`sec-expansion-path`. The motive stream passes a converging–diverging
nozzle of throat area :math:`A_\mathrm{t}` and exit area :math:`A_\mathrm{e}`.
The suction stream is drawn through the annulus of area
:math:`A_\mathrm{sy} = A_\mathrm{mix} - A_\mathrm{e}` between the nozzle lip and
the mixing-section wall. Both arrive at the mixing-section inlet. The static
pressure there, :math:`P_\mathrm{y}`, couples the two streams and is the
central unknown of the model (:ref:`operating_modes`).

.. _sec-motive-nozzle:

Motive nozzle
=============

Choked motive flow
------------------

The motive expansion uses :eq:`eq-expansion-path` with :math:`\eta = \eta_\mathrm{m}`
from the stagnation state m0. Its critical flux :math:`G^*` and throat pressure
:math:`P_\mathrm{t}` follow from :eq:`eq-critical-flux`. When the mixing-inlet
pressure lies below the throat pressure, :math:`P_\mathrm{y} < P_\mathrm{t}`, the
nozzle is choked and passes

.. math::
   :label: eq-motive-choked

   \dot m_\mathrm{m} = C_d\,G^*\,A_\mathrm{t} ,

independent of anything downstream. The discharge coefficient :math:`C_d`
(default 1) corrects the HEM critical flux for non-equilibrium and
non-uniform flow (:ref:`sec-hne`). :numref:`tab-motive-critical` lists the
throat states of the CO\ :sub:`2` motive conditions used in this reference.

.. _tab-motive-critical:

.. list-table:: Critical (throat) state of CO\ :sub:`2` motive nozzles, :math:`\eta_\mathrm{m} = 0.9`, :math:`C_d = 1`. Source: ``figures/data/nozzle_critical.csv``.
   :header-rows: 1
   :widths: 40 20 20 20

   * - Motive inlet
     - :math:`T_0` [°C]
     - :math:`P_\mathrm{t}` [bar]
     - :math:`G^*` [t m\ :sup:`-2` s\ :sup:`-1`]
   * - 47 bar, saturated liquid, HEM
     - 11.7
     - 34.2
     - 27.0
   * - 47 bar, saturated liquid, HNE :math:`\varphi = 0.5`
     - 11.7
     - 28.1
     - 38.9
   * - 84.5 bar, 32 °C, HEM
     - 32.0
     - 64.3
     - 43.9

Supersonic exit
---------------

Downstream of a choked throat the flow continues on the supersonic branch of
:math:`G(P)` until the exit area is filled. The exit pressure solves

.. math::
   :label: eq-motive-exit

   G(P_\mathrm{e})\,A_\mathrm{e} = \dot m_\mathrm{m}, \qquad P_\mathrm{e} < P_\mathrm{t} ,

and the exit velocity :math:`u_\mathrm{e}` follows from :eq:`eq-velocity`. For
:math:`A_\mathrm{e} = A_\mathrm{t}` (a converging nozzle), :math:`P_\mathrm{e} = P_\mathrm{t}`.
:math:`P_\mathrm{e}` generally differs from :math:`P_\mathrm{y}`: the nozzle is
under- or over-expanded. The pressure mismatch enters the mixing momentum
balance as the force :math:`P_\mathrm{e}A_\mathrm{e}` (:ref:`sec-mixing`).

.. admonition:: Where the discharge coefficient acts

   :eq:`eq-motive-exit` uses the *HEM* flux :math:`G(P)` without :math:`C_d`.
   :math:`C_d` is a throat correction: the fluid is assumed to relax to
   equilibrium in the diverging section, where the residence time is longer. A
   consequence is that the HEM exit can pass the corrected flow only if
   :math:`A_\mathrm{e}/A_\mathrm{t} \ge C_d`. For the Giacomelli nozzle
   (:math:`A_\mathrm{e}/A_\mathrm{t} = 1.31`, :math:`C_d = 1.20`) this holds. If it
   does not, the reference implementation clamps :math:`P_\mathrm{e}` at the lowest
   pressure of the search range, which is not physical. Before the C# port,
   choose between this throat-only interpretation and a uniform
   :math:`C_d` along the nozzle (:ref:`sec-numerics-open`).

.. _sec-unchoked-motive:

Unchoked motive flow
--------------------

If :math:`P_\mathrm{y} \ge P_\mathrm{t}` the motive nozzle does not choke. This
is the normal state of a liquid jet pump whose motive does not flash. The nozzle
is then treated as converging and exhausts at the mixing-inlet pressure:

.. math::
   :label: eq-motive-unchoked

   \dot m_\mathrm{m} = C_d\,G(P_\mathrm{y})\,A_\mathrm{t}, \qquad P_\mathrm{e} = P_\mathrm{y} .

The motive flow now depends on :math:`P_\mathrm{y}`, and so on the back pressure.
This is the coupling that makes the incompressible jet pump's motive flow
respond to its discharge pressure. :ref:`sec-val-fluids` verifies it against the
classical jet-pump equations.

.. _sec-suction:

Suction acceleration
====================

The suction stream expands from its stagnation state s0 to :math:`P_\mathrm{y}`
with :math:`\eta = \eta_\mathrm{s}`. Its own choke search :eq:`eq-critical-flux`
gives the largest flux the annulus can pass, :math:`G_\mathrm{s}^*`, at the
suction choke pressure :math:`P_\mathrm{y}^*`. The suction flow is

.. math::
   :label: eq-suction-flow

   \dot m_\mathrm{s} = \min\bigl(G_\mathrm{s}(P_\mathrm{y}),\,G_\mathrm{s}^*\bigr)\,A_\mathrm{sy},
   \qquad A_\mathrm{sy} = A_\mathrm{mix} - A_\mathrm{e} .

Gas and vapour suction choke at their sonic point. A **liquid** suction has no
sonic point before it flashes, so its "choke" lies at the saturation pressure.
:eq:`eq-suction-flow` then expresses the **cavitation limit** of a jet pump:
lowering :math:`P_\mathrm{y}` below :math:`P_\mathrm{sat}` cannot draw more
liquid. The test suite checks this limit for water at 20 °C, where the
suction-side pressure settles at :math:`P_\mathrm{sat} = 2.34` kPa
(:ref:`sec-val-suite`).

:math:`\eta_\mathrm{s}` lumps the losses of the suction chamber and of the
entrainment process. In calibrations it is the least well determined of the
efficiencies (:ref:`sec-val-giacomelli`).

Summary of the nozzle relations
===============================

For a given :math:`P_\mathrm{y}`, the nozzle relations deliver the inlet
conditions of the mixing section:

.. list-table::
   :header-rows: 1
   :widths: 30 35 35

   * - Quantity
     - Motive (choked / unchoked)
     - Suction
   * - mass flow
     - :eq:`eq-motive-choked` / :eq:`eq-motive-unchoked`
     - :eq:`eq-suction-flow`
   * - static pressure at y
     - :math:`P_\mathrm{e}` from :eq:`eq-motive-exit` / :math:`P_\mathrm{y}`
     - :math:`P_\mathrm{y}`
   * - velocity
     - :math:`u_\mathrm{e}` from :eq:`eq-velocity`
     - :math:`u_\mathrm{y}` from :eq:`eq-velocity`
   * - flow area
     - :math:`A_\mathrm{e}`
     - :math:`A_\mathrm{sy}`
