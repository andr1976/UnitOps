"""Multicomponent validation against the Shindo et al. (1985) benchmark (Case 1,
NH3/H2/N2, gamma=0.13), as tabulated by Dias & Pinto (2020). Grouped bars compare
this implementation's permeate composition to the Shindo reference for the
cross-flow and counter-current patterns (data/shindo_case1.csv)."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#002D40", "#D61F39", "#E6A740", "#82979F", "#4C4D4E"
plt.rcParams["font.family"] = "Arial"
plt.rcParams["font.size"] = 9

HERE = os.path.dirname(os.path.abspath(__file__))
rows = list(csv.DictReader(open(os.path.join(HERE, "data", "shindo_case1.csv"))))
comp = [r["component"] for r in rows]
mine_cross = [float(r["mine_cross"]) for r in rows]
ref_cross = [float(r["ref_cross"]) for r in rows]
mine_counter = [float(r["mine_counter"]) for r in rows]
ref_counter = [float(r["ref_counter"]) for r in rows]

x = np.arange(len(comp))
w = 0.38
fig, axes = plt.subplots(1, 2, figsize=(7.4, 3.8), sharey=True)
for ax, mine, ref, title in [
    (axes[0], mine_cross, ref_cross, "Cross-flow ($\\theta$=0.373)"),
    (axes[1], mine_counter, ref_counter, "Counter-current ($\\theta$=0.375)"),
]:
    ax.bar(x - w/2, ref, w, color=SLATE, label="Shindo (1985)")
    ax.bar(x + w/2, mine, w, color=NAVY, label="this work")
    ax.set_xticks(x); ax.set_xticklabels(comp)
    ax.set_title(title, color=NAVY, fontsize=10)
    ax.grid(True, axis="y", color=SLATE, alpha=0.25, lw=0.5)
    for xi, (m, r) in enumerate(zip(mine, ref)):
        d = abs(m - r)
        ax.annotate(f"$\\Delta$={d:.1e}", (xi, max(m, r) + 0.02), ha="center", fontsize=7, color=GREY)

axes[0].set_ylabel("permeate mole fraction")
axes[0].set_ylim(0, 0.9)
axes[0].legend(loc="upper right", fontsize=8)
fig.suptitle("Multicomponent benchmark: Shindo et al. (1985) Case 1 (NH$_3$/H$_2$/N$_2$)",
             color=NAVY, fontsize=11)
fig.tight_layout(rect=[0, 0, 1, 0.95])
fig.savefig(os.path.join(HERE, "val_shindo_case1.png"), dpi=150)
print("wrote val_shindo_case1.png")
