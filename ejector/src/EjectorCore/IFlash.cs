using System;

namespace EjectorCore
{
    /// <summary>
    /// The four flashes the ejector model needs, at a given overall composition.
    /// Implementations: a CAPE-OPEN property package through a scratch material
    /// object (Ejector.CapeOpen), CoolProp (tests), an ideal gas (tests).
    /// All quantities SI, mass basis; compositions are mole fractions in the
    /// implementation's own compound order (null for a pure fluid).
    /// Failure to converge must be reported as <see cref="FlashException"/>.
    /// </summary>
    public interface IFlash
    {
        /// <summary>Pressure-enthalpy flash.</summary>
        FluidState PH(double P, double h, double[]? z);
        /// <summary>Pressure-entropy flash.</summary>
        FluidState PS(double P, double s, double[]? z);
        /// <summary>Pressure-temperature flash.</summary>
        FluidState PT(double P, double T, double[]? z);
        /// <summary>Pressure-vapour-fraction flash, q = MOLAR vapour fraction (0 bubble, 1 dew).</summary>
        FluidState PQ(double P, double q, double[]? z);
        /// <summary>Molar mass [kg/mol] at composition z.</summary>
        double MolarMass(double[]? z);
        /// <summary>True if the fluid has more than one compound.</summary>
        bool IsMixture { get; }
        /// <summary>Triple-point pressure [Pa] of a pure fluid; 0 if unknown or a mixture.</summary>
        double TriplePointPressure { get; }
    }

    /// <summary>A flash did not converge or the state is outside the property model.</summary>
    public class FlashException : Exception
    {
        public FlashException(string message) : base(message) { }
        public FlashException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>A state falls below the triple-point pressure (e.g. dry ice for CO2).</summary>
    public class SolidPhaseRiskException : Exception
    {
        public SolidPhaseRiskException(string message) : base(message) { }
    }

    /// <summary>Brent root finder called with f(a) and f(b) of the same sign.</summary>
    public class RootBracketException : Exception
    {
        public RootBracketException(string message) : base(message) { }
    }

    /// <summary>The model cannot find a solution (infeasible geometry or duty).</summary>
    public class EjectorException : Exception
    {
        public EjectorException(string message) : base(message) { }
    }

    /// <summary>RateFlows: the given suction flow exceeds what the geometry can entrain.</summary>
    public class EntrainmentExceededException : EjectorException
    {
        public EntrainmentExceededException(string message, double erCritical, double pbCritical)
            : base(message)
        {
            ERCritical = erCritical;
            PbCritical = pbCritical;
        }

        /// <summary>Most-entraining feasible ER of the geometry.</summary>
        public double ERCritical { get; }
        /// <summary>Outlet pressure at <see cref="ERCritical"/> [Pa].</summary>
        public double PbCritical { get; }
    }

    /// <summary>
    /// Flash wrapper used by the model: counts flashes and raises
    /// <see cref="SolidPhaseRiskException"/> below the triple point of a pure fluid
    /// (the equation of state has no solid phase and would return a metastable state).
    /// </summary>
    internal sealed class Thermo
    {
        private readonly IFlash _f;

        public Thermo(IFlash flash, bool solidCheck)
        {
            _f = flash;
            IsMixture = flash.IsMixture;
            PTriple = IsMixture ? 0.0 : Math.Max(flash.TriplePointPressure, 0.0);
            SolidCheck = solidCheck && !IsMixture && PTriple > 0.0;
        }

        public bool IsMixture { get; }
        public double PTriple { get; }
        public bool SolidCheck { get; }
        public int FlashCount { get; set; }

        private void Guard(double P)
        {
            if (SolidCheck && P < PTriple)
                throw new SolidPhaseRiskException(FormattableString.Invariant(
                    $"P = {P / 1e5:F3} bar below the triple-point pressure {PTriple / 1e5:F3} bar: solid formation possible; HEM/EOS invalid"));
        }

        public FluidState PH(double P, double h, double[]? z) { Guard(P); FlashCount++; return _f.PH(P, h, z); }
        public FluidState PS(double P, double s, double[]? z) { Guard(P); FlashCount++; return _f.PS(P, s, z); }
        public FluidState PT(double P, double T, double[]? z) { Guard(P); FlashCount++; return _f.PT(P, T, z); }

        public FluidState PQ(double P, double q, double[]? z)
        {
            Guard(P);
            FlashCount++;
            var st = _f.PQ(P, q, z);
            if (q == 0.0 || q == 1.0) st.Q = q;    // exact end points (molar = mass there); else the flash's mass fraction
            return st;
        }

        public double MolarMass(double[]? z) => _f.MolarMass(z);
    }
}
