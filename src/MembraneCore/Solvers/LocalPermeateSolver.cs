using System;

namespace MembraneCore.Solvers
{
    /// <summary>
    /// Solves the local permeate composition at a membrane point from the local retentate
    /// composition, the pressure ratio, and the component permeances — the solution-diffusion
    /// flux-ratio relation used by Dias et al. (J. Membr. Sci. 613 (2020) 118278), Eqs. 20–22.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Physics: at a membrane point the permeate mole fraction of each component equals the ratio of
    /// its local permeation flux to the total local flux:
    /// <c>y_k = J_k / Σ_j J_j</c> with <c>J_k = S_k (x_k P_r − y_k P_p)</c> (ideal-gas partial-pressure
    /// driving force, constant permeance S_k). Solving for y_k gives
    /// <c>y_k = S_k x_k P_r / (J_tot + S_k P_p)</c>, and Σ_k y_k = 1 closes the system.
    /// </para>
    /// <para>
    /// This class introduces the dimensionless unknown <c>t = J_tot / P_r</c> (units of permeance), so
    /// <c>y_k = S_k x_k / (t + S_k γ)</c> with γ = P_p/P_r, and the closure is
    /// <c>H(t) = Σ_k S_k x_k / (t + S_k γ) − 1 = 0</c>.
    /// For t ≥ 0, S_k &gt; 0, γ ≥ 0 every denominator is strictly positive (no singularity), and H(t) is
    /// strictly decreasing, so the root is unique and bracketed by [0, Σ_k S_k x_k]. This is algebraically
    /// equivalent to the paper's (x_i/y_i) parameterisation but avoids its removable singularity, making
    /// the per-node solve deterministic and unconditionally convergent.
    /// </para>
    /// <para>
    /// Only permeance <em>ratios</em> and γ affect the resulting composition; absolute permeance scaling
    /// leaves y unchanged (t scales with it). Absolute flux magnitudes for the mass balances are computed
    /// separately by the marching models using S_k, P_r, P_p directly.
    /// </para>
    /// </remarks>
    public static class LocalPermeateSolver
    {
        /// <summary>Absolute tolerance on the closure residual H(t).</summary>
        public const double DefaultTolerance = 1e-12;

        /// <summary>Maximum bisection/Newton iterations (a backstop; convergence is typically &lt; 60).</summary>
        public const int DefaultMaxIterations = 200;

        /// <summary>
        /// Computes the local permeate mole fractions y[k] given local retentate mole fractions x[k],
        /// component permeances S[k] (any consistent units; only ratios matter), and pressure ratio
        /// γ = P_permeate / P_retentate.
        /// </summary>
        /// <param name="x">Local retentate mole fractions (need not be pre-normalised; treated as weights).</param>
        /// <param name="S">Component permeances, S[k] &gt; 0 for permeating species (0 = non-permeating).</param>
        /// <param name="gamma">Pressure ratio P_p/P_r, 0 ≤ γ &lt; 1 for net permeation.</param>
        /// <param name="a">Optional per-component retentate-side fugacity coefficients φ_k^r (null ⇒ 1, ideal gas).
        /// The driving force becomes S_k(a_k x_k − γ b_k y_k); the reformulation stays singularity-free.</param>
        /// <param name="b">Optional per-component permeate-side fugacity coefficients φ_k^p (null ⇒ 1, ideal gas).</param>
        /// <returns>Local permeate mole fractions y[k], summing to 1.</returns>
        public static double[] Solve(double[] x, double[] S, double gamma, double[]? a = null, double[]? b = null)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (S == null) throw new ArgumentNullException(nameof(S));
            if (x.Length != S.Length)
                throw new ArgumentException("x and S must have the same length.");
            int n = x.Length;
            if (n == 0) throw new ArgumentException("At least one component is required.");
            if (gamma < 0.0) throw new ArgumentOutOfRangeException(nameof(gamma), "γ must be ≥ 0.");

            // Normalise retentate fractions defensively (clamp tiny negatives from upstream round-off).
            var xw = new double[n];
            double xsum = 0.0;
            for (int k = 0; k < n; k++)
            {
                double xv = x[k] > 0.0 ? x[k] : 0.0;
                xw[k] = xv;
                xsum += xv;
            }
            if (xsum <= 0.0)
                throw new ArgumentException("Retentate composition sums to zero.");
            for (int k = 0; k < n; k++) xw[k] /= xsum;

            // Optional per-component fugacity coefficients (retentate a_k, permeate b_k); default 1 (ideal gas).
            var aa = new double[n];
            var bb = new double[n];
            for (int k = 0; k < n; k++)
            {
                aa[k] = (a != null && a.Length == n && a[k] > 0.0) ? a[k] : 1.0;
                bb[k] = (b != null && b.Length == n && b[k] > 0.0) ? b[k] : 1.0;
            }

            // Weight w_k = S_k * a_k * x_k (a_k folds the retentate fugacity coefficient into the driving
            // force). Non-permeating (S_k == 0) contribute no flux and get y_k = 0.
            var w = new double[n];
            double wsum = 0.0;
            for (int k = 0; k < n; k++)
            {
                double sk = S[k] > 0.0 ? S[k] : 0.0;
                if (sk == 0.0) { w[k] = 0.0; continue; }
                w[k] = sk * aa[k] * xw[k];
                wsum += w[k];
            }
            if (wsum <= 0.0)
                throw new InvalidOperationException("No permeating component has positive permeance and composition.");

            var y = new double[n];

            // Special case γ = 0 (vacuum permeate): t* = Σ S_k x_k, y_k = S_k x_k / Σ_j S_j x_j.
            if (gamma == 0.0)
            {
                for (int k = 0; k < n; k++) y[k] = w[k] / wsum;
                return y;
            }

            // Guard: with γ ≥ 1 there is no net separation driving force; H(0) = 1/γ − 1 ≤ 0.
            // Physically the membrane requires P_r > P_p. Signal rather than return garbage.
            if (gamma >= 1.0)
                throw new InvalidOperationException(
                    $"Pressure ratio γ = {gamma} ≥ 1 gives no permeation driving force (need P_permeate < P_retentate).");

            // Bracket the root of H(t) = Σ_k S_k x_k/(t + S_k γ) − 1 on t ∈ [0, Σ_k S_k x_k].
            // H(0) = 1/γ − 1 > 0; H(Σ S_k x_k) < 0. Bisection with Newton polish.
            double lo = 0.0;
            double hi = wsum; // Σ S_k x_k
            double t = 0.5 * (lo + hi);

            for (int iter = 0; iter < DefaultMaxIterations; iter++)
            {
                double h = -1.0;   // H(t)
                double dh = 0.0;   // H'(t)
                for (int k = 0; k < n; k++)
                {
                    if (S[k] <= 0.0) continue;
                    double denom = t + S[k] * gamma * bb[k];
                    double term = w[k] / denom;
                    h += term;
                    dh -= term / denom;
                }

                if (Math.Abs(h) <= DefaultTolerance)
                    break;

                // Maintain the bracket.
                if (h > 0.0) lo = t; else hi = t;

                // Newton step; fall back to bisection if it leaves the bracket or derivative is degenerate.
                double tNewton = dh != 0.0 ? t - h / dh : double.NaN;
                if (double.IsNaN(tNewton) || tNewton <= lo || tNewton >= hi)
                    t = 0.5 * (lo + hi);
                else
                    t = tNewton;

                if (hi - lo <= DefaultTolerance * Math.Max(1.0, hi))
                    break;
            }

            // Compose y and renormalise to kill residual drift.
            double ysum = 0.0;
            for (int k = 0; k < n; k++)
            {
                if (S[k] <= 0.0) { y[k] = 0.0; continue; }
                double yv = w[k] / (t + S[k] * gamma * bb[k]);
                if (yv < 0.0) yv = 0.0;
                y[k] = yv;
                ysum += yv;
            }
            if (ysum <= 0.0)
                throw new InvalidOperationException("Local permeate composition degenerate (sum ≤ 0).");
            for (int k = 0; k < n; k++) y[k] /= ysum;

            return y;
        }
    }
}
