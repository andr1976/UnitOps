using System;
using MembraneCore;
using MembraneCore.Models;
using Xunit;
using Xunit.Abstractions;

namespace MembraneCore.Tests
{
    /// <summary>
    /// Validates the co-/counter-current plug-flow models. Counter-current has exact literature targets
    /// (Dias et al. 2020, Shindo columns of Tables 4 &amp; 5). Co-current has no tabulated target, so it is
    /// checked by invariants and the physical separation ordering counter ≥ cross ≥ co (at equal stage cut).
    /// Case-2 uses the corrected S_H2 = 4.80e-10 (see CrossFlowModelTests remarks).
    /// </summary>
    public class PlugFlowModelTests
    {
        private readonly ITestOutputHelper _out;
        public PlugFlowModelTests(ITestOutputHelper o) { _out = o; }

        private static void Close(double expected, double actual, double tol, string what)
            => Assert.True(Math.Abs(actual - expected) <= tol,
                $"{what}: expected {expected} ± {tol}, got {actual} (Δ={Math.Abs(actual - expected):E2})");

        // Shindo counter-current, Case 1 (Table 4): NH3 0.7368, H2 0.2010, N2 0.0622 @ θ=0.3745
        [Fact]
        public void Case1_CounterCurrent_MatchesShindo_Table4()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var r = PlugFlowModel.SolveByStageCut(feed, S, 0.13, FlowPattern.CounterCurrent, 0.3745);
            _out.WriteLine($"C1 counter permeate: NH3={r.PermeateComposition[0]:F4} H2={r.PermeateComposition[1]:F4} N2={r.PermeateComposition[2]:F4}");
            Close(0.7368, r.PermeateComposition[0], 0.01, "NH3");
            Close(0.2010, r.PermeateComposition[1], 0.01, "H2");
            Close(0.0622, r.PermeateComposition[2], 0.01, "N2");
            Assert.True(r.MassBalanceResidual < 1e-9);
        }

        // Shindo counter-current, Case 2 (Table 5): H2 0.4742, CH4 0.0909, CO 0.1793, N2 0.1065, CO2 0.1495 @ θ=0.4146
        [Fact]
        public void Case2_CounterCurrent_MatchesShindo_Table5()
        {
            var feed = new[] { 0.30, 0.10, 0.25, 0.15, 0.20 };
            var S = new[] { 4.80e-10, 1.91e-10, 1.40e-10, 1.38e-10, 1.48e-10 };
            var r = PlugFlowModel.SolveByStageCut(feed, S, 0.10, FlowPattern.CounterCurrent, 0.4146);
            _out.WriteLine($"C2 counter permeate: H2={r.PermeateComposition[0]:F4} CH4={r.PermeateComposition[1]:F4} CO={r.PermeateComposition[2]:F4} N2={r.PermeateComposition[3]:F4} CO2={r.PermeateComposition[4]:F4}");
            Close(0.4742, r.PermeateComposition[0], 0.01, "H2");
            Close(0.0909, r.PermeateComposition[1], 0.01, "CH4");
            Close(0.1793, r.PermeateComposition[2], 0.01, "CO");
            Close(0.1065, r.PermeateComposition[3], 0.01, "N2");
            Close(0.1495, r.PermeateComposition[4], 0.01, "CO2");
            Assert.True(r.MassBalanceResidual < 1e-9);
        }

        [Fact]
        public void SeparationOrdering_Counter_ge_Cross_ge_Co()
        {
            // Fast-component permeate purity at equal stage cut: counter-current ≥ cross ≥ co-current.
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            double theta = 0.30;
            double co = PlugFlowModel.SolveByStageCut(feed, S, 0.13, FlowPattern.CoCurrent, theta).PermeateComposition[0];
            double cross = CrossFlowModel.SolveByStageCut(feed, S, 0.13, theta).PermeateComposition[0];
            double counter = PlugFlowModel.SolveByStageCut(feed, S, 0.13, FlowPattern.CounterCurrent, theta).PermeateComposition[0];
            _out.WriteLine($"NH3 permeate @θ0.30: co={co:F4} cross={cross:F4} counter={counter:F4}");
            Assert.True(counter >= cross - 1e-3, $"counter {counter} !>= cross {cross}");
            Assert.True(cross >= co - 1e-3, $"cross {cross} !>= co {co}");
        }

        [Theory]
        [InlineData(FlowPattern.CoCurrent)]
        [InlineData(FlowPattern.CounterCurrent)]
        public void MassBalanceCloses_AndFractionsSumToOne(FlowPattern pattern)
        {
            var feed = new[] { 0.30, 0.10, 0.25, 0.15, 0.20 };
            var S = new[] { 4.80e-10, 1.91e-10, 1.40e-10, 1.38e-10, 1.48e-10 };
            var r = PlugFlowModel.SolveByStageCut(feed, S, 0.10, pattern, 0.35);
            Assert.True(r.MassBalanceResidual < 1e-9);
            double ps = 0, rs = 0;
            for (int i = 0; i < feed.Length; i++) { ps += r.PermeateComposition[i]; rs += r.RetentateComposition[i]; }
            Assert.Equal(1.0, ps, 9);
            Assert.Equal(1.0, rs, 9);
        }

        [Theory]
        [InlineData(FlowPattern.CoCurrent)]
        [InlineData(FlowPattern.CounterCurrent)]
        public void FastGasEnriched_InPermeate(FlowPattern pattern)
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var r = PlugFlowModel.SolveByStageCut(feed, S, 0.13, pattern, 0.3);
            Assert.True(r.PermeateComposition[0] > feed[0], "fast gas enriched in permeate");
            Assert.True(r.RetentateComposition[0] < feed[0], "fast gas depleted in retentate");
        }

        [Fact]
        public void CellCount_Converged()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var coarse = PlugFlowModel.SolveByStageCut(feed, S, 0.13, FlowPattern.CounterCurrent, 0.3745, 150);
            var fine = PlugFlowModel.SolveByStageCut(feed, S, 0.13, FlowPattern.CounterCurrent, 0.3745, 800);
            for (int i = 0; i < feed.Length; i++)
                Close(fine.PermeateComposition[i], coarse.PermeateComposition[i], 5e-3, $"cell-convergence[{i}]");
        }
    }
}
