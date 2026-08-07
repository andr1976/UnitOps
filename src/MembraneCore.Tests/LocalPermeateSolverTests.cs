using System;
using MembraneCore.Solvers;
using Xunit;

namespace MembraneCore.Tests
{
    public class LocalPermeateSolverTests
    {
        private const double Tol = 1e-9;

        /// <summary>Fractions must always sum to 1 and be non-negative.</summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(0.02)]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void PermeateFractions_SumToOne_AndNonNegative(double gamma)
        {
            var x = new[] { 0.5, 0.105, 0.395 };
            var S = new[] { 204.2e-10, 60.2e-10, 13.1e-10 }; // Sada CO2/O2/N2
            var y = LocalPermeateSolver.Solve(x, S, gamma);

            double sum = 0.0;
            foreach (var yi in y) { Assert.True(yi >= 0.0, "fraction must be non-negative"); sum += yi; }
            Assert.Equal(1.0, sum, 12);
        }

        /// <summary>
        /// The returned composition must satisfy the defining flux-ratio relation
        /// y_k = J_k / Σ_j J_j with J_k = S_k (x_k − y_k·γ). This is the exact physics the solver targets.
        /// </summary>
        [Theory]
        [InlineData(0.04)]
        [InlineData(0.13)]
        [InlineData(0.6)]
        public void Composition_SatisfiesFluxRatioRelation(double gamma)
        {
            var x = new[] { 0.18, 0.64, 0.09, 0.06, 0.03 };
            var S = new[] { 2.98e-8, 2.27e-10, 4.29e-10, 4.53e-10, 1.92e-8 }; // Dias case 3 (CO2/CH4/C2/C3/C4+)
            var y = LocalPermeateSolver.Solve(x, S, gamma);

            double jtot = 0.0;
            var j = new double[x.Length];
            for (int k = 0; k < x.Length; k++)
            {
                j[k] = S[k] * (x[k] - y[k] * gamma); // P_r = 1 scaling
                Assert.True(j[k] >= -1e-14, "each local flux must be non-negative");
                jtot += j[k];
            }
            for (int k = 0; k < x.Length; k++)
                Assert.Equal(j[k] / jtot, y[k], 9);
        }

        /// <summary>γ = 0 has the closed form y_k = S_k x_k / Σ_j S_j x_j.</summary>
        [Fact]
        public void ZeroPressureRatio_MatchesClosedForm()
        {
            var x = new[] { 0.5, 0.5 };
            var S = new[] { 10.0, 1.0 };
            var y = LocalPermeateSolver.Solve(x, S, 0.0);

            Assert.Equal(10.0 / 11.0, y[0], Tol);
            Assert.Equal(1.0 / 11.0, y[1], Tol);
        }

        /// <summary>Single component permeates as pure product.</summary>
        [Fact]
        public void SingleComponent_IsPure()
        {
            var y = LocalPermeateSolver.Solve(new[] { 1.0 }, new[] { 5.0 }, 0.1);
            Assert.Equal(1.0, y[0], 12);
        }

        /// <summary>A non-permeating species (S = 0) yields zero permeate fraction.</summary>
        [Fact]
        public void NonPermeatingComponent_HasZeroPermeate()
        {
            var x = new[] { 0.5, 0.5 };
            var S = new[] { 100.0, 0.0 };
            var y = LocalPermeateSolver.Solve(x, S, 0.05);
            Assert.Equal(0.0, y[1], 12);
            Assert.Equal(1.0, y[0], 12);
        }

        /// <summary>Higher pressure ratio (worse driving force) reduces enrichment of the fast gas.</summary>
        [Fact]
        public void HigherPressureRatio_ReducesEnrichment()
        {
            var x = new[] { 0.5, 0.5 };
            var S = new[] { 10.0, 1.0 };
            double yFastLowGamma = LocalPermeateSolver.Solve(x, S, 0.02)[0];
            double yFastHighGamma = LocalPermeateSolver.Solve(x, S, 0.5)[0];
            Assert.True(yFastHighGamma < yFastLowGamma,
                $"expected enrichment to drop with γ: {yFastHighGamma} !< {yFastLowGamma}");
        }

        /// <summary>The fast component is always enriched in the permeate relative to the feed.</summary>
        [Fact]
        public void FastComponent_IsEnriched()
        {
            var x = new[] { 0.5, 0.5 };
            var S = new[] { 10.0, 1.0 };
            var y = LocalPermeateSolver.Solve(x, S, 0.1);
            Assert.True(y[0] > x[0], "fast gas must be enriched in permeate");
        }

        /// <summary>γ ≥ 1 is unphysical (no driving force) and must be rejected, not silently wrong.</summary>
        [Fact]
        public void PressureRatioAtLeastOne_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => LocalPermeateSolver.Solve(new[] { 0.5, 0.5 }, new[] { 10.0, 1.0 }, 1.0));
        }

        /// <summary>Deterministic: identical inputs give bit-identical outputs.</summary>
        [Fact]
        public void IsDeterministic()
        {
            var x = new[] { 0.3, 0.1, 0.25, 0.15, 0.2 };
            var S = new[] { 4.80e-9, 1.91e-10, 1.40e-10, 1.38e-10, 1.48e-10 };
            var a = LocalPermeateSolver.Solve(x, S, 0.1);
            var b = LocalPermeateSolver.Solve(x, S, 0.1);
            for (int k = 0; k < a.Length; k++)
                Assert.Equal(a[k], b[k]); // exact equality
        }

        /// <summary>Unnormalised retentate weights give the same answer as normalised ones.</summary>
        [Fact]
        public void UnnormalisedInput_IsHandled()
        {
            var xn = new[] { 0.5, 0.5 };
            var xu = new[] { 5.0, 5.0 };
            var S = new[] { 10.0, 1.0 };
            var yn = LocalPermeateSolver.Solve(xn, S, 0.1);
            var yu = LocalPermeateSolver.Solve(xu, S, 0.1);
            for (int k = 0; k < yn.Length; k++)
                Assert.Equal(yn[k], yu[k], 12);
        }
    }
}
