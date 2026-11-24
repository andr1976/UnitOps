using System;

namespace EjectorCore
{
    /// <summary>
    /// Thermodynamic state returned by a flash (SI units, mass basis).
    /// </summary>
    public sealed class FluidState
    {
        /// <summary>Pressure [Pa].</summary>
        public double P { get; set; }
        /// <summary>Specific enthalpy [J/kg].</summary>
        public double H { get; set; }
        /// <summary>Temperature [K].</summary>
        public double T { get; set; } = double.NaN;
        /// <summary>Specific entropy [J/(kg K)].</summary>
        public double S { get; set; } = double.NaN;
        /// <summary>Mixture density [kg/m3] (homogeneous for two-phase states).</summary>
        public double Rho { get; set; } = double.NaN;
        /// <summary>
        /// Vapour MASS fraction for two-phase states (0..1); exactly 0 or 1 on the
        /// saturation line; -1 for single-phase liquid; 2 for vapour/supercritical.
        /// </summary>
        public double Q { get; set; } = double.NaN;
        /// <summary>Overall mole fractions (null for a pure fluid).</summary>
        public double[]? Z { get; set; }
        /// <summary>Saturated liquid density [kg/m3] (two-phase states only).</summary>
        public double RhoL { get; set; } = double.NaN;
        /// <summary>Saturated vapour density [kg/m3] (two-phase states only).</summary>
        public double RhoV { get; set; } = double.NaN;

        /// <summary>True if 0 &lt; Q &lt; 1.</summary>
        public bool IsTwoPhase => Q > 0.0 && Q < 1.0;

        /// <summary>Shallow copy (Z is shared; it is never mutated).</summary>
        public FluidState Clone() => (FluidState)MemberwiseClone();

        /// <inheritdoc/>
        public override string ToString() =>
            FormattableString.Invariant($"P={P / 1e5:F4} bar T={T - 273.15:F3} C h={H:F1} s={S:F3} rho={Rho:F4} q={Q:F4}");
    }
}
