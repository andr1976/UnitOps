"""IG/RG stage-cut error on the MemPy propane/propylene system, at MATCHED stage cut and pressure, in
DeJaco's absolute convention (percentage POINTS, 100*(theta_IG - theta_RG)). Three curves: MemPy's
EOS-coupled 2-D model, and this solver with constant feed-evaluated phi and with local phi(theta)
(table + interpolation). For propane/propylene the two phi treatments nearly coincide (the species have
near-equal fugacity coefficients), so local phi does not close the gap to MemPy. Data: data/c3_ig_rg.csv."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#1F3A5F", "#C0392B", "#D68910", "#7F8C8D", "#34495E"
plt.rcParams["font.family"] = "Arial"
plt.rcParams["font.size"] = 9
plt.rcParams["mathtext.default"] = "regular"

HERE = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "c3_ig_rg.csv"))))
pmpa = np.array([float(r["Pf_bar"]) / 10.0 for r in rows])
tig = np.array([float(r["theta_ig"]) for r in rows])
tig_mp = np.array([float(r["theta_ig_mempy"]) for r in rows])
abs_mempy = 100.0 * (tig_mp - np.array([float(r["theta_rg_mempy"]) for r in rows]))
abs_const = 100.0 * (tig - np.array([float(r["theta_rg_const"]) for r in rows]))
abs_local = 100.0 * (tig - np.array([float(r["theta_rg_local"]) for r in rows]))

fig, ax = plt.subplots(figsize=(6.4, 4.3))
ax.plot(pmpa, abs_mempy, "s-", color=RED, lw=2, label="MemPy 2-D (DeJaco et al.), EOS-coupled")
ax.plot(pmpa, abs_const, "o-", color=NAVY, lw=2, label="this solver, constant feed-$\\varphi$")
ax.plot(pmpa, abs_local, "x--", color=AMBER, lw=1.6, ms=7, label="this solver, local $\\varphi(\\theta)$ (table+interp)")
ax.fill_between(pmpa, abs_const, abs_mempy, color=SLATE, alpha=0.15)
ax.axvline(0.9, color=SLATE, ls=":", lw=1)
ax.annotate("0.9 MPa", (0.9, 0.4), color=SLATE, fontsize=8, ha="center")
ax.set_xlabel("feed pressure [MPa]")
ax.set_ylabel(r"stage-cut over-prediction $\theta_{IG}-\theta_{RG}$ [percentage points]")
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="upper left", fontsize=8)
ax.set_title("Real-gas correction, propane/propylene (matched stage cut):\n"
             "constant vs. local $\\varphi$ vs. MemPy", color=NAVY, fontsize=9.5)
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_c3_ig_rg.png"), dpi=150)
print("wrote val_c3_ig_rg.png")
