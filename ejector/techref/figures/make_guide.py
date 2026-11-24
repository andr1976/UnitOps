"""Parameter guide figures.

guide_cd.png  : discharge coefficient needed by the HEM motive flux against reduced motive
                pressure, per measured point (data/guide_cd.csv)
guide_gea.png : GEA steam-jet vacuum pump chart against the optimum design, both mixing
                models (data/guide_gea.csv)
"""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, rows, save

# ---------------------------------------------------------------- Cd against reduced pressure
rs = rows("guide_cd.csv")
fig, ax = plt.subplots(figsize=(5.2, 3.3))
groups = [("Bodys2020", "subcooled liquid", "o", NAVY, "CO$_2$ subcooled liquid (Bodys)"),
          ("Bodys2020", "near-saturated liquid", "^", AMBER, "CO$_2$ near-saturated liquid (Bodys)"),
          ("Giacomelli2019", "subcooled liquid", "v", NAVY, "CO$_2$ subcooled liquid (Giacomelli)"),
          ("Giacomelli2019", "subcritical (dense) liquid", "s", SLATE, "CO$_2$ dense liquid (Giacomelli)"),
          ("Giacomelli2019", "supercritical", "D", GREY, "CO$_2$ supercritical (Giacomelli)"),
          ("Lawrence2012", "two-phase (flashed)", "P", RED, "R134a / R1234yf, flashed inlet (Lawrence)"),
          ("Falat2018", "superheated vapour", "x", GREY, "R134a superheated vapour (Falat)")]
for s, st, mk, c, lab in groups:
    sel = [r for r in rs if r["set"] == s and r["state"] == st]
    if not sel:
        continue
    filled = mk in ("x", "P")
    ax.plot(col(sel, "P_r"), col(sel, "cd_req"), mk, color=c, ms=3.8, mew=0.9,
            mfc=c if filled else "white", label=lab)
ax.axhline(1.0, color=GREY, lw=0.8)
ax.axhline(1.13759, color=GREY, lw=0.8, ls="--")
ax.text(0.42, 1.15, "R744 calibration 1.138", ha="left", va="bottom", fontsize=6.5, color=GREY)
ax.set_xlabel(r"reduced motive pressure $P_\mathrm{m0}/P_\mathrm{c}$ [-]")
ax.set_ylabel(r"$C_d$ matching the measured motive flow [-]")
ax.set_xlim(0.2, 1.45)
lo, hi = ax.get_ylim()
ax.set_ylim(0.75, max(hi, 1.9) + 0.55)
ax.legend(loc="upper right", fontsize=6.0, ncol=2, handletextpad=0.3, columnspacing=0.8)
grid(ax)
save(fig, "guide_cd.png")

# ---------------------------------------------------------------- GEA steam chart
g = [r for r in rows("guide_gea.csv") if r["chart"] == "steam" and r["eff_set"] == "GasEjector_Ma2017"]
fig, (a, b) = plt.subplots(1, 2, figsize=(7.0, 3.0))
curves = list(dict.fromkeys(r["curve"] for r in g))
cmap = plt.get_cmap("viridis")
TOP = 12.0
for i, cv in enumerate(curves):
    c = cmap(i / max(len(curves) - 1, 1) * 0.9)
    cpm = [r for r in g if r["curve"] == cv and r["mixing"] == "CPM"]
    x, y = col(cpm, "x"), col(cpm, "chart_value")
    o = np.argsort(x)
    a.plot(x[o], y[o], "-", color=c, lw=1.1, label=cv.replace("p1=", "$p_1$ = "))
    m = col(cpm, "model_value")
    ok = np.isfinite(m) & (m <= TOP)
    a.plot(x[ok], m[ok], "o", color=c, mfc="white", ms=3.6, mew=0.9)
    a.plot(x[~ok], np.full((~ok).sum(), TOP), "^", color=c, ms=4.0)
a.set_xscale("log")
a.set_yscale("log")
a.set_xlim(95, 650)
a.set_ylim(0.7, TOP * 1.6)
a.set_xticks([100, 200, 300, 400, 600])
a.set_xticklabels(["100", "200", "300", "400", "600"])
a.set_yticks([1, 2, 3, 5, 8, 12])
a.set_yticklabels(["1", "2", "3", "5", "8", ">12"])
a.set_xlabel(r"suction pressure $p_0$ [mbar]")
a.set_ylabel("steam / air [kg/kg]")
a.set_title("(a) GEA chart (lines) and CPM design (circles)", fontsize=8)
a.legend(loc="upper right", fontsize=5.8, ncol=2, handlelength=1.2)
grid(a)
for mixing, mk, color in (("CAM", "s", RED), ("CPM", "o", NAVY)):
    sel = [r for r in g if r["mixing"] == mixing]
    ch, mo = col(sel, "chart_value"), col(sel, "model_value")
    mo = np.where(np.isfinite(mo), np.minimum(mo, 30.0), 30.0)
    b.plot(ch, mo, mk, color=color, mfc="white", ms=3.6, mew=0.9, label=mixing)
for f, ls in ((1.0, "-"), (0.75, ":"), (1.25, ":")):
    b.plot([0.7, 30], [0.7 * f, 30 * f], ls, color=GREY, lw=0.8)
b.set_xscale("log")
b.set_yscale("log")
b.set_xlim(0.7, 10)
b.set_ylim(0.5, 35)
b.set_xticks([1, 2, 3, 5, 10])
b.set_xticklabels(["1", "2", "3", "5", "10"])
b.set_yticks([1, 2, 5, 10, 30])
b.set_yticklabels(["1", "2", "5", "10", "30"])
b.minorticks_off()
a.minorticks_off()
b.set_xlabel("GEA chart [kg steam/kg air]")
b.set_ylabel("optimum design [kg/kg] (30 = not reached)")
b.set_title(r"(b) parity, $\pm$25 % dotted", fontsize=8)
b.legend(loc="upper left", fontsize=6.5)
grid(b)
save(fig, "guide_gea.png")
