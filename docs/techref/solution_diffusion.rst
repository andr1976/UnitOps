.. _solution_diffusion:

===============================================
Solution--Diffusion Transport and Driving Force
===============================================

Permeability, permeance, and selectivity
========================================

Gas transport through a dense (non-porous) polymeric membrane is described by
the **solution--diffusion** mechanism: a penetrant dissolves into the upstream
face of the active layer, diffuses down its chemical-potential gradient, and
desorbs at the downstream face :cite:`wijmans1995,geankoplis`. For an ideal-gas
mixture with a partial-pressure driving force, the steady local molar flux of
component :math:`i` through an active layer of thickness :math:`\ell` is

.. math::
   :label: eq-flux

   J_i \;=\; \frac{\mathcal{P}_i}{\ell}\,\bigl(p_\mathrm{r}\,x_i - p_\mathrm{p}\,y_i\bigr)
        \;=\; Q_i\,\bigl(p_\mathrm{r}\,x_i - p_\mathrm{p}\,y_i\bigr),

where :math:`\mathcal{P}_i` is the **permeability** (a material property),
:math:`Q_i = \mathcal{P}_i/\ell` is the **permeance** (the membrane property the
unit actually uses), :math:`p_\mathrm{r} x_i` is the upstream partial pressure,
and :math:`p_\mathrm{p} y_i` is the downstream partial pressure. Permeability
factors into a thermodynamic solubility :math:`S_i` and a kinetic diffusivity
:math:`D_i`,

.. math::
   :label: eq-perm-sd

   \mathcal{P}_i \;=\; D_i\, S_i ,

which is why highly soluble but slowly diffusing species (CO\ :sub:`2`,
condensable vapours) and small fast-diffusing species (He, H\ :sub:`2`) can both
be "fast" gases, by different routes.

The **ideal selectivity** of :math:`i` over :math:`j` is the permeability (or
permeance) ratio

.. math::
   :label: eq-selectivity

   \alpha_{ij} \;=\; \frac{\mathcal{P}_i}{\mathcal{P}_j} \;=\; \frac{Q_i}{Q_j}.

Because both fluxes share the same :math:`p_\mathrm{r},p_\mathrm{p}`, the
*attainable* separation is always poorer than :math:`\alpha_{ij}`: the permeate
enrichment is capped by the pressure ratio (:ref:`sec-pressure-ratio-limit`).

.. note::

   Permeability is quoted in **Barrer** and permeance in **GPU** in most of the
   membrane literature and on manufacturer datasheets. The SI values used
   internally, and the conversion factors, are tabulated in
   :ref:`sec-unit-conversions`. As a rule of thumb,
   :math:`1~\mathrm{Barrer} = 3.348\times10^{-16}~\mathrm{mol\,m\,m^{-2}\,s^{-1}\,Pa^{-1}}`
   and
   :math:`1~\mathrm{GPU} = 3.348\times10^{-10}~\mathrm{mol\,m^{-2}\,s^{-1}\,Pa^{-1}}`.

.. _sec-fugacity:

Real-gas (fugacity) driving force
=================================

Equation :eq:`eq-flux` uses the ideal-gas partial pressure :math:`p\,x_i`. For a
real gas the thermodynamically correct driving force is the **fugacity**
difference,

.. math::
   :label: eq-flux-fug

   J_i \;=\; Q_i\bigl(f_i^{\mathrm{ret}} - f_i^{\mathrm{perm}}\bigr)
        \;=\; Q_i\bigl(\varphi_i^{\mathrm{r}}\,p_\mathrm{r} x_i
                       - \varphi_i^{\mathrm{p}}\,p_\mathrm{p} y_i\bigr),

where :math:`\varphi_i` are fugacity coefficients from the equation of state
(:math:`\varphi_i \to 1` as :math:`p \to 0`). The unit **defaults to this
fugacity form** (the ``DrivingForce`` parameter, :ref:`appendix_capeopen`),
delegating :math:`\varphi` to the PME. The coefficients are evaluated once at
feed conditions --- :math:`\varphi_i^{\mathrm{r}} = \varphi_i(T,p_\mathrm{r},z)`
on the retentate side and :math:`\varphi_i^{\mathrm{p}} = \varphi_i(T,p_\mathrm{p},z)`
on the permeate side --- and held constant along the module. This is a
first-order correction: :math:`\varphi` varies only weakly with the modest
composition change along the module, and the permeate-side coefficient tends to
unity at low :math:`p_\mathrm{p}`. If the property package cannot supply
:math:`\varphi`, the unit falls back to the partial-pressure form.

The reformulated local solver (:ref:`sec-local-permeate-solver`) absorbs the
coefficients without loss of robustness: the driving force becomes
:math:`S_i(a_i x_i - \gamma b_i y_i)` with :math:`a_i=\varphi_i^{\mathrm{r}}`,
:math:`b_i=\varphi_i^{\mathrm{p}}`, and :math:`H(t)` stays monotone and
singularity-free. Because real gases below their critical region have
:math:`\varphi<1` (attractive forces), the fugacity form gives a smaller driving
force --- and hence a **lower stage cut** --- than ideal gas: by :math:`\sim9\%`
at 60 bar and up to :math:`\sim15\%` at 100 bar for CO\ :sub:`2`/CH\ :sub:`4`
(:ref:`sec-val-ig-rg`), the same qualitative high-pressure trend DeJaco *et al.*
:cite:`dejaco2020` report (a quantitative cross-check on their propane/propylene
system is in :ref:`sec-val-ig-rg`).

.. _sec-pressure-ratio-limit:

The pressure-ratio limit
========================

Consider a binary feed with fast component :math:`A`. Two limiting regimes bound
the permeate purity :cite:`baker2004`:

* **Selectivity-controlled** (:math:`\alpha_{AB} \gamma \ll 1`): the pressure
  ratio is so favourable that the membrane's selectivity sets the separation,
  and the local permeate enrichment approaches :math:`\alpha_{AB}`.
* **Pressure-ratio-controlled** (:math:`\alpha_{AB} \gamma \gg 1`): the pressure
  ratio :math:`\gamma = p_\mathrm{p}/p_\mathrm{r}` limits the separation
  regardless of how selective the membrane is. In the extreme, the permeate mole
  fraction cannot exceed :math:`y_A \le x_A/\gamma`.

Setting the flux of :eq:`eq-flux` for the two components equal to the ratio in
which they appear in the local permeate gives the **local permeate
relationship** for a binary,

.. math::
   :label: eq-binary-local

   \frac{y_A}{y_B}
   \;=\; \frac{Q_A\,(p_\mathrm{r} x_A - p_\mathrm{p} y_A)}
              {Q_B\,(p_\mathrm{r} x_B - p_\mathrm{p} y_B)}
   \;=\; \alpha_{AB}\,\frac{x_A - \gamma\,y_A}{x_B - \gamma\,y_B},

which is the classic implicit equation for :math:`y_A` solved at every position
along the module. Its multicomponent generalisation is the local permeate solver
of :ref:`sec-local-permeate`.

.. _sec-local-permeate:

The local permeate composition
==============================

At any position the permeate leaving the active layer is composed of the local
fluxes, so its mole fractions are the flux fractions

.. math::
   :label: eq-yi-flux

   y_i \;=\; \frac{J_i}{\sum_k J_k}
         \;=\; \frac{Q_i\,(p_\mathrm{r} x_i - p_\mathrm{p} y_i)}
                    {\sum_k Q_k\,(p_\mathrm{r} x_k - p_\mathrm{p} y_k)}.

This is a coupled, implicit set in :math:`\{y_i\}` (the unknown appears on both
sides through the downstream partial pressure :math:`p_\mathrm{p} y_i`).
Dividing through by :math:`p_\mathrm{r}` and writing :math:`S_i = Q_i/Q_1` for
permeance ratios and :math:`\gamma = p_\mathrm{p}/p_\mathrm{r}`,

.. math::
   :label: eq-yi-reduced

   y_i \;=\; \frac{S_i\,(x_i - \gamma\,y_i)}{\sum_k S_k\,(x_k - \gamma\,y_k)} .

Solving :eq:`eq-yi-reduced` robustly, without an initial guess and without the
numerical singularity that a naive fixed-point iteration hits as
:math:`\gamma\to0`, is the job of the reformulated local permeate solver
described in :ref:`sec-local-permeate-solver`. Once :math:`\{y_i\}` is known at a
position, the **local total flux** follows from the denominator of
:eq:`eq-yi-flux`,

.. math::
   :label: eq-total-flux

   J_\mathrm{tot} \;=\; \sum_k Q_k\,\bigl(p_\mathrm{r} x_k - p_\mathrm{p} y_k\bigr) ,

and the permeate produced over an area element :math:`\dd a` is
:math:`J_\mathrm{tot}\,\dd a`.

Module material balances
========================

Integrating the local behaviour over the module couples the local permeate
relationship to the change in the retentate-side composition and flow. In
differential form, with area :math:`a` measured from the feed end and
:math:`R(a)` the local retentate molar flow,

.. math::
   :label: eq-mass-balance

   -\,\tder{R}{a} \;=\; J_\mathrm{tot}(a), \qquad
   -\,\tder{(R\,x_i)}{a} \;=\; J_i(a) \;=\; y_i(a)\, J_\mathrm{tot}(a) .

These are closed by the local permeate relationship :eq:`eq-yi-reduced`
evaluated at the local retentate composition :math:`\{x_i(a)\}`. How the
permeate side is treated distinguishes the three flow patterns:

* **Cross-flow** --- the permeate is swept away normal to the membrane; the
  local permeate composition depends only on the *local* retentate composition
  (:ref:`crossflow`).
* **Co-current / counter-current** --- the permeate flows alongside the feed and
  the driving force uses the *bulk permeate* composition on that side, which
  itself is governed by its own balance (:ref:`flow_patterns`).

The overall **stage cut** is the integral of the total flux over the whole area,
normalised by the feed,

.. math::
   :label: eq-stage-cut

   \theta \;=\; \frac{1}{F}\int_0^A J_\mathrm{tot}(a)\,\dd a
           \;=\; \frac{F - R(A)}{F}.

The stage cut is the single most important design variable: at fixed feed and
membrane, installing more area raises :math:`\theta`, which raises recovery of
the fast component but *dilutes* the permeate (later increments of area permeate
progressively slower gas). This trade-off is what the position profiles of
:ref:`sec-profiles` and the design/rating modes of :ref:`sec-spec-mode` make
explicit.
