using System.Text.Json;
using EjectorCore;
using Xunit.Abstractions;

namespace EjectorCore.Tests;

/// <summary>
/// The C# engine against the Python reference (ejector/prototype/ejector1d.py) on
/// identical thermodynamics (same CoolProp build). Fixtures:
/// regression_python.json, written by ejector/prototype/export_regression.py.
/// </summary>
public class PythonRegressionTests
{
    private readonly ITestOutputHelper _out;
    public PythonRegressionTests(ITestOutputHelper output) { _out = output; }

    private static JsonElement Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "regression_python.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    public static IEnumerable<object[]> CaseNames()
    {
        foreach (var c in Load().GetProperty("cases").EnumerateArray())
            yield return new object[] { c.GetProperty("name").GetString()! };
    }

    private static double? Num(JsonElement e, string key) =>
        e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static FluidState MakeInlet(Ejector ej, JsonElement d)
    {
        double P = d.GetProperty("P").GetDouble(), X = d.GetProperty("X").GetDouble();
        return d.GetProperty("kind").GetString() == "PT" ? ej.InletPT(P, X) : ej.InletPQ(P, X);
    }

    private static Ejector MakeEjector(IFlash flash, JsonElement eff)
    {
        var ej = new Ejector(flash);
        foreach (var p in eff.EnumerateObject())
        {
            switch (p.Name)
            {
                case "eta_m": ej.EtaM = p.Value.GetDouble(); break;
                case "eta_s": ej.EtaS = p.Value.GetDouble(); break;
                case "eta_mix": ej.EtaMix = p.Value.GetDouble(); break;
                case "eta_d": ej.EtaD = p.Value.GetDouble(); break;
                case "Cd_m": ej.CdM = p.Value.GetDouble(); break;
                case "phi_ne": ej.PhiNe = p.Value.GetDouble(); break;
                case "motive_model": ej.MotiveModel = Enum.Parse<MotiveModel>(p.Value.GetString()!); break;
                default: throw new InvalidOperationException("unknown efficiency key " + p.Name);
            }
        }
        return ej;
    }

    private void Close(string name, string what, double? expected, double actual, double rel, double abs = 0)
    {
        if (expected == null) return;
        double e = expected.Value;
        double tol = Math.Max(rel * Math.Abs(e), abs);
        _out.WriteLine($"{name} {what}: py={e:G12} cs={actual:G12} rel={(e == 0 ? actual : (actual - e) / e):E2}");
        Assert.True(Math.Abs(actual - e) <= tol, $"{name}.{what}: expected {e:G12}, got {actual:G12} (tol {tol:G3})");
    }

    [CoolPropTheory]
    [MemberData(nameof(CaseNames))]
    public void MatchesPythonReference(string caseName)
    {
        var cs = Load().GetProperty("cases").EnumerateArray().First(c => c.GetProperty("name").GetString() == caseName);
        string fluid = cs.GetProperty("motive").GetProperty("fluid").GetString()!;
        using var flash = new CoolPropFlash(fluid);
        var ej = MakeEjector(flash, cs.GetProperty("eff"));
        var m0 = MakeInlet(ej, cs.GetProperty("motive"));
        var s0 = MakeInlet(ej, cs.GetProperty("suction"));
        var ins = cs.GetProperty("inlet_states");
        Close(caseName, "m0.h", Num(ins.GetProperty("m0"), "h"), m0.H, 1e-12, 1e-9);
        Close(caseName, "s0.s", Num(ins.GetProperty("s0"), "s"), s0.S, 1e-12, 1e-9);

        EjectorResult Run() => cs.GetProperty("op").GetString() switch
        {
            "rate" => ej.Rate(m0, s0, Num(cs, "A_t")!.Value, Num(cs, "A_e")!.Value, Num(cs, "A_mix")!.Value, Num(cs, "P_out")!.Value),
            "rate_flows" => ej.RateFlows(m0, s0, Num(cs, "m_m")!.Value, Num(cs, "m_s")!.Value, Num(cs, "r_e")!.Value,
                                         Num(cs, "r_mix")!.Value, Num(cs, "A_t"), nCurve: cs.GetProperty("n_curve").GetInt32()),
            "design" => ej.Design(m0, s0, Num(cs, "m_m")!.Value, Num(cs, "ER")!.Value),
            "design_for_lift" => ej.DesignForLift(m0, s0, Num(cs, "m_m")!.Value, Num(cs, "P_out")!.Value),
            _ => throw new InvalidOperationException(),
        };

        var x = cs.GetProperty("result");
        if (x.TryGetProperty("error", out var err))
        {
            // the Python reference fails here on purpose: the C# engine must fail the same way
            var ex = Assert.ThrowsAny<Exception>(Run);
            _out.WriteLine($"{caseName}: expected failure '{err.GetString()}': {ex.Message}");
            switch (err.GetString())
            {
                case "solid": Assert.IsType<SolidPhaseRiskException>(ex); break;
                case "discontinuity": Assert.IsType<EjectorException>(ex); Assert.Contains("discontinuity", ex.Message); break;
                case "exit_below_cd": Assert.IsType<ArgumentException>(ex); Assert.Contains("below the discharge coefficient", ex.Message); break;
                default: throw new InvalidOperationException("unknown expected error " + err.GetString());
            }
            return;
        }
        var r = Run();
        _out.WriteLine($"{caseName}: {r}  flashes={r.FlashCount}");
        Assert.Equal(x.GetProperty("regime").GetString(), r.Regime.ToString().ToLowerInvariant());
        const double tp = 2e-6, tf = 2e-5;      // pressures / flows, areas, velocities (relative)
        Close(caseName, "P_out", Num(x, "P_out"), r.Pout, tp);
        Close(caseName, "P_y", Num(x, "P_y"), r.Py, tp);
        Close(caseName, "P_t", Num(x, "P_t"), r.Pt, tp);
        Close(caseName, "P_e", Num(x, "P_e"), r.Pe, tp);
        Close(caseName, "P_3", Num(x, "P_3"), r.P3, tp);
        Close(caseName, "m_m", Num(x, "m_m"), r.Mm, tf);
        Close(caseName, "m_s", Num(x, "m_s"), r.Ms, tf, 1e-9);
        Close(caseName, "ER", Num(x, "ER"), r.ER, tf, 1e-9);
        Close(caseName, "A_t", Num(x, "A_t"), r.At, tf);
        Close(caseName, "A_e", Num(x, "A_e"), r.Ae, tf);
        Close(caseName, "A_mix", Num(x, "A_mix"), r.Amix, tf);
        Close(caseName, "u_e", Num(x, "u_e"), r.Ue, tf);
        Close(caseName, "u_3", Num(x, "u_3"), r.U3, tf, 1e-6);
        Close(caseName, "h_out", Num(x, "h_out"), r.Out!.H, 1e-9, 1e-6);
        Close(caseName, "eta_ejector", Num(x, "eta_ejector"), r.EtaEjector, tf, 1e-7);
        Close(caseName, "ER_critical", Num(x, "ER_critical"), r.ERCritical, tf);
        Close(caseName, "P_b_critical", Num(x, "P_b_critical"), r.PbCritical, tp);
        Close(caseName, "motive_mismatch", Num(x, "motive_mismatch"), r.MotiveMismatch, 0, 1e-6);
        var curve = x.GetProperty("curve").EnumerateArray().ToList();
        Assert.Equal(curve.Count, r.Curve.Count);
        for (int i = 0; i < curve.Count; i++)
        {
            Close(caseName, $"curve[{i}].P", curve[i][0].GetDouble(), r.Curve[i].Pout, tp);
            Close(caseName, $"curve[{i}].ER", curve[i][1].GetDouble(), r.Curve[i].ER, tf, 1e-9);
        }
    }
}
