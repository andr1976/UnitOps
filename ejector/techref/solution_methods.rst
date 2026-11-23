.. _solution_methods:

==========================
Numerical Solution Methods
==========================

This chapter documents the numerical schemes of the reference implementation.
As in the membrane unit, every scheme is chosen for **robustness inside a
flowsheet solver**: no initial guesses from the PME, bracketed roots, and
results that do not depend on discretisation choices. One deviation from that
last rule was found and fixed while this reference was written
(:ref:`sec-rating-algorithm`).

.. _sec-choke-search:

Choke search
============

:eq:`eq-critical-flux` is a one-dimensional maximisation over
:math:`P \in [P_\mathrm{low}, P_0)`, where
:math:`P_\mathrm{low} = \max(1.001\,P_\mathrm{tp},\, 0.01\min(P_\mathrm{m0}, P_\mathrm{s0}))`.
:math:`G(P)` is not smooth: it has a kink where the path enters the two-phase
region (:numref:`fig-nozzle-flux`). A derivative-based method could
therefore stall at the kink. The search instead:

#. evaluates :math:`G` on 60 **log-spaced** pressures, which resolves the
   low-pressure end where gases choke and the high-pressure end where liquids
   start to flash;
#. takes the largest finite value, skipping points where the flash failed;
#. refines with a **bounded Brent** search between the neighbours of the grid
   maximum, to a pressure tolerance of :math:`10^{-6}P_0`, and keeps the grid
   value if the refinement does not improve on it.

If the maximum lies at :math:`P_\mathrm{low}`, the flow is not choked within the
range and :math:`(P_\mathrm{low}, G(P_\mathrm{low}))` is returned. A
mixture flash may fail at very low pressure, and a path with no converged
point at all raises an error. For ideal-gas nitrogen the search returns
:math:`P^*/P_0 = 0.527` against 0.528 (:ref:`sec-val-ideal-gas`).

Motive exit pressure
====================

:eq:`eq-motive-exit` is solved by **Brent's method** on
:math:`[P_\mathrm{low}, P_\mathrm{t}]`. :math:`G` is monotone on the supersonic
branch, so a root, if it exists, is unique. If no root exists (over-expansion
beyond :math:`P_\mathrm{low}`, or :math:`A_\mathrm{e}/A_\mathrm{t} < C_d`; see
:ref:`sec-motive-nozzle`), :math:`P_\mathrm{e}` is clamped to
:math:`P_\mathrm{low}`. This clamp is flagged for review in
:ref:`sec-numerics-open`.

.. _sec-mixing-root:

Mixing root
===========

The subsonic root of :eq:`eq-mix-residual` is found by **marching down** from the
:math:`u_3 \to 0` end, :math:`P_3 = F_\mathrm{in}/A_\mathrm{mix}` (where
:math:`R = -1`), in geometric steps of 4 %:

* when :math:`R` turns positive, the root is bracketed between the last two
  steps and refined by Brent to :math:`10^{-6}P_3`. The first sign change met
  from above is the subsonic root;
* if :math:`R` starts to *decrease* before turning positive, the march has passed
  the maximum of :math:`R` without a root. The section is choked and the trial
  :math:`P_\mathrm{y}` is infeasible. The march stops at once, which saves the
  flashes of a fruitless descent;
* a failed flash, or a pressure below the triple point, also marks the trial
  infeasible.

Including the Brent refinement, a trial costs 10–20 PH flashes. Infeasible
trials are at the low end, because the march stops at the maximum of :math:`R`.

.. _sec-rating-algorithm:

Rating algorithm
================

#. Compute the motive choke :math:`(P_\mathrm{t}, G^*)` and the suction choke
   :math:`(P_\mathrm{y}^*, G_\mathrm{s}^*)` (:ref:`sec-choke-search`).
#. Evaluate :math:`P_\mathrm{out}^\mathrm{calc}(P_\mathrm{y})` on
   ``n_scan`` (default 40) points spaced linearly from :math:`P_\mathrm{y}^*`
   to :math:`P_\mathrm{s0}`. Trial points where the mixing section is choked
   are infeasible.
#. **Locate the critical point.** The first feasible grid point is the most
   entraining one on the grid. If infeasible points lie below it, the true
   feasibility boundary (mixed flow choked at station 3) lies between it and its
   infeasible neighbour, and is found by **bisection** to
   :math:`10^{-6}P_\mathrm{s0}`. Its outlet pressure is the critical back
   pressure :math:`P_\mathrm{b}^*`.
#. If :math:`P_\mathrm{out} \le P_\mathrm{b}^*`: **critical**, return the
   boundary state.
#. Otherwise find a grid interval where :math:`P_\mathrm{out}^\mathrm{calc} - P_\mathrm{out}`
   changes sign, and solve for :math:`P_\mathrm{y}` by Brent to
   :math:`10^{-6}P_\mathrm{s0}`: **subcritical**.
#. If no interval brackets the specification: **backflow**,
   :math:`\dot m_\mathrm{s} = 0`.

.. admonition:: Fixed: critical entrainment depended on the scan resolution

   Before step 3 was added (November 2026), a critical-regime result returned
   the first feasible *grid* point. The critical ER then crept up with
   ``n_scan``. For Giacomelli case 1 it was 0.192 at 15 points, 0.206 at 25,
   0.213 at 40 and 0.216 at 320, an error of up to 10 %, while subcritical
   results were already converged. With the bisection, the critical ER is
   0.21598 for every ``n_scan`` from 10 to 320. The step also removed a staircase
   from the calibration objective. That staircase had moved the fitted
   efficiencies by up to 0.16 (:math:`\eta_\mathrm{d}` 0.64 → 0.80) at
   practically the same goodness of fit, which exposed how weakly the data
   separate :math:`\eta_\mathrm{mix}` and :math:`\eta_\mathrm{d}`
   (:ref:`sec-val-giacomelli`).

Optimum design
==============

:math:`P_\mathrm{out}(P_\mathrm{y})` is evaluated on 30 points between
:math:`\max(P_\mathrm{y}^*, P_\mathrm{low})` and :math:`0.995\,P_\mathrm{s0}`.
Infeasible points count as zero outlet pressure. A bounded Brent maximisation
between the neighbours of the best point follows, to :math:`10^{-5}P_\mathrm{s0}`.
For the worked example the result agrees with a 400-point brute-force scan
to :math:`10^{-4}` bar at ER = 0.05, 0.146 and 0.4. The optimum is interior and
smooth, so the scan resolution does not limit design mode.

Design for lift
===============

The largest ER that reaches the required :math:`P_\mathrm{out}` is bracketed by
doubling from ER = 0.1 (after checking that ER = :math:`10^{-4}` reaches it)
and then solved by Brent to :math:`10^{-5}` in ER. Each evaluation is a full
optimum design, which is why this mode is the most expensive one
(:ref:`sec-flash-cost`).

Diffuser
========

:eq:`eq-diffuser` is solved by Brent on
:math:`[P_3, P_\mathrm{hi}]`, starting from :math:`P_\mathrm{hi} = 1.5\,P_3` and
growing it by a factor of 1.5 until it brackets the root. The left-hand side
rises monotonically with pressure along an isentrope, so the root is unique.

.. _sec-numerics-open:

Open numerical items for the port
=================================

#. **Motive exit clamp.** Replace the :math:`P_\mathrm{low}` clamp of
   :eq:`eq-motive-exit` with an explicit decision: either apply :math:`C_d`
   along the whole nozzle (then the clamp only signals real over-expansion),
   or raise a geometry error when :math:`A_\mathrm{e}/A_\mathrm{t} < C_d`.
#. **Tabulated paths** (:ref:`sec-flash-cost`) with a regression test against
   the un-tabulated reference implementation.
#. **Inverse rating** for flowsheet use: given :math:`\dot m_\mathrm{s}`, find
   :math:`P_\mathrm{y}` on the subcritical branch (:ref:`sec-flowsheet-specs`).
   :math:`\dot m_\mathrm{s}(P_\mathrm{y})` is monotone there, so Brent applies.
#. **Reverse flow** through the suction port is not modelled. Backflow is reported,
   not solved.
