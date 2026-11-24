using System;

namespace EjectorCore
{
    /// <summary>
    /// Adiabatic expansion from a stagnation state (techref eq-expansion-path,
    /// eq-motive-path, eq-hne). One point on the path costs one PS and one PH flash.
    /// </summary>
    public sealed class Expansion
    {
        private readonly Thermo _th;
        private double _hLoss = double.NaN;

        internal Expansion(Thermo th, FluidState inlet, double eta, double phi = 1.0,
                           double pLoss = double.PositiveInfinity)
        {
            _th = th;
            Inlet = inlet;
            Eta = eta;
            Phi = phi;
            PLoss = pLoss;
        }

        /// <summary>Stagnation inlet state.</summary>
        public FluidState Inlet { get; }
        /// <summary>Isentropic efficiency of the expansion (below <see cref="PLoss"/>).</summary>
        public double Eta { get; }
        /// <summary>HNE flashing factor x = phi * x_eq in the density (1 = HEM).</summary>
        public double Phi { get; }
        /// <summary>The path is isentropic above this pressure (the motive throat pressure);
        /// +inf: eta acts along the whole path.</summary>
        public double PLoss { get; }

        /// <summary>Static state and velocity [m/s] after expanding to P.</summary>
        public (FluidState State, double U) At(double P)
        {
            var i = Inlet;
            if (P >= i.P) return (i, 0.0);
            double hIs = _th.PS(P, i.S, i.Z).H;
            double h;
            if (P >= PLoss)
            {
                h = hIs;
            }
            else
            {
                double hRef;
                if (double.IsPositiveInfinity(PLoss))
                {
                    hRef = i.H;
                }
                else
                {
                    if (double.IsNaN(_hLoss)) _hLoss = _th.PS(PLoss, i.S, i.Z).H;
                    hRef = _hLoss;
                }
                h = hRef - Eta * (hRef - hIs);
            }
            var st = _th.PH(P, h, i.Z);
            if (Phi < 1.0 && st.Q > 0.0 && st.Q < 1.0)
            {
                st = st.Clone();
                double x = Phi * st.Q;
                st.Rho = 1.0 / (x / st.RhoV + (1.0 - x) / st.RhoL);
            }
            return (st, Math.Sqrt(Math.Max(2.0 * (i.H - h), 0.0)));
        }

        /// <summary>Mass flux rho*u [kg/(m2 s)] at P.</summary>
        public double Flux(double P)
        {
            var (st, u) = At(P);
            return st.Rho * u;
        }

        /// <summary>Flux, or NaN where the flash fails or the state is below the triple point.</summary>
        internal double SafeFlux(double P)
        {
            try
            {
                return Flux(P);
            }
            catch (Exception ex) when (ex is FlashException || ex is SolidPhaseRiskException)
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// (P*, G*) maximising the mass flux between pMin and the inlet pressure
        /// (techref eq-critical-flux). A log-spaced scan brackets the maximum (the HEM
        /// flux has a kink at the saturation pressure), then a bounded Brent refine.
        /// If G keeps rising down to pMin the flow is not choked there and
        /// (pMin, G(pMin)) is returned.
        /// </summary>
        public (double P, double G) Critical(double pMin, int n = 60)
        {
            double p0 = Inlet.P;
            var ps = Numerics.Geomspace(pMin, p0 * 0.9999, n);
            var g = new double[n];
            int k = -1;
            for (int j = 0; j < n; j++)
            {
                g[j] = SafeFlux(ps[j]);
                if (!double.IsNaN(g[j]) && (k < 0 || g[j] > g[k])) k = j;
            }
            if (k < 0) throw new EjectorException("no converged flash along the expansion path");
            if (k == 0) return (ps[0], g[0]);
            double lo = ps[k - 1], hi = ps[Math.Min(k + 1, n - 1)];
            var r = Numerics.Fminbound(p => { double v = SafeFlux(p); return -(double.IsNaN(v) ? 0.0 : v); },
                                       lo, hi, 1e-6 * p0);
            return -r.F > g[k] ? (r.X, -r.F) : (ps[k], g[k]);
        }
    }
}
