"""Render the TikZ bodies to PNG for the HTML build.

The PDF build \\input{}s the *_body.tex files directly (conf.py copies them into
the LaTeX tree), so this only produces figures/tikz_<name>.png.  Needs pdflatex
and pdftoppm (TeX Live / poppler) on PATH.  Usage:  python render.py
"""
import os
import shutil
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
BUILD = os.path.join(HERE, "build")
WRAP = r"""\documentclass[border=5pt]{standalone}
\usepackage{amsmath}
\usepackage{tikz}\usetikzlibrary{arrows.meta,positioning,fit,backgrounds}
\usepackage{xcolor}
\definecolor{docnavy}{HTML}{1F3A5F}\definecolor{docred}{HTML}{C0392B}
\definecolor{docslate}{HTML}{7F8C8D}\definecolor{docgrey}{HTML}{34495E}\definecolor{docamber}{HTML}{D68910}
\renewcommand{\vec}[1]{\mathbf{#1}}
\begin{document}
\input{../%s_body.tex}
\end{document}
"""

os.makedirs(BUILD, exist_ok=True)
for name in ("ejector", "arch"):
    tex = os.path.join(BUILD, f"{name}_wrap.tex")
    with open(tex, "w") as f:
        f.write(WRAP % name)
    subprocess.run(["pdflatex", "-interaction=nonstopmode", "-halt-on-error", f"{name}_wrap.tex"],
                   cwd=BUILD, check=True, stdout=subprocess.DEVNULL)
    subprocess.run(["pdftoppm", "-png", "-r", "300", "-singlefile", f"{name}_wrap.pdf", name],
                   cwd=BUILD, check=True)
    shutil.copy(os.path.join(BUILD, f"{name}.png"), os.path.join(HERE, "..", f"tikz_{name}.png"))
    print(f"wrote figures/tikz_{name}.png")
