using System;
using MembraneCore.Solvers;

namespace MembraneCore.Models
{
    /// <summary>
    /// Cross-flow gas-permeation model (Dias et al., J. Membr. Sci. 613 (2020) 118278, the "2D model"):
    /// the retentate flows along the leaf length while permeate is withdrawn across the membrane; at every
    /// point the permeate composition is the local solution-diffusion equilibrium value (Eqs. 20–22) and
    /// drives the flux, and the product permeate is the flow-weighted collection of all local permeates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because the local permeate composition depends only on the local retentate composition, the pressure
    /// ratio γ and the permeance ratios β, the composition trajectory is a function of (feed, β, γ) alone,
    /// parametrised by the stage cut θ. Component balances per unit feed:
    /// <c>dr_i/dθ = −y_i^local(x)</c>, <c>x = r/Σr</c>, integrated from θ = 0. This is the classical
    /// cross-flow (Naylor–Backer / Weller–Steiner) differential-permeation model; it converges to the same
    /// answer as the paper's upwind 2D discretisation (which the authors report is mesh-converged by 50×50).
    /// </para>
    /// <para>
    /// Two entry points share this integrand: <see cref="SolveByStageCut"/> (dimensionless; used for
    /// validation and when the design target is a stage cut) and <see cref="SolveByArea"/> (dimensional;
    /// used by the unit operation, where membrane area + pressures + feed flow are the inputs and θ is an
    /// output). Isothermal, ideal-gas, partial-pressure driving force, constant permeance.
    /// </para>
    /// </remarks>
    public static class CrossFlowModel
    {
        /// <summary>Default number of RK4 steps for the stage-cut march (fine enough to be mesh-converged).</summary>
        public const int DefaultStageCutSteps = 2000;

        /// <summary>
        /// Solves the cross-flow model to a target overall stage cut θ (dimensionless; per unit feed).
        /// </summary>
        /// <param name="feed">Feed mole fractions (need not be normalised).</param>
        /// <param name="permeance">Component permeances S_i (any consistent units; only ratios matter here).</param>
        /// <param name="gamma">Pressure ratio γ = P_permeate / P_retentate, 0 ≤ γ &lt; 1.</param>
        /// <param name="stageCut">Target overall stage cut θ ∈ (0,1).</param>
        /// <param name="steps">RK4 steps (default 2000).</param>
        public static MembraneResult SolveByStageCut(double[] feed, double[] permeance, double gamma,
                                                     double stageCut, int steps = DefaultStageCutSteps,
                                                     double[]? a = null, double[]? b = null)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (permeance == null) throw new ArgumentNullException(nameof(permeance));
            if (feed.Length != permeance.Length) throw new ArgumentException("feed and permeance length mismatch.");
            if (stageCut <= 0.0 || stageCut >= 1.0) throw new ArgumentOutOfRangeException(nameof(stageCut), "θ must be in (0,1).");
            if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));

            int nc = feed.Length;
            var z = Normalize(feed);

            // State r_i = retentate component flow per unit feed; starts at feed composition (Σ = 1).
            var r = (double[])z.Clone();
            double dTheta = stageCut / steps;

            for (int s = 0; s < steps; s++)
            {
                // Classical RK4 on dr_i/dθ = −y_i^local(r/Σr).
                var k1 = Derivative(r, permeance, gamma, a, b);
                var r2 = Axpy(r, k1, 0.5 * dTheta);
                var k2 = Derivative(r2, permeance, gamma, a, b);
                var r3 = Axpy(r, k2, 0.5 * dTheta);
                var k3 = Derivative(r3, permeance, gamma, a, b);
                var r4 = Axpy(r, k3, dTheta);
                var k4 = Derivative(r4, permeance, gamma, a, b);

                for (int i = 0; i < nc; i++)
                {
                    r[i] += dTheta / 6.0 * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i]);
                    if (r[i] < 0.0) r[i] = 0.0; // guard against tiny negative overshoot
                }
            }

            return Assemble(z, r, stageCut, steps);
        }

        /// <summary>
        /// Solves the cross-flow model given membrane area, absolute pressures and feed flow (the unit-op form).
        /// Marches in membrane area; the stage cut is an output.
        /// </summary>
        /// <param name="feed">Feed mole fractions.</param>
        /// <param name="feedMolarFlow">Total feed molar flow [mol/s].</param>
        /// <param name="permeanceSI">Permeances S_i [mol·m⁻²·s⁻¹·Pa⁻¹].</param>
        /// <param name="retentatePressure">Feed/retentate pressure P_r [Pa].</param>
        /// <param name="permeatePressure">Permeate pressure P_p [Pa].</param>
        /// <param name="area">Total membrane area [m²].</param>
        /// <param name="steps">Number of area steps (default 4000).</param>
        public static MembraneResult SolveByArea(double[] feed, double feedMolarFlow, double[] permeanceSI,
                                                 double retentatePressure, double permeatePressure,
                                                 double area, int steps = 4000, int profilePoints = 0,
                                                 double[]? a = null, double[]? b = null)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (permeanceSI == null) throw new ArgumentNullException(nameof(permeanceSI));
            if (feed.Length != permeanceSI.Length) throw new ArgumentException("feed and permeance length mismatch.");
            if (feedMolarFlow <= 0.0) throw new ArgumentOutOfRangeException(nameof(feedMolarFlow));
            if (retentatePressure <= 0.0) throw new ArgumentOutOfRangeException(nameof(retentatePressure));
            if (permeatePressure <= 0.0) throw new ArgumentOutOfRangeException(nameof(permeatePressure));
            if (permeatePressure >= retentatePressure)
                throw new InvalidOperationException("Permeate pressure must be below retentate pressure (γ < 1).");
            if (area <= 0.0) throw new ArgumentOutOfRangeException(nameof(area));
            if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));

            int nc = feed.Length;
            double gamma = permeatePressure / retentatePressure;
            var z = Normalize(feed);

            // Per-component fugacity coefficients (retentate a_i, permeate b_i); default 1 (ideal gas).
            var aa = new double[nc];
            var bb = new double[nc];
            for (int i = 0; i < nc; i++)
            {
                aa[i] = (a != null && a.Length == nc && a[i] > 0.0) ? a[i] : 1.0;
                bb[i] = (b != null && b.Length == nc && b[i] > 0.0) ? b[i] : 1.0;
            }

            // Component molar flows [mol/s]. Explicit forward Euler in area (robust; fine step).
            var R = new double[nc];
            for (int i = 0; i < nc; i++) R[i] = feedMolarFlow * z[i];

            double dA = area / steps;
            double permeatedTotal = 0.0;

            // Optional position-dependent profile sampling (feed end → retentate outlet).
            MembraneProfile? profile = profilePoints > 1 ? new MembraneProfile(profilePoints, nc) : null;
            int[]? sampleAt = null;
            int sIdx = 0;
            if (profile != null)
            {
                sampleAt = new int[profilePoints];
                for (int k = 0; k < profilePoints; k++)
                    sampleAt[k] = (int)Math.Round((double)k * steps / (profilePoints - 1));
            }

            for (int s = 0; s < steps; s++)
            {
                double Rtot = 0.0;
                for (int i = 0; i < nc; i++) Rtot += R[i];
                if (Rtot <= 0.0) break;

                var x = new double[nc];
                for (int i = 0; i < nc; i++) x[i] = R[i] / Rtot;

                var yLocal = LocalPermeateSolver.Solve(x, permeanceSI, gamma, a, b);

                while (profile != null && sIdx < profilePoints && sampleAt![sIdx] == s)
                {
                    RecordSample(profile, sIdx, (double)s / steps, z, x, yLocal, (feedMolarFlow - Rtot) / feedMolarFlow);
                    sIdx++;
                }

                // Local per-component flux [mol/s]: J_i = S_i (a_i x_i P_r − b_i y_i P_p) dA (a,b = 1 ideal).
                for (int i = 0; i < nc; i++)
                {
                    double flux = permeanceSI[i] * (aa[i] * x[i] * retentatePressure - bb[i] * yLocal[i] * permeatePressure) * dA;
                    if (flux < 0.0) flux = 0.0; // clamp: no back-permeation in this local model
                    if (flux > R[i]) flux = R[i]; // cannot remove more than present
                    R[i] -= flux;
                    permeatedTotal += flux;
                }
            }

            // Fill any remaining samples (incl. position = 1) from the final retentate state.
            if (profile != null && sIdx < profilePoints)
            {
                double Rtot = 0.0;
                for (int i = 0; i < nc; i++) Rtot += R[i];
                var xf = new double[nc];
                for (int i = 0; i < nc; i++) xf[i] = Rtot > 0.0 ? R[i] / Rtot : 0.0;
                var yf = Rtot > 0.0 ? LocalPermeateSolver.Solve(xf, permeanceSI, gamma, a, b) : new double[nc];
                double scf = permeatedTotal / feedMolarFlow;
                while (sIdx < profilePoints) { RecordSample(profile, sIdx, (double)sampleAt![sIdx] / steps, z, xf, yf, scf); sIdx++; }
            }

            double stageCut = permeatedTotal / feedMolarFlow;
            // Recompose per-unit-feed retentate for the shared assembler.
            var rPerFeed = new double[nc];
            for (int i = 0; i < nc; i++) rPerFeed[i] = R[i] / feedMolarFlow;
            var result = Assemble(z, rPerFeed, stageCut, steps);
            result.Profile = profile;
            return result;
        }

        /// <summary>
        /// Records one profile sample: retentate composition, local permeate composition, and the cumulative
        /// collected permeate (product so far) derived by mass balance from the feed and the local retentate.
        /// </summary>
        internal static void RecordSample(MembraneProfile p, int k, double position, double[] feedZ,
                                          double[] retentateFrac, double[] permeateFrac, double stageCut)
        {
            double sc = stageCut < 0.0 ? 0.0 : stageCut;
            double retTot = 1.0 - sc;   // Σ retentate per unit feed
            p.Position[k] = position;
            p.StageCut[k] = sc;
            for (int i = 0; i < retentateFrac.Length; i++)
            {
                p.Retentate[i][k] = retentateFrac[i];
                p.Permeate[i][k] = permeateFrac[i];
                // collected_i = (z_i − r_i)/θ, with r_i = retentateFrac_i·(1−θ); at θ→0 use the local permeate.
                double collected = sc > 1e-9 ? (feedZ[i] - retentateFrac[i] * retTot) / sc : permeateFrac[i];
                p.PermeateCollected[i][k] = collected < 0.0 ? 0.0 : collected;
            }
        }

        // dr_i/dθ = −y_i^local(x), x = r/Σr.
        private static double[] Derivative(double[] r, double[] permeance, double gamma, double[]? a, double[]? b)
        {
            int nc = r.Length;
            double sum = 0.0;
            for (int i = 0; i < nc; i++) sum += r[i] > 0.0 ? r[i] : 0.0;
            var x = new double[nc];
            for (int i = 0; i < nc; i++) x[i] = (r[i] > 0.0 ? r[i] : 0.0) / sum;

            var y = LocalPermeateSolver.Solve(x, permeance, gamma, a, b);
            var d = new double[nc];
            for (int i = 0; i < nc; i++) d[i] = -y[i];
            return d;
        }

        private static double[] Axpy(double[] a, double[] b, double h)
        {
            var r = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                r[i] = a[i] + h * b[i];
                if (r[i] < 0.0) r[i] = 0.0;
            }
            return r;
        }

        private static double[] Normalize(double[] v)
        {
            int nc = v.Length;
            var z = new double[nc];
            double s = 0.0;
            for (int i = 0; i < nc; i++) { z[i] = v[i] > 0.0 ? v[i] : 0.0; s += z[i]; }
            if (s <= 0.0) throw new ArgumentException("Composition sums to zero.");
            for (int i = 0; i < nc; i++) z[i] /= s;
            return z;
        }

        // Build the result from the feed (z, Σ=1) and the final per-unit-feed retentate flows r.
        private static MembraneResult Assemble(double[] z, double[] r, double stageCut, int steps)
        {
            int nc = z.Length;

            double rtot = 0.0;
            for (int i = 0; i < nc; i++) rtot += r[i];

            var xret = new double[nc];
            for (int i = 0; i < nc; i++) xret[i] = rtot > 0.0 ? r[i] / rtot : 0.0;

            // Permeate (per unit feed) = feed − retentate; product composition = normalised.
            var p = new double[nc];
            double ptot = 0.0;
            for (int i = 0; i < nc; i++) { p[i] = z[i] - r[i]; if (p[i] < 0.0) p[i] = 0.0; ptot += p[i]; }

            var yperm = new double[nc];
            for (int i = 0; i < nc; i++) yperm[i] = ptot > 0.0 ? p[i] / ptot : 0.0;

            var recovery = new double[nc];
            for (int i = 0; i < nc; i++) recovery[i] = z[i] > 0.0 ? p[i] / z[i] : 0.0;

            double mbres = 0.0;
            for (int i = 0; i < nc; i++)
            {
                double res = Math.Abs(z[i] - r[i] - p[i]);
                if (res > mbres) mbres = res;
            }

            return new MembraneResult(xret, yperm, ptot, recovery, rtot, ptot, steps, mbres);
        }
    }
}
