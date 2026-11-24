using System;
using System.Collections.Generic;

namespace Ejector.CapeOpen
{
    /// <summary>One efficiency set: efficiencies, motive discharge coefficient, mixing model and (CPM) phi_p.</summary>
    internal readonly struct EffSet
    {
        public EffSet(double etaM, double etaS, double etaMix, double etaD, double cd, string mixing, double phiP)
        {
            EtaM = etaM; EtaS = etaS; EtaMix = etaMix; EtaD = etaD; Cd = cd; Mixing = mixing; PhiP = phiP;
        }
        public double EtaM { get; }
        public double EtaS { get; }
        public double EtaMix { get; }
        public double EtaD { get; }
        public double Cd { get; }
        public string Mixing { get; }
        public double PhiP { get; }
    }

    /// <summary>
    /// Named efficiency sets (techref data_library "Efficiency sets" and the parameter guide). Each set carries
    /// the mixing model it was fitted with; using a set with another mixing model is not supported. Screening (the
    /// default) and Vapour_CPM use constant-pressure mixing, the others constant-area mixing.
    /// </summary>
    internal static class Presets
    {
        public const string Custom = "Custom";
        public const string Screening = "Screening";
        public const string R744All = "R744_AllPoints";
        public const string R744Super = "R744_Supercritical";
        public const string R744Sub = "R744_SubcriticalSubcooled";
        public const string GasMa = "GasEjector_Ma2017";
        public const string VapourCpm = "Vapour_CPM";

        public const string ConstantArea = "ConstantArea";
        public const string ConstantPressure = "ConstantPressure";

        public static readonly string[] Names = { Screening, R744All, R744Super, R744Sub, GasMa, VapourCpm, Custom };

        public static readonly IReadOnlyDictionary<string, EffSet> Values =
            new Dictionary<string, EffSet>(StringComparer.Ordinal)
            {
                // default set; constant-pressure mixing predicts better without calibration (techref parameter guide)
                [Screening] = new EffSet(0.90, 0.85, 0.90, 0.80, 1.0, ConstantPressure, 1.0),
                [R744All] = new EffSet(0.90, 0.610, 0.825, 0.820, 1.138, ConstantArea, 1.0),
                [R744Super] = new EffSet(0.90, 0.638, 0.829, 0.814, 1.138, ConstantArea, 1.0),
                [R744Sub] = new EffSet(0.90, 0.872, 0.767, 0.790, 1.138, ConstantArea, 1.0),
                [GasMa] = new EffSet(0.95, 0.85, 0.95, 0.85, 1.0, ConstantArea, 1.0),
                // pooled fit on Huang 1999 (R141b), Falat 2018 (R134a), Shestopalov 2015 (R245fa):
                // techref figures/data/guide_transfer.csv (CPM, fitted on all three)
                [VapourCpm] = new EffSet(0.90, 0.85, 0.908, 0.85, 1.0, ConstantPressure, 0.869),
            };

        public const string Description =
            "Efficiency set: Screening = 0.90/0.85/0.90/0.80, Cd 1, constant-pressure mixing (default, an assumption); " +
            "R744_AllPoints / _Supercritical / " +
            "_SubcriticalSubcooled = fitted on 42 R744 points of one 2 mm ejector (Giacomelli 2019), Cd 1.138; " +
            "GasEjector_Ma2017 = 0.95/0.85/0.95/0.85 (single-phase gas ejectors, constant-area mixing); " +
            "Vapour_CPM = 0.90/0.85/0.908/0.85, phi_p 0.869, constant-pressure mixing (fitted on three single-phase vapour " +
            "ejector data sets, R141b/R134a/R245fa); Custom = use the efficiency and mixing parameters as entered.";
    }
}
