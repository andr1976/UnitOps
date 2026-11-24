"""Huang et al. (1999) R141b, 39 critical-mode points: entrainment ratio and
critical back pressure (as saturation temperature).  Data: data/huang1999.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, NAVY, RED, col, grid, parity, rows, save

rs = rows("huang1999.csv")
fig, (a, b) = plt.subplots(1, 2, figsize=(7.0, 3.1))
for s, color, mk, lab in (("Ma2017", NAVY, "o", "Ma 2017 set"),
                          ("eta_s0.7", RED, "^", r"$\eta_\mathrm{s}$ = 0.70")):
    sel = [r for r in rs if r["set"] == s]
    ex, sim = col(sel, "ER_exp"), col(sel, "ER_sim")
    e = (sim - ex) / ex * 100
    a.plot(ex, sim, mk, color=color, mfc="white", mew=1.1,
           label=f"{lab}: MAE {np.abs(e).mean():.1f} %")
    tx, ts = col(sel, "Td_exp_C"), col(sel, "Td_sim_C")
    b.plot(tx, ts, mk, color=color, mfc="white", mew=1.1,
           label=f"{lab}: MAE {np.abs(ts - tx).mean():.2f} K")
parity(a, 0.1, 0.9, band=0.15)
a.set_xlabel(r"measured critical ER$^*$ [-]")
a.set_ylabel(r"predicted ER$^*$ [-]")
a.text(0.88, 0.12, "dotted: ±15 %", fontsize=7, color="#7F8C8D", ha="right")
a.legend(loc="upper left", fontsize=6.5)
grid(a)
parity(b, 22, 46)
for d in (-2, 2):
    b.plot([22, 46], [22 + d, 46 + d], ":", color="#7F8C8D", lw=0.8)
b.set_xlabel(r"measured $T_\mathrm{sat}(P_\mathrm{b}^*)$ [°C]")
b.set_ylabel(r"predicted $T_\mathrm{sat}(P_\mathrm{b}^*)$ [°C]")
b.text(45.5, 23, "dotted: ±2 K", fontsize=7, color="#7F8C8D", ha="right")
b.legend(loc="upper left", fontsize=6.5)
grid(b)
save(fig, "val_huang.png")
