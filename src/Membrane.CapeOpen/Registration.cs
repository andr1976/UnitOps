using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// COM (un)registration helpers. On <c>regasm</c> these add the CAPE-OPEN component-category CATIDs and
    /// the <c>CapeDescription</c> keys under the class's CLSID, so the PME (COFE) discovers the unit and
    /// knows it consumes thermodynamics (1.0 and 1.1 material objects).
    /// </summary>
    public static class Registration
    {
        private static string ClsidRoot => $@"CLSID\{{{MembraneUnitIdentity.Clsid}}}";

        [ComRegisterFunction]
        public static void Register(Type t)
        {
            try
            {
                using (var clsid = Registry.ClassesRoot.CreateSubKey(ClsidRoot))
                {
                    if (clsid == null) return;

                    // Implemented Categories → the CAPE-OPEN CATIDs.
                    using (var cats = clsid.CreateSubKey("Implemented Categories"))
                    {
                        cats?.CreateSubKey("{" + CapeOpenCategories.UnitOperation + "}")?.Close();
                        cats?.CreateSubKey("{" + CapeOpenCategories.ConsumesThermo + "}")?.Close();
                        cats?.CreateSubKey("{" + CapeOpenCategories.SupportsThermo10 + "}")?.Close();
                        cats?.CreateSubKey("{" + CapeOpenCategories.SupportsThermo11 + "}")?.Close();
                    }

                    // CapeDescription metadata shown in the PME palette.
                    using (var desc = clsid.CreateSubKey("CapeDescription"))
                    {
                        desc?.SetValue("Name", MembraneUnitIdentity.Name);
                        desc?.SetValue("Description", MembraneUnitIdentity.Description);
                        desc?.SetValue("CapeVersion", "1.0");
                        desc?.SetValue("About", "ORS-Consulting membrane unit operation. Cross-flow gas permeation.");
                        desc?.SetValue("VersionNumber", "1.0.0");
                        desc?.SetValue("Vendor", "ORS Consulting");
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Log("Register failed: " + ex.Message);
                throw;
            }
        }

        [ComUnregisterFunction]
        public static void Unregister(Type t)
        {
            try { Registry.ClassesRoot.DeleteSubKeyTree(ClsidRoot, throwOnMissingSubKey: false); }
            catch (Exception ex) { Diagnostics.Log("Unregister failed: " + ex.Message); }
        }
    }
}
