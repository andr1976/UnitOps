using System;

namespace EjectorCore
{
    /// <summary>
    /// Calorically perfect ideal gas (constant cp, pure). Analytic flashes for
    /// verification tests and for the headless CAPE-OPEN adapter tests; it has no
    /// saturation, so <see cref="PQ"/> throws.
    /// h = cp (T - T0), s = cp ln(T/T0) - R ln(P/P0), rho = P/(R T); T0 = 298.15 K, P0 = 1 bar.
    /// </summary>
    public sealed class IdealGasFlash : IFlash
    {
        private const double T0 = 298.15, P0 = 1e5, Ru = 8.314462618;

        /// <param name="molarMass">Molar mass [kg/mol].</param>
        /// <param name="kappa">Ratio of specific heats cp/cv.</param>
        public IdealGasFlash(double molarMass = 0.0280134, double kappa = 1.4)
        {
            M = molarMass;
            R = Ru / molarMass;
            Cp = kappa / (kappa - 1.0) * R;
            Kappa = kappa;
        }

        public double M { get; }
        public double R { get; }
        public double Cp { get; }
        public double Kappa { get; }
        public bool IsMixture => false;
        public double TriplePointPressure => 0.0;

        private FluidState State(double P, double T, double[]? z)
        {
            if (!(T > 0) || !(P > 0)) throw new FlashException("ideal gas: non-positive T or P");
            return new FluidState
            {
                P = P, T = T, H = Cp * (T - T0), S = Cp * Math.Log(T / T0) - R * Math.Log(P / P0),
                Rho = P / (R * T), Q = 2.0, Z = z,
            };
        }

        public FluidState PH(double P, double h, double[]? z) => State(P, T0 + h / Cp, z);
        public FluidState PS(double P, double s, double[]? z) => State(P, T0 * Math.Exp((s + R * Math.Log(P / P0)) / Cp), z);
        public FluidState PT(double P, double T, double[]? z) => State(P, T, z);
        public FluidState PQ(double P, double q, double[]? z) => throw new FlashException("ideal gas has no saturation");
        public double MolarMass(double[]? z) => M;
    }
}
