"""Mass flux along the nozzle expansion path; its maximum defines choking.
(a) isentropic nitrogen vs the analytic ideal-gas curve, (b) flashing CO2 motive
states (HEM and delayed-flashing HNE).  Data: data/nozzle_flux.csv, nozzle_critical.csv."""
import numpy as np
import matplotlib.pyplot as plt
from plotstyle import AMBER, GREY, NAVY, RED, SLATE, col, grid, rows, save

flux, crit = rows("nozzle_flux.csv"), rows("nozzle_critical.csv")
fig, (a, b) = plt.subplots(1, 2, figsize=(7.0, 2.75))

n2 = [r for r in flux if r["case"].startswith("N2")]
r, G, Gig = col(n2, "P_over_P0"), col(n2, "G"), col(n2, "G_ideal_gas")
a.plot(r, Gig / 1e3, "-", color=AMBER, lw=2.4, label="ideal gas, analytic ($k$ = 1.4)")
a.plot(r, G / 1e3, "--", color=NAVY, label=r"real-fluid path, max$_P$ $\rho u$")
rc = (2 / 2.4) ** (1.4 / 0.4)
a.axvline(rc, color=GREY, lw=0.8, ls=":")
a.text(rc + 0.02, 0.3 * G.max() / 1e3, f"$P^*/P_0$ = {rc:.3f}", color=GREY, fontsize=7)
a.set_xlabel(r"$P/P_0$ [-]")
a.set_ylabel(r"$G = \rho u$ [t m$^{-2}$ s$^{-1}$]")
a.set_title("(a) N$_2$, 10 bar, 300 K, $\\eta$ = 1", fontsize=8)
a.legend(loc="lower center")
grid(a)

styles = {"CO2_47bar_satliq_HEM": (NAVY, "-", "47 bar sat. liquid, HEM"),
          "CO2_47bar_satliq_HNE0.5": (RED, "--", r"47 bar sat. liquid, HNE $\varphi$ = 0.5"),
          "CO2_84.5bar_32C_HEM": (AMBER, "-", "84.5 bar, 32 °C, HEM")}
for c in crit:
    color, ls, lab = styles[c["case"]]
    sel = [q for q in flux if q["case"] == c["case"]]
    b.plot(col(sel, "P_bar"), col(sel, "G") / 1e3, ls, color=color, label=lab)
    b.plot(float(c["Pt_bar"]), float(c["Gstar"]) / 1e3, "o", color=color, mfc="white", mew=1.4)
b.set_xlabel("static pressure $P$ [bar]")
b.set_ylabel(r"$G$ [t m$^{-2}$ s$^{-1}$]")
b.set_title(r"(b) CO$_2$ motive, $\eta_\mathrm{m}$ = 0.9 beyond the throat (o: $P_\mathrm{t}$, $G^*$)",
            fontsize=8)
gmax = max(float(c["Gstar"]) for c in crit) / 1e3
b.set_ylim(0, 1.35 * gmax)                     # headroom: legend above the curves
b.legend(loc="upper left", fontsize=6.5)
grid(b)
save(fig, "nozzle_flux.png")
