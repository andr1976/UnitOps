using MembraneCore.Fugacity;
using MembraneCore.Models;
using Xunit;

namespace MembraneCore.Tests
{
    /// <summary>
    /// Validates the position-dependent (local-φ) driving force: the <see cref="FugacityTable"/> interpolation,
    /// its reduction to the constant-φ result when the table is flat, and that a φ table which rises from a
    /// suppressed feed value toward unity downstream yields a stage cut bracketed by the constant-feed-φ and
    /// ideal-gas results.
    /// </summary>
    public class FugacityLocalTests
    {
        private static readonly double[] Feed = { 0.10, 0.90 };   // CO2 / CH4
        private static readonly double[] Perm = { 3.0e-8, 1.5e-9 };
        private const double Pr = 60e5, Pp = 2e5, Flow = 1.0, Area = 25.0;

        [Fact]
        public void Table_InterpolatesAndClamps()
        {
            var t = new FugacityTable(
                new[] { 0.0, 0.5, 1.0 },
                new[] { new[] { 0.80 }, new[] { 0.90 }, new[] { 1.00 } },
                new[] { new[] { 1.00 }, new[] { 1.00 }, new[] { 1.00 } });
            var a = new double[1]; var b = new double[1];
            t.At(0.0, a, b); Assert.Equal(0.80, a[0], 12);
            t.At(0.25, a, b); Assert.Equal(0.85, a[0], 12);   // linear midpoint
            t.At(0.5, a, b); Assert.Equal(0.90, a[0], 12);
            t.At(2.0, a, b); Assert.Equal(1.00, a[0], 12);    // clamp above
            t.At(-1.0, a, b); Assert.Equal(0.80, a[0], 12);   // clamp below
        }

        [Fact]
        public void FlatTable_MatchesConstantCoefficients()
        {
            var constResult = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area,
                a: new[] { 0.85, 0.90 }, b: new[] { 1.0, 1.0 });
            var flat = new FugacityTable(
                new[] { 0.0, 1.0 },
                new[] { new[] { 0.85, 0.90 }, new[] { 0.85, 0.90 } },
                new[] { new[] { 1.0, 1.0 }, new[] { 1.0, 1.0 } });
            var tableResult = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area, phiTable: flat);

            Assert.Equal(constResult.StageCut, tableResult.StageCut, 9);
            Assert.Equal(constResult.PermeateComposition[0], tableResult.PermeateComposition[0], 9);
        }

        [Fact]
        public void RisingTable_StageCutBetweenConstantFeedAndIdeal()
        {
            var ideal = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area);                    // φ = 1
            var constFeed = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area,
                a: new[] { 0.80, 0.90 }, b: new[] { 1.0, 1.0 });                                       // φ = feed everywhere
            // φ suppressed at the feed end, recovering toward unity as the retentate is stripped.
            var rising = new FugacityTable(
                new[] { 0.0, 1.0 },
                new[] { new[] { 0.80, 0.90 }, new[] { 1.0, 1.0 } },
                new[] { new[] { 1.0, 1.0 }, new[] { 1.0, 1.0 } });
            var local = CrossFlowModel.SolveByArea(Feed, Flow, Perm, Pr, Pp, Area, phiTable: rising);

            Assert.True(constFeed.StageCut < local.StageCut,
                $"local {local.StageCut} should exceed constant-feed {constFeed.StageCut}");
            Assert.True(local.StageCut < ideal.StageCut,
                $"local {local.StageCut} should be below ideal {ideal.StageCut}");
        }
    }
}
