"""Validation against the Aziaba et al. (Membranes 2022) hollow-fibre counter-current test cases.
TC2 = Sada et al. experiment (CO2/O2/N2), TC3 = Chowdhury et al. (H2/N2/CH4/Ar). Lines are this solver's
counter-current PlugFlowModel (permeate composition vs overall stage cut, area-free); markers are points
read from the paper's Figures 8 and 9. TC3 uses the corrected Ar permeance (7.0, not the printed 70).
Data: data/aziaba_tc2.csv, data/aziaba_tc3.csv."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#002D40", "#D61F39", "#E6A740", "#82979F", "#4C4D4E"
plt.rcParams["font.family"] = "Arial"; plt.rcParams["font.size"] = 9
plt.rcParams["mathtext.default"] = "regular"
HERE = os.path.dirname(os.path.abspath(__file__))

def load(name):
    rows = list(csv.DictReader(open(os.path.join(HERE, "data", name))))
    return rows, rows[0].keys()

fig, (axA, axB) = plt.subplots(1, 2, figsize=(9.6, 4.3))

# --- Panel A: TC2 (Sada), yCO2 vs theta, three feed pressures ---
r2, _ = load("aziaba_tc2.csv")
th2 = np.array([float(r["theta"]) for r in r2])
for key, col, lab in [("yCO2_15p7bar", NAVY, "15.7 bar"), ("yCO2_10p8bar", RED, "10.8 bar"),
                       ("yCO2_5p9bar", AMBER, "5.9 bar")]:
    axA.plot(th2, [float(r[key]) for r in r2], "-", color=col, lw=2, label=f"this work, {lab}")
# Sada et al. points read from Fig. 8 (track the family of curves)
sada = [(0.15, 0.862), (0.28, 0.840), (0.40, 0.818), (0.52, 0.793), (0.60, 0.772), (0.66, 0.735)]
axA.plot([p[0] for p in sada], [p[1] for p in sada], "+", color=GREY, ms=9, mew=1.6,
         label="Sada et al. (Fig. 8)")
axA.set_xlabel(r"stage cut $\theta$"); axA.set_ylabel(r"permeate $y_{CO_2}$")
axA.set_xlim(0, 0.7); axA.set_ylim(0.70, 0.90)
axA.grid(True, color=SLATE, alpha=0.25, lw=0.5); axA.legend(fontsize=7.5, loc="lower left")
axA.set_title("TC2: CO$_2$/O$_2$/N$_2$ vs. Sada et al. (experiment)", color=NAVY, fontsize=10)

# --- Panel B: TC3 (Chowdhury), H2 on left axis, minors on right axis ---
r3, _ = load("aziaba_tc3.csv")
th3 = np.array([float(r["theta"]) for r in r3])
axB.plot(th3, [float(r["yH2"]) for r in r3], "-", color=NAVY, lw=2, label="H$_2$ (this work)")
axB.set_xlabel(r"stage cut $\theta$"); axB.set_ylabel(r"permeate $y_{H_2}$", color=NAVY)
axB.set_ylim(0.90, 1.00); axB.tick_params(axis="y", colors=NAVY)
chow_h2 = [(0.325, 0.972), (0.375, 0.966), (0.42, 0.958), (0.45, 0.950), (0.48, 0.940)]
axB.plot([p[0] for p in chow_h2], [p[1] for p in chow_h2], "+", color=NAVY, ms=8, mew=1.5,
         label="Chowdhury H$_2$ (Fig. 9)")
axB2 = axB.twinx()
for key, col, lab, pts in [
    ("yN2", RED, "N$_2$", [(0.325, 0.011), (0.40, 0.016), (0.46, 0.024)]),
    ("yCH4", AMBER, "CH$_4$", [(0.325, 0.010), (0.40, 0.013), (0.46, 0.019)]),
    ("yAr", SLATE, "Ar", [(0.325, 0.005), (0.40, 0.007), (0.46, 0.010)])]:
    axB2.plot(th3, [float(r[key]) for r in r3], "--", color=col, lw=1.6, label=f"{lab} (this work)")
    axB2.plot([p[0] for p in pts], [p[1] for p in pts], "x", color=col, ms=6)
axB2.set_ylabel("permeate $y_{N_2}, y_{CH_4}, y_{Ar}$", color=GREY); axB2.set_ylim(0, 0.05)
axB.set_xlim(0.30, 0.50)
axB.grid(True, color=SLATE, alpha=0.25, lw=0.5)
h1, l1 = axB.get_legend_handles_labels(); h2, l2 = axB2.get_legend_handles_labels()
axB.legend(h1 + h2, l1 + l2, fontsize=7, loc="center left")
axB.set_title("TC3: H$_2$/N$_2$/CH$_4$/Ar vs. Chowdhury et al. (Ar=7.0)", color=NAVY, fontsize=10)

fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_aziaba.png"), dpi=150)
print("wrote val_aziaba.png")
