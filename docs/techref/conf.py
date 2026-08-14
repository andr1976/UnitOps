# -- Membrane Unit Operation: Technical Reference, Sphinx config --
#
# Scaffolding mirrors the GERG-2008 and ThermoCalc technical references
# (docs/techref) so the sibling references share a look, build, and
# bibliography style. Build with:
#     make html       (HTML, alabaster theme)
#     make latexpdf   (PDF via pdflatex + latexmk)

project = 'Membrane Unit Operation: Technical Reference'
author = 'Anders Andreasen'
copyright = '2026, Anders Andreasen'
release = '0.1'

# -- General --
extensions = [
    'sphinx.ext.mathjax',
    'sphinxcontrib.bibtex',
]

bibtex_bibfiles = ['references.bib']
bibtex_default_style = 'unsrt'

numfig = True
math_numfig = True
numfig_format = {
    'figure': 'Figure %s',
    'table': 'Table %s',
    'code-block': 'Listing %s',
}

# -- LaTeX / PDF output --
latex_engine = 'pdflatex'

latex_elements = {
    'papersize': 'a4paper',
    'pointsize': '11pt',
    'preamble': r'''
\usepackage{amsmath}
\usepackage{amssymb}
\usepackage{booktabs}
\usepackage{siunitx}
\usepackage{longtable}
\usepackage{pdflscape}

% Palette: navy chapter/section headings, red accents.
\usepackage{xcolor}
\definecolor{docnavy}{HTML}{1F3A5F}
\definecolor{docred}{HTML}{C0392B}
\definecolor{docslate}{HTML}{7F8C8D}
\definecolor{docgrey}{HTML}{34495E}
\definecolor{docamber}{HTML}{D68910}
\usepackage{tikz}
\usetikzlibrary{arrows.meta,positioning,fit,backgrounds}
\usepackage{sectsty}
\chapterfont{\color{docnavy}}
\sectionfont{\color{docnavy}}
\subsectionfont{\color{docnavy}}

% Make stray Unicode from prose (arrows, math symbols, sub/superscripts) safe for pdflatex,
% so the nicer glyphs can stay in the HTML build without breaking the PDF.
\DeclareUnicodeCharacter{2192}{\ensuremath{\rightarrow}}
\DeclareUnicodeCharacter{2190}{\ensuremath{\leftarrow}}
\DeclareUnicodeCharacter{00D7}{\ensuremath{\times}}
\DeclareUnicodeCharacter{00B7}{\ensuremath{\cdot}}
\DeclareUnicodeCharacter{2212}{\ensuremath{-}}
\DeclareUnicodeCharacter{2248}{\ensuremath{\approx}}
\DeclareUnicodeCharacter{2264}{\ensuremath{\le}}
\DeclareUnicodeCharacter{2265}{\ensuremath{\ge}}
\DeclareUnicodeCharacter{00B9}{\ensuremath{^{1}}}
\DeclareUnicodeCharacter{00B2}{\ensuremath{^{2}}}
\DeclareUnicodeCharacter{00B3}{\ensuremath{^{3}}}
\DeclareUnicodeCharacter{2070}{\ensuremath{^{0}}}
\DeclareUnicodeCharacter{2074}{\ensuremath{^{4}}}
\DeclareUnicodeCharacter{2075}{\ensuremath{^{5}}}
\DeclareUnicodeCharacter{2076}{\ensuremath{^{6}}}
\DeclareUnicodeCharacter{2077}{\ensuremath{^{7}}}
\DeclareUnicodeCharacter{2078}{\ensuremath{^{8}}}
\DeclareUnicodeCharacter{2079}{\ensuremath{^{9}}}
\DeclareUnicodeCharacter{207A}{\ensuremath{^{+}}}
\DeclareUnicodeCharacter{207B}{\ensuremath{^{-}}}
\DeclareUnicodeCharacter{2080}{\ensuremath{_{0}}}
\DeclareUnicodeCharacter{2081}{\ensuremath{_{1}}}
\DeclareUnicodeCharacter{2082}{\ensuremath{_{2}}}
\DeclareUnicodeCharacter{2083}{\ensuremath{_{3}}}
\DeclareUnicodeCharacter{2084}{\ensuremath{_{4}}}
\DeclareUnicodeCharacter{03C6}{\ensuremath{\varphi}}
\DeclareUnicodeCharacter{03B3}{\ensuremath{\gamma}}
\DeclareUnicodeCharacter{03B8}{\ensuremath{\theta}}
\DeclareUnicodeCharacter{03B1}{\ensuremath{\alpha}}

% Custom macros (shared with the GERG-2008 / ThermoCalc references, plus membrane-specific ones)
\newcommand{\dd}{\mathrm{d}}                            % upright differential
\newcommand{\pder}[2]{\frac{\partial #1}{\partial #2}}  % partial derivative
\newcommand{\tder}[2]{\frac{\dd #1}{\dd #2}}            % total derivative
\newcommand{\Rgas}{R}                                   % universal gas constant
\renewcommand{\vec}[1]{\mathbf{#1}}
''',
    'extraclassoptions': 'openany',
    'tableofcontents': r'''
\pagenumbering{roman}
\tableofcontents
\pagenumbering{arabic}
''',
}

latex_toplevel_sectioning = 'chapter'

# Copy the TikZ diagram bodies into the LaTeX build tree so the raw-latex
# \input{} in introduction.rst / architecture.rst resolves (single source; the
# same bodies are rendered to PNG for the HTML build).
latex_additional_files = ['figures/tikz/module_body.tex', 'figures/tikz/arch_body.tex']

latex_documents = [
    ('index', 'membrane_techref.tex',
     'Membrane Unit Operation: Technical Reference',
     'Anders Andreasen', 'manual'),
]

# -- HTML output --
html_theme = 'alabaster'
html_static_path = ['_static']
html_theme_options = {
    'description': 'Gas-permeation membrane unit operation for CAPE-OPEN / COFE: models, numerics, validation',
    'page_width': '1040px',
    'sidebar_width': '270px',
    'fixed_sidebar': True,
    'sidebar_collapse': True,
    # Palette
    'gray_1': '#1F3A5F',     # headings / dark text
    'link': '#C0392B',
    'link_hover': '#1F3A5F',
    'sidebar_header': '#1F3A5F',
    'sidebar_link': '#1F3A5F',
}
