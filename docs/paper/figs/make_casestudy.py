"""Case-study sweep figures: two-stage CO2/CH4 NG sweetening.
Sweep B (permeate pressure) is the COFE parametric study (data/sweepB_permP.csv,
derived from data/cofe_permeate_pressure_sweep.csv). Sweeps A and C are from the
simulator-independent core, validated against COFE. Figures are sized for a
double-column single-column slot (~3.5 in wide) with real point sizes; captions
carry the description, so no in-figure titles. Run: python make_casestudy.py"""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#1F3A5F", "#C0392B", "#D68910", "#7F8C8D", "#34495E"
plt.rcParams.update({
    "font.family": "Arial", "font.size": 8,
    "axes.labelsize": 9, "axes.titlesize": 9,
    "xtick.labelsize": 8, "ytick.labelsize": 8,
    "legend.fontsize": 7, "lines.linewidth": 1.6, "lines.markersize": 4.5,
    "axes.linewidth": 0.8, "savefig.dpi": 300, "figure.dpi": 300,
})
FIG = (3.5, 2.65)  # single-column, ~1:1 at \linewidth in cas-dc
HERE = os.path.dirname(os.path.abspath(__file__))
def load(n):
    r = list(csv.DictReader(open(os.path.join(HERE, "data", n))))
    return {k: np.array([float(x[k]) for x in r]) for k in r[0]}
def grid(ax):
    ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)

# A: feed pressure, ideal-gas vs real-gas
A = load("sweepA_feedP.csv")
fig, ax = plt.subplots(figsize=FIG)
ax.plot(A["Pr_bar"], A["areaP_rg"], "o-", color=RED, label="real gas")
ax.plot(A["Pr_bar"], A["areaP_ig"], "s--", color=NAVY, label="ideal gas")
ax.fill_between(A["Pr_bar"], A["areaP_ig"], A["areaP_rg"], color=AMBER, alpha=0.25, label="under-sizing")
ax.set_xlabel("feed pressure [bar]"); ax.set_ylabel("primary area [m$^2$]")
grid(ax); ax.legend(loc="upper right", framealpha=0.9)
ax2 = ax.twinx(); under = 100 * (A["areaP_rg"] - A["areaP_ig"]) / A["areaP_rg"]
ax2.plot(A["Pr_bar"], under, ":", color=GREY, lw=1.4); ax2.set_ylim(0, 40)
ax2.set_ylabel("IG area\nunder-prediction [%]", color=GREY); ax2.tick_params(axis="y", colors=GREY)
fig.tight_layout(); fig.savefig(os.path.join(HERE, "case_realgas.png"))

# B: permeate pressure trade-off (COFE parametric study)
B = load("sweepB_permP.csv")
fig, ax = plt.subplots(figsize=FIG)
ax.plot(B["Pp1_bar"], B["areaTot"], "o-", color=NAVY)
ax.set_xlabel("stage-1 permeate pressure [bar]"); ax.set_ylabel("total area [m$^2$]", color=NAVY)
ax.tick_params(axis="y", colors=NAVY); grid(ax)
ax2 = ax.twinx(); ax2.plot(B["Pp1_bar"], B["power"], "s--", color=RED)
ax2.set_ylabel("compression power [MW]", color=RED); ax2.tick_params(axis="y", colors=RED)
n = len(B["Pp1_bar"])
for i in range(n):
    if i % 2 == 0 or i == n - 1:
        ax.annotate(f"{B['ch4rec'][i]:.0f}%", (B["Pp1_bar"][i], B["areaTot"][i]),
                    fontsize=6.5, color=GREY, xytext=(2, 3), textcoords="offset points")
fig.tight_layout(); fig.savefig(os.path.join(HERE, "case_permeate.png"))

# C: membrane selectivity
C = load("sweepC_alpha.csv")
fig, ax = plt.subplots(figsize=FIG)
ax.plot(C["alpha"], C["ch4rec"], "o-", color=RED)
ax.set_xlabel("CO$_2$/CH$_4$ selectivity α"); ax.set_ylabel("CH$_4$ recovery [%]")
grid(ax)
ax.axvline(20, color=SLATE, ls=":", lw=1); ax.text(20.7, 79, "base\n(α=20)", color=GREY, fontsize=7)
ax.axvline(5, color=SLATE, ls=":", lw=1); ax.text(5.6, 79, "CA (α≈5)", color=GREY, fontsize=7)
fig.tight_layout(); fig.savefig(os.path.join(HERE, "case_selectivity.png"))

# D: block-flow diagram of the two-stage permeate-recycle configuration (full width)
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
MEMB, AUX = "#DCE6EC", "#EEF2F4"
fig, ax = plt.subplots(figsize=(7.2, 3.9)); ax.set_xlim(0, 10); ax.set_ylim(0, 5); ax.axis("off")
def box(x, y, w, h, label, fc):
    ax.add_patch(FancyBboxPatch((x - w/2, y - h/2), w, h, boxstyle="round,pad=0.02,rounding_size=0.06",
                                linewidth=1.4, edgecolor=NAVY, facecolor=fc))
    ax.text(x, y, label, ha="center", va="center", fontsize=10, color=NAVY)
def arrow(x1, y1, x2, y2, color=GREY):
    ax.add_patch(FancyArrowPatch((x1, y1), (x2, y2), arrowstyle="-|>", mutation_scale=13, lw=1.5, color=color, shrinkA=0, shrinkB=0))
box(2.0, 3.6, 1.1, 0.7, "Mixer", AUX)
box(4.5, 3.6, 1.5, 0.85, "Primary\nmembrane", MEMB)
box(4.5, 1.4, 1.7, 0.85, "3-stage\ncompressor", AUX)
box(7.6, 1.4, 1.6, 0.85, "Secondary\nmembrane", MEMB)
arrow(0.4, 3.6, 1.45, 3.6); ax.text(0.9, 3.87, "feed", ha="center", fontsize=9, color=GREY)
arrow(2.55, 3.6, 3.75, 3.6)
arrow(5.25, 3.6, 6.7, 3.6); ax.text(6.25, 3.95, "sweet gas (2 mol% CO$_2$)", ha="center", fontsize=9, color=GREY)
arrow(4.5, 3.15, 4.5, 1.85); ax.text(5.6, 2.5, "permeate\n(2 bar)", ha="center", fontsize=9, color=GREY)
arrow(5.35, 1.4, 6.8, 1.4)
arrow(7.6, 0.95, 7.6, 0.2); ax.text(8.65, 0.55, "CO$_2$ vent", ha="center", fontsize=9, color=GREY)
for seg in [((8.4, 1.4), (9.35, 1.4)), ((9.35, 1.4), (9.35, 4.55)), ((9.35, 4.55), (2.0, 4.55))]:
    ax.add_patch(FancyArrowPatch(seg[0], seg[1], arrowstyle="-", lw=1.5, color=RED))
arrow(2.0, 4.55, 2.0, 3.98, color=RED)
ax.text(5.7, 4.75, "recycle (secondary retentate)", ha="center", fontsize=9, color=RED)
fig.tight_layout(); fig.savefig(os.path.join(HERE, "case_bfd.png"))
print("wrote case_realgas.png, case_permeate.png, case_selectivity.png, case_bfd.png")
