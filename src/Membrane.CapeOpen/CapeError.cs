using System;
using System.Runtime.InteropServices;
using CAPEOPEN;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// A CAPE-OPEN error object: a COM-visible exception implementing the CO error common interfaces
    /// <see cref="ECapeRoot"/> (name) and <see cref="ECapeUser"/> (code/description/scope/…). Throwing this
    /// from a COM method lets the PME retrieve the error <em>name</em> and <em>description</em> via
    /// QueryInterface/IErrorInfo — without it, COFE reports "failed to get error name for ECapeUnknown".
    /// The HRESULT still carries the specific CO error category (spec §8).
    /// </summary>
    [ComVisible(true)]
    [Guid("A7F3C9E4-6B2D-4C81-9E5A-1D3F7B2C8A96")]
    [ClassInterface(ClassInterfaceType.None)]
    public class CapeError : Exception, ECapeRoot, ECapeUser
    {
        private readonly string _name;
        private readonly string _operation;

        public CapeError(int hr, string errorName, string description, string operation = "")
            : base(description)
        {
            HResult = hr;
            _name = errorName;
            _operation = operation;
        }

        // ECapeRoot
        public string name => _name;

        // ECapeUser
        public int code => HResult;
        public string description => Message;
        public string scope => string.Empty;
        public string interfaceName => string.Empty;
        public string operation => _operation;
        public string moreInfo => "Membrane unit operation (see membrane_capeopen.log next to the DLL).";
    }
}
