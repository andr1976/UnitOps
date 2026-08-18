# Paper — working directory

Draft materials for a journal article about the gas-permeation membrane CAPE-OPEN
unit operation in this repository.

- **Target journal:** *Computers & Chemical Engineering* (Elsevier, hybrid —
  subscription with optional open access, **no mandatory APC**). CAPE-OPEN
  interoperability sits squarely in its computer-aided-process-engineering scope.
- **Framing:** software / tool paper (full-length article) — the validated, open,
  standards-based unit operation is the contribution; model, numerics, validation
  and an illustrative flowsheet case study are included.
- **Status:** skeleton only. Nothing here is submission-ready.

## Files

| File | Purpose |
|---|---|
| `paper.tex` | The manuscript — Elsevier CAS double-column (`cas-dc`), built from the outline. |
| `references.bib` | Curated bibliography, reused verbatim from the verified `docs/techref/references.bib`. |
| `outline.md` | Section-by-section planning notes + authoring guidance (not the manuscript). |
| `cas-dc.cls`, `cas-sc.cls`, `cas-common.sty`, `cas-model2-names.bst` | Vendored Elsevier CAS template files (LPPL) so the paper builds standalone. |

Figures, tables and validated numbers should be reused from the technical
reference (`docs/techref/`, especially `validation.rst` and `figures/`) rather than
recomputed — single source of truth. While drafting, `paper.tex` pulls figures
directly from `../techref/figures/`; at submission, copy them into a local `figs/`.

## Build

```sh
cd docs/paper
latexmk -pdf -bibtex paper.tex      # -> paper.pdf
```

Notes:
- Double-column by default. For single-column, change `\documentclass[...]{cas-dc}`
  to `cas-sc` (both class files are vendored).
- Reference style is **author–year** (`cas-model2-names.bst`, the only style shipped
  with the CAS bundle). If the journal requires **numbered** references, drop in
  Elsevier's `model1-num-names.bst` and switch `\usepackage[numbers]{natbib}` +
  `\bibliographystyle{model1-num-names}`.
- Build artifacts (`*.pdf`, `*.aux`, `*.bbl`, `*.blg`, `*.log`, …) are git-ignored;
  only the sources are tracked.

## Submission checklist

Content
- [ ] Decide the **illustrative case study** (currently **parked** — see the marked
      section in `manuscript.md`). This is the paper's main piece of new content.
- [ ] Fill author(s) + affiliation(s) (placeholders in `manuscript.md`).
- [ ] Write the abstract + highlights once the case study is chosen.
- [ ] Pull validation figures/tables from `docs/techref/`.
- [ ] Positioning vs. existing options (DWSIM membrane, MemPy, bespoke Aspen models).

Reproducibility / journal requirements (outward-facing — require the maintainer's go-ahead)
- [ ] Make the GitHub repository **public**.
- [ ] Tag a release (e.g. `v1.0.0`).
- [ ] Archive the tagged snapshot on **Zenodo** → citable DOI for the "Code availability" statement.
- [ ] Confirm every `VERIFY`-marked reference against its version of record.
