using CAPEOPEN;
using EjectorCore;

namespace Ejector.CapeOpen.Tests;

/// <summary>
/// In-memory CAPE-OPEN 1.1 material object for ONE pure compound whose flashes are real: CalcEquilibrium
/// runs the requested PT/PH/PS/PQ flash on a backing <see cref="IFlash"/> (CoolProp or an ideal gas) and
/// stores the resulting phases ("Vapor"/"Liquid") with molar phase properties, as a PME would. Used to test
/// the adapter headless: PmeFlash's molar/phase-to-mass conversion, the inlet logic and the outlet flash.
/// </summary>
public sealed class FlashMaterialObject : ICapeThermoMaterial, ICapeThermoCompounds, ICapeThermoPhases,
    ICapeThermoEquilibriumRoutine, ICapeThermoPropertyRoutine
{
    private readonly IFlash _f;
    private readonly string _id;
    private readonly double _mw;      // kg/mol
    private readonly Dictionary<string, double[]> _overall = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _vapourFractionSpec = new(StringComparer.OrdinalIgnoreCase);
    private double _t, _p;
    private List<(string Label, double Beta, FluidState St)> _phases = new();

    /// <summary>CalcEquilibrium specifications this "package" refuses (e.g. "entropy").</summary>
    public HashSet<string> RejectSpecs { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Whether GetOverallProp("enthalpy") is answered (some PMEs do, some do not).</summary>
    public bool ExposeOverallEnthalpy { get; set; }
    public int CalcEquilibriumCalls { get; private set; }

    public FlashMaterialObject(IFlash flash, string compoundId)
    {
        _f = flash;
        _id = compoundId;
        _mw = flash.MolarMass(null);
    }

    /// <summary>An inlet stream flashed at (P, T), or at (P, vapour fraction) when q is given.</summary>
    public static FlashMaterialObject Stream(IFlash f, string id, double P, double T, double molarFlow, double? q = null,
                                             bool exposeEnthalpy = false)
    {
        var m = new FlashMaterialObject(f, id) { ExposeOverallEnthalpy = exposeEnthalpy };
        m.SetOverallProp("pressure", null!, new[] { P });
        m.SetOverallProp("fraction", "mole", new[] { 1.0 });
        m.SetOverallProp("totalFlow", "mole", new[] { molarFlow });
        if (q.HasValue)
        {
            m.SetSinglePhaseProp("phaseFraction", "Vapor", "mole", new[] { q.Value });
            m.CalcEquilibrium(new[] { "pressure", null!, "Overall" }, new[] { "phaseFraction", "mole", "Vapor" }, "Unspecified");
        }
        else
        {
            m.SetOverallProp("temperature", null!, new[] { T });
            m.CalcEquilibrium(new[] { "temperature", null!, "Overall" }, new[] { "pressure", null!, "Overall" }, "Unspecified");
        }
        return m;
    }

    public double[] Get(string property) => _overall.TryGetValue(property, out var v) ? v : Array.Empty<double>();
    public double Temperature => _t;
    public double Pressure => _p;
    public IReadOnlyList<(string Label, double Beta, FluidState St)> Phases => _phases;

    // ---- equilibrium
    public void CalcEquilibrium(object specification1, object specification2, string solutionType)
    {
        CalcEquilibriumCalls++;
        var s1 = (string[])specification1;
        var s2 = (string[])specification2;
        var specs = new[] { s1[0], s2[0] };
        foreach (var s in specs) if (RejectSpecs.Contains(s)) throw new InvalidOperationException($"mock package: {s} specification not supported");
        bool Has(string p) => Array.Exists(specs, s => s.Equals(p, StringComparison.OrdinalIgnoreCase));
        double P = _overall["pressure"][0];
        FluidState st;
        if (Has("temperature")) st = _f.PT(P, _overall["temperature"][0], null);
        else if (Has("enthalpy")) st = _f.PH(P, _overall["enthalpy"][0] / _mw, null);
        else if (Has("entropy")) st = _f.PS(P, _overall["entropy"][0] / _mw, null);
        else if (Has("phaseFraction"))
        {
            double q = _vapourFractionSpec.TryGetValue("Vapor", out var v) ? v : throw new InvalidOperationException("no vapour fraction set");
            st = _f.PQ(P, q, null);
            st.Q = q;
        }
        else throw new InvalidOperationException("mock: unsupported flash specification " + string.Join("/", specs));
        _p = P;
        _t = st.T;
        _phases = new();
        if (st.Q > 0.0 && st.Q < 1.0)
        {
            var l = _f.PQ(P, 0.0, null);
            var v = _f.PQ(P, 1.0, null);
            _phases.Add(("Vapor", st.Q, v));
            _phases.Add(("Liquid", 1.0 - st.Q, l));
        }
        else if (st.Q == 0.0 || st.Q == -1.0) _phases.Add(("Liquid", 1.0, st));
        else _phases.Add(("Vapor", 1.0, st));
        _overall["temperature"] = new[] { st.T };
        double h = 0.0;
        foreach (var (_, b, s) in _phases) h += b * s.H * _mw;
        _overall["enthalpy"] = new[] { h };
    }

    public bool CheckEquilibriumSpec(object specification1, object specification2, string solutionType) => true;

    // ---- ICapeThermoMaterial
    public void GetOverallTPFraction(ref double temperature, ref double pressure, ref object composition)
    {
        temperature = _t; pressure = _p; composition = new[] { 1.0 };
    }

    public void GetOverallProp(string property, string basis, ref object results)
    {
        if (property.Equals("enthalpy", StringComparison.OrdinalIgnoreCase) && !ExposeOverallEnthalpy)
            throw new InvalidOperationException("mock: overall enthalpy not available");
        if (_overall.TryGetValue(property, out var v)) results = v;
        else throw new InvalidOperationException($"mock has no overall '{property}'");
    }

    public void SetOverallProp(string property, string basis, object values)
    {
        var arr = (double[])values;
        _overall[property] = (double[])arr.Clone();
        if (property.Equals("temperature", StringComparison.OrdinalIgnoreCase)) _t = arr[0];
        else if (property.Equals("pressure", StringComparison.OrdinalIgnoreCase)) _p = arr[0];
    }

    public void ClearAllProps()
    {
        _overall.Clear();
        _vapourFractionSpec.Clear();
        _phases = new();
    }

    public object CreateMaterial()
    {
        var m = new FlashMaterialObject(_f, _id) { ExposeOverallEnthalpy = ExposeOverallEnthalpy };
        foreach (var r in RejectSpecs) m.RejectSpecs.Add(r);
        return m;
    }

    public void GetPresentPhases(ref object phaseLabels, ref object phaseStatus)
    {
        phaseLabels = _phases.Select(p => p.Label).ToArray();
        phaseStatus = _phases.Select(_ => 1).ToArray();
    }

    public void SetPresentPhases(object phaseLabels, object phaseStatus) { }

    public void GetSinglePhaseProp(string property, string phaseLabel, string basis, ref object results)
    {
        var ph = _phases.FirstOrDefault(p => p.Label.Equals(phaseLabel, StringComparison.OrdinalIgnoreCase));
        if (ph.St == null) throw new InvalidOperationException($"mock: phase {phaseLabel} not present");
        results = property.ToLowerInvariant() switch
        {
            "phasefraction" => new[] { ph.Beta },
            "fraction" => new[] { 1.0 },
            "density" => new[] { ph.St.Rho / _mw },          // mol/m3
            "enthalpy" => new[] { ph.St.H * _mw },           // J/mol
            "entropy" => new[] { ph.St.S * _mw },            // J/(mol K)
            _ => throw new NotSupportedException($"mock GetSinglePhaseProp '{property}'"),
        };
    }

    public void SetSinglePhaseProp(string property, string phaseLabel, string basis, object values)
    {
        if (property.Equals("phaseFraction", StringComparison.OrdinalIgnoreCase))
            _vapourFractionSpec[phaseLabel] = ((double[])values)[0];
    }

    public void CalcSinglePhaseProp(object props, string phaseLabel) { /* properties are stored at flash time */ }

    // ---- ICapeThermoCompounds
    public void GetCompoundList(ref object compIds, ref object formulae, ref object names, ref object boilTemps,
                                ref object molwts, ref object casnos)
    {
        compIds = new[] { _id };
        formulae = new[] { _id };
        names = new[] { _id };
        boilTemps = new[] { 0.0 };
        molwts = new[] { _mw * 1000.0 };     // g/mol, as CAPE-OPEN reports it
        casnos = new[] { "" };
    }

    public object GetCompoundConstant(object props, object compIds)
    {
        var p = ((string[])props)[0];
        if (p.Equals("triplePointPressure", StringComparison.OrdinalIgnoreCase)) return new object[] { _f.TriplePointPressure };
        if (p.Equals("molecularWeight", StringComparison.OrdinalIgnoreCase)) return new object[] { _mw * 1000.0 };
        throw new NotSupportedException(p);
    }

    public int GetNumCompounds() => 1;
    public object GetConstPropList() => new[] { "molecularWeight", "triplePointPressure" };
    public void GetPDependentProperty(object props, double pressure, object compIds, ref object propVals) => throw new NotSupportedException();
    public object GetPDependentPropList() => Array.Empty<string>();
    public void GetTDependentProperty(object props, double temperature, object compIds, ref object propVals) => throw new NotSupportedException();
    public object GetTDependentPropList() => Array.Empty<string>();

    // ---- ICapeThermoPhases
    public int GetNumPhases() => 2;
    public object GetPhaseInfo(string phaseLabel, string phaseAttribute) =>
        phaseAttribute.Equals("StateOfAggregation", StringComparison.OrdinalIgnoreCase) ? phaseLabel : "";
    public void GetPhaseList(ref object phaseLabels, ref object stateOfAggregation, ref object keyCompoundId)
    {
        phaseLabels = new[] { "Vapor", "Liquid" };
        stateOfAggregation = new[] { "Vapor", "Liquid" };
        keyCompoundId = new string[] { null!, null! };
    }

    // ---- ICapeThermoPropertyRoutine (rest)
    public void CalcTwoPhaseProp(object props, object phaseLabels) => throw new NotSupportedException();
    public void CalcAndGetLnPhi(string phaseLabel, double temperature, double pressure, object moleNumbers, int fFlags,
                                ref object lnPhi, ref object lnPhiDT, ref object lnPhiDP, ref object lnPhiDMoles) => throw new NotSupportedException();
    public bool CheckSinglePhasePropSpec(string property, string phaseLabel) => true;
    public bool CheckTwoPhasePropSpec(string property, object phaseLabels) => false;
    public object GetSinglePhasePropList() => new[] { "density", "enthalpy", "entropy" };
    public object GetTwoPhasePropList() => Array.Empty<string>();

    // ---- unused
    public void CopyFromMaterial(ref object source) => throw new NotSupportedException();
    public void GetTPFraction(string phaseLabel, ref double temperature, ref double pressure, ref object composition) => throw new NotSupportedException();
    public void GetTwoPhaseProp(string property, object phaseLabels, string basis, ref object results) => throw new NotSupportedException();
    public void SetTwoPhaseProp(string property, object phaseLabels, string basis, object values) => throw new NotSupportedException();
}
