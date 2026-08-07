using System;
using MembraneCore;
using MembraneCore.Models;
using Xunit;
using Xunit.Abstractions;

namespace MembraneCore.Tests
{
    /// <summary>
    /// Validates the cross-flow model against the experimentally-grounded reference in Dias et al. (2020):
    /// Shindo's cross-flow results (Tables 4 &amp; 5), which Shindo validated against literature experimental
    /// data. The continuous cross-flow integration reproduces those to ~3–4 decimals.
    /// </summary>
    /// <remarks>
    /// NOTE (data correction): Table 1 prints Case-2 S_H2 = 4.80e-9, which implies ~34x selectivity and
    /// ~3x H2 enrichment — inconsistent with the reported 1.57x enrichment (permeate H2 0.4707, feed 0.30)
    /// and with microporous-glass Knudsen selectivity (H2/N2 ∝ √(28/2) ≈ 3.7). The self-consistent value is
    /// S_H2 = 4.80e-10; with it, this model reproduces Shindo's cross-flow to 4 decimals (0.4708 vs 0.4707).
    /// See docs/03-validation-and-findings.md.
    /// </remarks>
    public class CrossFlowModelTests
    {
        private readonly ITestOutputHelper _out;
        public CrossFlowModelTests(ITestOutputHelper output) { _out = output; }

        // Absolute-tolerance comparison (xunit's decimal-places rounding is brittle at rounding boundaries).
        private static void Close(double expected, double actual, double tol = 1.5e-3)
            => Assert.True(Math.Abs(actual - expected) <= tol,
                $"expected {expected} ± {tol}, got {actual} (Δ={Math.Abs(actual - expected):E2})");

        // ---- Case 1 (polyethylene, NH3/H2/N2), Shindo cross-flow (Table 4) ----
        // Feed 0.45/0.25/0.30 ; S = 2.63e-10/8.35e-11/1.72e-11 ; γ = 0.13
        // Shindo cross-flow: NH3 0.7338, H2 0.2035, N2 0.0627 ; θ = 0.3726
        [Fact]
        public void Case1_CrossFlow_MatchesShindo_Table4()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var r = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.3726);

            _out.WriteLine($"Case1 cross permeate: NH3={r.PermeateComposition[0]:F4} H2={r.PermeateComposition[1]:F4} N2={r.PermeateComposition[2]:F4}");
            Close(0.7338, r.PermeateComposition[0]);
            Close(0.2035, r.PermeateComposition[1]);
            Close(0.0627, r.PermeateComposition[2]);
            Assert.True(r.MassBalanceResidual < 1e-9);
        }

        // ---- Case 2 (microporous glass, H2/CH4/CO/N2/CO2), Shindo cross-flow (Table 5) ----
        // Feed 0.30/0.10/0.25/0.15/0.20 ; S = 4.80e-10(*)/1.91e-10/1.40e-10/1.38e-10/1.48e-10 ; γ = 0.10
        // Shindo cross-flow: H2 0.4707, CH4 0.0910, CO 0.1806, N2 0.1072, CO2 0.1505 ; θ = 0.4131
        // (*) corrected permeance — see class remarks.
        [Fact]
        public void Case2_CrossFlow_MatchesShindo_Table5()
        {
            var feed = new[] { 0.30, 0.10, 0.25, 0.15, 0.20 };
            var S = new[] { 4.80e-10, 1.91e-10, 1.40e-10, 1.38e-10, 1.48e-10 };
            var r = CrossFlowModel.SolveByStageCut(feed, S, 0.10, 0.4131);

            _out.WriteLine($"Case2 cross permeate: H2={r.PermeateComposition[0]:F4} CH4={r.PermeateComposition[1]:F4} CO={r.PermeateComposition[2]:F4} N2={r.PermeateComposition[3]:F4} CO2={r.PermeateComposition[4]:F4}");
            Close(0.4707, r.PermeateComposition[0]);
            Close(0.0910, r.PermeateComposition[1]);
            Close(0.1806, r.PermeateComposition[2]);
            Close(0.1072, r.PermeateComposition[3]);
            Close(0.1505, r.PermeateComposition[4]);
            Assert.True(r.MassBalanceResidual < 1e-9);
        }

        [Fact]
        public void MassBalance_ClosesForAllComponents()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var r = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.4);
            Assert.True(r.MassBalanceResidual < 1e-9);
            double ps = 0, rs = 0;
            for (int i = 0; i < feed.Length; i++) { ps += r.PermeateComposition[i]; rs += r.RetentateComposition[i]; }
            Assert.Equal(1.0, ps, 9);
            Assert.Equal(1.0, rs, 9);
        }

        [Fact]
        public void FastGas_EnrichedInPermeate_DepletedInRetentate()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var r = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.3);
            Assert.True(r.PermeateComposition[0] > feed[0]);
            Assert.True(r.RetentateComposition[0] < feed[0]);
        }

        [Fact]
        public void HigherStageCut_ReducesPermeatePurity()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            double lo = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.10).PermeateComposition[0];
            double hi = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.50).PermeateComposition[0];
            Assert.True(hi < lo);
        }

        [Fact]
        public void StageCutMarch_IsConvergedInStepCount()
        {
            var feed = new[] { 0.45, 0.25, 0.30 };
            var S = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            var coarse = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.3726, 500);
            var fine = CrossFlowModel.SolveByStageCut(feed, S, 0.13, 0.3726, 8000);
            for (int i = 0; i < feed.Length; i++)
                Assert.Equal(fine.PermeateComposition[i], coarse.PermeateComposition[i], 4);
        }

        [Fact]
        public void AreaMarch_ReproducesStageCutMarch_AtSameTheta()
        {
            // Area chosen to give a moderate stage cut (~0.3), then compare to the θ-march at that θ.
            var feed = new[] { 0.45, 0.25, 0.30 };
            var Ssi = new[] { 2.63e-10, 8.35e-11, 1.72e-11 };
            double Pr = 3.0e6, Pp = 0.13 * 3.0e6, Nf = 1.0, area = 300.0;

            var byArea = CrossFlowModel.SolveByArea(feed, Nf, Ssi, Pr, Pp, area, 40000);
            var byCut = CrossFlowModel.SolveByStageCut(feed, Ssi, Pp / Pr, byArea.StageCut, 8000);

            _out.WriteLine($"area θ={byArea.StageCut:F4}");
            Assert.InRange(byArea.StageCut, 0.05, 0.95);
            for (int i = 0; i < feed.Length; i++)
                Assert.Equal(byCut.PermeateComposition[i], byArea.PermeateComposition[i], 2);
        }
    }
}
