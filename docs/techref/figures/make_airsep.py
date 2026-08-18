"""Model-vs-experiment validation on the DeJaco et al. (MemPy) N2/O2 air-separation
data (12 operating points, Table 2). Lines/open markers are this implementation's
cross-flow solver (alpha*=2.54 from mid-range single-gas permeances); filled markers
are the measured permeate O2. Data: data/mempy_airsep.csv."""
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
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "mempy_airsep.csv"))))
th = np.array([float(r["theta"]) for r in rows])
pf = np.array([float(r["Pfeed_kPa"]) for r in rows])
meas = np.array([float(r["permO2_meas"]) for r in rows])
pred = np.array([float(r["permO2_pred"]) for r in rows])

fig, ax = plt.subplots(figsize=(3.5, 2.65))
for p, col, lab in [(205.0, NAVY, "205--206 kPa"), (274.0, RED, "274 kPa")]:
    m = np.abs(pf - p) < 5
    order = np.argsort(th[m])
    ax.plot(th[m], meas[m], "o", color=col, ms=7, label=f"measured, {lab}")
    ax.plot(th[m][order], pred[m][order], "--", color=col, lw=1.8, marker="x", ms=6,
            label=f"cross-flow (this work), {lab}")

ax.set_xlabel(r"stage cut $\theta$")
ax.set_ylabel(r"permeate O$_2$ mole fraction")
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="upper right", fontsize=7, ncol=1)
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_airsep.png"), dpi=400)
print("wrote val_airsep.png")
