using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CAPEOPEN;
using EjectorCore;
using EjectorCore.Tests;
using Xunit.Abstractions;

namespace Ejector.CapeOpen.Tests;

/// <summary>
/// The CAPE-OPEN unit driven headless through <see cref="FlashMaterialObject"/>: Validate/Calculate,
/// the three modes, the outlet stream (mass and energy balance), error reporting and persistence.
/// With CoolProp behind the mock, the unit's result is compared with the engine run directly on
/// CoolProp, which checks the molar/phase-to-mass conversion of PmeFlash end to end.
/// </summary>
public class AdapterTests
{
    private readonly ITestOutputHelper _out;
    public AdapterTests(ITestOutputHelper output) { _out = output; }

    [DllImport("ole32.dll")]
    private static extern int CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, out IStream ppstm);

    private static ICapeParameter P(EjectorUnitOperation u, string name) =>
        (ICapeParameter)((ICapeCollection)u.parameters).Item(name);
    private static double V(EjectorUnitOperation u, string name) => Convert.ToDouble(P(u, name).value);

    private static EjectorUnitOperation Unit(object motive, object suction, object outlet, params (string, object)[] settings)
    {
        var u = new EjectorUnitOperation();
        u.Initialize();
        var ports = (ICapeCollection)u.ports;
        ((ICapeUnitPort)ports.Item("Motive")).Connect(motive);
        ((ICapeUnitPort)ports.Item("Suction")).Connect(suction);
        ((ICapeUnitPort)ports.Item("Outlet")).Connect(outlet);
        foreach (var (n, v) in settings)
        {
            P(u, n).value = v;
            u.ApplyModes();
        }
        string msg = "";
        Assert.True(u.Validate(ref msg), msg);
        return u;
    }

    // ------------------------------------------------------------ ideal gas (always runs)
    [Fact]
    public void IdealGas_Design_ClosesMassAndEnergyBalance()
    {
        var gas = new IdealGasFlash();
        var mo = FlashMaterialObject.Stream(gas, "N2", 10e5, 400.0, 10.0);
        var so = FlashMaterialObject.Stream(gas, "N2", 1.5e5, 300.0, 3.0);
        var outlet = new FlashMaterialObject(gas, "N2") { ExposeOverallEnthalpy = true };
        var u = Unit(mo, so, outlet);
        u.Calculate();
        double pOut = V(u, "OutletPressure");
        Assert.InRange(pOut, 1.5e5, 10e5);
        Assert.Equal(13.0, outlet.Get("totalFlow")[0], 12);                       // mol/s
        double h = (10.0 * gas.Cp * (400 - 298.15) + 3.0 * gas.Cp * (300 - 298.15)) * gas.M / 13.0;
        Assert.Equal(h, outlet.Get("enthalpy")[0], 6);                             // J/mol, adiabatic mixing
        Assert.Equal(pOut, outlet.Pressure, 6);
        Assert.Equal("Design", P(u, "Regime").value);
        // the engine on the same gas gives the same answer
        var ej = new EjectorCore.Ejector(gas) { EtaM = 0.9, EtaS = 0.85, EtaMix = 0.9, EtaD = 0.8 };
        var d = ej.Design(ej.InletPT(10e5, 400.0), ej.InletPT(1.5e5, 300.0), 10.0 * gas.M, 0.3);
        Assert.Equal(d.Pout, pOut, 3);
        Assert.Equal(Geometry.Diameter(d.At), V(u, "ThroatDiameter"), 9);
        _out.WriteLine(u.LastTextReport);
    }

    [Fact]
    public void IdealGas_RatingFixedShape_InvertsTheDesign()
    {
        var gas = new IdealGasFlash();
        var mo = FlashMaterialObject.Stream(gas, "N2", 10e5, 400.0, 10.0);
        var so = FlashMaterialObject.Stream(gas, "N2", 1.5e5, 300.0, 3.0);
        var u = Unit(mo, so, new FlashMaterialObject(gas, "N2"));
        u.Calculate();
        double rE = V(u, "AreaRatioExit"), rMix = V(u, "AreaRatioMixing");
        // the designed shape, rated at the same flows: a design point lies on (or near) the critical plateau
        var r = Unit(mo, so, new FlashMaterialObject(gas, "N2"), ("Mode", "Rating"), ("AreaRatioExit", rE), ("AreaRatioMixing", rMix));
        r.Calculate();
        Assert.Contains((string)P(r, "Regime").value, new[] { "Critical", "Subcritical" });
        Assert.True(V(r, "CriticalEntrainmentRatio") >= 0.3 * (1 - 1e-6));
        Assert.True(((object[])P(r, "Curve_OutletPressure").value).Length >= 2);
        _out.WriteLine($"design P_out={V(u, "OutletPressure"):F1}  rated P_out={V(r, "OutletPressure"):F1} ({P(r, "Regime").value})");
    }

    [Fact]
    public void Validate_RejectsUnconnectedPortsAndBadGeometry()
    {
        var u = new EjectorUnitOperation();
        string msg = "";
        Assert.False(u.Validate(ref msg));
        Assert.Contains("Motive", msg);
        var gas = new IdealGasFlash();
        var mo = FlashMaterialObject.Stream(gas, "N2", 10e5, 400.0, 10.0);
        var so = FlashMaterialObject.Stream(gas, "N2", 1.5e5, 300.0, 3.0);
        var ports = (ICapeCollection)u.ports;
        ((ICapeUnitPort)ports.Item("Motive")).Connect(mo);
        ((ICapeUnitPort)ports.Item("Suction")).Connect(so);
        ((ICapeUnitPort)ports.Item("Outlet")).Connect(new FlashMaterialObject(gas, "N2"));
        P(u, "Mode").value = "Rating";
        u.ApplyModes();
        P(u, "AreaRatioExit").value = 5.0;
        P(u, "AreaRatioMixing").value = 4.0;
        Assert.False(u.Validate(ref msg));
        Assert.Contains("AreaRatio", msg);
    }

    [Fact]
    public void ParameterDirections_FollowModeGeometryAndPreset()
    {
        var u = new EjectorUnitOperation();
        Assert.Equal(CapeParamMode.CAPE_OUTPUT, P(u, "ThroatDiameter").Mode);          // Design: geometry computed
        Assert.Equal(CapeParamMode.CAPE_OUTPUT, P(u, "MotiveEfficiency").Mode);        // preset Screening
        P(u, "Mode").value = "Rating";
        P(u, "GeometrySpec").value = "FixedSize";
        P(u, "EfficiencySet").value = "Custom";
        u.ApplyModes();
        Assert.Equal(CapeParamMode.CAPE_INPUT, P(u, "ThroatDiameter").Mode);
        Assert.Equal(CapeParamMode.CAPE_OUTPUT, P(u, "AreaRatioExit").Mode);
        Assert.Equal(CapeParamMode.CAPE_INPUT, P(u, "MotiveEfficiency").Mode);
        P(u, "EfficiencySet").value = "R744_AllPoints";
        u.ApplyModes();
        Assert.Equal(1.138, V(u, "DischargeCoefficient"), 12);
    }

    [Fact]
    public void Persistence_RoundTripsSettingsAndResults()
    {
        var gas = new IdealGasFlash();
        var u = Unit(FlashMaterialObject.Stream(gas, "N2", 10e5, 400.0, 10.0), FlashMaterialObject.Stream(gas, "N2", 1.5e5, 300.0, 3.0),
                     new FlashMaterialObject(gas, "N2"), ("EfficiencySet", "Custom"), ("MixingEfficiency", 0.77), ("CurvePoints", 5.0));
        u.Calculate();
        Assert.Equal(0, CreateStreamOnHGlobal(IntPtr.Zero, true, out var stm));
        u.Save(stm, true);
        stm.Seek(0, 0, IntPtr.Zero);
        var v = new EjectorUnitOperation();
        v.Load(stm);
        Assert.Equal("Custom", P(v, "EfficiencySet").value);
        Assert.Equal(0.77, V(v, "MixingEfficiency"), 12);
        Assert.Equal(V(u, "OutletPressure"), V(v, "OutletPressure"), 9);
        Assert.Equal(((object[])P(u, "Curve_EntrainmentRatio").value).Length, ((object[])P(v, "Curve_EntrainmentRatio").value).Length);
        Assert.Equal(CapeParamMode.CAPE_INPUT, P(v, "MixingEfficiency").Mode);
    }

    // ------------------------------------------------------------ CO2 with CoolProp behind the mock
    private static (EjectorCore.Ejector Ej, FluidState M0, FluidState S0) Direct(CoolPropFlash cp, double etaM, double etaS,
        double etaMix, double etaD, double cd, Func<EjectorCore.Ejector, FluidState> m0, Func<EjectorCore.Ejector, FluidState> s0)
    {
        var ej = new EjectorCore.Ejector(cp) { EtaM = etaM, EtaS = etaS, EtaMix = etaMix, EtaD = etaD, CdM = cd };
        return (ej, m0(ej), s0(ej));
    }

    [CoolPropFact]
    public void CO2_DutyDesign_SaturatedInlets_MatchesEngineOnCoolProp()
    {
        using var cp = new CoolPropFlash("CO2");
        double M = cp.MolarMass(null);
        // the user's duty: 13.66 kg/s saturated liquid at 47 bar, 2 kg/s saturated vapour at 15 bar
        var mo = FlashMaterialObject.Stream(cp, "CO2", 47e5, 0, 13.66 / M, q: 0.0);
        var so = FlashMaterialObject.Stream(cp, "CO2", 15e5, 0, 2.0 / M, q: 1.0);
        var outlet = new FlashMaterialObject(cp, "CO2");
        var u = Unit(mo, so, outlet);
        u.Calculate();
        var (ej, m0, s0) = Direct(cp, 0.9, 0.85, 0.9, 0.8, 1.0, e => e.InletPQ(47e5, 0.0), e => e.InletPQ(15e5, 1.0));
        var d = ej.Design(m0, s0, 13.66, 2.0 / 13.66);
        _out.WriteLine($"unit P_out={V(u, "OutletPressure") / 1e5:F5} bar  engine P_out={d.Pout / 1e5:F5} bar  flashes={V(u, "FlashCount")}");
        _out.WriteLine(u.LastTextReport);
        Assert.Equal(1.0, V(u, "OutletPressure") / d.Pout, 5);
        Assert.Equal(1.0, V(u, "ThroatDiameter") / Geometry.Diameter(d.At), 5);
        Assert.Equal(d.Out!.Q, V(u, "OutletVapourFraction"), 4);
        Assert.True(V(u, "SolidMargin") > 2.0);
        // outlet stream: PH flash at the engine's pressure and the mixed enthalpy -> two-phase, ~d.Out.Q vapour
        Assert.Equal(2, outlet.Phases.Count);
        Assert.Equal(d.Out.Q, outlet.Phases.First(p => p.Label == "Vapor").Beta, 4);
    }

    [CoolPropFact]
    public void CO2_RatingFixedSize_ReproducesForwardRatingOfGiacomelliCase1()
    {
        using var cp = new CoolPropFlash("CO2");
        double M = cp.MolarMass(null);
        const double At = 6.3617e-7;             // 0.9 mm
        double dT = 0.9e-3, dE = 1.03e-3, dMix = 2.0e-3;
        var (ej, m0, s0) = Direct(cp, 0.90, 0.61, 0.825, 0.82, 1.138, e => e.InletPT(104.1e5, 310.05), e => e.InletPT(30.83e5, 283.65));
        var f = ej.Rate(m0, s0, Geometry.Area(dT), Geometry.Area(dE), Geometry.Area(dMix), 44e5);
        Assert.Equal(Regime.Subcritical, f.Regime);
        foreach (double scale in new[] { 1.0, 1.1 })
        {
            var mo = FlashMaterialObject.Stream(cp, "CO2", 104.1e5, 310.05, scale * f.Mm / M);
            var so = FlashMaterialObject.Stream(cp, "CO2", 30.83e5, 283.65, scale * f.Ms / M);
            var u = Unit(mo, so, new FlashMaterialObject(cp, "CO2"), ("Mode", "Rating"), ("GeometrySpec", "FixedSize"),
                         ("ThroatDiameter", dT), ("ExitDiameter", dE), ("MixingDiameter", dMix), ("EfficiencySet", "R744_AllPoints"));
            u.Calculate();
            _out.WriteLine($"scale {scale}: P_out={V(u, "OutletPressure") / 1e5:F5} bar mismatch={V(u, "MotiveFlowMismatch"):+0.000000;-0.000000} " +
                           $"ER*={V(u, "CriticalEntrainmentRatio"):F5}");
            Assert.Equal(1.0, V(u, "OutletPressure") / 44e5, 5);              // scale invariance: same ER, same P_out
            Assert.Equal(1.0 / scale - 1.0, V(u, "MotiveFlowMismatch"), 4);
            Assert.Equal("Subcritical", P(u, "Regime").value);
        }
        _ = At;
    }

    [CoolPropFact]
    public void CO2_ExcessSuction_RaisesClearError()
    {
        using var cp = new CoolPropFlash("CO2");
        double M = cp.MolarMass(null);
        var mo = FlashMaterialObject.Stream(cp, "CO2", 104.1e5, 310.05, 0.0484 / M);
        var so = FlashMaterialObject.Stream(cp, "CO2", 30.83e5, 283.65, 0.0484 * 0.25 / M);   // ER 0.25 > ER* ~ 0.20
        var u = Unit(mo, so, new FlashMaterialObject(cp, "CO2"), ("Mode", "Rating"), ("AreaRatioExit", 1.31),
                     ("AreaRatioMixing", 4.938), ("EfficiencySet", "R744_AllPoints"));
        var ex = Assert.ThrowsAny<Exception>(() => u.Calculate());
        _out.WriteLine(ex.Message);
        Assert.Contains("critical ER", ex.Message);
        Assert.Contains("critical ER", ((ECapeUser)u).description);   // what DWSIM reads after a failed Calculate
    }

    [CoolPropFact]
    public void CO2_PackageWithoutPSFlash_FallsBackToTemperatureSearch()
    {
        using var cp = new CoolPropFlash("CO2");
        double M = cp.MolarMass(null);
        var mo = FlashMaterialObject.Stream(cp, "CO2", 84.5e5, 305.15, 13.66 / M);
        var so = FlashMaterialObject.Stream(cp, "CO2", 15e5, 0, 2.0 / M, q: 1.0);
        mo.RejectSpecs.Add("entropy");                                             // inherited by the scratch material
        var u = Unit(mo, so, new FlashMaterialObject(cp, "CO2"), ("EfficiencySet", "R744_AllPoints"));
        u.Calculate();
        var (ej, m0, s0) = Direct(cp, 0.90, 0.610, 0.825, 0.820, 1.138, e => e.InletPT(84.5e5, 305.15), e => e.InletPQ(15e5, 1.0));
        var d = ej.Design(m0, s0, 13.66, 2.0 / 13.66);
        _out.WriteLine($"unit (PS by T-search) P_out={V(u, "OutletPressure") / 1e5:F5} bar  engine {d.Pout / 1e5:F5} bar  flashes={V(u, "FlashCount")}");
        Assert.Equal(1.0, V(u, "OutletPressure") / d.Pout, 3);
    }
}
