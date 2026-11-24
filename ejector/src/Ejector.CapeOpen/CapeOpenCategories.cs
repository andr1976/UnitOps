namespace Ejector.CapeOpen
{
    /// <summary>CAPE-OPEN COM component category IDs (CATIDs).</summary>
    internal static class CapeOpenCategories
    {
        /// <summary>CAPE-OPEN 1.0 Unit Operation category.</summary>
        public const string UnitOperation = "678C09A5-7D66-11D2-A67D-00105A42887F";
        /// <summary>Component consumes the thermodynamic subsystem.</summary>
        public const string ConsumesThermo = "4150C28A-EE06-403F-A871-87AFEC38A249";
        /// <summary>Component supports CAPE-OPEN thermodynamics 1.0 material objects.</summary>
        public const string SupportsThermo10 = "0D562DC8-EA8E-4210-AB39-B66513C0CD09";
        /// <summary>Component supports CAPE-OPEN thermodynamics 1.1 material objects.</summary>
        public const string SupportsThermo11 = "4667023A-5A8E-4CCA-AB6D-9D78C5112FED";
    }

    /// <summary>Stable identity of the ejector unit operation COM class. Never change once published.</summary>
    internal static class EjectorUnitIdentity
    {
        public const string Clsid = "661320FB-F98D-47CB-9DFD-6A84D72680A9";
        public const string ProgId = "Ejector.EjectorUnitOperation.1";
        public const string Name = "Ejector (1D real-fluid)";
        public const string Description =
            "Ejector / jet pump / eductor: one-dimensional real-fluid model with constant-area mixing and " +
            "homogeneous-equilibrium flashing flow. Motive and suction inlets, one outlet; computes the discharge " +
            "pressure for the given flows (ideal design or given geometry). Thermodynamics from the flowsheet property package.";
    }
}
