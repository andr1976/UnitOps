"""IG/RG stage-cut error on the MemPy propane/propylene system, this solver vs. DeJaco et al., at MATCHED
stage cut and pressure. Plotted in DeJaco's own convention: the ABSOLUTE stage-cut difference in
percentage POINTS, 100*(theta_IG - theta_RG) -- so our MemPy curve is directly comparable to their Fig. 8
(~13 points max). Data: data/c3_ig_rg.csv."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#002D40", "#D61F39", "#E6A740", "#82979F", "#4C4D4E"
plt.rcParams["font.family"] = "Arial"
plt.rcParams["font.size"] = 9
plt.rcParams["mathtext.default"] = "regular"

HERE = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "c3_ig_rg.csv"))))
pmpa = np.array([float(r["Pf_bar"]) / 10.0 for r in rows])   # bar -> MPa
tig = np.array([float(r["theta_ig_mempy"]) for r in rows])   # matched IG stage cut
# absolute stage-cut difference in percentage points (DeJaco Fig. 8 convention)
abs_mempy = 100.0 * (tig - np.array([float(r["theta_rg_mempy"]) for r in rows]))
abs_ours = 100.0 * (np.array([float(r["theta_ig_ours"]) for r in rows])
                    - np.array([float(r["theta_rg_ours"]) for r in rows]))

fig, ax = plt.subplots(figsize=(6.4, 4.3))
ax.plot(pmpa, abs_mempy, "s-", color=RED, lw=2, label="MemPy 2-D (DeJaco et al., Fig. 8), EOS-coupled")
ax.plot(pmpa, abs_ours, "o-", color=NAVY, lw=2, label="this solver (feed-evaluated PR $\\varphi$)")
ax.fill_between(pmpa, abs_ours, abs_mempy, color=SLATE, alpha=0.18)
ax.axvline(0.9, color=SLATE, ls=":", lw=1)
ax.annotate("0.9 MPa", (0.9, 0.4), color=SLATE, fontsize=8, ha="center")
ax.set_xlabel("feed pressure [MPa]")
ax.set_ylabel(r"stage-cut over-prediction $\theta_{IG}-\theta_{RG}$ [percentage points]")
ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)
ax.legend(loc="upper left", fontsize=8)
ax.set_title("Real-gas correction, propane/propylene, matched stage cut\n"
             "(DeJaco's absolute convention): this solver vs. MemPy", color=NAVY, fontsize=9.5)
fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_c3_ig_rg.png"), dpi=150)
print("wrote val_c3_ig_rg.png")
