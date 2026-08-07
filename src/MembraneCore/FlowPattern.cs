namespace MembraneCore
{
    /// <summary>Membrane flow configuration (how the permeate stream is arranged relative to the retentate).</summary>
    public enum FlowPattern
    {
        /// <summary>Permeate removed locally (no permeate flow along the membrane); local-equilibrium driving force.</summary>
        CrossFlow,

        /// <summary>Permeate flows in the same direction as the retentate, both from the feed end.</summary>
        CoCurrent,

        /// <summary>Permeate flows counter to the retentate, exiting at the feed end (best separation).</summary>
        CounterCurrent
    }
}
