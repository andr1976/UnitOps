using System;

namespace MembraneCore
{
    /// <summary>
    /// Position-dependent concentration profiles along the membrane module (feed end → retentate outlet),
    /// sampled at a fixed number of points for plotting (e.g. in COFE). Retentate is the local bulk
    /// composition; permeate is the local permeate-side composition at that position (the flowing-permeate
    /// stream for co-/counter-current; the local-equilibrium permeate for cross-flow).
    /// </summary>
    public sealed class MembraneProfile
    {
        /// <summary>Dimensionless position along the membrane area, 0 (feed end) → 1 (retentate outlet).</summary>
        public double[] Position { get; }

        /// <summary>Cumulative stage cut (permeate collected up to this position / feed) at each position.</summary>
        public double[] StageCut { get; }

        /// <summary>Retentate mole fraction, indexed [component][point].</summary>
        public double[][] Retentate { get; }

        /// <summary>Permeate mole fraction (local, at that position), indexed [component][point].</summary>
        public double[][] Permeate { get; }

        /// <summary>Cumulative collected permeate mole fraction (product so far), indexed [component][point].</summary>
        public double[][] PermeateCollected { get; }

        public MembraneProfile(int points, int components)
        {
            if (points < 2) throw new ArgumentOutOfRangeException(nameof(points));
            Position = new double[points];
            StageCut = new double[points];
            Retentate = new double[components][];
            Permeate = new double[components][];
            PermeateCollected = new double[components][];
            for (int i = 0; i < components; i++)
            {
                Retentate[i] = new double[points];
                Permeate[i] = new double[points];
                PermeateCollected[i] = new double[points];
            }
        }

        public int Points => Position.Length;
        public int Components => Retentate.Length;
    }
}
