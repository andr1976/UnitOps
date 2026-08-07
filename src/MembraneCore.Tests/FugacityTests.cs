using MembraneCore.Models;
using MembraneCore.Solvers;
using Xunit;

namespace MembraneCore.Tests
{
    /// <summary>
    /// Validates the real-gas fugacity driving force S_i(a_i x_i − γ b_i y_i): it must reduce to the
    /// ideal-gas result when the coefficients are unity, and a retentate fugacity coefficient below one
    /// (attractive real gas at high pressure) must lower the stage cut — the effect DeJaco et al. report,
    /// where the ideal-gas assumption over-predicts the stage cut.
    /// </summary>
    public class FugacityTests
    {
        private static readonly double[] Feed = { 0.10, 0.90 };   // CO2 / CH4
        private static readonly double[] Perm = { 3.0e-8, 1.5e-9 };
        private const double Pr = 60e5, Pp = 2e5, Flow = 1.0, Area = 25.0;

        [Fact]
        public void UnitCoefficients_ReproduceIdealGasExactly()
        {
            var ideal = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area);
            var unit = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area,
                a: new[] { 1.0, 1.0 }, b: new[] { 1.0, 1.0 });

            Assert.Equal(ideal.StageCut, unit.StageCut, 12);
            for (int i = 0; i < Feed.Length; i++)
                Assert.Equal(ideal.PermeateComposition[i], unit.PermeateComposition[i], 12);
        }

        [Fact]
        public void RetentateFugacityBelowOne_LowersStageCut()
        {
            var ideal = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area);
            // High-pressure real gas: φ < 1 on the retentate side reduces the effective driving force.
            var real = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area,
                a: new[] { 0.80, 0.95 }, b: new[] { 1.0, 1.0 });

            Assert.True(real.StageCut < ideal.StageCut,
                $"real-gas stage cut {real.StageCut} should be below ideal {ideal.StageCut}");
        }

        [Fact]
        public void LocalSolver_UnitCoefficients_MatchIdeal_AndSumToOne()
        {
            double[] x = { 0.10, 0.90 };
            double gamma = Pp / Pr;
            var yIdeal = LocalPermeateSolver.Solve(x, Perm, gamma);
            var yUnit = LocalPermeateSolver.Solve(x, Perm, gamma, new[] { 1.0, 1.0 }, new[] { 1.0, 1.0 });
            var yReal = LocalPermeateSolver.Solve(x, Perm, gamma, new[] { 0.8, 0.95 }, new[] { 1.0, 1.0 });

            for (int i = 0; i < x.Length; i++) Assert.Equal(yIdeal[i], yUnit[i], 12);
            double s = 0; foreach (var v in yReal) s += v;
            Assert.Equal(1.0, s, 9);
            // Suppressing the fast component's (CO2) fugacity relative to CH4 makes the permeate less CO2-rich.
            Assert.True(yReal[0] < yIdeal[0], "reduced CO2 retentate fugacity lowers permeate CO2");
        }
    }
}
