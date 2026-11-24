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

The motive expansion follows :eq:`eq-motive-path` from the stagnation state m0:
isentropic up to the throat, with :math:`\eta_\mathrm{m}` acting only beyond it.
The critical flux :math:`G^*` and throat pressure :math:`P_\mathrm{t}` follow
from :eq:`eq-critical-flux` on the isentrope, so they depend on the inlet
state alone and not on :math:`\eta_\mathrm{m}`. When the mixing-inlet
pressure lies below the throat pressure, :math:`P_\mathrm{y} < P_\mathrm{t}`, the
nozzle is choked and passes

.. math::
   :label: eq-motive-choked

   \dot m_\mathrm{m} = C_d\,G^*\,A_\mathrm{t} ,

independent of anything downstream. The discharge coefficient :math:`C_d`
(default 1) corrects the isentropic HEM critical flux for everything the 1D
throat state misses: losses in the converging section, non-uniform flow, and
non-equilibrium (:ref:`sec-hne`). :numref:`tab-motive-critical` lists the
throat states of the CO\ :sub:`2` motive conditions used in this reference.

.. _tab-motive-critical:

.. list-table:: Critical (throat) state of CO\ :sub:`2` motive nozzles: isentropic HEM path, :math:`C_d = 1`. Source: ``figures/data/nozzle_critical.csv``.
   :header-rows: 1
   :widths: 40 20 20 20

   * - Motive inlet
     - :math:`T_0` [°C]
     - :math:`P_\mathrm{t}` [bar]
     - :math:`G^*` [t m\ :sup:`-2` s\ :sup:`-1`]
   * - 47 bar, saturated liquid, HEM
     - 11.7
     - 34.1
     - 28.6
   * - 47 bar, saturated liquid, HNE :math:`\varphi = 0.5`
     - 11.7
     - 28.0
     - 41.2
   * - 84.5 bar, 32 °C, HEM
     - 32.0
     - 64.1
     - 46.4

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

Here :math:`G(P)` is the flux on the path :eq:`eq-motive-path`: it equals
:math:`G^*` at the throat and carries the loss :math:`\eta_\mathrm{m}` beyond it,
so the exit velocity :math:`u_\mathrm{e}` is the only place where
:math:`\eta_\mathrm{m}` enters.

.. admonition:: Where the discharge coefficient acts

   :eq:`eq-motive-exit` uses the *HEM* flux :math:`G(P)` without :math:`C_d`.
   :math:`C_d` is a throat correction: the fluid is assumed to relax to
   equilibrium in the diverging section, where the residence time is longer.
   The HEM exit can then pass the corrected flow only if
   :math:`A_\mathrm{e}/A_\mathrm{t} \ge C_d`. For the Giacomelli nozzle
   (:math:`A_\mathrm{e}/A_\mathrm{t} = 1.31`, :math:`C_d = 1.14`) this holds. A
   nozzle with :math:`1 < A_\mathrm{e}/A_\mathrm{t} < C_d` is rejected with an
   error. An earlier version clamped :math:`P_\mathrm{e}` at the floor of the
   search range instead, which silently doubled the entrainment. A converging
   nozzle (:math:`A_\mathrm{e} = A_\mathrm{t}`) exhausts at the throat state and
   is not affected. Applying :math:`C_d` uniformly along the nozzle would remove
   the restriction, but it would also make the design results depend on
   :math:`C_d`; the choice is open (:ref:`sec-numerics-open`).

.. _sec-unchoked-motive:

Back pressure above the throat pressure
---------------------------------------

**Converging nozzle.** If :math:`P_\mathrm{y} \ge P_\mathrm{t}` the nozzle does
not choke. This is the normal state of a liquid jet pump whose motive does not
flash. The nozzle exhausts at the mixing-inlet pressure:

.. math::
   :label: eq-motive-unchoked

   \dot m_\mathrm{m} = C_d\,G(P_\mathrm{y})\,A_\mathrm{t}, \qquad P_\mathrm{e} = P_\mathrm{y} .

The motive flow now depends on :math:`P_\mathrm{y}`, and so on the back pressure.
This is the coupling that makes the incompressible jet pump's motive flow
respond to its discharge pressure. :ref:`sec-val-fluids` verifies it against the
classical jet-pump equations. The expansion lies on the isentropic part of
:eq:`eq-motive-path`, so the flow is continuous when the nozzle starts to choke.
Nozzle friction in a liquid jet pump can only enter through :math:`C_d < 1`.
That reduces the flow but leaves the jet velocity at its loss-free value, so the
jet momentum is over-estimated by a factor :math:`\sqrt{1+K_\mathrm{n}}` for a
nozzle loss coefficient :math:`K_\mathrm{n}` (about 2.5 % for
:math:`K_\mathrm{n} = 0.05`).

**Converging–diverging nozzle.** A diverging section keeps the throat choked
for back pressures well above :math:`P_\mathrm{t}`, as in any de Laval nozzle.
The model distinguishes three ranges, all with the exit mass flux
:math:`G_\mathrm{e} = C_d G^* A_\mathrm{t}/A_\mathrm{e}`:

* :math:`P_\mathrm{y} < P_\mathrm{se}`: choked, **supersonic exit** at the
  design expansion :eq:`eq-motive-exit`. The jet may be over- or
  under-expanded; the mismatch acts in the mixing momentum balance.
  :math:`P_\mathrm{se}` is the pressure behind a normal shock standing at the
  exit. It is computed with the constant-area solver (:ref:`sec-mixing-root`)
  applied to the exit state alone, loss-free.
* :math:`P_\mathrm{se} \le P_\mathrm{y} < P_\mathrm{fc}`: choked, with a **normal
  shock inside** the diverging section. The exit is subsonic at
  :math:`P_\mathrm{e} = P_\mathrm{y}`, with the same stagnation enthalpy and exit
  mass flux, and a higher entropy than the isentrope.
* :math:`P_\mathrm{y} \ge P_\mathrm{fc}`: **unchoked**. The exit controls the
  flow, :math:`\dot m_\mathrm{m} = G(P_\mathrm{y})\,A_\mathrm{e}`. The
  first-critical pressure :math:`P_\mathrm{fc}` is the subsonic-branch pressure
  where :math:`G(P_\mathrm{fc})A_\mathrm{e} = C_d G^* A_\mathrm{t}`.

The motive flow is continuous everywhere, and the exit state is continuous at
:math:`P_\mathrm{fc}`. At :math:`P_\mathrm{se}` the jet switches from the
supersonic state to the post-shock state. The two conserve mass, momentum and
energy, but :math:`\eta_\mathrm{mix}` multiplies only the momentum flux, so the
mixing balance sees a small step. For nitrogen from 10 to 6 bar with
:math:`A_\mathrm{e}/A_\mathrm{t} = 1.5`, the step is 0.11 bar in outlet pressure.
It vanishes for :math:`\eta_\mathrm{mix} = 1`. A forward rating whose
specified outlet pressure falls in such a step raises an error rather than
return a different pressure (:ref:`sec-rating-algorithm`). Earlier versions
treated every nozzle as converging above :math:`P_\mathrm{t}`: the jet then
broke continuity at the exit, and the outlet pressure jumped by up to 0.4 bar at
:math:`P_\mathrm{t}` without warning.

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
