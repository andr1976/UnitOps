.. _index:

==================================================
ORS Membrane Unit Operation: Technical Reference
==================================================

.. only:: latex

   .. raw:: latex

      \vspace{2cm}
      \begin{center}
      {\Large Background theory, model equations, numerical solution methods,
      software architecture, validation, and a permeability/permeance data
      library for the ORS gas-permeation membrane unit operation for
      CAPE-OPEN / COFE.}
      \end{center}
      \vspace{1cm}

      \begin{tabular}{ll}
      \textbf{Version:}  & 0.1 (draft) \\
      \textbf{Date:}     & August 2026 \\
      \textbf{Author:}   & Anders Andreasen \\
      \textbf{Org:}      & ORS Consulting \\
      \end{tabular}

      \vspace{1cm}

      \noindent\textbf{Revision History}

      \begin{tabular}{lll}
      \toprule
      Version & Date       & Description \\
      \midrule
      0.1     & 2026-08    & Initial draft: models, numerics, architecture, validation, data library \\
      \bottomrule
      \end{tabular}

      \newpage

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
   solution_diffusion


.. _part-models:

.. raw:: latex

   \part{Membrane Models}

.. toctree::
   :maxdepth: 2
   :caption: Part II: Membrane Models

   crossflow
   flow_patterns
   nonisothermal


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
