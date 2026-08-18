"""Cross-flow validation against Geankoplis Example 13.6-1 (Weller-Steiner analytic
air separation, O2/N2, alpha*=10, p_l/p_h=0.10, theta=0.20). Lines are this
implementation's solver output (data/geankoplis_crossflow_profile.csv); markers are
the book's Table 13.6-1 tabulated path values."""
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
    "axes.linewidth": 0.8, "savefig.dpi": 400, "figure.dpi": 400,
})

HERE = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "geankoplis_crossflow_profile.csv"))))
th = np.array([float(r["theta_local"]) for r in rows])
x_o2 = np.array([float(r["x_O2"]) for r in rows])
y_loc = np.array([float(r["y_local_O2"]) for r in rows])
y_col = np.array([float(r["y_collected_O2"]) for r in rows])

# Geankoplis Table 13.6-1 (theta*, x_O2, y_p == cumulative mixed permeate)
G_th = [0.0, 0.04876, 0.0992, 0.1482, 0.2000]
G_x = [0.209, 0.1870, 0.1642, 0.1420, 0.1190]
G_y = [0.6550, 0.6383, 0.6158, 0.5940, 0.5690]

fig, ax = plt.subplots(figsize=(3.5, 2.65))
ax.plot(th, y_col, "-", color=NAVY, lw=2, label="collected permeate $y_{O_2}$ (this work)")
ax.plot(th, y_loc, "--", color=RED, lw=1.6, label="local permeate $y_{O_2}$ (this work)")
ax.plot(th, x_o2, "-", color=SLATE, lw=2, label="retentate $x_{O_2}$ (this work)")
ax.plot(G_th, G_y, "o", color=NAVY, ms=6, mfc="white", mew=1.5, label="Geankoplis Table 13.6-1 (mixed permeate)")
ax.plot(G_th, G_x, "s", color=SLATE, ms=6, mfc="white", mew=1.5, label="Geankoplis Table 13.6-1 (retentate)")

ax.set_xlabel(r"cumulative stage cut $\theta$")
ax.set_ylabel(r"O$_2$ mole fraction")
ax.set_xlim(0, 0.20)
ax.set_ylim(0.10, 0.70)
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="center right", framealpha=0.95, fontsize=6.5)
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_geankoplis.png"), dpi=400)
print("wrote val_geankoplis.png")
