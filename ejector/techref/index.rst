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
      \textbf{Version:}  & 0.1 (draft) \\
      \textbf{Date:}     & November 2026 \\
      \textbf{Author:}   & Anders Andreasen \\
      \end{tabular}

      \vspace{1cm}

      \noindent\textbf{Revision History}

      \begin{tabular}{lll}
      \toprule
      Version & Date       & Description \\
      \midrule
      0.1     & 2026-11    & Initial draft: models, numerics, planned architecture, validation, calibration \\
      \bottomrule
      \end{tabular}

      \newpage

.. admonition:: Status of this draft

   The models, numerics and validation in this reference are those of the
   **Python reference implementation** (``ejector/prototype/ejector1d.py``),
   and every "this work" number is produced by it (:ref:`validation`). The
   CAPE-OPEN unit operation itself is **not yet built**. :ref:`architecture`
   and :ref:`appendix_capeopen` are its specification.

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
