using System;

namespace MembraneCore
{
    /// <summary>Result of a membrane permeation calculation (dimensionless where flows are per unit feed).</summary>
    public sealed class MembraneResult
    {
        /// <summary>Retentate (residue) mole fractions, indexed as the input components.</summary>
        public double[] RetentateComposition { get; }

        /// <summary>Permeate product mole fractions, indexed as the input components.</summary>
        public double[] PermeateComposition { get; }

        /// <summary>Overall stage cut θ = total permeate molar flow / feed molar flow.</summary>
        public double StageCut { get; }

        /// <summary>Per-component recovery to permeate = permeate_i / feed_i (0..1).</summary>
        public double[] ComponentRecovery { get; }

        /// <summary>Retentate total molar flow (same units as the feed flow supplied; = 1−θ if per-unit-feed).</summary>
        public double RetentateMolarFlow { get; }

        /// <summary>Permeate total molar flow (same units as the feed flow supplied; = θ if per-unit-feed).</summary>
        public double PermeateMolarFlow { get; }

        /// <summary>Number of integration steps actually taken.</summary>
        public int Steps { get; }

        /// <summary>Worst per-component mass-balance closure residual |feed − retentate − permeate| (should be ~0).</summary>
        public double MassBalanceResidual { get; }

        /// <summary>Position-dependent profiles along the module (null unless profile capture was requested).</summary>
        public MembraneProfile? Profile { get; set; }

        /// <summary>Creates an immutable membrane result.</summary>
        public MembraneResult(
            double[] retentateComposition,
            double[] permeateComposition,
            double stageCut,
            double[] componentRecovery,
            double retentateMolarFlow,
            double permeateMolarFlow,
            int steps,
            double massBalanceResidual)
        {
            RetentateComposition = retentateComposition ?? throw new ArgumentNullException(nameof(retentateComposition));
            PermeateComposition = permeateComposition ?? throw new ArgumentNullException(nameof(permeateComposition));
            StageCut = stageCut;
            ComponentRecovery = componentRecovery ?? throw new ArgumentNullException(nameof(componentRecovery));
            RetentateMolarFlow = retentateMolarFlow;
            PermeateMolarFlow = permeateMolarFlow;
            Steps = steps;
            MassBalanceResidual = massBalanceResidual;
        }
    }
}
