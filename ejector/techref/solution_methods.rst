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
  the maximum of :math:`R` on the grid. The maximum is refined by a bounded Brent
  search between the last three grid points. If it is positive, the subsonic
  root lies between it and the higher grid point and is refined by Brent. Only
  if it is not positive is the section choked and the trial :math:`P_\mathrm{y}`
  infeasible. Without the refinement a narrow positive peak between two 4 %
  steps was missed: that moved the feasibility boundary and raised
  :math:`P_\mathrm{b}^*` by up to 0.14 bar for critical Giacomelli points;
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
#. If :math:`P_\mathrm{out} \le P_\mathrm{b}^*`: **critical**. The flows and
   internal states are those of the boundary. The excess lift is lost in shocks
   downstream, so the outlet stream is the mixed stagnation enthalpy at the
   specified :math:`P_\mathrm{out}`, and :math:`\eta_\mathrm{ej}` is evaluated
   there. :math:`P_\mathrm{b}^*` and the critical ER are reported separately.
#. Otherwise find a grid interval where :math:`P_\mathrm{out}^\mathrm{calc} - P_\mathrm{out}`
   changes sign, and solve for :math:`P_\mathrm{y}` by Brent to
   :math:`10^{-6}P_\mathrm{s0}`: **subcritical**. If the converged outlet
   pressure misses the specification by more than :math:`10^{-4}` (relative),
   Brent has converged onto a step of the characteristic (a motive-nozzle
   regime change, :ref:`sec-unchoked-motive`), and the rating raises an error
   that names the step instead of returning a different pressure.
#. If no interval brackets the specification: **backflow**,
   :math:`\dot m_\mathrm{s} = 0`, with the outlet equal to the motive stream at
   the specified :math:`P_\mathrm{out}` and :math:`\eta_\mathrm{ej} = 0`.

.. admonition:: Fixed: critical entrainment depended on the scan resolution

   Before step 3 was added (November 2026), a critical-regime result returned
   the first feasible *grid* point. The numbers below are for the model
   version of that date (losses up to the throat, :math:`C_d = 1.205`). The critical ER then crept up with
   ``n_scan``. For Giacomelli case 1 it was 0.192 at 15 points, 0.206 at 25,
   0.213 at 40 and 0.216 at 320, an error of up to 10 %, while subcritical
   results were already converged. With the bisection, the critical ER is
   0.21598 for every ``n_scan`` from 10 to 320. The step also removed a staircase
   from the calibration objective. That staircase had moved the fitted
   efficiencies by up to 0.16 (:math:`\eta_\mathrm{d}` 0.64 → 0.80) at
   practically the same goodness of fit, which exposed how weakly the data
   separate :math:`\eta_\mathrm{mix}` and :math:`\eta_\mathrm{d}`
   (:ref:`sec-val-giacomelli`).

.. _sec-inverse-rating:

Inverse rating (both flows given)
=================================

In a flowsheet both inlet flows are fixed and the outlet pressure is the
unknown (:ref:`sec-flowsheet-specs`). The geometry enters as its **shape**,
:math:`r_\mathrm{e} = A_\mathrm{e}/A_\mathrm{t}` and
:math:`r_\mathrm{mix} = A_\mathrm{mix}/A_\mathrm{t}`. For a trial
:math:`P_\mathrm{y}` the throat is sized to pass the motive flow,

.. math::
   :label: eq-throat-sizing

   A_\mathrm{t}(P_\mathrm{y}) = \frac{\dot m_\mathrm{m}}{C_d\,G_\mathrm{t}(P_\mathrm{y})},
   \qquad
   G_\mathrm{t} = \begin{cases} G^*, & P_\mathrm{y} < P_\mathrm{t},\\
   G(P_\mathrm{y}), & P_\mathrm{y} \ge P_\mathrm{t}, \end{cases}

and :math:`A_\mathrm{e} = r_\mathrm{e}A_\mathrm{t}`, :math:`A_\mathrm{mix} = r_\mathrm{mix}A_\mathrm{t}`.
The pass of the rating algorithm then gives :math:`\mathrm{ER}(P_\mathrm{y})` and
:math:`P_\mathrm{out}(P_\mathrm{y})`. For a choked motive the throat is the same at
every trial; for an unchoked (liquid) motive it follows :math:`P_\mathrm{y}`,
which keeps the motive flow equal to the port flow.

#. Scan and bisect exactly as in :ref:`sec-rating-algorithm` to find the
   critical point: :math:`\mathrm{ER}^*` and :math:`P_\mathrm{b}^*`.
#. If :math:`\mathrm{ER} > \mathrm{ER}^*(1+10^{-6})`: **no solution**; the error
   reports :math:`\mathrm{ER}^*` and :math:`P_\mathrm{b}^*`.
#. If :math:`\mathrm{ER} \ge \mathrm{ER}^*(1-10^{-6})`: **critical**, with
   :math:`P_\mathrm{out} = P_\mathrm{b}^*`.
#. Otherwise append the zero-entrainment point (:math:`P_\mathrm{y} = P_\mathrm{s0}`,
   ER = 0) to the scan, find the grid interval where
   :math:`\mathrm{ER}(P_\mathrm{y}) - \mathrm{ER}` changes sign, and solve for
   :math:`P_\mathrm{y}` by Brent to :math:`10^{-7}P_\mathrm{s0}`: **subcritical**.

The scan points between the critical point and zero entrainment are the
**operating curve** of the sized geometry at these inlet states. It comes at no
extra cost, or re-sampled on a chosen number of points. Rating a geometry at an
outlet pressure and then inverse-rating it with the resulting flows returns
the same outlet pressure to :math:`10^{-6}` and the same throat to
:math:`10^{-9}`, for choked CO\ :sub:`2` and for the unchoked water jet pump
(``test_rate_flows_*`` in the prototype's test suite).

.. _sec-solution-design:

Optimum design
==============

:math:`P_\mathrm{out}(P_\mathrm{y})` is evaluated on 30 points between
:math:`\max(P_\mathrm{y}^*, P_\mathrm{low})` and :math:`0.995\,P_\mathrm{s0}`,
plus 10 points log-spaced in :math:`P_\mathrm{s0} - P_\mathrm{y}` between
:math:`0.996` and :math:`0.9999\,P_\mathrm{s0}`. Infeasible points count as zero
outlet pressure. A bounded Brent maximisation between the neighbours of the
best point follows, to :math:`10^{-5}P_\mathrm{s0}`. For the worked example the
result agrees with a 400-point brute-force scan to :math:`10^{-4}` bar at
ER = 0.05, 0.146 and 0.4. The cluster near :math:`P_\mathrm{s0}` matters at very
small ER, where the feasible window is a narrow band just below the suction
pressure. Without it, designs at ER of the order of 0.01 were declared
infeasible although they reach a higher outlet pressure than any larger ER.

Design for lift
===============

:math:`P_\mathrm{out}^\mathrm{opt}(\mathrm{ER})` is not monotone at small ER
(:ref:`sec-design`). It is evaluated on a fixed ER grid
(:math:`10^{-4}` to 20, roughly doubling), its maximum is refined by a bounded
Brent search, and the requirement is declared unreachable only if it exceeds
that maximum; the message gives the maximum and the ER where it occurs.
Otherwise the largest ER that reaches the requirement is the root on the
falling branch, solved by Brent to :math:`10^{-5}` in ER. Each evaluation is a
full optimum design, which is why this mode is the most expensive one
(:ref:`sec-flash-cost`).

Diffuser
========

:eq:`eq-diffuser` is solved by Brent on
:math:`[P_3, P_\mathrm{hi}]`, starting from :math:`P_\mathrm{hi} = 1.5\,P_3` and
growing it by a factor of 1.5 until it brackets the root. The left-hand side
rises monotonically with pressure along an isentrope, so the root is unique.

.. _sec-numerics-open:

Open numerical items
====================

#. **Where** :math:`C_d` **acts.** :math:`C_d` acts at the throat only, so
   :math:`1 < A_\mathrm{e}/A_\mathrm{t} < C_d` is now rejected with an error.
   An earlier version clamped the exit pressure silently. A uniform
   :math:`C_d` along the nozzle would accept those nozzles, but it would
   change the design results and the calibration, so the choice is left open.
#. **Step at the shock-at-exit pressure.** :math:`\eta_\mathrm{mix}` acts on the
   momentum flux only, so the switch between the supersonic and the post-shock
   exit state is not exactly continuous (:ref:`sec-unchoked-motive`). Applying
   the mixing loss to the impulse function would remove the step, but it
   would change the calibrated meaning of :math:`\eta_\mathrm{mix}`.
#. **Tabulated paths** (:ref:`sec-flash-cost`) with a regression test against
   the un-tabulated reference implementation.
#. **Reverse flow** through the suction port is not modelled. Backflow is reported,
   not solved.
