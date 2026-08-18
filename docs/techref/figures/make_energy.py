"""Non-isothermal energy-layer validation. A synthetic linear Joule-Thomson fluid
(mu = 1e-6 K/Pa) is fed to the adiabatic energy balance for a CO2/CH4 cross-flow
separation (theta=0.30, p_p=2 bar) over a range of feed pressures. The permeate
cools by exactly mu*(p_r-p_p) while the retentate stays at the feed temperature;
an ideal-gas fluid shows no change. Data: data/energy_jt.csv."""
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
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "energy_jt.csv"))))
pr = np.array([float(r["Pr_bar"]) for r in rows])
tp_jt = np.array([float(r["Tp_jt"]) for r in rows])
tr_jt = np.array([float(r["Tr_jt"]) for r in rows])
tp_id = np.array([float(r["Tp_ideal"]) for r in rows])
Tfeed = 313.15
pp_bar = 2.0
mu = 1.0e-6  # K/Pa

fig, ax = plt.subplots(figsize=(3.5, 2.65))
ax.axhline(Tfeed, color=GREY, lw=1, ls=":", label=f"feed T = {Tfeed:.2f} K")
ax.plot(pr, tp_jt, "o-", color=RED, lw=2, label="permeate T (JT fluid)")
ax.plot(pr, tr_jt, "s-", color=NAVY, lw=2, label="retentate T (JT fluid)")
ax.plot(pr, tp_id, "^--", color=SLATE, lw=1.6, label="permeate T (ideal gas)")

# analytic line: Tp = Tfeed + mu*(pp - pr)
pr_pa = pr * 1e5
ax.plot(pr, Tfeed + mu * (pp_bar * 1e5 - pr_pa), "-", color=AMBER, lw=1,
        label=r"analytic $T_f + \mu(p_p - p_r)$")

ax.set_xlabel("feed (retentate) pressure $p_r$ [bar]")
ax.set_ylabel("outlet temperature [K]")
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="lower left", fontsize=7)
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_energy_jt.png"), dpi=300)
print("wrote val_energy_jt.png")
