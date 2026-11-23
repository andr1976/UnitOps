"""Incompressible limit: loss-free water jet pump vs fluids.liquid_jet_pump (K = 0).
Data: data/fluids_limit.csv (gen_data.py fluids)."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, rows, save

rs = rows("fluids_limit.csv")
P5, ms, mm = col(rs, "P5_bar"), col(rs, "m_s"), col(rs, "m_m")
P5f, mmf = col(rs, "P5_fluids_bar"), col(rs, "m_m_fluids")
reg = np.array([r["regime"] for r in rs])
sub = reg == "subcritical"

fig, ax = plt.subplots(figsize=(3.6, 2.8))
ax.plot(ms[sub], P5[sub], "-", color=NAVY, lw=2, label="this work (rating, subcritical)")
ax.plot(ms[sub], P5f[sub], "o", color=RED, mfc="white", mew=1.3, label="fluids.liquid_jet_pump")
crit = reg == "critical"
ax.plot(ms[crit], P5[crit], "s", color=AMBER, ms=3.5, label="this work: cavitation-limited")
back = reg == "backflow"
ax.plot(ms[back], P5[back], "^", color=SLATE, ms=3.5, label="this work: no entrainment")
ax.set_xlabel(r"suction flow $\dot m_\mathrm{s}$ [kg s$^{-1}$]")
ax.set_ylabel(r"outlet pressure $P_5$ [bar]")
dev = np.nanmax(np.abs(P5f[sub] / P5[sub] - 1)) * 100
devm = np.nanmax(np.abs(mmf[sub] / mm[sub] - 1)) * 100
ax.text(0.03, 0.05, f"max |ΔP$_5$| = {dev:.3f} %\nmax |Δ$\\dot m_m$| = {devm:.3f} %",
        transform=ax.transAxes, fontsize=7, color=GREY)
ax.legend(loc="upper right")
grid(ax)
save(fig, "val_fluids.png")
