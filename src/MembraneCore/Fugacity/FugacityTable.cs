using System;

namespace MembraneCore.Fugacity
{
    /// <summary>
    /// Position-dependent fugacity coefficients for the real-gas driving force, tabulated against cumulative
    /// stage cut θ and looked up by linear interpolation. Because the cross-flow composition trajectory is a
    /// one-parameter family in θ (independent of membrane area), φ evaluated along that trajectory is a
    /// function of θ alone — so a handful of PME evaluations build a table that the marching solver reads
    /// cheaply at every step, giving "local φ" without a PME call per step.
    /// </summary>
    public sealed class FugacityTable
    {
        private readonly double[] _theta;   // ascending breakpoints in cumulative stage cut
        private readonly double[][] _a;      // retentate-side φ, [breakpoint][component]
        private readonly double[][] _b;      // permeate-side  φ, [breakpoint][component]

        public int Components { get; }
        public int Points => _theta.Length;

        public FugacityTable(double[] theta, double[][] a, double[][] b)
        {
            if (theta == null || a == null || b == null) throw new ArgumentNullException();
            if (theta.Length < 1 || a.Length != theta.Length || b.Length != theta.Length)
                throw new ArgumentException("theta, a and b must be non-empty and the same length.");
            for (int k = 1; k < theta.Length; k++)
                if (theta[k] <= theta[k - 1]) throw new ArgumentException("theta breakpoints must be strictly ascending.");
            Components = a[0].Length;
            for (int k = 0; k < theta.Length; k++)
                if (a[k].Length != Components || b[k].Length != Components)
                    throw new ArgumentException("all φ rows must have the same component count.");
            _theta = theta; _a = a; _b = b;
        }

        /// <summary>
        /// Writes the interpolated retentate (a) and permeate (b) fugacity coefficients at cumulative stage
        /// cut <paramref name="theta"/> into the caller's buffers (clamped to the tabulated range).
        /// </summary>
        public void At(double theta, double[] aOut, double[] bOut)
        {
            int n = _theta.Length;
            if (n == 1 || theta <= _theta[0]) { Copy(_a[0], aOut); Copy(_b[0], bOut); return; }
            if (theta >= _theta[n - 1]) { Copy(_a[n - 1], aOut); Copy(_b[n - 1], bOut); return; }

            int k = 1;
            while (k < n && _theta[k] < theta) k++;   // bracket [k-1, k]
            double f = (theta - _theta[k - 1]) / (_theta[k] - _theta[k - 1]);
            for (int i = 0; i < Components; i++)
            {
                aOut[i] = _a[k - 1][i] + f * (_a[k][i] - _a[k - 1][i]);
                bOut[i] = _b[k - 1][i] + f * (_b[k][i] - _b[k - 1][i]);
            }
        }

        private static void Copy(double[] src, double[] dst)
        {
            for (int i = 0; i < src.Length; i++) dst[i] = src[i];
        }
    }
}
