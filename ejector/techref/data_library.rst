.. _data_library:

========================================
Data Library: Efficiencies and Data Sets
========================================

An ejector model is only as good as its loss parameters. Unlike membrane
permeances, these are not material properties. They depend on geometry, scale,
fluid and operating regime. This chapter collects the parameter sets used or
derived in this reference, the experimental data sets considered for validation
and why each was or was not used, and the property models behind the reference
implementation.

Efficiency sets
===============

.. list-table:: Efficiency sets (:math:`\eta_\mathrm{m}/\eta_\mathrm{s}/\eta_\mathrm{mix}/\eta_\mathrm{d}`, :math:`C_d`)
   :header-rows: 1
   :widths: 26 26 10 38

   * - Set
     - Values
     - :math:`C_d`
     - Origin and range of validity
   * - Ma *et al.* (R141b)
     - 0.95 / 0.85 / 0.95 / 0.85
     - 1
     - used by :cite:`ma2017` for the Huang data; predictive here to 0.9 K in :math:`P_\mathrm{b}^*`, +14 % in ER
   * - typical (this work)
     - 0.90 / 0.85 / 0.90 / 0.80
     - 1
     - a screening assumption; over-predicts the Giacomelli suction flow by 32 % (MAE)
   * - R744, all points
     - 0.90 / 0.608 / 0.840 / 0.801
     - 1.205
     - fitted on 42 points :cite:`giacomelli2019`: motive 71–104 bar, suction 24–39 bar, lift ≤ 1.6, :math:`D_\mathrm{mix}` = 2 mm
   * - R744, supercritical
     - 0.90 / 0.632 / 0.844 / 0.798
     - 1.205
     - the 27 supercritical-motive points of the same set
   * - R744, subcritical + subcooled
     - 0.90 / 0.880 / 0.783 / 0.751
     - 1.205
     - the 15 subcritical and subcooled points of the same set

The three R744 sets fit their own data equally well (:ref:`sec-val-giacomelli`).
They are listed separately because their spread measures how much the
efficiencies change with operating regime. That spread should be carried as an
uncertainty band in any application outside the data range.

.. _sec-datasets:

Experimental data sets
======================

.. list-table::
   :header-rows: 1
   :widths: 20 14 26 40

   * - Source
     - Fluid, points
     - What is measured
     - Status in this reference
   * - Huang *et al.* 1999 :cite:`huang1999`, via Ma 2017 Table 1 :cite:`ma2017`
     - R141b, 39
     - critical ER and back pressure, 11 geometries
     - **used**: predictive validation (:ref:`sec-val-huang`); data in ``validate_huang1999.py``
   * - Giacomelli *et al.* 2019 :cite:`giacomelli2019`
     - R744, 42
     - all flows and pressures; full geometry (Fig. 2)
     - **used**: calibration (:ref:`sec-val-giacomelli`); ``data/giacomelli2019.csv`` from Tables 3–4 of the accepted manuscript
   * - Gonçalves *et al.* 2024 :cite:`goncalves2024`
     - R744, 3
     - motive and suction flow, outlet pressure, motive density; geometry (Fig. 5)
     - **used**: motive-flow validation (:ref:`sec-val-goncalves`); data in ``validate_goncalves2024.py``
   * - Ringstad *et al.* 2020, appendix :cite:`ringstad2020`
     - R744, several sources
     - compiled operating points from SINTEF-related studies
     - **not used**: no geometry. Transcription defects were found: suction pressures of one block duplicate Giacomelli points 9–13, two outlet pressures are printed in a different unit from the rest of the column, and one block has no entrainment column
   * - Palacz *et al.* 2015 :cite:`palacz2015`
     - R744, 24
     - absolute flows, suction 27–32 bar
     - **not used**: the SINTEF ejector tested is not identified, and the mixing-section dimensions are not published
   * - Bodys *et al.* 2020 :cite:`bodys2020`
     - R744
     - subcritical motive (about 50–70 bar) on SINTEF geometries
     - **candidate**: closest to a liquid motive at 47 bar; the accepted manuscript could not be downloaded
   * - Cunningham 1974 :cite:`cunningham1974`
     - water / air
     - liquid jet pump compressing a non-condensable gas
     - **candidate**: the only liquid-motive / gas-suction data set identified; paywalled

Geometry must come from the primary source. A data set whose geometry has to
be guessed calibrates the guess, not the model. That is why the larger SINTEF
compilations are not used even though they cover more operating points.

Design guidance not yet used
============================

* **Flügel** :cite:`flugel1939` gives the classical design rules for the
  mixing length and the optimum nozzle-to-mixing area ratio. They would supply
  :math:`L_\mathrm{mix}/D_\mathrm{mix}` in design mode, a quantity the
  constant-area model does not determine.
* **Hatziavramidis** :cite:`hatziavramidis1991` and Cunningham
  :cite:`cunningham1974,cunningham1995` give liquid/liquid and liquid/gas jet-pump
  models. With a non-flashing property model they are an analytic cross-check
  for the liquid-motive, gas-suction duty.
* Constant-pressure-mixing models with a hypothetical throat :cite:`munday1977,huang1999,ma2017`,
  the one-dimensional compressible-flow model of :cite:`marnier2012`, and gas-ejector design methods
  :cite:`liao2008` are alternatives for gas–gas ejectors, should a
  specialised gas mode be wanted.

Property models in the reference implementation
===============================================

The reference implementation uses CoolProp's Helmholtz-energy backend
:cite:`coolprop`: the Span–Wagner equation of state for CO\ :sub:`2`
:cite:`span1996`, CoolProp's reference equations for R141b, water and nitrogen,
and GERG-2008-type mixing rules for mixtures :cite:`kunz2012`. In the CAPE-OPEN
unit every property comes from the flowsheet's property package instead, so the
model inherits whatever equation of state, including any solid-phase handling,
the user has chosen.

Data files in the repository
============================

.. list-table::
   :header-rows: 1
   :widths: 46 54

   * - File
     - Content
   * - ``ejector/prototype/data/giacomelli2019.csv``
     - 42 R744 operating points, as transcribed
   * - ``ejector/techref/figures/data/*.csv``
     - every model result used in this reference, written by ``gen_data.py``
   * - ``ejector/techref/figures/data/giacomelli_fit.csv``
     - the fitted R744 parameter sets with standard errors and correlations
