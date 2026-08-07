using System;
using MembraneCore;
using MembraneCore.Models;
using Xunit;

namespace MembraneCore.Tests
{
    /// <summary>Validates the position-dependent profile capture (100-point profiles for plotting).</summary>
    public class MembraneProfileTests
    {
        private static readonly double[] Feed = { 0.10, 0.90 };     // CO2 / CH4
        private static readonly double[] S = { 3.0e-8, 1.5e-9 };
        private const double Pr = 6.0e6, Pp = 2.0e5, Nf = 1.0, Area = 25.0;

        [Fact]
        public void CrossFlow_Profile_IsWellFormed_AndConsistentWithResult()
        {
            var r = CrossFlowModel.SolveByArea(Feed, Nf, S, Pr, Pp, Area, 4000, profilePoints: 100);
            var pr = r.Profile;
            Assert.NotNull(pr);
            Assert.Equal(100, pr!.Points);
            Assert.Equal(2, pr.Components);

            // Position runs 0 → 1, strictly increasing.
            Assert.Equal(0.0, pr.Position[0], 6);
            Assert.Equal(1.0, pr.Position[99], 6);
            for (int k = 1; k < pr.Points; k++)
                Assert.True(pr.Position[k] > pr.Position[k - 1], "position must increase");

            // Stage cut increases 0 → θ.
            Assert.True(pr.StageCut[0] < 1e-6);
            for (int k = 1; k < pr.Points; k++)
                Assert.True(pr.StageCut[k] >= pr.StageCut[k - 1] - 1e-9, "stage cut must be non-decreasing");
            Assert.Equal(r.StageCut, pr.StageCut[99], 2);

            // Retentate fractions sum to 1 at every point; fast gas (CO2) depletes along the module.
            for (int k = 0; k < pr.Points; k++)
                Assert.Equal(1.0, pr.Retentate[0][k] + pr.Retentate[1][k], 6);
            Assert.True(pr.Retentate[0][99] < pr.Retentate[0][0], "CO2 should deplete in retentate along position");

            // Endpoint retentate composition matches the scalar result.
            Assert.Equal(r.RetentateComposition[0], pr.Retentate[0][99], 2);
        }

        [Theory]
        [InlineData(FlowPattern.CoCurrent)]
        [InlineData(FlowPattern.CounterCurrent)]
        public void PlugFlow_Profile_IsWellFormed(FlowPattern pattern)
        {
            var r = PlugFlowModel.SolveByArea(Feed, Nf, S, Pr, Pp, pattern, Area, 400, profilePoints: 100);
            var pr = r.Profile;
            Assert.NotNull(pr);
            Assert.Equal(100, pr!.Points);
            Assert.Equal(0.0, pr.Position[0], 6);
            Assert.Equal(1.0, pr.Position[99], 6);
            for (int k = 0; k < pr.Points; k++)
            {
                Assert.Equal(1.0, pr.Retentate[0][k] + pr.Retentate[1][k], 6);
                Assert.True(pr.Permeate[0][k] >= -1e-9 && pr.Permeate[0][k] <= 1.0 + 1e-9);
            }
        }
    }
}
