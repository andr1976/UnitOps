using System;
using MembraneCore;
using MembraneCore.Energy;
using MembraneCore.Models;
using Xunit;

namespace MembraneCore.Tests
{
    /// <summary>
    /// Validates the adiabatic non-isothermal energy layer against synthetic enthalpy providers with known
    /// closed-form behaviour: an ideal gas (no pressure dependence → no temperature change) and a linear
    /// Joule–Thomson gas (isenthalpic expansion cools the permeate by exactly μ·ΔP). The separation itself is
    /// produced by the already-validated cross-flow model and is never altered by the energy layer.
    /// </summary>
    public class NonIsothermalEnergyTests
    {
        // CO2/CH4 example (feed 10% CO2), selectivity ~20, high-pressure feed.
        private static readonly double[] Feed = { 0.10, 0.90 };
        private static readonly double[] Perm = { 3.0e-8, 1.5e-9 };   // mol m^-2 s^-1 Pa^-1
        private const double Pr = 60.0e5, Pp = 2.0e5, Nf = 1.0, FeedT = 313.15, Area = 25.0;
        private static readonly double[] Cp = { 37.0, 36.0 };          // J/(mol K)

        private static MembraneResult Separation()
            => CrossFlowModel.SolveByArea(Feed, Nf, Perm, Pr, Pp, Area, steps: 4000, profilePoints: 100);

        /// <summary>h = Σ xᵢ Cpᵢ (T − Tref); no pressure dependence.</summary>
        private sealed class IdealGas : IEnthalpyProvider
        {
            private const double Tref = 298.15;
            public double MolarEnthalpy(double t, double p, double[] x)
            {
                double h = 0.0;
                for (int i = 0; i < x.Length; i++) h += x[i] * Cp[i] * (t - Tref);
                return h;
            }
        }

        /// <summary>h = Σ xᵢ Cpᵢ (T − Tref) − μ Σ xᵢ Cpᵢ (P − Pref): a uniform Joule–Thomson coefficient μ &gt; 0,
        /// so an isenthalpic expansion (P falls) lowers the temperature by exactly μ·ΔP.</summary>
        private sealed class JouleThomson : IEnthalpyProvider
        {
            public const double Mu = 1.0e-6;   // K/Pa (≈0.1 K/bar)
            private const double Tref = 298.15, Pref = 101325.0;
            public double MolarEnthalpy(double t, double p, double[] x)
            {
                double h = 0.0;
                for (int i = 0; i < x.Length; i++) h += x[i] * Cp[i] * ((t - Tref) - Mu * (p - Pref));
                return h;
            }
        }

        [Fact]
        public void IdealGas_BothOutletsReturnToFeedTemperature()
        {
            var sep = Separation();
            var e = NonIsothermalEnergy.Solve(new IdealGas(), FeedT, Pr, Pp, Nf, Feed, sep);

            Assert.Equal(FeedT, e.PermeateTemperature, 4);
            Assert.Equal(FeedT, e.RetentateTemperature, 4);
            Assert.True(e.EnergyBalanceResidual < 1e-9, $"energy not conserved: {e.EnergyBalanceResidual}");
        }

        [Fact]
        public void JouleThomson_PermeateCoolsByMuDeltaP_RetentateStaysAtFeed()
        {
            var sep = Separation();
            var e = NonIsothermalEnergy.Solve(new JouleThomson(), FeedT, Pr, Pp, Nf, Feed, sep);

            // Isenthalpic expansion Pr → Pp: ΔT = μ (Pp − Pr) < 0.
            double expectedPermT = FeedT + JouleThomson.Mu * (Pp - Pr);
            Assert.Equal(expectedPermT, e.PermeateTemperature, 2);
            Assert.True(e.PermeateTemperature < FeedT - 5.0, "permeate should cool by ~5.8 K");

            // Enthalpy is linear in composition here, so the retentate (constant pressure) stays at feed T.
            Assert.Equal(FeedT, e.RetentateTemperature, 3);

            Assert.True(e.EnergyBalanceResidual < 1e-9, $"energy not conserved: {e.EnergyBalanceResidual}");
        }

        [Fact]
        public void Profiles_HaveExpectedShapeAndMatchOutletEndpoints()
        {
            var sep = Separation();
            var e = NonIsothermalEnergy.Solve(new JouleThomson(), FeedT, Pr, Pp, Nf, Feed, sep);

            Assert.NotNull(e.RetentateTemperatureProfile);
            Assert.NotNull(e.PermeateTemperatureProfile);
            var rt = e.RetentateTemperatureProfile!;
            var pt = e.PermeateTemperatureProfile!;
            Assert.Equal(100, rt.Length);
            Assert.Equal(100, pt.Length);

            // Feed end starts at feed temperature; retentate outlet endpoint equals the overall retentate T.
            Assert.Equal(FeedT, rt[0], 3);
            Assert.Equal(e.RetentateTemperature, rt[99], 3);

            // Permeate is colder than the feed everywhere it exists (Joule–Thomson expansion).
            for (int k = 1; k < pt.Length; k++)
                Assert.True(pt[k] < FeedT, $"permeate warmer than feed at point {k}");
        }

        [Fact]
        public void Solve_DoesNotAlterTheSeparationResult()
        {
            var sep = Separation();
            double thetaBefore = sep.StageCut;
            double co2Before = sep.PermeateComposition[0];

            NonIsothermalEnergy.Solve(new JouleThomson(), FeedT, Pr, Pp, Nf, Feed, sep);

            Assert.Equal(thetaBefore, sep.StageCut, 12);
            Assert.Equal(co2Before, sep.PermeateComposition[0], 12);
        }
    }
}
