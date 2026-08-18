"""Validation against the Aziaba et al. (Membranes 2022) hollow-fibre counter-current test cases.
TC2 = Sada et al. experiment (CO2/O2/N2); TC3 = Chowdhury et al. (H2/N2/CH4/Ar).

Solid/dashed lines are this solver's counter-current PlugFlowModel (permeate composition vs overall
stage cut, area-free). The TC2 reference markers are the paper's three DWSIM prediction curves,
pixel-digitized from Figure 8 (see digitize_aziaba.py -> data/dwsim_tc2.csv); the experimental crosses
share each pressure's colour and cannot be separated, but the paper reports the experiment lies on those
curves to <0.84 %. TC3 (Figure 9) is a broken-axis plot that does not calibrate reliably, so its only
plotted reference is the single value the paper states in text (H2 approximately 97.5 % at theta = 0.3);
the rest of TC3 is compared numerically in the text. TC3 uses the corrected Ar permeance (7.0, not the
printed 70). Data: data/aziaba_tc2.csv, data/aziaba_tc3.csv, data/dwsim_tc2.csv."""
import csv, os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

NAVY, RED, AMBER, SLATE, GREY = "#1F3A5F", "#C0392B", "#D68910", "#7F8C8D", "#34495E"
plt.rcParams.update({
    "font.family": "Arial", "font.size": 8, "mathtext.default": "regular",
    "axes.labelsize": 9, "xtick.labelsize": 8, "ytick.labelsize": 8,
    "legend.fontsize": 7, "lines.linewidth": 1.4, "lines.markersize": 4.5,
    "axes.linewidth": 0.8, "savefig.dpi": 300, "figure.dpi": 300,
})
HERE = os.path.dirname(os.path.abspath(__file__))

def load(name):
    return list(csv.DictReader(open(os.path.join(HERE, "data", name))))

fig, (axA, axB) = plt.subplots(1, 2, figsize=(7.2, 3.0))

# --- Panel A: TC2 (Sada), yCO2 vs theta, three feed pressures: this work vs digitized DWSIM ---
r2 = load("aziaba_tc2.csv")
th2 = np.array([float(r["theta"]) for r in r2])
dw = load("dwsim_tc2.csv")
thd = np.array([float(r["theta"]) for r in dw])
for key, dwk, col, lab in [("yCO2_15p7bar", "y157", NAVY, "15.7 bar"),
                           ("yCO2_10p8bar", "y108", RED, "10.8 bar"),
                           ("yCO2_5p9bar", "y59", AMBER, "5.9 bar")]:
    axA.plot(th2, [float(r[key]) for r in r2], "-", color=col, lw=2, label=f"this work, {lab}")
    axA.plot(thd, [float(r[dwk]) for r in dw], "o", color=col, ms=5, mfc="none", mew=1.3,
             label=f"DWSIM (digitized), {lab}")
axA.set_xlabel(r"stage cut $\theta$"); axA.set_ylabel(r"permeate $y_{CO_2}$")
axA.set_xlim(0, 0.7); axA.set_ylim(0.70, 0.90)
axA.grid(True, color=SLATE, alpha=0.25, lw=0.5); axA.legend(fontsize=7, loc="lower left", ncol=1)
axA.set_title("TC2: CO$_2$/O$_2$/N$_2$ vs. DWSIM (Sada exp.)", color=NAVY, fontsize=10)

# --- Panel B: TC3 (Chowdhury), H2 on left axis, minors on right axis (this work) ---
r3 = load("aziaba_tc3.csv")
th3 = np.array([float(r["theta"]) for r in r3])
axB.plot(th3, [float(r["yH2"]) for r in r3], "-", color=NAVY, lw=2, label="H$_2$ (this work)")
# Only reference the paper states numerically for TC3: H2 ~ 97.5 % at theta = 0.3.
axB.plot([0.30], [0.975], "o", color=NAVY, ms=7, mfc="none", mew=1.6,
         label="Chowdhury/DWSIM, $\\theta$=0.3 (paper text)")
axB.set_xlabel(r"stage cut $\theta$"); axB.set_ylabel(r"permeate $y_{H_2}$", color=NAVY)
axB.set_ylim(0.90, 1.00); axB.tick_params(axis="y", colors=NAVY)
axB2 = axB.twinx()
for key, col, lab in [("yN2", RED, "N$_2$"), ("yCH4", AMBER, "CH$_4$"), ("yAr", SLATE, "Ar")]:
    axB2.plot(th3, [float(r[key]) for r in r3], "--", color=col, lw=1.6, label=f"{lab} (this work)")
axB2.set_ylabel("permeate $y_{N_2}, y_{CH_4}, y_{Ar}$", color=GREY); axB2.set_ylim(0, 0.05)
axB.set_xlim(0.30, 0.50)
axB.grid(True, color=SLATE, alpha=0.25, lw=0.5)
h1, l1 = axB.get_legend_handles_labels(); h2, l2 = axB2.get_legend_handles_labels()
axB.legend(h1 + h2, l1 + l2, fontsize=7, loc="center left")
axB.set_title("TC3: H$_2$/N$_2$/CH$_4$/Ar (Ar=7.0), model output", color=NAVY, fontsize=10)

fig.tight_layout()
fig.savefig(os.path.join(HERE, "val_aziaba.png"), dpi=300)
print("wrote val_aziaba.png")
