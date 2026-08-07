using System;
using System.Runtime.InteropServices;
using CAPEOPEN;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// A CAPE-OPEN material port. Implements <see cref="ICapeUnitPort"/> and <see cref="ICapeIdentification"/>.
    /// The PME connects a Material Object (implementing <see cref="ICapeThermoMaterialObject"/>) via
    /// <see cref="Connect"/>; the unit retrieves it through <see cref="connectedObject"/>.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("C0A6F2D1-1B3E-4A55-9D6C-2E4B8A17C301")]
    public class MaterialPort : ICapeUnitPort, ICapeIdentification
    {
        private object? _connected;

        public MaterialPort(string name, CapePortDirection direction, string description)
        {
            ComponentName = name;
            Direction = direction;
            ComponentDescription = description;
        }

        internal CapePortDirection Direction { get; }

        /// <summary>The connected Material Object, or null if unconnected.</summary>
        internal object? ConnectedObject => _connected;

        // ---- ICapeUnitPort ----
        public CapePortType portType => CapePortType.CAPE_MATERIAL;
        public CapePortDirection direction => Direction;
        public object connectedObject => _connected!;

        public void Connect(object objectToConnect)
        {
            if (objectToConnect == null)
                throw ComError.InvalidArgument("Cannot connect a null object to a material port.");
            // Accept either a CO 1.1 (ICapeThermoMaterial) or CO 1.0 (ICapeThermoMaterialObject) material.
            if (!(objectToConnect is ICapeThermoMaterial || objectToConnect is ICapeThermoMaterialObject))
                throw ComError.InvalidArgument($"Port '{ComponentName}' requires a CAPE-OPEN Material Object (1.1 or 1.0).");
            ReleaseConnected();          // release any previously-held reference before replacing
            _connected = objectToConnect;
        }

        public void Disconnect()
        {
            ReleaseConnected();
        }

        /// <summary>
        /// Releases the stored Material Object COM reference (we AddRef'd it by storing it on Connect).
        /// Balances the reference so the PME does not report a leak on teardown (Field Note #3). No-op for
        /// plain managed test doubles. Never throws across the COM boundary.
        /// </summary>
        internal void ReleaseConnected()
        {
            var obj = _connected;
            _connected = null;
            if (obj != null && Marshal.IsComObject(obj))
            {
                try { Marshal.ReleaseComObject(obj); } catch { }
            }
        }

        // ---- ICapeIdentification ----
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }
    }
}
