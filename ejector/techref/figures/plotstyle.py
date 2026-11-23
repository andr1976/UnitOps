"""Shared matplotlib style for the ejector reference (same palette and sizes as
the membrane reference in docs/techref/figures)."""
import csv
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

NAVY, RED, AMBER, SLATE, GREY = "#1F3A5F", "#C0392B", "#D68910", "#7F8C8D", "#34495E"
plt.rcParams.update({
    "font.family": "Arial", "font.size": 8, "mathtext.default": "regular",
    "axes.labelsize": 9, "xtick.labelsize": 8, "ytick.labelsize": 8,
    "legend.fontsize": 7, "lines.linewidth": 1.4, "lines.markersize": 4.5,
    "axes.linewidth": 0.8, "savefig.dpi": 400, "figure.dpi": 400,
})
HERE = os.path.dirname(os.path.abspath(__file__))


def rows(name):
    with open(os.path.join(HERE, "data", name)) as f:
        return list(csv.DictReader(f))


def col(rs, key):
    import numpy as np
    return np.array([float(r[key]) for r in rs])


def grid(ax):
    ax.grid(True, color=SLATE, alpha=0.25, lw=0.5)


def parity(ax, lo, hi, band=None):
    ax.plot([lo, hi], [lo, hi], "-", color=GREY, lw=0.8)
    if band:
        for f in (1 - band, 1 + band):
            ax.plot([lo, hi], [lo * f, hi * f], ":", color=SLATE, lw=0.8)
    ax.set_xlim(lo, hi)
    ax.set_ylim(lo, hi)
    ax.set_aspect("equal")


def save(fig, name):
    fig.tight_layout()
    fig.savefig(os.path.join(HERE, name), dpi=400)
    print(f"wrote {name}")
