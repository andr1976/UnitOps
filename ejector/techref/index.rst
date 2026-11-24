.. _index:

===========================================
Ejector Unit Operation: Technical Reference
===========================================

.. only:: latex

   .. raw:: latex

      \vspace{2cm}
      \begin{center}
      {\Large Background theory, model equations, numerical solution methods,
      software architecture, validation and calibration data for the real-fluid
      one-dimensional ejector / jet pump unit operation for CAPE-OPEN.}
      \end{center}
      \vspace{1cm}

      \begin{tabular}{ll}
      \textbf{Version:}  & 0.2 (draft) \\
      \textbf{Date:}     & November 2026 \\
      \textbf{Author:}   & Anders Andreasen \\
      \end{tabular}

      \vspace{1cm}

      \noindent\textbf{Revision History}

      \begin{tabular}{llp{0.7\linewidth}}
      \toprule
      Version & Date       & Description \\
      \midrule
      0.1     & 2026-11    & Initial draft: models, numerics, planned architecture, validation, calibration \\
      0.2     & 2026-11    & Loss-free motive throat; delayed flashing vs.\ discharge coefficient; C\# unit implemented; fixes from an adversarial review (nozzle regimes, exit-area check, small-ER design, solid guard) \\
      \bottomrule
      \end{tabular}

      \newpage

.. admonition:: Status of this draft

   The models, numerics and validation in this reference are those of the
   **Python reference implementation** (``ejector/prototype/ejector1d.py``),
   and every "this work" number is produced by it (:ref:`validation`). The
   CAPE-OPEN unit (``ejector/src``) is implemented: a C# engine that
   reproduces the Python results exactly on identical thermodynamics, and an
   adapter tested headless (:ref:`sec-val-port`). It has **not yet been run
   inside a process modelling environment**.

.. toctree::
   :maxdepth: 2
   :caption: Preliminaries

   notation


.. _part-foundations:

.. raw:: latex

   \part{Foundations}

.. toctree::
   :maxdepth: 2
   :caption: Part I: Foundations

   introduction
   flow_fundamentals


.. _part-models:

.. raw:: latex

   \part{Ejector Models}

.. toctree::
   :maxdepth: 2
   :caption: Part II: Ejector Models

   nozzles
   mixing_diffuser
   operating_modes


.. _part-software:

.. raw:: latex

   \part{Software Architecture and Numerics}

.. toctree::
   :maxdepth: 2
   :caption: Part III: Software Architecture and Numerics

   architecture
   solution_methods


.. _part-validation:

.. raw:: latex

   \part{Validation and Data}

.. toctree::
   :maxdepth: 2
   :caption: Part IV: Validation and Data

   validation
   data_library
   worked_example


.. raw:: latex

   \part{Appendices}

.. toctree::
   :maxdepth: 2
   :caption: Appendices

   appendix_capeopen


.. only:: latex

   Bibliography
   ============

.. bibliography::
   :all:
