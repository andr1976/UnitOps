namespace MembraneCore.Energy
{
    /// <summary>
    /// Supplies real-fluid molar enthalpy for the non-isothermal energy balance. The membrane physics core is
    /// PME-agnostic, so it never computes thermodynamics itself: the CAPE-OPEN adapter implements this by
    /// delegating to the connected Material Object's property package (which encodes the equation of state and
    /// therefore the Joule–Thomson behaviour). Tests supply synthetic providers.
    /// </summary>
    public interface IEnthalpyProvider
    {
        /// <summary>
        /// Molar enthalpy [J/mol] of a single (vapour) phase at the given temperature [K], pressure [Pa] and
        /// mole fractions. Must be continuous and monotonically increasing in temperature (Cp &gt; 0); the
        /// energy solver root-finds temperature on this function.
        /// </summary>
        double MolarEnthalpy(double temperatureK, double pressurePa, double[] moleFractions);
    }
}
