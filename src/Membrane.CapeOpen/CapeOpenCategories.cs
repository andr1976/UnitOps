using System;

namespace Membrane.CapeOpen
{
    /// <summary>CAPE-OPEN COM component category IDs (CATIDs) and this component's own identity GUIDs.</summary>
    internal static class CapeOpenCategories
    {
        /// <summary>CAPE-OPEN 1.0 Unit Operation category (spec §7).</summary>
        public const string UnitOperation = "678C09A5-7D66-11D2-A67D-00105A42887F";

        /// <summary>Component consumes the thermodynamic subsystem.</summary>
        public const string ConsumesThermo = "4150C28A-EE06-403F-A871-87AFEC38A249";

        /// <summary>Component supports CAPE-OPEN thermodynamics 1.0 material objects.</summary>
        public const string SupportsThermo10 = "0D562DC8-EA8E-4210-AB39-B66513C0CD09";

        /// <summary>Component supports CAPE-OPEN thermodynamics 1.1 material objects.</summary>
        public const string SupportsThermo11 = "4667023A-5A8E-4CCA-AB6D-9D78C5112FED";
    }

    /// <summary>Stable identity of the membrane unit operation COM class. Do not change once published.</summary>
    internal static class MembraneUnitIdentity
    {
        // Fresh GUIDs for this component (generated for the ORS membrane unit operation).
        public const string Clsid = "B2E8A6C1-4F3D-4E7A-9C21-7A9F5D2E1B44";
        public const string ProgId = "ORS.MembraneUnitOperation.1";
        public const string Name = "ORS Membrane (Gas Permeation, Cross-Flow)";
        public const string Description =
            "Spiral-wound gas-permeation membrane (cross-flow, solution-diffusion, isothermal). " +
            "Physics validated against Shindo/Dias et al. (2020). Thermodynamics delegated to the flowsheet property package.";
    }
}
