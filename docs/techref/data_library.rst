.. _data_library:

=====================================
Permeability / Permeance Data Library
=====================================

This chapter collects citable permeability and permeance data for configuring
the unit. The membrane property the unit consumes is the **permeance**
:math:`Q_i` in SI (mol m\ :sup:`-2` s\ :sup:`-1` Pa\ :sup:`-1`); most literature
reports **permeability** in Barrer (a material property) or **permeance** in GPU
(a membrane property). The conversions are given first.

.. _sec-unit-conversions:

Units and conversions
=====================

.. list-table:: Permeability / permeance units
   :header-rows: 1
   :widths: 20 34 46

   * - Unit
     - Definition
     - SI equivalent
   * - Barrer
     - :math:`10^{-10}\ \mathrm{cm^3(STP)\,cm\,cm^{-2}\,s^{-1}\,cmHg^{-1}}`
     - :math:`3.348\times10^{-16}\ \mathrm{mol\,m\,m^{-2}\,s^{-1}\,Pa^{-1}}` (permeability)
   * - GPU
     - :math:`10^{-6}\ \mathrm{cm^3(STP)\,cm^{-2}\,s^{-1}\,cmHg^{-1}}`
     - :math:`3.348\times10^{-10}\ \mathrm{mol\,m^{-2}\,s^{-1}\,Pa^{-1}}` (permeance)

Two relations are worth memorising:

.. math::
   :label: eq-conv

   Q_i\;[\mathrm{GPU}] = \frac{\mathcal{P}_i\;[\mathrm{Barrer}]}{\ell\;[\mu\mathrm{m}]},
   \qquad
   1\ \mathrm{GPU} = 3.348\times10^{-10}\ \mathrm{mol\,m^{-2}\,s^{-1}\,Pa^{-1}}.

That is, **1 Barrer through a 1 µm active layer = 1 GPU**. For example a CO\ :sub:`2`
permeability of 2700 Barrer (silicone rubber) in a 0.1 µm active layer is
:math:`2700/0.1 = 27{,}000` GPU :math:`= 9.0\times10^{-6}` mol m\ :sup:`-2`
s\ :sup:`-1` Pa\ :sup:`-1`.

.. warning::

   Permeability (Barrer) is a **material** property and cannot be used directly:
   it must be divided by the (often unknown) active-layer thickness to get the
   permeance the unit needs. When only Barrer values are available, either use a
   representative thin-film thickness (0.1--1 µm for modern asymmetric membranes)
   or, preferably, use a measured GPU permeance for the actual membrane.

Typical active-layer thickness
==============================

The permeance is the permeability divided by the thickness of the **selective
(active) layer only** --- not the whole membrane. Representative values:

.. list-table::
   :header-rows: 1
   :widths: 38 24 38

   * - Membrane type
     - Active layer :math:`\ell`
     - Note
   * - Dense isotropic lab film
     - 10--100 µm
     - what the Table 13.3-1 permeabilities are measured on
   * - Integrally-skinned asymmetric
     - 0.05--1 µm (~0.1 typical)
     - industrial hollow-fibre / spiral-wound
   * - Thin-film composite (TFC)
     - 0.05--0.5 µm
     - e.g. MTR Polaris, PEBAX coatings

Worked example (:math:`1\,\mathrm{Barrer}/\mathrm{\mu m} = 1\,\mathrm{GPU}`):
silicone-rubber CO\ :sub:`2` at 2700 Barrer in a 0.1 µm skin gives
:math:`2700/0.1 = 27{,}000` GPU :math:`= 9.0\times10^{-6}` mol m\ :sup:`-2`
s\ :sup:`-1` Pa\ :sup:`-1`.

Permeability of gases in polymers (Geankoplis Table 13.3-1)
===========================================================

Pure-gas permeabilities in **Barrer** at 25--30 °C, from Geankoplis Table 13.3-1
:cite:`geankoplis`. (The tabulated numbers are numerically equal to Barrer.)

.. list-table:: Permeability :math:`\mathcal{P}` (Barrer), 25--30 °C
   :header-rows: 1
   :widths: 34 10 10 10 10 10 10

   * - Material
     - He
     - H\ :sub:`2`
     - CH\ :sub:`4`
     - CO\ :sub:`2`
     - O\ :sub:`2`
     - N\ :sub:`2`
   * - Silicone rubber
     - 300
     - 550
     - 800
     - 2700
     - 500
     - 250
   * - Natural rubber
     - 31
     - 49
     - 30
     - 131
     - 24
     - 8.1
   * - Polycarbonate (Lexan)
     - 15
     - 12
     - --
     - 5.6--10
     - 1.4
     - --
   * - Nylon 66
     - 1.0
     - --
     - --
     - 0.17
     - 0.034
     - 0.008
   * - Polyester (Permasep)
     - --
     - 1.65
     - 0.035
     - 0.31
     - --
     - 0.031
   * - Silicone--polycarbonate (57 % Si)
     - --
     - 210
     - --
     - 970
     - 160
     - 70
   * - Teflon FEP
     - 62
     - --
     - 1.4
     - --
     - --
     - 2.5
   * - Ethyl cellulose
     - 35.7
     - 49.2
     - 7.47
     - 47.5
     - 11.2
     - 3.29
   * - Polystyrene
     - 40.8
     - 56.0
     - 2.72
     - 23.3
     - 7.47
     - 2.55

Typical ideal separation factors (Geankoplis Table 13.8-1)
==========================================================

Reported ranges of the ideal selectivity :math:`\alpha^*` for industrial
membranes :cite:`geankoplis`:

.. list-table::
   :header-rows: 1
   :widths: 30 24 46

   * - Pair
     - :math:`\alpha^*`
     - Notes
   * - H\ :sub:`2`\ O / CH\ :sub:`4`
     - 500
     - dehydration
   * - He / CH\ :sub:`4`
     - 5--44
     - He recovery
   * - H\ :sub:`2` / CO
     - 35--80
     - syngas ratio adjustment
   * - H\ :sub:`2` / N\ :sub:`2`
     - 3--200
     - ammonia purge-gas H\ :sub:`2` recovery
   * - H\ :sub:`2` / CH\ :sub:`4`
     - 6--200
     - refinery H\ :sub:`2` recovery
   * - O\ :sub:`2` / N\ :sub:`2`
     - 2--12
     - air separation / N\ :sub:`2` generation
   * - CO\ :sub:`2` / CH\ :sub:`4`
     - 3--50
     - natural-gas sweetening
   * - CO\ :sub:`2` / O\ :sub:`2`
     - 3--6
     - --

Validated case permeances
=========================

The following permeance sets are **fully specified in the literature** and are
the ones used in :ref:`validation`; they are convenient known-good starting
points. All in :math:`10^{-10}\ \mathrm{mol\,m^{-2}\,s^{-1}\,Pa^{-1}}` unless
noted.

.. list-table::
   :header-rows: 1
   :widths: 30 18 34 18

   * - System / membrane
     - :math:`\gamma`
     - Permeances (:math:`\times10^{-10}`)
     - Source
   * - NH\ :sub:`3`/H\ :sub:`2`/N\ :sub:`2`, polyethylene
     - 0.13
     - 2.63 / 0.835 / 0.172
     - :cite:`shindo1985,diaspinto2020`
   * - H\ :sub:`2`/CH\ :sub:`4`/CO/N\ :sub:`2`/CO\ :sub:`2`, microporous glass
     - 0.10
     - 4.80 / 1.91 / 1.40 / 1.38 / 1.48
     - :cite:`shindo1985,diaspinto2020`
   * - CO\ :sub:`2`/CH\ :sub:`4`/C\ :sub:`2`/C\ :sub:`3`/C\ :sub:`4`\ :sup:`+`, cellulose acetate
     - 0.04
     - 298 / 2.27 / 4.29 / 4.53 / 192
     - :cite:`diaspinto2020`
   * - CO\ :sub:`2`/O\ :sub:`2`/N\ :sub:`2`, Polaris™ (flue gas)
     - 0.12
     - 3890 / 311 / 144
     - :cite:`diaspinto2020`
   * - N\ :sub:`2`/O\ :sub:`2`, air separation (MemPy)
     - --
     - N\ :sub:`2` 52--63 GPU, O\ :sub:`2` 131--159 GPU
     - :cite:`dejaco2020`
   * - CO\ :sub:`2`/O\ :sub:`2`/N\ :sub:`2`, cellulose triacetate HF
     - --
     - 204.2 / 60.2 / 13.1
     - :cite:`aziaba2022`

.. note::

   For the H\ :sub:`2`/CH\ :sub:`4`/CO/N\ :sub:`2`/CO\ :sub:`2` microporous-glass
   case the hydrogen permeance is :math:`4.80\times10^{-10}` (the
   :math:`4.80\times10^{-9}` printed in :cite:`diaspinto2020` is a typo; see
   :ref:`sec-val-shindo`).

.. _sec-polymer-permeability:

Pure-gas permeability of common membrane polymers
=================================================

Representative pure-gas permeabilities from the primary literature. **Absolute
permeabilities scatter by roughly 1.5--3\ :math:`\times` between laboratories**
(casting solvent, film physical aging, feed pressure, CO\ :sub:`2` dual-mode
sorption), so single values are indicative; the **ideal selectivities**
(:numref:`tab-ideal-sel`) are considerably more reproducible.

.. list-table:: Pure-gas permeability (Barrer), 25--35 °C
   :header-rows: 1
   :name: tab-permeability
   :widths: 26 11 11 11 11 11 19

   * - Material
     - H\ :sub:`2`
     - CO\ :sub:`2`
     - O\ :sub:`2`
     - N\ :sub:`2`
     - CH\ :sub:`4`
     - Reference
   * - Cellulose acetate
     - ~24
     - 6.0
     - 0.8
     - 0.20
     - 0.20
     - :cite:`Puleo1989,Baker2012`
   * - Polysulfone (Udel)
     - 14.4
     - 5.6
     - 1.4
     - 0.25
     - 0.25
     - :cite:`MizrahiRodriguez2022`
   * - Polycarbonate (bisphenol-A)
     - 12
     - 6.0
     - 1.4
     - 0.27
     - 0.26
     - :cite:`Hellums1989`
   * - PPO
     - ~100
     - 61
     - 16
     - 3.0
     - 3.5
     - :cite:`AguilarVega1993`
   * - Teflon AF2400
     - ~2300
     - 2800
     - 990
     - 490
     - 400
     - :cite:`PinnauToy1996`
   * - Matrimid 5218 (polyimide)
     - ~24
     - 8.0
     - 1.9
     - 0.25
     - 0.20
     - :cite:`CastroMunozMembranes2018`
   * - P84 (polyimide)
     - ~10
     - 0.99
     - 0.24
     - 0.025
     - 0.02
     - :cite:`Barsema2003`
   * - PDMS (silicone rubber)
     - 650
     - 3230
     - 620
     - 280
     - 950
     - :cite:`Robb1968`
   * - PEBAX 1657
     - 6.5
     - 66
     - 2.9
     - 1.2
     - 3.4
     - :cite:`BernardoClarizia2020`
   * - PEBAX 2533
     - 43
     - 217
     - 22
     - 9.1
     - 27
     - :cite:`Clarizia2018`
   * - PIM-1 (methanol-treated)
     - 3600
     - 6500
     - 1300
     - 340
     - 430
     - :cite:`Thomas2009`

.. list-table:: Ideal (pure-gas) selectivity of the same polymers
   :header-rows: 1
   :name: tab-ideal-sel
   :widths: 30 16 16 14 16

   * - Material
     - CO\ :sub:`2`/CH\ :sub:`4`
     - CO\ :sub:`2`/N\ :sub:`2`
     - O\ :sub:`2`/N\ :sub:`2`
     - H\ :sub:`2`/CH\ :sub:`4`
   * - Cellulose acetate
     - 30
     - 27
     - 3.8
     - 115
   * - Polysulfone
     - 22
     - 22
     - 5.5
     - 53
   * - Polycarbonate
     - 23
     - 22
     - 5.0
     - 47
   * - PPO
     - 18
     - 20
     - 4.4
     - 26
   * - Matrimid 5218
     - 32
     - 29
     - 6.4
     - 80
   * - P84
     - **44**
     - 40
     - **10.0**
     - 240
   * - PDMS
     - 3.3
     - 9.5
     - 2.1
     - 0.68 (reverse)
   * - PEBAX 1657
     - 19.5
     - **58**
     - 2.5
     - 1.9
   * - PIM-1 (MeOH)
     - 15
     - 19
     - 3.8
     - 8.4

The conventional glassy polymers (CA, PSf, PC) cluster at moderate flux and
selectivity; polyimides (Matrimid, and especially **P84**) trade flux for
selectivity; **PPO**, **Teflon AF** and **PIM-1** are far more permeable but less
size-selective; **PDMS** is uniquely *reverse-selective* (it favours the larger,
more condensable species --- the basis of vapour/heavy-hydrocarbon recovery); and
the **PEBAX** rubbery grades are the CO\ :sub:`2`-selective standard behind
post-combustion capture membranes. All temperatures 25--35 °C; see the cited
sources for exact conditions :cite:`Sanders2013,Bernardo2009`.

.. _sec-robeson:

The Robeson upper bound
=======================

Permeability and selectivity are in fundamental tension: opening a polymer to
raise the fast-gas flux almost always erodes its size/sorption discrimination. On
a log--log plot of selectivity :math:`\alpha_{xy}` versus the fast-gas
permeability :math:`\mathcal{P}_x`, the best materials fall beneath a straight
"upper bound" line :cite:`Robeson1991,Robeson2008`,

.. math::
   :label: eq-robeson

   \mathcal{P}_x \;=\; k\,\bigl(\alpha_{xy}\bigr)^{n},

with a negative slope :math:`n` set almost entirely by the ratio of the two
gases' kinetic diameters and a front factor :math:`k` that rises with backbone
stiffness and free volume :cite:`Freeman1999`. Materials **above** the line beat
the prevailing trade-off; the bound has shifted upward over time (perfluoropolymers,
PIMs, thermally-rearranged polymers). Verified constants (Barrer):

.. list-table:: Robeson (2008) upper-bound constants
   :header-rows: 1
   :widths: 24 30 20 26

   * - Pair
     - :math:`k` (Barrer)
     - :math:`n`
     - Source
   * - CO\ :sub:`2`/CH\ :sub:`4`
     - :math:`5.369\times10^{6}`
     - :math:`-2.636`
     - :cite:`Robeson2008,ComesanaGandara2019`
   * - CO\ :sub:`2`/N\ :sub:`2`
     - :math:`30.967\times10^{6}`
     - :math:`-2.888`
     - :cite:`Robeson2008,ComesanaGandara2019`

.. note::

   The O\ :sub:`2`/N\ :sub:`2`, H\ :sub:`2`/N\ :sub:`2` and H\ :sub:`2`/CO\ :sub:`2`
   constants are in Table III of :cite:`Robeson2008` (not reproduced in an
   openly-accessible source; consult the paper directly). The 2019 *revised*
   CO\ :sub:`2` bounds :cite:`ComesanaGandara2019` are CO\ :sub:`2`/CH\ :sub:`4`
   :math:`k=22.584\times10^6, n=-2.401` and CO\ :sub:`2`/N\ :sub:`2`
   :math:`k=755.58\times10^6, n=-3.409`.

.. _sec-commercial-membranes:

Commercial modules and brands
=============================

Manufacturers publish product purities/recoveries far more often than permeance
or selectivity. The quantitative anchors that *are* well-published are few;
material-class selectivities should be cited from the reviews, not attributed to
a specific product.

.. list-table:: Commercial gas-separation membranes
   :header-rows: 1
   :widths: 20 18 20 24 18

   * - Brand
     - Company
     - Material
     - Separation
     - Published figure
   * - Separex
     - Honeywell UOP
     - Cellulose acetate
     - CO\ :sub:`2`/CH\ :sub:`4` (NG)
     - qualitative (CA :math:`\alpha\approx30`--40 pure-gas)
   * - MEDAL
     - Air Liquide
     - Polyimide
     - CO\ :sub:`2`/CH\ :sub:`4`; H\ :sub:`2`
     - qualitative
   * - NG membranes
     - MTR
     - PEBAX-type TFC
     - CO\ :sub:`2`/CH\ :sub:`4`
     - CO\ :sub:`2` :math:`\approx1000`--2000 GPU :cite:`BakerLokhandwala2008`
   * - Cynara
     - SLB
     - Cellulose triacetate
     - CO\ :sub:`2`/CH\ :sub:`4` (high CO\ :sub:`2`)
     - qualitative
   * - SEPURAN Green
     - Evonik
     - Polyimide HF
     - biogas CO\ :sub:`2`/CH\ :sub:`4`
     - pure-gas CO\ :sub:`2`/CH\ :sub:`4` :math:`\approx50` :cite:`EvonikSepuranGreen`
   * - PRISM
     - Air Products
     - Polysulfone HF
     - H\ :sub:`2` recovery
     - 90--98 % H\ :sub:`2` recovery (qual.)
   * - Generon
     - Generon IGS
     - TB-bisphenol-A PC
     - O\ :sub:`2`/N\ :sub:`2` (N\ :sub:`2`)
     - O\ :sub:`2`/N\ :sub:`2` :math:`\approx7.5` :cite:`YongZhang2021`
   * - N\ :sub:`2` generators
     - Parker
     - PPO HF
     - O\ :sub:`2`/N\ :sub:`2` (N\ :sub:`2`)
     - N\ :sub:`2` 95--99.5 % (qual.)
   * - Polaris Gen-1
     - MTR
     - PEBAX-type TFC
     - CO\ :sub:`2`/N\ :sub:`2` (flue gas)
     - CO\ :sub:`2` :math:`\approx1000` GPU, CO\ :sub:`2`/N\ :sub:`2` :math:`\approx50` :cite:`Merkel2010`

The best-published brand anchors are **MTR Polaris Gen-1** (:math:`\sim1000` GPU
CO\ :sub:`2`, CO\ :sub:`2`/N\ :sub:`2` :math:`\sim50`) :cite:`Merkel2010`, MTR's
CO\ :sub:`2`-removal composites (:math:`\sim1000`--2000 GPU CO\ :sub:`2`)
:cite:`BakerLokhandwala2008`, and Generon's TB-bisphenol-A polycarbonate material
(O\ :sub:`2`/N\ :sub:`2` :math:`\approx7.5`) :cite:`YongZhang2021`; other brand
figures are manufacturer claims or qualitative.
