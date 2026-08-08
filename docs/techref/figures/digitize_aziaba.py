"""Digitize the three DWSIM prediction curves from Aziaba et al. (Membranes 2022) Figure 8 (test case 2,
Sada CO2/O2/N2), for the counter-current validation. Each curve is extracted as the median coloured pixel
per image column, so the sparse experimental crosses -- which share each pressure's colour and therefore
cannot be separated from the DWSIM markers -- do not perturb it; the in-plot legend box is masked.

Requires the source PDF (copyrighted, not distributed in the repo). Writes data/dwsim_tc2.csv.
(Figure 9 / test case 3 is a split-axis plot that does not calibrate reliably by this method; TC3 is
compared numerically in the text against the paper's stated values instead of a digitized curve.)
"""
import os
import numpy as np
import fitz  # PyMuPDF

HERE = os.path.dirname(os.path.abspath(__file__))
PDF = os.path.normpath(os.path.join(HERE, "..", "..", "..", "membrane",
                                    "membranes-12-01186-s001", "membranes-12-01186.pdf"))
DATA = os.path.join(HERE, "data")

d = fitz.open(PDF)
if d.needs_pass:
    d.authenticate("")
pg = d[15]
R = pg.rect
clip = fitz.Rect(R.x0 + 0.22 * R.width, R.y0 + 0.24 * R.height, R.x0 + 0.86 * R.width, R.y0 + 0.60 * R.height)
pm = pg.get_pixmap(dpi=300, clip=clip)
im = np.frombuffer(pm.samples, dtype=np.uint8).reshape(pm.height, pm.width, pm.n)[:, :, :3].astype(int)
d.close()

Rr, Gg, Bb = im[:, :, 0], im[:, :, 1], im[:, :, 2]
dark = (Rr < 110) & (Gg < 110) & (Bb < 110)
H, W = dark.shape
rd, cd = dark.sum(1), dark.sum(0)
top = int(np.argmax(rd[:H // 2])); bot = H // 2 + int(np.argmax(rd[H // 2:]))
left = int(np.argmax(cd[:W // 2])); right = W // 2 + int(np.argmax(cd[W // 2:]))

masks = {"157": (Rr > 150) & (Gg < 110) & (Bb < 110),
         "108": (Bb > 140) & (Rr < 120) & (Gg < 150),
         "59": (Gg > 120) & (Rr < 120) & (Bb < 120)}
legend = (0.0, 0.27, 0.735, 0.80)   # in-plot legend box (data coords) to skip
grid = np.round(np.arange(0.10, 0.601, 0.05), 2)   # clean sampling range (avoids noisy endpoints)

def digit(mask):
    xs, ys = [], []
    for px in range(left + 3, right - 2):
        th = (px - left) / (right - left) * 0.7
        col = np.where(mask[top + 2:bot - 2, px])[0]
        if len(col) == 0:
            continue
        y = 0.90 + (np.median(col) + 2) / (bot - top) * (-0.20)
        if legend[0] <= th <= legend[1] and legend[2] <= y <= legend[3]:
            continue
        xs.append(th); ys.append(y)
    xs, ys = np.array(xs), np.array(ys)
    o = np.argsort(xs)
    return np.interp(grid, xs[o], ys[o])

cur = {k: digit(m) for k, m in masks.items()}
with open(os.path.join(DATA, "dwsim_tc2.csv"), "w") as f:
    f.write("theta,y157,y108,y59\n")
    for i, th in enumerate(grid):
        f.write(f"{th},{cur['157'][i]:.4f},{cur['108'][i]:.4f},{cur['59'][i]:.4f}\n")
print("wrote dwsim_tc2.csv; theta=0.4 ->",
      {k: round(float(cur[k][list(grid).index(0.4)]), 3) for k in cur})
