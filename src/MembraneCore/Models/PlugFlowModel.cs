using System;
using MembraneCore.Solvers;

namespace MembraneCore.Models
{
    /// <summary>
    /// Shindo-type 1D plug-flow permeation models for CO- and COUNTER-current arrangements (as implemented
    /// and validated in Dias et al. 2020, Tables 4–5). Unlike cross-flow, the permeate flows alongside the
    /// membrane, so the local flux is driven by the <em>bulk permeate-stream</em> partial pressure:
    /// <c>J_i = S_i (x_i P_r − y_i P_p)</c> with y_i the flowing-permeate composition at that point.
    /// </summary>
    /// <remarks>
    /// Finite-volume discretisation into N cells along the membrane area. Retentate flows cell 0 → N−1.
    /// <list type="bullet">
    /// <item><b>Co-current:</b> permeate flows with the retentate (0 → N−1); a single explicit forward
    /// sweep (initial-value problem). y in a cell = permeate accumulated up to that cell.</item>
    /// <item><b>Counter-current:</b> permeate flows opposite (N−1 → 0) and exits at the feed end; a
    /// boundary-value problem solved by fixed-point iteration of the permeate-composition profile.</item>
    /// </list>
    /// At the end where the permeate flow is zero, the incipient permeate composition is bootstrapped from
    /// the local solution-diffusion equilibrium (<see cref="LocalPermeateSolver"/>). Isothermal, ideal-gas,
    /// partial-pressure driving force, constant permeance. Composition vs. stage cut depends only on the
    /// permeance ratios, γ and the feed; absolute loading only sets how far along (the stage cut) you go.
    /// </remarks>
    public static class PlugFlowModel
    {
        public const int DefaultCells = 400;
        private const int MaxCounterIterations = 2000;
        private const double CounterTolerance = 1e-10;

        /// <summary>Solve to a target overall stage cut (dimensionless; for design/validation).</summary>
        public static MembraneResult SolveByStageCut(double[] feed, double[] permeance, double gamma,
                                                     FlowPattern pattern, double stageCut, int cells = DefaultCells,
                                                     double[]? a = null, double[]? b = null)
        {
            if (stageCut <= 0.0 || stageCut >= 1.0) throw new ArgumentOutOfRangeException(nameof(stageCut));
            // Composition depends only on (feed, β, γ, θ); root-find the dimensionless loading Λ that yields θ,
            // using P_r = 1, P_p = γ, F_f = 1 so "area×permeance" is the single loading knob.
            double lo = 1e-6, hi = 1.0;
            Func<double, MembraneResult> solveAt = lambda =>
                SolveCore(feed, permeance, 1.0, gamma, pattern, lambda, 1.0, cells, 0, a, b);

            double thi = solveAt(hi).StageCut;
            int guard = 0;
            while (thi < stageCut && guard++ < 80) { hi *= 2.0; thi = solveAt(hi).StageCut; }

            for (int it = 0; it < 200; it++)
            {
                double mid = 0.5 * (lo + hi);
                double th = solveAt(mid).StageCut;
                if (Math.Abs(th - stageCut) < 1e-8) { lo = hi = mid; break; }
                if (th < stageCut) lo = mid; else hi = mid;
            }
            return solveAt(0.5 * (lo + hi));
        }

        /// <summary>Solve given membrane area, absolute pressures and feed flow (the unit-op form).</summary>
        public static MembraneResult SolveByArea(double[] feed, double feedMolarFlow, double[] permeanceSI,
                                                 double retentatePressure, double permeatePressure,
                                                 FlowPattern pattern, double area, int cells = DefaultCells,
                                                 int profilePoints = 0, double[]? a = null, double[]? b = null)
        {
            if (feedMolarFlow <= 0.0) throw new ArgumentOutOfRangeException(nameof(feedMolarFlow));
            if (permeatePressure >= retentatePressure)
                throw new InvalidOperationException("Permeate pressure must be below retentate pressure (γ < 1).");
            if (area <= 0.0) throw new ArgumentOutOfRangeException(nameof(area));
            // Loading Λ = (max permeance)·P_r·area. SolveCore scales per-cell flux by permeanceSI directly.
            double sm = 0.0; foreach (var s in permeanceSI) if (s > sm) sm = s;
            double lambda = sm * retentatePressure * area;
            return SolveCore(feed, permeanceSI, retentatePressure, permeatePressure, pattern, lambda, feedMolarFlow, cells, profilePoints, a, b);
        }

        /// <summary>
        /// Core finite-volume solve. <paramref name="lambda"/> = S_m·P_r·Area is the dimensionless loading;
        /// per-cell membrane "conductance" is (S_i/S_m)·(lambda/N)·(feed/P_r-scaling handled via pressures).
        /// </summary>
        private static MembraneResult SolveCore(double[] feed, double[] permeance, double pr, double pp,
                                                FlowPattern pattern, double lambda, double feedFlow, int n,
                                                int profilePoints = 0, double[]? a = null, double[]? b = null)
        {
            int nc = feed.Length;
            var z = Normalize(feed);
            double gamma = pp / pr;

            double sm = 0.0; foreach (var s in permeance) if (s > sm) sm = s;
            var beta = new double[nc];
            for (int i = 0; i < nc; i++) beta[i] = permeance[i] / sm;

            // Per-cell conductance g so that flux_i(cell) = beta_i*(x_i - gamma*y_i)*g, in units of feed flow.
            // Total loading lambda maps to Σcells g = lambda / (sm*pr) * (sm*pr) ... choose g = lambda/n.
            double g = lambda / n;

            switch (pattern)
            {
                case FlowPattern.CoCurrent: return CoCurrent(z, beta, gamma, g, feedFlow, n, nc, permeance, profilePoints, a, b);
                case FlowPattern.CounterCurrent: return CounterCurrent(z, beta, gamma, g, feedFlow, n, nc, permeance, profilePoints, a, b);
                case FlowPattern.CrossFlow:
                    // Delegate to the dedicated (RK4) cross-flow model for consistency with its validation.
                    return CrossFlowModel.SolveByArea(z, feedFlow, permeance, pr, pp, lambda / (sm * pr), 4000, profilePoints, a, b);
                default: throw new ArgumentOutOfRangeException(nameof(pattern));
            }
        }

        // ---- Co-current: single explicit forward sweep (IVP) ----
        private static MembraneResult CoCurrent(double[] z, double[] beta, double gamma, double g,
                                                double feedFlow, int n, int nc, double[] permeance, int profilePoints,
                                                double[]? a, double[]? b)
        {
            var aa = Coeffs(a, nc); var bb = Coeffs(b, nc);
            var r = new double[nc];   // retentate component flow (per unit feed)
            var p = new double[nc];   // permeate component flow accumulated (per unit feed)
            for (int i = 0; i < nc; i++) r[i] = z[i];

            var prof = MakeProfile(profilePoints, nc, n, out int[]? sampleAt);
            int sIdx = 0;

            for (int c = 0; c < n; c++)
            {
                var x = Fractions(r, nc);
                double psum = 0.0; for (int i = 0; i < nc; i++) psum += p[i];
                double[] y = psum > 1e-14 ? Fractions(p, nc) : LocalPermeateSolver.Solve(x, permeance, gamma, a, b);

                while (prof != null && sIdx < prof.Points && sampleAt![sIdx] == c)
                { CrossFlowModel.RecordSample(prof, sIdx, (double)c / (n - 1), z, x, y, psum); sIdx++; }

                for (int i = 0; i < nc; i++)
                {
                    double flux = beta[i] * (aa[i] * x[i] - gamma * bb[i] * y[i]) * g;
                    if (flux < 0.0) flux = 0.0;
                    if (flux > r[i]) flux = r[i];
                    r[i] -= flux;
                    p[i] += flux;
                }
            }
            var result = Assemble(z, r, feedFlow, n);
            result.Profile = prof;
            return result;
        }

        // ---- Counter-current: fixed-point iteration of the permeate profile (BVP) ----
        private static MembraneResult CounterCurrent(double[] z, double[] beta, double gamma, double g,
                                                     double feedFlow, int n, int nc, double[] permeance, int profilePoints,
                                                     double[]? a, double[]? b)
        {
            var aa = Coeffs(a, nc); var bb = Coeffs(b, nc);
            // y[c] = permeate-stream composition driving cell c. Initialise at the feed composition.
            var y = new double[n][];
            for (int c = 0; c < n; c++) { y[c] = new double[nc]; Array.Copy(z, y[c], nc); }

            var rEnter = new double[n][];       // retentate entering each cell
            var flux = new double[n][];         // per-cell component flux (per unit feed)
            for (int c = 0; c < n; c++) { rEnter[c] = new double[nc]; flux[c] = new double[nc]; }
            var rOut = new double[nc];

            for (int iter = 0; iter < MaxCounterIterations; iter++)
            {
                // Forward retentate sweep with current y.
                var r = new double[nc]; Array.Copy(z, r, nc);
                for (int c = 0; c < n; c++)
                {
                    Array.Copy(r, rEnter[c], nc);
                    var x = Fractions(r, nc);
                    for (int i = 0; i < nc; i++)
                    {
                        double f = beta[i] * (aa[i] * x[i] - gamma * bb[i] * y[c][i]) * g;
                        if (f < 0.0) f = 0.0;
                        if (f > r[i]) f = r[i];
                        flux[c][i] = f;
                        r[i] -= f;
                    }
                }
                Array.Copy(r, rOut, nc);

                // Backward permeate accumulation: Pperm[c] = Σ_{k>=c} flux[k] flows past node c toward product.
                // Update y[c] = composition of permeate entering cell c from the c+1 side; bootstrap the
                // zero-flow end (c=n-1) with the local equilibrium of its retentate.
                double maxDelta = 0.0;
                var pPermNext = new double[nc];   // permeate entering from the higher-index side (starts 0 at c=n-1 top)
                for (int c = n - 1; c >= 0; c--)
                {
                    double[] yNew;
                    double sNext = 0.0; for (int i = 0; i < nc; i++) sNext += pPermNext[i];
                    if (sNext > 1e-14)
                        yNew = Fractions(pPermNext, nc);
                    else
                        yNew = LocalPermeateSolver.Solve(Fractions(rEnter[c], nc), permeance, gamma, a, b);

                    for (int i = 0; i < nc; i++)
                    {
                        double d = Math.Abs(yNew[i] - y[c][i]);
                        if (d > maxDelta) maxDelta = d;
                        y[c][i] = yNew[i];
                        pPermNext[i] += flux[c][i];   // now includes cell c, becomes "entering" for cell c-1
                    }
                }

                if (maxDelta < CounterTolerance) break;
            }

            var result = Assemble(z, rOut, feedFlow, n);

            // Profile from the converged per-cell states: retentate entering each cell and the flowing
            // permeate composition driving it; cumulative stage cut = fraction of feed stripped by position.
            var prof = MakeProfile(profilePoints, nc, n, out int[]? sampleAt);
            if (prof != null)
            {
                for (int k = 0; k < prof.Points; k++)
                {
                    int c = sampleAt![k];
                    if (c > n - 1) c = n - 1;
                    var x = Fractions(rEnter[c], nc);
                    double stripped = 0.0; for (int i = 0; i < nc; i++) stripped += rEnter[c][i];
                    CrossFlowModel.RecordSample(prof, k, (double)c / (n - 1), z, x, y[c], 1.0 - stripped);
                }
                result.Profile = prof;
            }
            return result;
        }

        /// <summary>Allocates a profile and its cell-sample indices (0..n-1), or null if not requested.</summary>
        private static MembraneProfile? MakeProfile(int profilePoints, int nc, int n, out int[]? sampleAt)
        {
            sampleAt = null;
            if (profilePoints <= 1) return null;
            var prof = new MembraneProfile(profilePoints, nc);
            sampleAt = new int[profilePoints];
            for (int k = 0; k < profilePoints; k++)
                sampleAt[k] = (int)Math.Round((double)k * (n - 1) / (profilePoints - 1));
            return prof;
        }

        // ---- shared helpers ----
        // Per-component fugacity coefficients with default 1 (ideal gas) when the array is absent/mismatched.
        private static double[] Coeffs(double[]? c, int nc)
        {
            var r = new double[nc];
            for (int i = 0; i < nc; i++) r[i] = (c != null && c.Length == nc && c[i] > 0.0) ? c[i] : 1.0;
            return r;
        }

        private static double[] Fractions(double[] v, int nc)
        {
            double s = 0.0; for (int i = 0; i < nc; i++) s += v[i] > 0.0 ? v[i] : 0.0;
            var f = new double[nc];
            if (s <= 0.0) return f;
            for (int i = 0; i < nc; i++) f[i] = (v[i] > 0.0 ? v[i] : 0.0) / s;
            return f;
        }

        private static double[] Normalize(double[] v)
        {
            int nc = v.Length; var z = new double[nc]; double s = 0.0;
            for (int i = 0; i < nc; i++) { z[i] = v[i] > 0.0 ? v[i] : 0.0; s += z[i]; }
            if (s <= 0.0) throw new ArgumentException("Composition sums to zero.");
            for (int i = 0; i < nc; i++) z[i] /= s;
            return z;
        }

        private static MembraneResult Assemble(double[] z, double[] rOut, double feedFlow, int cells)
        {
            int nc = z.Length;
            double rtot = 0.0; for (int i = 0; i < nc; i++) rtot += rOut[i];
            var xret = new double[nc];
            for (int i = 0; i < nc; i++) xret[i] = rtot > 0.0 ? rOut[i] / rtot : 0.0;

            var p = new double[nc]; double ptot = 0.0;
            for (int i = 0; i < nc; i++) { p[i] = z[i] - rOut[i]; if (p[i] < 0.0) p[i] = 0.0; ptot += p[i]; }
            var yperm = new double[nc];
            for (int i = 0; i < nc; i++) yperm[i] = ptot > 0.0 ? p[i] / ptot : 0.0;

            var recovery = new double[nc];
            for (int i = 0; i < nc; i++) recovery[i] = z[i] > 0.0 ? p[i] / z[i] : 0.0;

            double mb = 0.0;
            for (int i = 0; i < nc; i++) { double res = Math.Abs(z[i] - rOut[i] - p[i]); if (res > mb) mb = res; }

            return new MembraneResult(xret, yperm, ptot, recovery, rtot * feedFlow, ptot * feedFlow, cells, mb);
        }
    }
}
