"""Worked example: optimum-design outlet pressure vs entrainment ratio for liquid
(47 bar) and dense supercritical (84.5 bar, 32 C) CO2 motive entraining saturated
vapour at 15 bar.  Data: data/duty_curves.csv, duty_point.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, rows, save

rs = rows("duty_curves.csv")


def curve(mot, eff):
    sel = sorted([r for r in rs if r["motive"] == mot and r["eff_set"] == eff], key=lambda r: float(r["ER"]))
    return col(sel, "ER"), col(sel, "Pout_bar")


fig, ax = plt.subplots(figsize=(3.8, 2.9))
x, y = curve("47bar_satliq", "typical")
ax.plot(x, y, "-", color=SLATE, lw=1.8, label="47 bar sat. liquid, typical $\\eta$")
x, y = curve("84.5bar_32C", "typical")
ax.plot(x, y, "--", color=NAVY, lw=1.6, label="84.5 bar / 32 °C, typical $\\eta$")
fits = [curve("84.5bar_32C", e) for e in ("fit_all", "fit_supercritical", "fit_sub_subcooled")]
lo = np.min([f[1] for f in fits], axis=0)
hi = np.max([f[1] for f in fits], axis=0)
ax.fill_between(fits[0][0], lo, hi, color=RED, alpha=0.25, lw=0)
ax.plot(*fits[0], "-", color=RED, lw=2, label="84.5 bar / 32 °C, calibrated (band: 3 fits)")
ax.axhline(28, color=AMBER, lw=1.2, ls="-.")
ax.text(0.40, 28.3, "required 28 bar", color=AMBER, fontsize=7)
ER0 = 2.0 / 13.66
ax.axvline(ER0, color=GREY, lw=0.8, ls=":")
ax.text(ER0 + 0.006, 0.02, "duty ER = 0.146", color=GREY, fontsize=7,
        transform=ax.get_xaxis_transform(), va="bottom")
ax.set_xlabel("entrainment ratio ER [-]")
ax.set_ylabel(r"optimum-design $P_\mathrm{out}$ [bar]")
ax.set_xlim(0, 0.5)
ax.legend(loc="upper right", fontsize=6.5)
grid(ax)
save(fig, "case_duty.png")
