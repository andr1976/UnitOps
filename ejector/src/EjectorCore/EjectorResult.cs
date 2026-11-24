using System;
using System.Collections.Generic;

namespace EjectorCore
{
    /// <summary>Operating regime of a solution.</summary>
    public enum Regime
    {
        /// <summary>Optimum design (geometry computed).</summary>
        Design,
        /// <summary>Most-entraining point: suction or mixing section choked; ER independent of back pressure.</summary>
        Critical,
        /// <summary>Off-design: entrainment set by the back pressure.</summary>
        Subcritical,
        /// <summary>Back pressure above the zero-entrainment limit; suction flow reported as 0.</summary>
        Backflow,
    }

    /// <summary>Geometry helpers.</summary>
    public static class Geometry
    {
        /// <summary>Circle area [m2] from diameter [m].</summary>
        public static double Area(double d) => Math.PI * d * d / 4.0;
        /// <summary>Circle diameter [m] from area [m2].</summary>
        public static double Diameter(double a) => Math.Sqrt(4.0 * a / Math.PI);
    }

    /// <summary>Result of a rating or design calculation (SI units).</summary>
    public sealed class EjectorResult
    {
        public Regime Regime { get; set; }
        /// <summary>Motive mass flow [kg/s].</summary>
        public double Mm { get; set; }
        /// <summary>Suction mass flow [kg/s].</summary>
        public double Ms { get; set; }
        /// <summary>Entrainment ratio Ms/Mm.</summary>
        public double ER { get; set; }
        /// <summary>Outlet (diffuser exit) pressure [Pa].</summary>
        public double Pout { get; set; }
        /// <summary>Motive throat pressure [Pa].</summary>
        public double Pt { get; set; } = double.NaN;
        /// <summary>Motive nozzle exit pressure [Pa].</summary>
        public double Pe { get; set; } = double.NaN;
        /// <summary>Suction static pressure at the mixing-section inlet [Pa].</summary>
        public double Py { get; set; } = double.NaN;
        /// <summary>Pressure at the end of the mixing section [Pa].</summary>
        public double P3 { get; set; } = double.NaN;
        public double At { get; set; } = double.NaN;
        public double Ae { get; set; } = double.NaN;
        public double Amix { get; set; } = double.NaN;
        public double Ue { get; set; } = double.NaN;
        public double Uy { get; set; } = double.NaN;
        public double U3 { get; set; } = double.NaN;
        /// <summary>Outlet stagnation state (pressure, mixed enthalpy and composition).</summary>
        public FluidState? Out { get; set; }
        /// <summary>Ejector efficiency (Koehler et al. / Elbel and Hrnjak), techref eq-eta-ej.</summary>
        public double EtaEjector { get; set; } = double.NaN;
        /// <summary>Lowest static pressure / triple-point pressure (+inf if no triple point).</summary>
        public double SolidMargin { get; set; } = double.PositiveInfinity;
        public FluidState? StateM0 { get; set; }
        public FluidState? StateS0 { get; set; }
        public FluidState? StateE { get; set; }
        public FluidState? StateY { get; set; }
        public FluidState? StateMix { get; set; }

        // ---- RateFlows only
        /// <summary>Most-entraining feasible ER of the sized geometry.</summary>
        public double ERCritical { get; set; } = double.NaN;
        /// <summary>Outlet pressure at <see cref="ERCritical"/> (critical back pressure) [Pa].</summary>
        public double PbCritical { get; set; } = double.NaN;
        /// <summary>Cd*G*At of a fixed-size throat [kg/s].</summary>
        public double MotiveCapacity { get; set; } = double.NaN;
        /// <summary>MotiveCapacity / Mm - 1.</summary>
        public double MotiveMismatch { get; set; } = double.NaN;
        /// <summary>Rating characteristic of the sized geometry: (Pout [Pa], ER) pairs.</summary>
        public List<(double Pout, double ER)> Curve { get; set; } = new List<(double, double)>();
        /// <summary>Flashes used by this calculation.</summary>
        public int FlashCount { get; set; }

        /// <summary>Solid margin; raises <see cref="SolidPhaseRiskException"/> if the solution sits on the
        /// search floor (1.001 x the triple-point pressure), where every search stops: such a result is an
        /// expansion truncated at the triple point, not a valid state.</summary>
        internal EjectorResult CheckSolid(double pTriple)
        {
            double pmin = Math.Min(Pe, Math.Min(Py, P3));
            SolidMargin = pTriple > 0 ? pmin / pTriple : double.PositiveInfinity;
            if (pTriple > 0 && pmin <= 1.0011 * pTriple)
                throw new SolidPhaseRiskException(FormattableString.Invariant(
                    $"the solution reaches the triple-point pressure (lowest static pressure {pmin / 1e5:F3} bar, triple point {pTriple / 1e5:F3} bar): solid formation would occur; the equation of state has no solid phase"));
            return this;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            static double D(double a) => Geometry.Diameter(a) * 1e3;
            return FormattableString.Invariant($"[{Regime}] m_m={Mm:G4} kg/s m_s={Ms:G4} kg/s ER={ER:F4} P_out={Pout / 1e5:F3} bar ")
                + FormattableString.Invariant($"P_t={Pt / 1e5:F2} P_e={Pe / 1e5:F2} P_y={Py / 1e5:F2} P_3={P3 / 1e5:F2} bar | ")
                + FormattableString.Invariant($"D_t={D(At):F3} D_e={D(Ae):F3} D_mix={D(Amix):F3} mm eta_ej={EtaEjector:F3} margin={SolidMargin:F2}");
        }
    }
}
