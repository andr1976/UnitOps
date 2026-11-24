"""Rating characteristic of the Giacomelli et al. (2019) ejector at the case-1
inlet states (calibrated efficiencies): critical plateau, subcritical branch,
backflow.  Data: data/offdesign_case1.csv, giacomelli_sim.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, rows, save

rs = rows("offdesign_case1.csv")
Po, ER = col(rs, "PO_bar"), col(rs, "ER")
reg = np.array([r["regime"] for r in rs])
exp1 = [r for r in rows("giacomelli_sim.csv") if r["case"] == "1"][0]

fig, ax = plt.subplots(figsize=(3.6, 2.7))
for name, color in (("critical", NAVY), ("subcritical", RED), ("backflow", SLATE)):
    k = reg == name
    if k.any():
        ax.plot(Po[k], ER[k], "-", color=color, lw=2.2, label=name)
ax.plot(float(exp1["PO_bar"]), float(exp1["ER_exp"]), "*", color=AMBER, ms=9, mec=GREY, mew=0.6,
        label="measured, case 1")
pb = float(rs[0]["Pb_crit_bar"])                    # exact critical back pressure (bisected)
ax.axvline(pb, color=GREY, lw=0.8, ls=":")
ax.text(pb - 0.3, 0.13, "critical back\npressure $P_\\mathrm{b}^*$", ha="right",
        fontsize=7, color=GREY)
ax.set_xlabel(r"outlet pressure $P_\mathrm{out}$ [bar]")
ax.set_ylabel("entrainment ratio ER [-]")
ax.set_ylim(bottom=0)
ax.legend(loc="lower left", fontsize=6.5)
grid(ax)
save(fig, "offdesign_case1.png")
