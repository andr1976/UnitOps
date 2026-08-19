using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using CAPEOPEN;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// A CAPE-OPEN real (double) public unit parameter. The same object serves as its own
    /// <see cref="ICapeParameterSpec"/>/<see cref="ICapeRealParameterSpec"/> (returned from
    /// <see cref="Specification"/>), which is the conventional CO 1.0 pattern.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("D1B7C3E2-2C4F-4B66-8E7D-3F5C9B28D412")]
    public class RealParameter : ICapeParameter, ICapeParameterSpec, ICapeRealParameterSpec, ICapeIdentification
    {
        private readonly double[] _dimensionality;

        public RealParameter(string name, string description, double defaultValue,
                             double lowerBound, double upperBound, CapeParamMode mode,
                             double[]? dimensionality = null)
        {
            ComponentName = name;
            ComponentDescription = description;
            DefaultValueCore = defaultValue;
            LowerBoundCore = lowerBound;
            UpperBoundCore = upperBound;
            ModeCore = mode;
            ValueCore = defaultValue;
            _dimensionality = dimensionality ?? new double[6]; // SI base-dimension vector; zeros = dimensionless
        }

        internal double DefaultValueCore { get; }
        internal double LowerBoundCore { get; }
        internal double UpperBoundCore { get; }
        internal CapeParamMode ModeCore { get; private set; }
        internal double ValueCore { get; private set; }

        /// <summary>Used by the unit to write output parameters (bypasses the read-only guard).</summary>
        internal void SetInternal(double v) => ValueCore = v;

        // ---- ICapeParameter ----
        public object value
        {
            get => ValueCore;
            set
            {
                if (ModeCore == CapeParamMode.CAPE_OUTPUT)
                    throw ComError.InvalidArgument($"Parameter '{ComponentName}' is read-only (output).");
                double v = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (v < LowerBoundCore || v > UpperBoundCore)
                    throw ComError.OutOfBounds($"Parameter '{ComponentName}' = {v} out of [{LowerBoundCore}, {UpperBoundCore}].");
                ValueCore = v;
            }
        }

        public CapeParamMode Mode { get => ModeCore; set => ModeCore = value; }

        public CapeValidationStatus ValStatus =>
            (ValueCore >= LowerBoundCore && ValueCore <= UpperBoundCore)
                ? CapeValidationStatus.CAPE_VALID : CapeValidationStatus.CAPE_INVALID;

        public object Specification => this;

        public bool Validate(ref string message)
        {
            if (ValueCore < LowerBoundCore || ValueCore > UpperBoundCore)
            {
                message = $"'{ComponentName}' = {ValueCore.ToString(CultureInfo.InvariantCulture)} " +
                          $"is outside [{LowerBoundCore}, {UpperBoundCore}].";
                return false;
            }
            message = string.Empty;
            return true;
        }

        public void Reset() => ValueCore = DefaultValueCore;

        // ---- ICapeParameterSpec ----
        public CapeParamType Type => CapeParamType.CAPE_REAL;
        public object Dimensionality => _dimensionality;

        // ---- ICapeRealParameterSpec ----
        double ICapeRealParameterSpec.DefaultValue => DefaultValueCore;
        double ICapeRealParameterSpec.LowerBound => LowerBoundCore;
        double ICapeRealParameterSpec.UpperBound => UpperBoundCore;
        public bool Validate(double v, ref string message)
        {
            if (v < LowerBoundCore || v > UpperBoundCore)
            {
                message = $"'{ComponentName}' = {v.ToString(CultureInfo.InvariantCulture)} outside [{LowerBoundCore}, {UpperBoundCore}].";
                return false;
            }
            message = string.Empty;
            return true;
        }

        // ---- ICapeIdentification ----
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }
    }

    /// <summary>A CAPE-OPEN option (string enumeration) public unit parameter, e.g. flow pattern.</summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("E2C8D4F3-3D5A-4C77-9F8E-4A6DAC39E523")]
    public class OptionParameter : ICapeParameter, ICapeParameterSpec, ICapeOptionParameterSpec, ICapeIdentification
    {
        private readonly string[] _options;

        public OptionParameter(string name, string description, string defaultValue,
                               string[] options, CapeParamMode mode)
        {
            ComponentName = name;
            ComponentDescription = description;
            DefaultValueCore = defaultValue;
            _options = options;
            ModeCore = mode;
            ValueCore = defaultValue;
        }

        internal string DefaultValueCore { get; }
        internal CapeParamMode ModeCore { get; private set; }
        internal string ValueCore { get; private set; }

        // ---- ICapeParameter ----
        public object value
        {
            get => ValueCore;
            set
            {
                string s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (Array.IndexOf(_options, s) < 0)
                    throw ComError.InvalidArgument($"'{ComponentName}': '{s}' is not one of [{string.Join(", ", _options)}].");
                ValueCore = s;
            }
        }

        public CapeParamMode Mode { get => ModeCore; set => ModeCore = value; }
        public CapeValidationStatus ValStatus =>
            Array.IndexOf(_options, ValueCore) >= 0 ? CapeValidationStatus.CAPE_VALID : CapeValidationStatus.CAPE_INVALID;
        public object Specification => this;

        public bool Validate(ref string message)
        {
            if (Array.IndexOf(_options, ValueCore) < 0)
            {
                message = $"'{ComponentName}' = '{ValueCore}' is not a valid option.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        public void Reset() => ValueCore = DefaultValueCore;

        // ---- ICapeParameterSpec ----
        public CapeParamType Type => CapeParamType.CAPE_OPTION;
        public object Dimensionality => new double[6];

        // ---- ICapeOptionParameterSpec ----
        string ICapeOptionParameterSpec.DefaultValue => DefaultValueCore;
        // Must marshal as SAFEARRAY(BSTR) i.e. a plain string[] (VT_ARRAY|VT_BSTR): DWSIM casts OptionList directly
        // to String() (CapeOpenUO.vb) and COFE also expects the string array. (An object[]/VARIANT array breaks DWSIM
        // with "Unable to cast Object[] to String[]".)
        public object OptionList => _options;
        public bool RestrictedToList => true;
        public bool Validate(string v, ref string message)
        {
            if (Array.IndexOf(_options, v) < 0)
            {
                message = $"'{ComponentName}': '{v}' is not a valid option.";
                return false;
            }
            message = string.Empty;
            return true;
        }

        // ---- ICapeIdentification ----
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }
    }

    /// <summary>
    /// A CAPE-OPEN collection (<see cref="ICapeCollection"/>) of items that implement
    /// <see cref="ICapeIdentification"/>. 1-based indexing; <see cref="Item"/> accepts a 1-based integer
    /// index or the item's ComponentName (case-insensitive).
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("F3D9E5A4-4E6B-4D88-A09F-5B7EBD4AF634")]
    public class CapeCollection : ICapeCollection
    {
        private readonly List<object> _items = new List<object>();

        internal void Add(object item) => _items.Add(item);
        internal IReadOnlyList<object> Items => _items;

        public int Count() => _items.Count;

        public object Item(object id)
        {
            if (id == null) throw ComError.InvalidArgument("Collection index is null.");

            if (id is string name)
            {
                foreach (var it in _items)
                    if (it is ICapeIdentification idn &&
                        string.Equals(idn.ComponentName, name, StringComparison.OrdinalIgnoreCase))
                        return it;
                throw ComError.InvalidArgument($"No collection item named '{name}'.");
            }

            int ix;
            try { ix = Convert.ToInt32(id, CultureInfo.InvariantCulture); }
            catch { throw ComError.InvalidArgument("Collection index must be an integer or a name."); }

            if (ix < 1 || ix > _items.Count)
                throw ComError.OutOfBounds($"Collection index {ix} out of range 1..{_items.Count}.");
            return _items[ix - 1];
        }
    }
}
