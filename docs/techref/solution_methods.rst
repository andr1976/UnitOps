.. _solution_methods:

==========================
Numerical Solution Methods
==========================

This chapter details the numerical schemes behind the models of Part II. Every
scheme is chosen for **robustness inside a flowsheet solver**: no user-supplied
initial guesses, no singularities at the operating limits, and deterministic
convergence.

.. _sec-local-permeate-solver:

The local permeate solver (singularity-free)
============================================

At every integration step the local permeate composition must be found from the
implicit set :eq:`eq-yi-reduced`. A naive fixed-point iteration
:math:`y_i \leftarrow S_i(x_i-\gamma y_i)/\sum_k S_k(x_k-\gamma y_k)` is slow and
becomes ill-conditioned as :math:`\gamma \to 0` (large permeate pull), where the
denominator collapses. The implementation instead reformulates the problem as a
**single scalar root-find** with no singularity.

Introduce the scalar :math:`t = \sum_k S_k(x_k - \gamma y_k)` (the reduced total
flux, the common denominator of :eq:`eq-yi-reduced`). Then each permeate mole
fraction is *explicit* in :math:`t`,

.. math::
   :label: eq-yk-t

   y_k \;=\; \frac{S_k\,x_k}{t + S_k\,\gamma} ,

and the closure :math:`\sum_k y_k = 1` becomes a single equation in :math:`t`,

.. math::
   :label: eq-Ht

   H(t) \;=\; \sum_k \frac{S_k\,x_k}{t + S_k\,\gamma} - 1 \;=\; 0 .

Because :math:`S_k, x_k, \gamma \ge 0` and :math:`t \ge 0`, every denominator is
strictly positive --- **the singularity is gone**. :math:`H(t)` is strictly
decreasing, with :math:`H(0^+) > 0` (for any non-zero feed) and
:math:`H(\sum_k S_k x_k) < 0`, so the root is unique and bracketed in
:math:`\bigl(0,\ \sum_k S_k x_k\bigr)`. It is solved by **bisection with a Newton
acceleration** (:math:`H'(t) = -\sum_k S_k x_k/(t+S_k\gamma)^2` is available in
closed form), giving quadratic convergence with a guaranteed-bracket fallback.
The permeate mole fractions then follow explicitly from :eq:`eq-yk-t`. This
solver is exercised by 16 dedicated unit tests spanning binary and
multicomponent feeds and the :math:`\gamma\to0` and high-selectivity limits.

.. _sec-crossflow-integration:

Cross-flow integration
=======================

The cross-flow composition ODE :eq:`eq-cf-composition` is marched in the
retentate flow :math:`R` from :math:`F` downward with a fixed-step
**fourth-order Runge--Kutta** integrator (default 4000 steps), evaluating the
local permeate solver at each stage. Two quantities are accumulated alongside:

* the **area** by the quadrature :eq:`eq-cf-area-quad`,
  :math:`a \mathrel{+}= \Delta R / J_\mathrm{tot}(x)`, using the local total flux
  :eq:`eq-total-flux`; and
* the profile samples (:ref:`sec-profiles`), recorded at 100 evenly spaced
  positions.

``SolveByStageCut`` marches until the target cut :math:`\theta` is reached;
``SolveByArea`` marches until the accumulated area reaches the specified
:math:`A`, interpolating the final partial step. The 4000-step default
reproduces the Shindo benchmark to :math:`\sim2\times10^{-4}`
(:ref:`sec-val-shindo`); the step count is a compile-time constant that trades
runtime for accuracy and is far below the point of diminishing returns.

.. _sec-counter-bvp:

Counter-current boundary-value problem
======================================

Counter-current (:eq:`eq-fp-perm`) is a two-point BVP: the permeate flow is zero
at the retentate outlet (:math:`a=A`) but the permeate composition at the feed
end is unknown. It is solved by an **iterative cell march**: the module is
discretised into cells; the permeate composition field is initialised (from a
co-current or cross-flow pass) and the retentate and permeate profiles are swept
repeatedly --- retentate feed-end→outlet, permeate outlet→feed-end --- updating
the coupled bulk compositions until the profiles stop changing to a set
tolerance. Co-current, by contrast, is a pure initial-value problem and needs a
single forward RK sweep. Both are validated against the Shindo counter-current
table (:ref:`sec-val-shindo`).

.. _sec-required-area:

Design mode: required-area root-find
====================================

In design mode the target is a stage cut and the unknown is the area. Since
:math:`\theta(A)` is continuous and **monotonically increasing** in :math:`A`
(more area always permeates more), the required area is found by **bracketing +
bisection**: an upper bound is grown geometrically (:math:`A \times 4`) until the
solved stage cut exceeds the target, then the bracket is halved until
:math:`|\theta - \theta_\mathrm{target}| < 10^{-6}`. Each evaluation is a full
model solve, but profiles are switched off during the search and only computed
once for the final area, so the cost is a few tens of cheap solves.

.. _sec-temperature-solve:

Temperature root-find (energy balance)
======================================

Each outlet temperature in :eq:`eq-energy` is the solution of
:math:`h(T,p,\vec x) = h_\mathrm{target}` for :math:`T`. Molar enthalpy is
monotone increasing in :math:`T` (:math:`c_p > 0`), so the solve is a
**bracketed bisection with a secant hint**: an initial bracket of
:math:`T_\mathrm{guess} \pm 300~\mathrm{K}` is expanded until it straddles the
root, then contracted with a secant/bisection hybrid until the enthalpy residual
falls below :math:`10^{-6}` of its scale. The scheme uses **only enthalpy
evaluations** --- it does not require the property package to expose a
pressure--enthalpy flash --- which is what makes the energy layer portable
across PMEs. Profile temperatures reuse the previous point's temperature as the
warm-start guess, so each profile point converges in a couple of iterations.

.. _sec-enthalpy-provider:

The enthalpy provider
=====================

:class:`EnthalpyProvider` is the adapter's implementation of the core's
:class:`IEnthalpyProvider` port. To evaluate :math:`h(T,p,\vec x)` it:

#. clones a **private scratch Material Object** from the feed
   (``CreateMaterial``) so the real feed/retentate/permeate objects are never
   disturbed;
#. sets the overall :math:`T,p,\vec x` and a unit total flow;
#. flashes at :math:`T,p` (``CalcEquilibrium``) to establish the present phases;
#. calls ``CalcSinglePhaseProp("enthalpy")`` on each present phase and reads it
   back with ``GetSinglePhaseProp``, summing the phase enthalpies weighted by
   phase fraction to get the overall molar enthalpy.

The scratch material is created once and reused for all queries in a solve, then
released. A one-shot ``TryProbe`` at the start lets ``Calculate`` decide, before
writing any stream, whether the property package can deliver enthalpies at all;
if not, the unit reports the reason and falls back to an isothermal result.

Tolerances and defaults
=======================

.. list-table::
   :header-rows: 1
   :widths: 46 24 30

   * - Quantity
     - Default
     - Set in
   * - Cross-flow RK4 steps
     - 4000
     - ``CrossFlowModel``
   * - Counter-current cells
     - 400
     - ``PlugFlowModel``
   * - Profile sample points
     - 100
     - adapter (``ProfilePoints``)
   * - Local-permeate root tolerance
     - :math:`\sim 10^{-12}`
     - ``LocalPermeateSolver``
   * - Required-area stage-cut tolerance
     - :math:`10^{-6}`
     - adapter (``RequiredArea``)
   * - Temperature enthalpy-residual tolerance
     - :math:`10^{-6}\times`\ scale
     - ``NonIsothermalEnergy``
