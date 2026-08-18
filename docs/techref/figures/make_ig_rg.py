"""Ideal-gas vs. real-gas (fugacity) driving force, this implementation's own comparison.
CO2/CH4 cross-flow at fixed membrane area over a range of feed pressures; fugacity coefficients
from a Peng-Robinson EOS (CO2/CH4). The ideal-gas partial-pressure driving force progressively
over-predicts the stage cut as the feed pressure rises and phi departs from unity. Data: data/ig_rg.csv."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#1F3A5F", "#C0392B", "#D68910", "#7F8C8D", "#34495E"
plt.rcParams.update({
    "font.family": "Arial", "font.size": 8, "mathtext.default": "regular",
    "axes.labelsize": 9, "xtick.labelsize": 8, "ytick.labelsize": 8,
    "legend.fontsize": 7, "lines.linewidth": 1.4, "lines.markersize": 4.5,
    "axes.linewidth": 0.8, "savefig.dpi": 300, "figure.dpi": 300,
})

HERE = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "ig_rg.csv"))))
pr = np.array([float(r["Pr_bar"]) for r in rows])
tig = np.array([float(r["theta_ig"]) for r in rows])
trg = np.array([float(r["theta_rg"]) for r in rows])
phiCO2 = np.array([float(r["phiCO2"]) for r in rows])

fig, ax = plt.subplots(figsize=(3.5, 2.65))
ax.plot(pr, tig, "o-", color=NAVY, lw=2, label=r"ideal gas ($\varphi$=1), partial pressure")
ax.plot(pr, trg, "s--", color=RED, lw=2, label=r"real gas (fugacity), PR EOS")
ax.fill_between(pr, trg, tig, color=AMBER, alpha=0.25, label="over-prediction")
ax.set_xlabel("feed (retentate) pressure $p_r$ [bar]")
ax.set_ylabel(r"stage cut $\theta$")
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="upper left", fontsize=7)

# secondary axis: CO2 fugacity coefficient (the source of the correction)
ax2 = ax.twinx()
ax2.plot(pr, phiCO2, ":", color=SLATE, lw=1.6)
ax2.set_ylabel(r"$\varphi_{CO_2}$ (PR)", color=SLATE)
ax2.tick_params(axis="y", colors=SLATE)
ax2.set_ylim(0.6, 1.0)

# annotate the gap at 60 bar
i60 = int(np.argmin(np.abs(pr - 60)))
gap = 100 * (tig[i60] - trg[i60]) / trg[i60]
ax.annotate(f"+{gap:.0f}% at 60 bar", (pr[i60], 0.5 * (tig[i60] + trg[i60])),
            fontsize=8, color=GREY, ha="left")
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_ig_rg.png"), dpi=300)
print("wrote val_ig_rg.png")
