using System;
using System.Collections;
using System.Globalization;
using CAPEOPEN;

namespace Membrane.CapeOpen
{
    /// <summary>Feed conditions read from an inlet Material Object.</summary>
    internal sealed class FeedState
    {
        public string[] ComponentIds = Array.Empty<string>();
        public double Temperature;   // K
        public double Pressure;      // Pa
        public double MolarFlow;     // mol/s
        public double[] MoleFractions = Array.Empty<double>();
    }

    /// <summary>
    /// Reads feed state and writes/flashes product streams through a CAPE-OPEN Material Object. Supports
    /// CO 1.1 (<see cref="ICapeThermoMaterial"/>, preferred — COFE always uses 1.1) and falls back to CO 1.0
    /// (<see cref="ICapeThermoMaterialObject"/>). All properties are SI (K, Pa, mol/s). Inlet objects are
    /// read-only (spec §2.1.6); outlets are set then flashed at T,P. Every PME call is logged so failures
    /// are pinpointed in membrane_capeopen.log.
    /// </summary>
    internal static class MaterialObjectAdapter
    {
        // Basis strings: CO 1.1 uses lowercase "mole"; T/P take an UNDEFINED (null) basis.
        private const string BasisMole = "mole";

        // ---------------- compound list (config; safe during Validate) ----------------

        public static string[] ReadComponentIds(object mo)
        {
            if (mo is ICapeThermoCompounds c11)   // CO 1.1 material also implements ICapeThermoCompounds
            {
                Diagnostics.Log("ReadComponentIds: CO 1.1 GetCompoundList");
                object ids = null!, formulae = null!, names = null!, boil = null!, mw = null!, cas = null!;
                c11.GetCompoundList(ref ids, ref formulae, ref names, ref boil, ref mw, ref cas);
                return ToStringArray(ids);
            }
            if (mo is ICapeThermoMaterialObject mo10)
            {
                Diagnostics.Log("ReadComponentIds: CO 1.0 ComponentIds");
                return ToStringArray(mo10.ComponentIds);
            }
            throw ComError.Unknown("Connected object exposes neither ICapeThermoCompounds (1.1) nor ICapeThermoMaterialObject (1.0).");
        }

        // ---------------- feed read (state; only at Calculate) ----------------

        public static FeedState ReadFeed(object mo)
        {
            var s = new FeedState { ComponentIds = ReadComponentIds(mo) };

            if (mo is ICapeThermoMaterial m11)
            {
                Diagnostics.Log("ReadFeed(1.1): GetOverallTPFraction");
                double t = 0.0, p = 0.0; object comp = null!;
                m11.GetOverallTPFraction(ref t, ref p, ref comp);
                s.Temperature = t;
                s.Pressure = p;
                s.MoleFractions = ToDoubleArray(comp);
                s.MolarFlow = ReadTotalFlow11(m11);
            }
            else if (mo is ICapeThermoMaterialObject m10)
            {
                Diagnostics.Log("ReadFeed(1.0): GetProp temperature/pressure/totalFlow/fraction");
                s.Temperature = Scalar(m10.GetProp("temperature", "Overall", null, null, null));
                s.Pressure = Scalar(m10.GetProp("pressure", "Overall", null, null, null));
                s.MolarFlow = Scalar(m10.GetProp("totalFlow", "Overall", null, "Mixture", "Mole"));
                s.MoleFractions = ToDoubleArray(m10.GetProp("fraction", "Overall", null, "Mixture", "Mole"));
            }
            else
            {
                throw ComError.Unknown("Connected feed is not a CO 1.1 or 1.0 Material Object.");
            }

            if (s.MoleFractions.Length != s.ComponentIds.Length)
                throw ComError.Unknown($"Feed composition length ({s.MoleFractions.Length}) != compound count ({s.ComponentIds.Length}).");

            Diagnostics.Log($"ReadFeed OK: T={s.Temperature:F3}K P={s.Pressure:F1}Pa F={s.MolarFlow:E4}mol/s " +
                            $"comps=[{string.Join(",", s.ComponentIds)}] x=[{string.Join(",", s.MoleFractions)}]");
            return s;
        }

        private static double ReadTotalFlow11(ICapeThermoMaterial m)
        {
            try
            {
                Diagnostics.Log("ReadFeed(1.1): GetOverallProp totalFlow");
                object f = null!;
                m.GetOverallProp("totalFlow", BasisMole, ref f);
                return Scalar(f);
            }
            catch (Exception ex)
            {
                // Fallback: sum per-component overall flows.
                Diagnostics.Log("ReadFeed(1.1): totalFlow unavailable (" + ex.Message + "); summing 'flow'");
                object comp = null!;
                m.GetOverallProp("flow", BasisMole, ref comp);
                var flows = ToDoubleArray(comp);
                double sum = 0.0; foreach (var v in flows) sum += v;
                return sum;
            }
        }

        // ---------------- outlet write + flash ----------------

        public static void WriteStream(object mo, double[] moleFractions, double molarFlow,
                                       double pressurePa, double temperatureK)
        {
            var x = NormalizeComposition(moleFractions);
            double flow = molarFlow < 0.0 ? 0.0 : molarFlow;

            if (mo is ICapeThermoMaterial m11)
            {
                Diagnostics.Log($"WriteStream(1.1): ClearAllProps + SetOverallProp (T={temperatureK:F2} P={pressurePa:F0} F={flow:E3})");
                m11.ClearAllProps();
                m11.SetOverallProp("temperature", null, new[] { temperatureK });
                m11.SetOverallProp("pressure", null, new[] { pressurePa });
                m11.SetOverallProp("fraction", BasisMole, x);
                m11.SetOverallProp("totalFlow", BasisMole, new[] { flow });

                if (mo is ICapeThermoEquilibriumRoutine eq)
                {
                    // CalcEquilibrium specifications are CapeArrayString [propertyId, basis, phase]. Pass a
                    // string[] (marshals as SAFEARRAY(BSTR)); an object[] marshals as SAFEARRAY(VARIANT),
                    // which COCO rejects with ECapeUnknown.
                    string[] spec1 = { "temperature", null, "Overall" };
                    string[] spec2 = { "pressure", null, "Overall" };
                    try
                    {
                        eq.CalcEquilibrium(spec1, spec2, "Unspecified");
                        Diagnostics.Log("WriteStream(1.1): CalcEquilibrium TP OK");
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal: the overall T/P/composition/flow are already set, so the PME can flash
                        // the product stream itself during the flowsheet solve. Log and continue.
                        Diagnostics.Log("WriteStream(1.1): CalcEquilibrium failed (" + ex.Message + "); leaving flash to PME.");
                    }
                }
                else
                {
                    Diagnostics.Log("WriteStream(1.1): no ICapeThermoEquilibriumRoutine; overall props set, PME will flash.");
                }
                return;
            }

            if (mo is ICapeThermoMaterialObject m10)
            {
                Diagnostics.Log($"WriteStream(1.0): SetProp + CalcEquilibrium TP (T={temperatureK:F2} P={pressurePa:F0} F={flow:E3})");
                m10.SetProp("fraction", "Overall", null, "Mixture", "Mole", x);
                m10.SetProp("totalFlow", "Overall", null, "Mixture", "Mole", new[] { flow });
                m10.SetProp("pressure", "Overall", null, null, null, new[] { pressurePa });
                m10.SetProp("temperature", "Overall", null, null, null, new[] { temperatureK });
                try { m10.CalcEquilibrium("TP", null); }
                catch (Exception ex) { throw ComError.SolvingError("Outlet TP flash (1.0) failed: " + ex.Message); }
                return;
            }

            throw ComError.Unknown("Outlet is not a CO 1.1 or 1.0 Material Object.");
        }

        // ---------------- helpers ----------------

        private static double[] NormalizeComposition(double[] moleFractions)
        {
            var x = (double[])moleFractions.Clone();
            double sum = 0.0;
            for (int i = 0; i < x.Length; i++) { if (x[i] < 0.0) x[i] = 0.0; sum += x[i]; }
            if (sum <= 0.0) throw ComError.SolvingError("Outlet composition sums to zero.");
            for (int i = 0; i < x.Length; i++) x[i] /= sum;
            return x;
        }

        private static double Scalar(object v)
        {
            var a = ToDoubleArray(v);
            if (a.Length == 0) throw ComError.Unknown("Expected a scalar property value, got empty array.");
            return a[0];
        }

        internal static double[] ToDoubleArray(object v)
        {
            switch (v)
            {
                case null: return Array.Empty<double>();
                case double[] d: return d;
                case double d0: return new[] { d0 };
                case float[] f:
                    { var r = new double[f.Length]; for (int i = 0; i < f.Length; i++) r[i] = f[i]; return r; }
                case IEnumerable en:
                    {
                        var list = new System.Collections.Generic.List<double>();
                        foreach (var o in en) list.Add(Convert.ToDouble(o, CultureInfo.InvariantCulture));
                        return list.ToArray();
                    }
                default: return new[] { Convert.ToDouble(v, CultureInfo.InvariantCulture) };
            }
        }

        internal static string[] ToStringArray(object v)
        {
            switch (v)
            {
                case null: return Array.Empty<string>();
                case string[] s: return s;
                case string s0: return new[] { s0 };
                case IEnumerable en:
                    {
                        var list = new System.Collections.Generic.List<string>();
                        foreach (var o in en) list.Add(Convert.ToString(o, CultureInfo.InvariantCulture) ?? string.Empty);
                        return list.ToArray();
                    }
                default: return new[] { Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty };
            }
        }
    }
}
