using EjectorCore;

namespace EjectorCore.Tests;

/// <summary>Verification of the engine on analytic thermodynamics (always run, no CoolProp).</summary>
public class EngineTests
{
    private static Ejector Gas(out FluidState m0, out FluidState s0, double etaAll = 0.9)
    {
        var ej = new Ejector(new IdealGasFlash()) { EtaM = etaAll, EtaS = etaAll, EtaMix = etaAll, EtaD = etaAll };
        m0 = ej.InletPT(10e5, 400.0);
        s0 = ej.InletPT(1.5e5, 300.0);
        return ej;
    }

    [Fact]
    public void Brentq_FindsRoot_AndRejectsBadBracket()
    {
        double r = Numerics.Brentq(x => x * x - 2.0, 0.0, 2.0, 1e-14);
        Assert.Equal(Math.Sqrt(2.0), r, 12);
        Assert.Throws<RootBracketException>(() => Numerics.Brentq(x => x * x + 1.0, -1.0, 1.0, 1e-12));
    }

    [Fact]
    public void Fminbound_FindsMinimum()
    {
        var (x, f) = Numerics.Fminbound(v => (v - 1.3) * (v - 1.3) + 0.5, 0.0, 4.0, 1e-8);
        Assert.Equal(1.3, x, 6);
        Assert.Equal(0.5, f, 10);
    }

    [Fact]
    public void Geomspace_And_Linspace_HaveExactEndpoints()
    {
        var g = Numerics.Geomspace(2.0, 2000.0, 7);
        Assert.Equal(2.0, g[0]);
        Assert.Equal(2000.0, g[6]);
        Assert.Equal(20.0, g[2], 10);
        var l = Numerics.Linspace(1.0, 2.0, 5);
        Assert.Equal(new[] { 1.0, 1.25, 1.5, 1.75, 2.0 }, l);
    }

    [Fact]
    public void IdealGasNozzle_CriticalPressureRatio()
    {
        // isentropic ideal gas, k = 1.4: P*/P0 = (2/(k+1))^(k/(k-1)) = 0.52828, and
        // G* = P0 sqrt(k/(R T0)) (2/(k+1))^((k+1)/(2(k-1)))
        var gas = new IdealGasFlash();
        var ej = new Ejector(gas) { EtaM = 0.7 };          // eta_m must not affect the throat
        var m0 = ej.InletPT(10e5, 300.0);
        var (pt, g) = ej.MotiveThroat(m0, 1e5);
        Assert.Equal(Math.Pow(2.0 / 2.4, 1.4 / 0.4), pt / m0.P, 4);
        double gExact = 10e5 * Math.Sqrt(1.4 / (gas.R * 300.0)) * Math.Pow(2.0 / 2.4, 2.4 / 0.8);
        Assert.Equal(1.0, g / gExact, 8);
        ej.CdM = 1.1;
        Assert.Equal(1.1, ej.MotiveThroat(m0, 1e5).Gstar / g, 12);
    }

    [Fact]
    public void Design_ConservesEnergy_AndRatingRecoversDesignER()
    {
        var ej = Gas(out var m0, out var s0);
        var d = ej.Design(m0, s0, 1.0, 0.3);
        Assert.Equal((m0.H + 0.3 * s0.H) / 1.3, d.Out!.H, 6);
        Assert.True(d.Pout > s0.P && d.Pout < m0.P);
        var r = ej.Rate(m0, s0, d.At, d.Ae, d.Amix, d.Pout);
        Assert.InRange(r.ER / 0.3, 0.98, 1.02);
    }

    [Theory]
    [InlineData(1.6e5)]
    [InlineData(2.4e5)]
    [InlineData(2.9e5)]
    public void RateFlows_InvertsRate(double pOut)
    {
        var ej = Gas(out var m0, out var s0);
        var d = ej.Design(m0, s0, 1.0, 0.3);
        var f = ej.Rate(m0, s0, d.At, d.Ae, d.Amix, pOut);
        if (f.Regime == Regime.Backflow) return;
        var r = ej.RateFlows(m0, s0, f.Mm, f.Ms, d.Ae / d.At, d.Amix / d.At, d.At);
        Assert.Equal(f.Regime, r.Regime);
        Assert.Equal(1.0, r.At / d.At, 9);
        Assert.True(Math.Abs(r.MotiveMismatch) < 1e-9);
        if (f.Regime == Regime.Subcritical) Assert.Equal(1.0, r.Pout / pOut, 6);
        else Assert.Equal(r.PbCritical, r.Pout);
    }

    [Fact]
    public void RateFlows_ScalesWithMotiveFlow_AndReportsMismatch()
    {
        var ej = Gas(out var m0, out var s0);
        var d = ej.Design(m0, s0, 1.0, 0.3);
        var a = ej.RateFlows(m0, s0, 1.0, 0.25, d.Ae / d.At, d.Amix / d.At);
        var b = ej.RateFlows(m0, s0, 2.0, 0.50, d.Ae / d.At, d.Amix / d.At, d.At);
        Assert.Equal(a.Pout, b.Pout, 3);                    // scale invariance: same ER, same P_out
        Assert.Equal(2.0, b.At / a.At, 9);
        Assert.Equal(-0.5, b.MotiveMismatch, 6);           // given throat passes half the port flow
        Assert.Throws<EntrainmentExceededException>(() =>
            ej.RateFlows(m0, s0, 1.0, 1.05 * a.ERCritical, d.Ae / d.At, d.Amix / d.At));
    }

    [Fact]
    public void DesignOptimum_IsAMaximumOverPy()
    {
        var ej = Gas(out var m0, out var s0);
        var opt = ej.Design(m0, s0, 1.0, 0.3);
        foreach (double f in new[] { 0.97, 1.03 })
        {
            double py = opt.Py * f;
            if (py >= s0.P) continue;
            var other = ej.Design(m0, s0, 1.0, 0.3, py);
            Assert.True(other.Pout <= opt.Pout + 1e-3);
        }
    }

    [Fact]
    public void DesignForLift_ReachesRequiredPressure()
    {
        var ej = Gas(out var m0, out var s0);
        var d = ej.DesignForLift(m0, s0, 1.0, 2.5e5);
        Assert.InRange(d.Pout, 2.5e5 - 10.0, 2.5e5 + 10.0);   // brentq xtol on ER
    }
}
