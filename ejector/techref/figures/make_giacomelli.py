"""Calibration on Giacomelli et al. (2019), 42 R744 points: motive flow (stage 1,
C_d) and suction flow (stage 2, eta_s/eta_mix/eta_d), before/after calibration
and the cross-regime check.  Data: data/giacomelli_sim.csv, giacomelli_fit.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, parity, rows, save

rs = rows("giacomelli_sim.csv")
fit = {r["fit_set"]: r for r in rows("giacomelli_fit.csv")}
inl = np.array([r["inlet"] for r in rs])
fig, (a, b) = plt.subplots(1, 2, figsize=(7.0, 3.2))

mx, m0, m1 = col(rs, "mm_exp_gs"), col(rs, "mm_uncal_gs"), col(rs, "mm_cal_gs")
mae = lambda s, x: np.abs((s - x) / x).mean() * 100
a.plot(mx, m0, "x", color=SLATE, label=f"HEM, $C_d$ = 1: MAE {mae(m0, mx):.1f} %")
for reg, mk in (("supercritical", "o"), ("subcritical", "s"), ("subcooled", "^")):
    k = inl == reg
    a.plot(mx[k], m1[k], mk, color=NAVY, mfc="white", mew=1.1, label=f"$C_d$ = {float(fit['all']['Cd_m']):.3f}, {reg}")
parity(a, 20, 55, band=0.05)
a.set_xlabel(r"measured $\dot m_\mathrm{m}$ [g s$^{-1}$]")
a.set_ylabel(r"predicted $\dot m_\mathrm{m}$ [g s$^{-1}$]")
a.set_title(f"(a) motive flow; calibrated MAE {mae(m1, mx):.1f} %, dotted ±5 %", fontsize=8)
a.legend(loc="upper left", fontsize=6.5)
grid(a)

sx, s0, s1, sc = col(rs, "ms_exp_gs"), col(rs, "ms_uncal_gs"), col(rs, "ms_cal_gs"), col(rs, "ms_cross_gs")
f = fit["all"]
b.plot(sx, s0, "x", color=SLATE, label=f"uncalibrated: MAE {mae(s0, sx):.1f} %")
b.plot(sx, sc, "d", color=AMBER, mfc="white", mew=1.0, ms=3.8,
       label=f"cross-regime: MAE {mae(sc, sx):.1f} %")
b.plot(sx, s1, "o", color=RED, mfc="white", mew=1.1,
       label=f"fit on all 42: MAE {mae(s1, sx):.1f} %")
for c, dy in ((21, 4), (15, -6)):                # leader lines to the two worst points
    k = col(rs, "case") == c
    b.annotate(f"case {c}", (sx[k][0], s1[k][0]), xytext=(42, dy), textcoords="offset points",
               fontsize=6.5, color=GREY, va="center",
               arrowprops=dict(arrowstyle="-", lw=0.5, color=GREY, shrinkA=0, shrinkB=3))
parity(b, 0, 25, band=0.15)
b.set_xlabel(r"measured $\dot m_\mathrm{s}$ [g s$^{-1}$]")
b.set_ylabel(r"predicted $\dot m_\mathrm{s}$ [g s$^{-1}$]")
b.set_title(r"(b) suction flow; dotted ±15 %", fontsize=8)
b.legend(loc="upper left", fontsize=6.5)
grid(b)
save(fig, "val_giacomelli.png")
