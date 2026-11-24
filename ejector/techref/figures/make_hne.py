"""Delayed flashing (HNE factor phi) vs discharge coefficient Cd on the 42
Giacomelli et al. (2019) motive flows: the correction each point needs on its
own, and the single value fitted on all points.  Data: data/giacomelli_hne.csv,
giacomelli_hne_summary.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, col, grid, rows, save

rs = rows("giacomelli_hne.csv")
summ = {r["correction"]: r for r in rows("giacomelli_hne_summary.csv")}
case = col(rs, "case")
inl = np.array([r["inlet"] for r in rs])
marks = (("supercritical", "o", NAVY), ("subcritical", "s", RED), ("subcooled", "^", AMBER))

fig, (a, b) = plt.subplots(1, 2, figsize=(7.0, 2.8))
for ax, key, name, sym in ((a, "Cd_required", "Cd", r"$C_d$"),
                           (b, "phi_required", "phi", r"$\varphi$")):
    y = col(rs, key)
    for reg, mk, color in marks:
        k = inl == reg
        ax.plot(case[k], y[k], mk, color=color, mfc="white", mew=1.1, ms=4.5, label=reg)
    s = summ[name]
    ax.axhline(float(s["value"]), color=GREY, lw=1.0, ls="--")
    ax.set_xlabel("Giacomelli case [-]")
    ax.set_ylabel(f"{sym} matching the measured flow [-]")
    ax.set_title(f"({'a' if name == 'Cd' else 'b'}) one {sym} for all = {float(s['value']):.3f}: "
                 f"MAE {float(s['MAE_pct']):.1f} %, max {float(s['max_pct']):.1f} %", fontsize=8)
    grid(ax)
lo, hi = a.get_ylim()
a.set_ylim(lo, hi + 0.35 * (hi - lo))           # headroom for the legend
a.legend(loc="upper left", fontsize=6.5, ncol=3)
b.set_ylim(0, 1.0)
b.legend(loc="upper left", fontsize=6.5, ncol=3)
save(fig, "val_hne.png")
