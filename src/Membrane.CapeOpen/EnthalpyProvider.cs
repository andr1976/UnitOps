using System;
using System.Runtime.InteropServices;
using CAPEOPEN;
using MembraneCore.Energy;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// Real-fluid molar enthalpy for the non-isothermal energy balance, delegated to the PME's property
    /// package via a CAPE-OPEN 1.1 Material Object. A private scratch material (cloned from the feed with
    /// <c>CreateMaterial</c>) is reused for every query so the real feed/retentate/permeate objects are never
    /// disturbed. For each (T, P, x) it sets the overall state, flashes, and sums the phase enthalpies
    /// weighted by phase fraction.
    ///
    /// The PME's enthalpy encodes the equation of state, so the Joule–Thomson behaviour comes for free. If the
    /// property package cannot deliver an enthalpy (older/limited PME), <see cref="TryProbe"/> reports it up
    /// front so the caller can fall back to an isothermal result rather than failing the solve.
    /// </summary>
    internal sealed class EnthalpyProvider : IEnthalpyProvider, IDisposable
    {
        private const string BasisMole = "mole";
        private readonly object _scratch;
        private readonly bool _ownsScratch;
        private bool _logged;

        private EnthalpyProvider(object scratch, bool ownsScratch)
        {
            _scratch = scratch;
            _ownsScratch = ownsScratch;
        }

        /// <summary>
        /// Builds a provider from the feed Material Object, cloning a scratch material when possible. Returns
        /// null if the feed is not a CO 1.1 material (the energy layer only supports 1.1 thermo).
        /// </summary>
        public static EnthalpyProvider? Create(object feedMo)
        {
            if (feedMo is not ICapeThermoMaterial m11) return null;
            try
            {
                object scratch = m11.CreateMaterial();
                Diagnostics.Log("EnthalpyProvider: scratch material created via CreateMaterial");
                return new EnthalpyProvider(scratch, ownsScratch: true);
            }
            catch (Exception ex)
            {
                // Some PMEs may not support CreateMaterial; fall back to the feed object itself (read of
                // enthalpy is non-destructive to T/P/composition, but we avoid ClearAllProps on it).
                Diagnostics.Log("EnthalpyProvider: CreateMaterial failed (" + ex.Message + "); using feed object directly");
                return new EnthalpyProvider(feedMo, ownsScratch: false);
            }
        }

        /// <summary>One trial evaluation so the caller can decide isothermal-vs-adiabatic before writing streams.</summary>
        public bool TryProbe(double t, double p, double[] x, out string reason)
        {
            try
            {
                double h = MolarEnthalpy(t, p, x);
                if (double.IsNaN(h) || double.IsInfinity(h)) { reason = "enthalpy is not finite"; return false; }
                reason = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        public double MolarEnthalpy(double temperatureK, double pressurePa, double[] moleFractions)
        {
            var m = (ICapeThermoMaterial)_scratch;

            // Set the overall state on the scratch material.
            if (_ownsScratch) m.ClearAllProps();
            m.SetOverallProp("temperature", null, new[] { temperatureK });
            m.SetOverallProp("pressure", null, new[] { pressurePa });
            m.SetOverallProp("fraction", BasisMole, moleFractions);
            m.SetOverallProp("totalFlow", BasisMole, new[] { 1.0 });

            // Flash at T,P so the property package establishes the present phases.
            if (_scratch is ICapeThermoEquilibriumRoutine eq)
            {
                string[] spec1 = { "temperature", null!, "Overall" };
                string[] spec2 = { "pressure", null!, "Overall" };
                try { eq.CalcEquilibrium(spec1, spec2, "Unspecified"); }
                catch (Exception ex) { if (!_logged) Diagnostics.Log("EnthalpyProvider: flash failed (" + ex.Message + "), reading phases as-is"); }
            }

            // Sum phase enthalpies weighted by phase mole fraction → overall molar enthalpy [J/mol].
            var routine = _scratch as ICapeThermoPropertyRoutine
                ?? throw ComError.Unknown("Material Object does not implement ICapeThermoPropertyRoutine (needed for enthalpy).");

            object labelsObj = null!, statusObj = null!;
            m.GetPresentPhases(ref labelsObj, ref statusObj);
            string[] phases = MaterialObjectAdapter.ToStringArray(labelsObj);
            if (phases.Length == 0)
                throw ComError.Unknown("No present phases after flash; cannot evaluate enthalpy.");

            double hTotal = 0.0, fracSum = 0.0;
            foreach (var phase in phases)
            {
                routine.CalcSinglePhaseProp(new[] { "enthalpy" }, phase);
                object hObj = null!;
                m.GetSinglePhaseProp("enthalpy", phase, BasisMole, ref hObj);
                double hPhase = First(hObj);

                double frac = 1.0;
                if (phases.Length > 1)
                {
                    try
                    {
                        object pfObj = null!;
                        m.GetSinglePhaseProp("phaseFraction", phase, BasisMole, ref pfObj);
                        frac = First(pfObj);
                    }
                    catch { frac = 1.0 / phases.Length; }
                }
                hTotal += hPhase * frac;
                fracSum += frac;
            }
            if (fracSum > 0.0 && Math.Abs(fracSum - 1.0) > 1e-6) hTotal /= fracSum;   // normalise if fractions imperfect

            if (!_logged)
            {
                Diagnostics.Log($"EnthalpyProvider: first eval OK h={hTotal:E6} J/mol at T={temperatureK:F2}K P={pressurePa:F0}Pa phases=[{string.Join(",", phases)}]");
                _logged = true;
            }
            return hTotal;
        }

        /// <summary>
        /// Per-component fugacity coefficients φ_i at (T, p, x) for the (vapour) phase, from the PME's EOS.
        /// Returns false (and the caller falls back to a partial-pressure driving force) if the property
        /// package cannot deliver them.
        /// </summary>
        public bool TryFugacityCoefficients(double temperatureK, double pressurePa, double[] moleFractions,
            out double[] phi)
        {
            phi = Array.Empty<double>();
            try
            {
                var m = (ICapeThermoMaterial)_scratch;
                if (_ownsScratch) m.ClearAllProps();
                m.SetOverallProp("temperature", null, new[] { temperatureK });
                m.SetOverallProp("pressure", null, new[] { pressurePa });
                m.SetOverallProp("fraction", BasisMole, moleFractions);
                m.SetOverallProp("totalFlow", BasisMole, new[] { 1.0 });

                if (_scratch is ICapeThermoEquilibriumRoutine eq)
                {
                    string[] spec1 = { "temperature", null!, "Overall" };
                    string[] spec2 = { "pressure", null!, "Overall" };
                    try { eq.CalcEquilibrium(spec1, spec2, "Unspecified"); } catch { /* read phases as-is */ }
                }

                if (_scratch is not ICapeThermoPropertyRoutine routine) return false;
                object labelsObj = null!, statusObj = null!;
                m.GetPresentPhases(ref labelsObj, ref statusObj);
                var phases = MaterialObjectAdapter.ToStringArray(labelsObj);
                if (phases.Length == 0) return false;

                // Gas permeation: a single vapour phase. fugacityCoefficient has an UNDEFINED basis.
                string phase = phases[0];
                routine.CalcSinglePhaseProp(new[] { "fugacityCoefficient" }, phase);
                object res = null!;
                m.GetSinglePhaseProp("fugacityCoefficient", phase, null, ref res);
                var arr = MaterialObjectAdapter.ToDoubleArray(res);
                if (arr.Length != moleFractions.Length) return false;
                foreach (var v in arr)
                    if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0.0) return false;
                phi = arr;
                return true;
            }
            catch (Exception ex)
            {
                Diagnostics.Log("Fugacity coefficient evaluation failed: " + ex.Message);
                return false;
            }
        }

        private static double First(object v)
        {
            var a = MaterialObjectAdapter.ToDoubleArray(v);
            if (a.Length == 0) throw ComError.Unknown("Property returned an empty array.");
            return a[0];
        }

        public void Dispose()
        {
            if (_ownsScratch && _scratch != null && Marshal.IsComObject(_scratch))
            {
                try { Marshal.ReleaseComObject(_scratch); } catch { }
            }
        }
    }
}
