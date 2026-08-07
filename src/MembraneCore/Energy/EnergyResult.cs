namespace MembraneCore.Energy
{
    /// <summary>
    /// Outcome of the non-isothermal (adiabatic) energy balance layered on top of an isothermal separation
    /// result. Separation (compositions, stage cut) is unchanged; this only assigns outlet temperatures and,
    /// optionally, temperature profiles along the module.
    /// </summary>
    public sealed class EnergyResult
    {
        /// <summary>Retentate outlet temperature [K].</summary>
        public double RetentateTemperature { get; }

        /// <summary>Permeate outlet temperature [K] (reflects the Joule–Thomson expansion to permeate pressure).</summary>
        public double PermeateTemperature { get; }

        /// <summary>Feed temperature [K] (echoed for reference).</summary>
        public double FeedTemperature { get; }

        /// <summary>
        /// Relative energy-balance closure |H_feed − H_permeate − H_retentate| / |H_feed| evaluated at the
        /// solved outlet temperatures. Should be ~0; a large value signals a temperature root-find that did
        /// not converge.
        /// </summary>
        public double EnergyBalanceResidual { get; }

        /// <summary>Retentate temperature [K] along the profile positions (null if profiles not requested).</summary>
        public double[]? RetentateTemperatureProfile { get; set; }

        /// <summary>Permeate temperature [K] along the profile positions (null if profiles not requested).</summary>
        public double[]? PermeateTemperatureProfile { get; set; }

        public EnergyResult(double retentateTemperature, double permeateTemperature, double feedTemperature,
            double energyBalanceResidual)
        {
            RetentateTemperature = retentateTemperature;
            PermeateTemperature = permeateTemperature;
            FeedTemperature = feedTemperature;
            EnergyBalanceResidual = energyBalanceResidual;
        }
    }
}
