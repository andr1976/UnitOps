using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using CAPEOPEN;
using EjectorCore;

namespace Ejector.CapeOpen
{
    /// <summary>An inlet stream as read from its material object (SI, molar).</summary>
    internal sealed class InletStream
    {
        public string[] ComponentIds = Array.Empty<string>();
        public double T, P, MolarFlow;
        public double[] Z = Array.Empty<double>();
        /// <summary>Overall molar enthalpy [J/mol] if the material object exposes it.</summary>
        public double? MolarEnthalpy;
        /// <summary>Present phases and their mole fractions, as reported by the stream.</summary>
        public string[] Phases = Array.Empty<string>();
        public double[] PhaseFractions = Array.Empty<double>();
    }

    /// <summary>
    /// Reads the inlet material objects and writes the outlet (CAPE-OPEN 1.1; the ejector needs PH/PS/PQ
    /// flashes on a scratch material, which CO 1.0 cannot provide). Inlets are read-only.
    /// </summary>
    internal static class StreamAdapter
    {
        private const string Mole = "mole";

        public static string[] ReadComponentIds(object mo)
        {
            if (mo is ICapeThermoCompounds c11)
            {
                object ids = null!, formulae = null!, names = null!, boil = null!, mw = null!, cas = null!;
                c11.GetCompoundList(ref ids, ref formulae, ref names, ref boil, ref mw, ref cas);
                return ToStringArray(ids);
            }
            if (mo is ICapeThermoMaterialObject mo10) return ToStringArray(mo10.ComponentIds);
            throw ComError.Unknown("Connected object exposes neither ICapeThermoCompounds (1.1) nor ICapeThermoMaterialObject (1.0).");
        }

        /// <summary>Compound molecular weights [kg/mol] (CAPE-OPEN reports g/mol).</summary>
        public static double[] ReadMolecularWeights(object mo, string[] ids)
        {
            if (mo is not ICapeThermoCompounds c11)
                throw ComError.Unknown("The ejector needs CAPE-OPEN 1.1 thermodynamics (ICapeThermoCompounds).");
            double[] mw = Array.Empty<double>();
            try
            {
                object idsO = null!, formulae = null!, names = null!, boil = null!, mwO = null!, cas = null!;
                c11.GetCompoundList(ref idsO, ref formulae, ref names, ref boil, ref mwO, ref cas);
                mw = ToDoubleArray(mwO);
            }
            catch (Exception ex) { Diagnostics.Log("GetCompoundList molecular weights failed: " + ex.Message); }
            if (mw.Length != ids.Length || Array.Exists(mw, v => !(v > 0)))
            {
                object v = c11.GetCompoundConstant(new[] { "molecularWeight" }, ids);
                mw = ToDoubleArray(v);
            }
            if (mw.Length != ids.Length) throw ComError.Unknown("Could not read the compound molecular weights.");
            var kg = new double[mw.Length];
            for (int i = 0; i < mw.Length; i++) kg[i] = mw[i] / 1000.0;
            return kg;
        }

        /// <summary>Triple-point pressure [Pa] of a pure compound, 0 if the package does not provide it.</summary>
        public static double ReadTriplePointPressure(object mo, string id)
        {
            try
            {
                if (mo is ICapeThermoCompounds c11)
                {
                    double v = First(c11.GetCompoundConstant(new[] { "triplePointPressure" }, new[] { id }));
                    return v > 0 && !double.IsInfinity(v) ? v : 0.0;
                }
            }
            catch (Exception ex) { Diagnostics.Log("triplePointPressure not available: " + ex.Message); }
            return 0.0;
        }

        public static InletStream ReadInlet(object mo, string what)
        {
            if (mo is not ICapeThermoMaterial m)
                throw ComError.Unknown($"{what}: the ejector needs a CAPE-OPEN 1.1 material object.");
            var s = new InletStream { ComponentIds = ReadComponentIds(mo) };
            double t = 0.0, p = 0.0; object comp = null!;
            m.GetOverallTPFraction(ref t, ref p, ref comp);
            s.T = t; s.P = p; s.Z = ToDoubleArray(comp);
            try
            {
                object f = null!;
                m.GetOverallProp("totalFlow", Mole, ref f);
                s.MolarFlow = First(f);
            }
            catch (Exception ex)
            {
                Diagnostics.Log($"{what}: totalFlow unavailable ({ex.Message}); summing 'flow'");
                object f = null!;
                m.GetOverallProp("flow", Mole, ref f);
                double sum = 0.0; foreach (var v in ToDoubleArray(f)) sum += v;
                s.MolarFlow = sum;
            }
            try
            {
                object h = null!;
                m.GetOverallProp("enthalpy", Mole, ref h);
                double hv = First(h);
                if (!double.IsNaN(hv) && !double.IsInfinity(hv)) s.MolarEnthalpy = hv;
            }
            catch { /* not every material object stores an overall enthalpy */ }
            try
            {
                object labels = null!, status = null!;
                m.GetPresentPhases(ref labels, ref status);
                s.Phases = ToStringArray(labels);
                s.PhaseFractions = new double[s.Phases.Length];
                for (int k = 0; k < s.Phases.Length; k++)
                {
                    if (s.Phases.Length == 1) { s.PhaseFractions[k] = 1.0; continue; }
                    object b = null!;
                    m.GetSinglePhaseProp("phaseFraction", s.Phases[k], Mole, ref b);
                    s.PhaseFractions[k] = First(b);
                }
            }
            catch (Exception ex) { Diagnostics.Log($"{what}: present phases unavailable: {ex.Message}"); }
            if (s.Z.Length != s.ComponentIds.Length)
                throw ComError.Unknown($"{what}: composition length ({s.Z.Length}) != compound count ({s.ComponentIds.Length}).");
            Diagnostics.Log(FormattableString.Invariant(
                $"{what}: T={s.T:F3} K P={s.P:F1} Pa F={s.MolarFlow:E5} mol/s H={s.MolarEnthalpy?.ToString("E6", CultureInfo.InvariantCulture) ?? "n/a"} phases=[{string.Join(",", s.Phases)}] beta=[{string.Join(",", s.PhaseFractions)}]"));
            return s;
        }

        /// <summary>
        /// Stagnation state of an inlet. Order of preference: the stream's own overall enthalpy (PH flash);
        /// a two-phase stream by its vapour fraction (PQ flash); otherwise a TP flash, corrected to the
        /// saturated end point the stream reports when the TP flash lands on the other side of the
        /// saturation line (a saturated liquid or vapour specified by vapour fraction).
        /// </summary>
        public static FluidState InletState(EjectorCore.Ejector ej, InletStream s, ISet<string> vapourPhases, double[]? z)
        {
            if (s.MolarEnthalpy.HasValue)
                return ej.InletPH(s.P, s.MolarEnthalpy.Value / ej.MolarMass(z), z);
            double bV = 0.0, bL = 0.0;
            for (int k = 0; k < s.Phases.Length; k++)
            {
                if (vapourPhases.Contains(s.Phases[k])) bV += s.PhaseFractions[k]; else bL += s.PhaseFractions[k];
            }
            if (bV > 1e-9 && bL > 1e-9) return ej.InletPQ(s.P, bV / (bV + bL), z);
            FluidState? st = null;
            try { st = ej.InletPT(s.P, s.T, z); }
            catch (FlashException ex) { Diagnostics.Log("Inlet TP flash failed (" + ex.Message + ")"); }
            bool liquidOnly = bL > 0 && bV <= 1e-9, vapourOnly = bV > 0 && bL <= 1e-9;
            // a TP flash exactly at saturation may fail or land on the other side of (or inside) the dome
            bool wrongSide = st == null || (liquidOnly && st.Q >= 0.0) || (vapourOnly && st.Q < 1.0);
            if (wrongSide && (liquidOnly || vapourOnly))
            {
                try
                {
                    var sat = ej.InletPQ(s.P, liquidOnly ? 0.0 : 1.0, z);
                    if (st == null || Math.Abs(sat.T - s.T) < 0.05) return sat;   // the stream is saturated
                }
                catch (FlashException) { /* supercritical or no saturation: keep the TP state */ }
            }
            return st ?? throw new FlashException("the inlet state could not be flashed at the stream's T and P");
        }

        /// <summary>Writes the outlet (P, composition, flow, enthalpy) and asks the PME for a PH flash; if the
        /// package cannot do PH, falls back to a TP flash at the temperature of the engine's outlet state.</summary>
        public static void WriteOutlet(object mo, double[] z, double molarFlow, double pressurePa,
                                       double molarEnthalpy, double temperatureK)
        {
            if (mo is not ICapeThermoMaterial m)
                throw ComError.Unknown("Outlet: the ejector needs a CAPE-OPEN 1.1 material object.");
            var x = Normalize(z);
            m.ClearAllProps();
            m.SetOverallProp("pressure", null, new[] { pressurePa });
            m.SetOverallProp("fraction", Mole, x);
            m.SetOverallProp("totalFlow", Mole, new[] { Math.Max(molarFlow, 0.0) });
            if (mo is not ICapeThermoEquilibriumRoutine eq)
            {
                m.SetOverallProp("temperature", null, new[] { temperatureK });
                Diagnostics.Log("Outlet: no ICapeThermoEquilibriumRoutine; T/P set, PME will flash");
                return;
            }
            try
            {
                m.SetOverallProp("enthalpy", Mole, new[] { molarEnthalpy });
                eq.CalcEquilibrium(new[] { "pressure", null!, "Overall" }, new[] { "enthalpy", null!, "Overall" }, "Unspecified");
                Diagnostics.Log("Outlet: PH flash OK");
            }
            catch (Exception ex)
            {
                Diagnostics.Log("Outlet: PH flash failed (" + ex.Message + "); TP flash at the engine's outlet temperature");
                m.SetOverallProp("temperature", null, new[] { temperatureK });
                try { eq.CalcEquilibrium(new[] { "temperature", null!, "Overall" }, new[] { "pressure", null!, "Overall" }, "Unspecified"); }
                catch (Exception ex2) { throw ComError.SolvingError("Outlet flash failed: " + ex2.Message); }
            }
        }

        private static double[] Normalize(double[] v)
        {
            var x = (double[])v.Clone();
            double sum = 0.0;
            for (int i = 0; i < x.Length; i++) { if (x[i] < 0.0) x[i] = 0.0; sum += x[i]; }
            if (sum <= 0.0) throw ComError.SolvingError("Outlet composition sums to zero.");
            for (int i = 0; i < x.Length; i++) x[i] /= sum;
            return x;
        }

        internal static double First(object v)
        {
            var a = ToDoubleArray(v);
            if (a.Length == 0) throw new FlashException("Property returned an empty array.");
            return a[0];
        }

        internal static double[] ToDoubleArray(object v)
        {
            switch (v)
            {
                case null: return Array.Empty<double>();
                case double[] d: return d;
                case double d0: return new[] { d0 };
                case float[] f: { var r = new double[f.Length]; for (int i = 0; i < f.Length; i++) r[i] = f[i]; return r; }
                case IEnumerable en:
                    {
                        var list = new List<double>();
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
                        var list = new List<string>();
                        foreach (var o in en) list.Add(Convert.ToString(o, CultureInfo.InvariantCulture) ?? string.Empty);
                        return list.ToArray();
                    }
                default: return new[] { Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty };
            }
        }
    }
}
