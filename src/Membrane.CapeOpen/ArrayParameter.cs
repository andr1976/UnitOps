using System;
using System.Runtime.InteropServices;
using CAPEOPEN;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// A CAPE-OPEN real array (CAPE_ARRAY) public unit parameter — used to expose position-dependent
    /// profiles (composition/stage-cut vs. membrane position) that COFE's plotting utility can chart.
    /// Homogeneous 1-D array of doubles; output (read-only to the PME, written by the unit).
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("A4C1E7B2-8D3F-4A61-B2C5-6E9D0F1A2B37")]
    public class ArrayParameter : ICapeParameter, ICapeParameterSpec, ICapeArrayParameterSpec, ICapeIdentification
    {
        private double[] _value;
        private readonly double[] _dimensionality;

        public ArrayParameter(string name, string description, int length, double[]? dimensionality = null)
        {
            ComponentName = name;
            ComponentDescription = description;
            _value = new double[length];
            _dimensionality = dimensionality ?? new double[6]; // dimensionless (fractions / position / stage cut)
        }

        /// <summary>Unit-side setter for output values.</summary>
        internal void SetInternal(double[] v) => _value = (double[])v.Clone();

        /// <summary>Unit-side getter of the backing values (for persistence).</summary>
        internal double[] GetInternal() => (double[])_value.Clone();

        // ---- ICapeParameter ----
        public object value
        {
            // Per the Parameter CI errata §12: the value MUST be a SAFEARRAY(VARIANT) (VT_ARRAY|VT_VARIANT)
            // with each element a VT_R8. A managed double[] marshals as VT_ARRAY|VT_R8 (wrong) — return an
            // object[] of boxed doubles so it marshals as VT_ARRAY|VT_VARIANT (which COFE requires).
            get
            {
                var v = new object[_value.Length];
                for (int i = 0; i < _value.Length; i++) v[i] = _value[i];
                return v;
            }
            set
            {
                switch (value)
                {
                    case null: break;
                    case double[] d: _value = (double[])d.Clone(); break;
                    case object[] o:
                        {
                            var a = new double[o.Length];
                            for (int i = 0; i < o.Length; i++) a[i] = Convert.ToDouble(o[i]);
                            _value = a; break;
                        }
                    default: throw ComError.InvalidArgument($"'{ComponentName}' expects a real array value.");
                }
            }
        }

        public CapeParamMode Mode { get; set; } = CapeParamMode.CAPE_OUTPUT;
        public CapeValidationStatus ValStatus => CapeValidationStatus.CAPE_VALID;
        public object Specification => this;
        public bool Validate(ref string message) { message = string.Empty; return true; }
        public void Reset() => Array.Clear(_value, 0, _value.Length);

        // ---- ICapeParameterSpec ----
        // Parameter CI errata §11.1: CAPE_ARRAY ↔ ICapeArrayParameterSpec. (The older draft example used
        // CAPE_REAL; the errata supersedes it.) The array's real nature is conveyed via ItemsSpecification.
        public CapeParamType Type => CapeParamType.CAPE_ARRAY;
        public object Dimensionality => _dimensionality;

        // ---- ICapeArrayParameterSpec ----
        public int NumDimensions => 1;
        public object Size => new[] { _value.Length };

        /// <summary>Per-item specifications: an array of real specs (one shared instance) so the PME can
        /// recognise this as a plottable homogeneous real array.</summary>
        public object ItemsSpecifications
        {
            get
            {
                var shared = new RealItemSpec(_dimensionality);
                var items = new object[_value.Length];
                for (int i = 0; i < items.Length; i++) items[i] = shared;
                return items;
            }
        }
        public object Validate(object v, ref object message) { message = string.Empty; return true; }

        // ---- ICapeIdentification ----
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }
    }

    /// <summary>Lightweight real-item specification for the elements of an <see cref="ArrayParameter"/>.</summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("B5D2F8C3-9E4A-4B72-A3D6-7F0E1A2B3C48")]
    public class RealItemSpec : ICapeParameterSpec, ICapeRealParameterSpec
    {
        private readonly double[] _dim;
        public RealItemSpec(double[] dim) => _dim = dim;

        public CapeParamType Type => CapeParamType.CAPE_REAL;
        public object Dimensionality => _dim;

        public double DefaultValue => 0.0;
        public double LowerBound => -1.0e300;
        public double UpperBound => 1.0e300;
        public bool Validate(double value, ref string message) { message = string.Empty; return true; }
    }
}
