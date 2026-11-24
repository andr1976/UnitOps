using System;
using System.Collections.Generic;

namespace Ejector.CapeOpen
{
    /// <summary>
    /// Named efficiency sets (techref data_library, "Efficiency sets"). The R744 sets are fitted on 42 points of
    /// one small ejector (Giacomelli et al. 2019, 2 mm mixing section) with the isentropic-throat motive model;
    /// applying them to other sizes or fluids is an extrapolation.
    /// </summary>
    internal static class Presets
    {
        public const string Custom = "Custom";
        public const string Screening = "Screening";
        public const string R744All = "R744_AllPoints";
        public const string R744Super = "R744_Supercritical";
        public const string R744Sub = "R744_SubcriticalSubcooled";
        public const string GasMa = "GasEjector_Ma2017";

        public static readonly string[] Names = { Screening, R744All, R744Super, R744Sub, GasMa, Custom };

        /// <summary>(eta_m, eta_s, eta_mix, eta_d, Cd).</summary>
        public static readonly IReadOnlyDictionary<string, (double EtaM, double EtaS, double EtaMix, double EtaD, double Cd)> Values =
            new Dictionary<string, (double, double, double, double, double)>(StringComparer.Ordinal)
            {
                [Screening] = (0.90, 0.85, 0.90, 0.80, 1.0),
                [R744All] = (0.90, 0.610, 0.825, 0.820, 1.138),
                [R744Super] = (0.90, 0.638, 0.829, 0.814, 1.138),
                [R744Sub] = (0.90, 0.872, 0.767, 0.790, 1.138),
                [GasMa] = (0.95, 0.85, 0.95, 0.85, 1.0),
            };

        public const string Description =
            "Efficiency set: Screening = 0.90/0.85/0.90/0.80, Cd 1 (default, an assumption); R744_AllPoints / _Supercritical / " +
            "_SubcriticalSubcooled = fitted on 42 R744 points of one 2 mm ejector (Giacomelli 2019), Cd 1.138; " +
            "GasEjector_Ma2017 = 0.95/0.85/0.95/0.85 (single-phase gas ejectors); Custom = use the efficiency parameters as entered.";
    }
}
