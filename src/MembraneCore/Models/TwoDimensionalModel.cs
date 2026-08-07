using System;
using MembraneCore.Solvers;

namespace MembraneCore.Models
{
    /// <summary>
    /// EXPERIMENTAL — not yet validated. Attempt at Dias et al. (2020) iterative 2D cross-flow permeation
    /// model (their "2D model"): an upwind finite-difference march over the unwound spiral-wound leaf,
    /// where the retentate flux f_i marches along the length (x, index n) and the permeate flux g_i
    /// accumulates along the width (y, index m), with the driving force using the accumulated
    /// permeate-stream composition y_i = g_i/g (Eq. 16).
    /// </summary>
    /// <remarks>
    /// KNOWN GAP: as implemented, the g_i/g accumulation closely tracks the local-equilibrium composition,
    /// so this reduces to <see cref="CrossFlowModel"/> and is insensitive to S_y/S_x. It therefore does
    /// NOT reproduce the paper's Table 6/7 "2D model" numbers, which separate ~0.05–0.09 (fast-gas fraction)
    /// LESS than cross-flow at the same stage cut. The additional permeate-side coupling that produces that
    /// reduction has not been reverse-engineered from the paper alone (the reference implementation is not
    /// available). Do NOT use this in the shipping unit operation — use <see cref="CrossFlowModel"/>, which
    /// is validated against Shindo's cross-flow (Tables 4–5) to 4 decimals. See docs/03-validation-and-findings.md.
    /// </remarks>
    /// <remarks>
    /// Governing discretised equations (Eqs. 23–24):
    ///   f_i(m,n+1) = f_i(m,n) − Δz1 · β_i (x_i − γ y_i) S_x
    ///   g_i(m+1,n) = g_i(m,n) + Δz2 · β_i (x_i − γ y_i) S_y
    /// with x_i = f_i/Σf (Eq. 15), y_i = g_i/Σg (Eq. 16), β_i = S_i/S_m (Eq. 8), γ = P_p/P_r (Eq. 7).
    /// Boundary conditions (Eqs. 18–19): f_i(m,0) = x_{i,f} (feed along the whole x=0 edge);
    /// g_i(0,n) = 0 (no permeate at the sealed y=0 edge). At the y=0 edge the permeate composition is
    /// bootstrapped from the local solution-diffusion equilibrium (Eqs. 20–22) since g=0 there.
    /// Overall balances are taken by difference (permeate = feed − retentate) so mass closes exactly.
    /// S_x and S_y are the dimensionless permeation groups (Eqs. 11–12); their ratio S_y/S_x = (W·h_x)/(L·h_y)
    /// is fixed by geometry, while the absolute S_x sets the membrane loading (hence the stage cut).
    /// Isothermal, ideal-gas, partial-pressure driving force, constant permeance.
    /// </remarks>
    public static class TwoDimensionalModel
    {
        /// <summary>Solves the 2D field for given dimensionless loading groups S_x, S_y and grid.</summary>
        /// <param name="feed">Feed mole fractions (normalised internally).</param>
        /// <param name="permeance">Component permeances S_i (only ratios β_i matter here).</param>
        /// <param name="gamma">Pressure ratio γ = P_p/P_r ∈ (0,1).</param>
        /// <param name="sx">Dimensionless retentate-direction permeation group S_x (loading).</param>
        /// <param name="sy">Dimensionless permeate-direction permeation group S_y.</param>
        /// <param name="nx">Grid points along x (retentate), ≥ 2.</param>
        /// <param name="ny">Grid points along y (permeate), ≥ 2.</param>
        public static MembraneResult SolveField(double[] feed, double[] permeance, double gamma,
                                                double sx, double sy, int nx = 200, int ny = 200)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (permeance == null) throw new ArgumentNullException(nameof(permeance));
            if (feed.Length != permeance.Length) throw new ArgumentException("feed/permeance length mismatch.");
            if (gamma <= 0.0 || gamma >= 1.0) throw new ArgumentOutOfRangeException(nameof(gamma));
            if (sx <= 0.0) throw new ArgumentOutOfRangeException(nameof(sx));
            if (sy <= 0.0) throw new ArgumentOutOfRangeException(nameof(sy));
            if (nx < 2) throw new ArgumentOutOfRangeException(nameof(nx));
            if (ny < 2) throw new ArgumentOutOfRangeException(nameof(ny));

            int nc = feed.Length;
            var z = Normalize(feed);

            // β_i = S_i / S_m, with S_m = max permeance (base component).
            double sm = 0.0;
            for (int i = 0; i < nc; i++) if (permeance[i] > sm) sm = permeance[i];
            var beta = new double[nc];
            for (int i = 0; i < nc; i++) beta[i] = permeance[i] / sm;

            double dz1 = 1.0 / (nx - 1);
            double dz2 = 1.0 / (ny - 1);

            // Row buffers: fRow = f_i along n for the current row m (recomputed each row from the feed edge);
            // gCur/gNext = permeate flux accumulated to this row / the next row (all n). Memory O(nc*nx).
            var fRow = new double[nx, nc];     // f_i(m, n) for the current row m
            var gCur = new double[nx, nc];     // g_i(m, n) accumulated up to current row m
            var gNext = new double[nx, nc];    // g_i(m+1, n)

            // Retentate outlet accumulation: ∫ f_i(z2, z1=1) dz2  (trapezoidal over m).
            var retOut = new double[nc];

            for (int m = 0; m < ny; m++)
            {
                // Initialise this row's retentate at the feed edge n=0.
                for (int i = 0; i < nc; i++) fRow[0, i] = z[i];

                for (int n = 0; n < nx; n++)
                {
                    // x_i = f_i/Σf at (m,n).
                    double fsum = 0.0;
                    for (int i = 0; i < nc; i++) fsum += fRow[n, i] > 0.0 ? fRow[n, i] : 0.0;
                    var x = new double[nc];
                    for (int i = 0; i < nc; i++) x[i] = (fRow[n, i] > 0.0 ? fRow[n, i] : 0.0) / (fsum > 0.0 ? fsum : 1.0);

                    // y_i = g_i/Σg at (m,n); at the sealed edge (m=0, g=0) bootstrap with local equilibrium.
                    double gsum = 0.0;
                    for (int i = 0; i < nc; i++) gsum += gCur[n, i] > 0.0 ? gCur[n, i] : 0.0;
                    double[] y;
                    if (gsum <= 0.0)
                        y = LocalPermeateSolver.Solve(x, permeance, gamma);
                    else
                    {
                        y = new double[nc];
                        for (int i = 0; i < nc; i++) y[i] = (gCur[n, i] > 0.0 ? gCur[n, i] : 0.0) / gsum;
                    }

                    // Local permeation term Φ_i = β_i (x_i − γ y_i), clamped to non-negative.
                    var phi = new double[nc];
                    for (int i = 0; i < nc; i++)
                    {
                        double v = beta[i] * (x[i] - gamma * y[i]);
                        phi[i] = v > 0.0 ? v : 0.0;
                    }

                    // March retentate in n: f_i(m,n+1) = f_i(m,n) − Δz1 Φ_i S_x.
                    if (n < nx - 1)
                    {
                        for (int i = 0; i < nc; i++)
                        {
                            double next = fRow[n, i] - dz1 * phi[i] * sx;
                            fRow[n + 1, i] = next > 0.0 ? next : 0.0;
                        }
                    }

                    // Accumulate permeate in m: g_i(m+1,n) = g_i(m,n) + Δz2 Φ_i S_y.
                    for (int i = 0; i < nc; i++)
                        gNext[n, i] = gCur[n, i] + dz2 * phi[i] * sy;
                }

                // Trapezoidal weight for the retentate-outlet integral over z2.
                double w = (m == 0 || m == ny - 1) ? 0.5 * dz2 : dz2;
                for (int i = 0; i < nc; i++) retOut[i] += w * fRow[nx - 1, i];

                // Advance g to the next row.
                var tmp = gCur; gCur = gNext; gNext = tmp;
                Array.Clear(gNext, 0, gNext.Length);
            }

            // Overall balances by difference (feed − retentate); mass closes by construction.
            var perm = new double[nc];
            double retTot = 0.0, permTot = 0.0;
            for (int i = 0; i < nc; i++)
            {
                double r = retOut[i];
                if (r < 0.0) r = 0.0;
                if (r > z[i]) r = z[i];
                retOut[i] = r;
                perm[i] = z[i] - r;
                retTot += r;
                permTot += perm[i];
            }

            var xret = new double[nc];
            var yperm = new double[nc];
            var recovery = new double[nc];
            for (int i = 0; i < nc; i++)
            {
                xret[i] = retTot > 0.0 ? retOut[i] / retTot : 0.0;
                yperm[i] = permTot > 0.0 ? perm[i] / permTot : 0.0;
                recovery[i] = z[i] > 0.0 ? perm[i] / z[i] : 0.0;
            }

            double mbres = 0.0;
            for (int i = 0; i < nc; i++)
            {
                double res = Math.Abs(z[i] - retOut[i] - perm[i]);
                if (res > mbres) mbres = res;
            }

            return new MembraneResult(xret, yperm, permTot, recovery, retTot, permTot, nx * ny, mbres);
        }

        /// <summary>
        /// Solves the 2D model to a target overall stage cut θ by root-finding the loading S_x (with S_y
        /// fixed by the geometry ratio S_y/S_x). Used for validation against reported (θ, composition) pairs.
        /// </summary>
        public static MembraneResult SolveToStageCut(double[] feed, double[] permeance, double gamma,
                                                     double syOverSx, double stageCut,
                                                     int nx = 200, int ny = 200)
        {
            if (syOverSx <= 0.0) throw new ArgumentOutOfRangeException(nameof(syOverSx));
            if (stageCut <= 0.0 || stageCut >= 1.0) throw new ArgumentOutOfRangeException(nameof(stageCut));

            // θ(Sx) is monotonically increasing. Bracket then bisect.
            double lo = 1e-6, hi = 1.0;
            Func<double, double> thetaOf = sx =>
                SolveField(feed, permeance, gamma, sx, sx * syOverSx, nx, ny).StageCut;

            // Expand hi until θ(hi) exceeds target (θ → 1 as loading → ∞).
            double thi = thetaOf(hi);
            int guard = 0;
            while (thi < stageCut && guard++ < 60) { hi *= 2.0; thi = thetaOf(hi); }

            for (int it = 0; it < 100; it++)
            {
                double mid = 0.5 * (lo + hi);
                double th = thetaOf(mid);
                if (Math.Abs(th - stageCut) < 1e-6) { lo = hi = mid; break; }
                if (th < stageCut) lo = mid; else hi = mid;
            }
            double sxStar = 0.5 * (lo + hi);
            return SolveField(feed, permeance, gamma, sxStar, sxStar * syOverSx, nx, ny);
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
    }
}
