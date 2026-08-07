using System;
using System.Collections.Generic;
using CAPEOPEN;

namespace Membrane.CapeOpen.Tests
{
    /// <summary>
    /// Minimal in-memory CO 1.1 Material Object for headless testing of the adapter without a PME.
    /// Implements <see cref="ICapeThermoMaterial"/>, <see cref="ICapeThermoCompounds"/> and
    /// <see cref="ICapeThermoEquilibriumRoutine"/> (as COFE's materials do). The 1.1 interop declares the
    /// [out] parameters as <c>ref</c>, so the implementations match that. A feed mock is preloaded with
    /// T/P/flow/composition; outlet mocks capture what the unit sets; the flash is a no-op counter.
    /// </summary>
    public sealed class MockMaterialObject : ICapeThermoMaterial, ICapeThermoCompounds,
        ICapeThermoEquilibriumRoutine, ICapeThermoPropertyRoutine
    {
        private readonly string[] _ids;
        private double _t, _p;
        private double[] _composition = Array.Empty<double>();
        private readonly Dictionary<string, double[]> _overall =
            new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double[]> _phaseProp =
            new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);

        // Synthetic linear Joule-Thomson enthalpy [J/mol]: h = Cp(T-Tref) - mu*Cp(P-Pref), so an
        // isenthalpic expansion cools by mu*dP and a constant-pressure stream keeps its temperature.
        private const double Cp = 30.0, Mu = 1.0e-6, Tref = 298.15, Pref = 101325.0;
        private static double MolarEnthalpy(double t, double p) => Cp * ((t - Tref) - Mu * (p - Pref));

        /// <summary>Optional per-component fugacity coefficients returned by the property routine (null ⇒ ideal, all 1).</summary>
        public double[]? Fugacity;

        /// <summary>When true, φ_i = 1 − 0.4·x_i (composition-dependent), so it varies along the module —
        /// used to exercise the FugacityLocal (position-dependent) driving force.</summary>
        public bool CompositionDependentFugacity;

        public int CalcEquilibriumCalls { get; private set; }

        public MockMaterialObject(string[] componentIds) => _ids = componentIds;

        public static MockMaterialObject Feed(string[] ids, double tempK, double pressPa, double molarFlow, double[] fractions)
        {
            var m = new MockMaterialObject(ids) { _t = tempK, _p = pressPa, _composition = (double[])fractions.Clone() };
            m._overall["totalFlow"] = new[] { molarFlow };
            m._overall["fraction"] = (double[])fractions.Clone();
            m._overall["temperature"] = new[] { tempK };
            m._overall["pressure"] = new[] { pressPa };
            return m;
        }

        /// <summary>Test accessor: value last stored for an overall property (case-insensitive).</summary>
        public double[] Get(string property) => _overall.TryGetValue(property, out var v) ? v : Array.Empty<double>();

        // ---- ICapeThermoMaterial (used by the adapter) ----
        public void GetOverallTPFraction(ref double temperature, ref double pressure, ref object composition)
        { temperature = _t; pressure = _p; composition = _composition; }

        public void GetOverallProp(string property, string basis, ref object results)
        {
            if (_overall.TryGetValue(property, out var v)) results = v;
            else throw new ArgumentException($"mock has no overall '{property}'");
        }

        public void SetOverallProp(string property, string basis, object values)
        {
            var arr = (double[])values;
            _overall[property] = (double[])arr.Clone();
            if (property.Equals("temperature", StringComparison.OrdinalIgnoreCase)) _t = arr[0];
            else if (property.Equals("pressure", StringComparison.OrdinalIgnoreCase)) _p = arr[0];
            else if (property.Equals("fraction", StringComparison.OrdinalIgnoreCase)) _composition = (double[])arr.Clone();
        }

        public void ClearAllProps() { _overall.Clear(); }

        // ---- ICapeThermoEquilibriumRoutine ----
        public void CalcEquilibrium(object specification1, object specification2, string solutionType) => CalcEquilibriumCalls++;
        public bool CheckEquilibriumSpec(object specification1, object specification2, string solutionType) => true;

        // ---- ICapeThermoCompounds (compound list) ----
        public void GetCompoundList(ref object compIds, ref object formulae, ref object names,
                                    ref object boilTemps, ref object molwts, ref object casnos)
        {
            compIds = _ids;
            formulae = new string[_ids.Length];
            names = _ids;
            boilTemps = new double[_ids.Length];
            molwts = new double[_ids.Length];
            casnos = new string[_ids.Length];
        }
        public int GetNumCompounds() => _ids.Length;

        // ---- ICapeThermoMaterial phase access + ICapeThermoPropertyRoutine (enthalpy for the energy layer) ----
        public object CreateMaterial() => new MockMaterialObject(_ids)
        { Fugacity = Fugacity, CompositionDependentFugacity = CompositionDependentFugacity };

        public void GetPresentPhases(ref object phaseLabels, ref object phaseStatus)
        {
            phaseLabels = new[] { "Vapor" };
            phaseStatus = new[] { 2 };   // Cape_AtEquilibrium
        }

        public void CalcSinglePhaseProp(object props, string phaseLabel)
        {
            // Enthalpy from the overall T/P; fugacity coefficients from the configured array (default ideal, all 1).
            _phaseProp["enthalpy"] = new[] { MolarEnthalpy(_t, _p) };
            var phi = new double[_ids.Length];
            for (int i = 0; i < _ids.Length; i++)
            {
                if (CompositionDependentFugacity)
                    phi[i] = 1.0 - 0.4 * (i < _composition.Length ? _composition[i] : 0.0);
                else
                    phi[i] = (Fugacity != null && Fugacity.Length == _ids.Length) ? Fugacity[i] : 1.0;
            }
            _phaseProp["fugacityCoefficient"] = phi;
        }

        public void GetSinglePhaseProp(string property, string phaseLabel, string basis, ref object results)
        {
            if (property.Equals("enthalpy", StringComparison.OrdinalIgnoreCase) &&
                _phaseProp.TryGetValue("enthalpy", out var h)) { results = h; return; }
            if (property.Equals("fugacityCoefficient", StringComparison.OrdinalIgnoreCase) &&
                _phaseProp.TryGetValue("fugacityCoefficient", out var f)) { results = f; return; }
            if (property.Equals("phaseFraction", StringComparison.OrdinalIgnoreCase)) { results = new[] { 1.0 }; return; }
            throw new NotSupportedException($"mock GetSinglePhaseProp '{property}'");
        }

        public void CalcTwoPhaseProp(object props, object phaseLabels) => throw new NotSupportedException();
        public void CalcAndGetLnPhi(string phaseLabel, double temperature, double pressure, object moleNumbers,
            int fFlags, ref object lnPhi, ref object lnPhiDT, ref object lnPhiDP, ref object lnPhiDMoles)
            => throw new NotSupportedException();
        public bool CheckSinglePhasePropSpec(string property, string phaseLabel) => true;
        public bool CheckTwoPhasePropSpec(string property, object phaseLabels) => false;
        public object GetSinglePhasePropList() => new[] { "enthalpy" };
        public object GetTwoPhasePropList() => Array.Empty<string>();

        // ---- unused members ----
        public void CopyFromMaterial(ref object source) => throw new NotSupportedException();
        public void GetTPFraction(string phaseLabel, ref double temperature, ref double pressure, ref object composition) => throw new NotSupportedException();
        public void GetTwoPhaseProp(string property, object phaseLabels, string basis, ref object results) => throw new NotSupportedException();
        public void SetPresentPhases(object phaseLabels, object phaseStatus) => throw new NotSupportedException();
        public void SetSinglePhaseProp(string property, string phaseLabel, string basis, object values) => throw new NotSupportedException();
        public void SetTwoPhaseProp(string property, object phaseLabels, string basis, object values) => throw new NotSupportedException();
        public object GetCompoundConstant(object props, object compIds) => throw new NotSupportedException();
        public object GetConstPropList() => throw new NotSupportedException();
        public void GetPDependentProperty(object props, double pressure, object compIds, ref object propVals) => throw new NotSupportedException();
        public object GetPDependentPropList() => throw new NotSupportedException();
        public void GetTDependentProperty(object props, double temperature, object compIds, ref object propVals) => throw new NotSupportedException();
        public object GetTDependentPropList() => throw new NotSupportedException();
    }
}
