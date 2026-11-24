using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CAPEOPEN;
using EjectorCore;

namespace Ejector.CapeOpen
{
    /// <summary>
    /// CAPE-OPEN 1.0 ejector / jet-pump unit operation. Two material inlets (Motive, Suction) and one outlet
    /// (Outlet). The flowsheet fixes both inlet streams, flows included; the unit computes the discharge
    /// pressure (techref sec-flowsheet-specs):
    /// <list type="bullet">
    /// <item><b>Design</b> ("ideal" ejector): the optimum geometry for the port flows, which delivers the highest
    /// outlet pressure the efficiencies allow; the diameters and area ratios are outputs.</item>
    /// <item><b>Rating</b>: a given geometry. FixedShape = area ratios Ae/At and Amix/At; the throat is sized to pass
    /// the motive port flow (always solvable). FixedSize = diameters; the motive port flow is still used, and the
    /// mismatch with the capacity Cd*G*At of the given throat is reported for the flowsheet to close.</item>
    /// </list>
    /// Physics in EjectorCore; thermodynamics through PmeFlash (scratch material object, CO 1.1).
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(EjectorUnitIdentity.Clsid)]
    [ProgId(EjectorUnitIdentity.ProgId)]
    [ComDefaultInterface(typeof(ICapeUnit))]
    public class EjectorUnitOperation :
        ICapeUnit, ICapeIdentification, ICapeUtilities, ICapeUnitReport, IPersistStreamInit,
        ECapeUser, ECapeRoot
    {
        private const int PersistVersion = 1;
        internal const string ModeDesign = "Design", ModeRating = "Rating";
        internal const string ShapeFixed = "FixedShape", SizeFixed = "FixedSize";
        private const string RegimeNone = "NotCalculated";

        private static readonly double[] DimPressure = { -1, 1, -2, 0, 0, 0 };
        private static readonly double[] DimLength = { 1, 0, 0, 0, 0, 0 };
        private static readonly double[] DimVelocity = { 1, 0, -1, 0, 0, 0 };
        private static readonly double[] DimMassFlow = { 0, 1, -1, 0, 0, 0 };
        private static readonly double[] DimTime = { 0, 0, 1, 0, 0, 0 };

        // ---- ports
        private readonly MaterialPort _motive = new MaterialPort("Motive", CapePortDirection.CAPE_INLET, "Motive (high-pressure) inlet");
        private readonly MaterialPort _suction = new MaterialPort("Suction", CapePortDirection.CAPE_INLET, "Suction (entrained) inlet");
        private readonly MaterialPort _outlet = new MaterialPort("Outlet", CapePortDirection.CAPE_OUTLET, "Discharge (diffuser outlet)");
        private readonly CapeCollection _ports = new CapeCollection();

        // ---- inputs
        private readonly OptionParameter _mode = new OptionParameter("Mode",
            "Design = ideal ejector: optimum geometry for the port flows (geometry is computed); Rating = given geometry (see GeometrySpec). Both compute the outlet pressure.",
            ModeDesign, new[] { ModeDesign, ModeRating }, CapeParamMode.CAPE_INPUT);
        private readonly OptionParameter _geometry = new OptionParameter("GeometrySpec",
            "Rating only. FixedShape = area ratios given, throat sized to pass the motive port flow; FixedSize = diameters given, motive capacity mismatch reported.",
            ShapeFixed, new[] { ShapeFixed, SizeFixed }, CapeParamMode.CAPE_INPUT);
        private readonly RealParameter _rE = new RealParameter("AreaRatioExit", "Motive nozzle exit / throat area Ae/At", 1.31, 1.0, 1e3, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _rMix = new RealParameter("AreaRatioMixing", "Mixing section / throat area Amix/At", 4.94, 1.0, 1e5, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _dT = new RealParameter("ThroatDiameter", "Motive nozzle throat diameter", 0.9e-3, 1e-6, 10.0, CapeParamMode.CAPE_OUTPUT, DimLength);
        private readonly RealParameter _dE = new RealParameter("ExitDiameter", "Motive nozzle exit diameter", 1.03e-3, 1e-6, 10.0, CapeParamMode.CAPE_OUTPUT, DimLength);
        private readonly RealParameter _dMix = new RealParameter("MixingDiameter", "Constant-area mixing section diameter", 2.0e-3, 1e-6, 10.0, CapeParamMode.CAPE_OUTPUT, DimLength);
        private readonly OptionParameter _preset = new OptionParameter("EfficiencySet", Presets.Description,
            Presets.Screening, Presets.Names, CapeParamMode.CAPE_INPUT);
        private readonly RealParameter _etaM = new RealParameter("MotiveEfficiency", "Motive nozzle isentropic efficiency (acts beyond the throat)", 0.90, 0.01, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _etaS = new RealParameter("SuctionEfficiency", "Suction isentropic efficiency", 0.85, 0.01, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _etaMix = new RealParameter("MixingEfficiency", "Mixing efficiency on the inlet momentum flux", 0.90, 0.01, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _etaD = new RealParameter("DiffuserEfficiency", "Diffuser isentropic efficiency", 0.80, 0.01, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _cd = new RealParameter("DischargeCoefficient", "Motive discharge coefficient on the isentropic HEM critical flux", 1.0, 0.1, 3.0, CapeParamMode.CAPE_OUTPUT);
        private readonly OptionParameter _motiveModel = new OptionParameter("MotiveModel",
            "HEM = homogeneous equilibrium (default); HNE = delayed flashing, x = phi * x_eq in the density (sensitivity studies; Cd is the recommended correction)",
            "HEM", new[] { "HEM", "HNE" }, CapeParamMode.CAPE_INPUT);
        private readonly RealParameter _phi = new RealParameter("FlashingFactor", "HNE flashing factor phi (1 = HEM)", 1.0, 0.01, 1.0, CapeParamMode.CAPE_INPUT);
        private readonly OptionParameter _mixing = new OptionParameter("MixingModel",
            "ConstantPressure = mixing at the pressure of a hypothetical throat where the suction stream chokes, then a normal shock (Huang 1999; default); ConstantArea = mixing over the constant-area section. Input with EfficiencySet Custom, else set by the efficiency set.",
            Presets.ConstantPressure, new[] { Presets.ConstantPressure, Presets.ConstantArea }, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _phiP = new RealParameter("PrimaryCoreCoefficient",
            "ConstantPressure only: fraction phi_p of the isentropic motive-core area counted at the hypothetical throat (Huang's phi_p; 1 = none). Input with EfficiencySet Custom.",
            1.0, 0.3, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _curvePoints = new RealParameter("CurvePoints",
            "Operating curve: 0 = the points of the rating scan only (no extra cost); n > 0 = n points re-sampled from the critical point to zero entrainment (extra flashes). In Design mode n > 0 rates the designed geometry.",
            0.0, 0.0, 200.0, CapeParamMode.CAPE_INPUT);

        // ---- outputs
        private readonly RealParameter _pOut = Out("OutletPressure", "Outlet (discharge) pressure", DimPressure);
        private readonly RealParameter _er = Out("EntrainmentRatio", "Suction / motive mass flow");
        private readonly RealParameter _lift = Out("PressureLift", "Outlet pressure / suction pressure");
        private readonly OptionParameter _regime = new OptionParameter("Regime",
            "Design; Critical = most-entraining point (outlet pressure = critical back pressure); Subcritical = entrainment set by the back pressure",
            RegimeNone, new[] { RegimeNone, "Design", "Critical", "Subcritical", "Backflow" }, CapeParamMode.CAPE_OUTPUT);
        private readonly RealParameter _pt = Out("ThroatPressure", "Motive throat pressure", DimPressure);
        private readonly RealParameter _pe = Out("NozzleExitPressure", "Motive nozzle exit pressure", DimPressure);
        private readonly RealParameter _py = Out("SuctionStaticPressure", "Suction static pressure at the mixing inlet", DimPressure);
        private readonly RealParameter _p3 = Out("MixingOutletPressure", "Pressure at the end of the mixing section", DimPressure);
        private readonly RealParameter _ue = Out("NozzleExitVelocity", "Motive jet velocity", DimVelocity);
        private readonly RealParameter _u3 = Out("MixingOutletVelocity", "Velocity at the end of the mixing section", DimVelocity);
        private readonly RealParameter _etaEj = Out("EjectorEfficiency", "Ejector efficiency (Koehler / Elbel and Hrnjak)");
        private readonly RealParameter _solid = Out("SolidMargin", "Lowest static pressure / triple-point pressure (pure fluids; 0 = not available)");
        private readonly RealParameter _erCrit = Out("CriticalEntrainmentRatio", "Rating: most-entraining ER of the geometry");
        private readonly RealParameter _pbCrit = Out("CriticalBackPressure", "Rating: outlet pressure at the critical ER", DimPressure);
        private readonly RealParameter _mCap = Out("MotiveCapacity", "FixedSize: Cd*G*At of the given throat", DimMassFlow);
        private readonly RealParameter _mMis = Out("MotiveFlowMismatch", "FixedSize: MotiveCapacity / motive port flow - 1");
        private readonly RealParameter _xOut = Out("OutletVapourFraction", "Outlet vapour mass fraction (-1 liquid, 2 vapour/supercritical)");
        private readonly RealParameter _nFlash = Out("FlashCount", "Property-package flashes used by the last calculation");
        private readonly RealParameter _tCalc = Out("CalculationTime", "Wall-clock time of the last calculation", DimTime);
        private readonly ArrayParameter _curveP = new ArrayParameter("Curve_OutletPressure", "Operating curve: outlet pressure (critical point to zero entrainment)", 0, DimPressure);
        private readonly ArrayParameter _curveEr = new ArrayParameter("Curve_EntrainmentRatio", "Operating curve: entrainment ratio", 0);

        private readonly CapeCollection _params = new CapeCollection();
        private object? _simulationContext;
        private CapeValidationStatus _valStatus = CapeValidationStatus.CAPE_NOT_VALIDATED;
        private CapeError? _lastError;
        private string _lastReport = "No calculation has been performed yet.";
        private ReportData? _lastData;

        private static RealParameter Out(string name, string desc, double[]? dim = null) =>
            new RealParameter(name, desc, 0.0, double.NegativeInfinity, double.PositiveInfinity, CapeParamMode.CAPE_OUTPUT, dim);

        static EjectorUnitOperation()
        {
            AssemblyResolver.Ensure();
        }

        public EjectorUnitOperation()
        {
            ComponentName = EjectorUnitIdentity.Name;
            ComponentDescription = EjectorUnitIdentity.Description;
            _ports.Add(_motive);
            _ports.Add(_suction);
            _ports.Add(_outlet);
            foreach (var p in new object[]
                     {
                         _mode, _geometry, _rE, _rMix, _dT, _dE, _dMix, _preset, _etaM, _etaS, _etaMix, _etaD, _cd,
                         _motiveModel, _phi, _mixing, _phiP, _curvePoints,
                         _pOut, _er, _lift, _regime, _pt, _pe, _py, _p3, _ue, _u3, _etaEj, _solid, _erCrit, _pbCrit,
                         _mCap, _mMis, _xOut, _nFlash, _tCalc, _curveP, _curveEr,
                     })
                _params.Add(p);
            ApplyModes();
        }

        // =================== ICapeUnit ===================
        public object ports => _ports;
        public CapeValidationStatus ValStatus => _valStatus;

        public bool Validate(ref string message)
        {
            try
            {
                if (_motive.ConnectedObject == null) return Invalid(ref message, "Motive port is not connected.");
                if (_suction.ConnectedObject == null) return Invalid(ref message, "Suction port is not connected.");
                if (_outlet.ConnectedObject == null) return Invalid(ref message, "Outlet port is not connected.");
                // compound lists only (stream state is undefined before the solve)
                var idsM = StreamAdapter.ReadComponentIds(_motive.ConnectedObject!);
                var idsS = StreamAdapter.ReadComponentIds(_suction.ConnectedObject!);
                if (!idsM.SequenceEqual(idsS, StringComparer.OrdinalIgnoreCase))
                    return Invalid(ref message, "Motive and suction streams must use the same compound list (one property package).");
                if (!(_motive.ConnectedObject is ICapeThermoMaterial))
                    return Invalid(ref message, "The ejector needs CAPE-OPEN 1.1 material objects (PH/PS flashes on a scratch material).");
                ApplyModes();
                string? geo = CheckGeometry();
                if (geo != null) return Invalid(ref message, geo);
                message = string.Empty;
                _valStatus = CapeValidationStatus.CAPE_VALID;
                return true;
            }
            catch (Exception ex)
            {
                return Invalid(ref message, "Validation error: " + ex.Message);
            }
        }

        private bool Invalid(ref string message, string reason)
        {
            message = reason;
            Diagnostics.Log("Validate -> INVALID: " + reason);
            _valStatus = CapeValidationStatus.CAPE_INVALID;
            return false;
        }

        private string? CheckGeometry()
        {
            if (!IsRating()) return null;
            double rE;
            if (IsFixedSize())
            {
                if (!(_dE.ValueCore >= _dT.ValueCore)) return "ExitDiameter must not be smaller than ThroatDiameter.";
                if (!(_dMix.ValueCore > _dE.ValueCore)) return "MixingDiameter must be larger than ExitDiameter.";
                rE = Math.Pow(_dE.ValueCore / _dT.ValueCore, 2);
            }
            else
            {
                if (!(_rMix.ValueCore > _rE.ValueCore && _rE.ValueCore >= 1.0)) return "Need 1 <= AreaRatioExit < AreaRatioMixing.";
                rE = _rE.ValueCore;
            }
            double cd = Efficiencies().Cd;
            if (rE > 1.0001 && rE < cd)
                return FormattableString.Invariant(
                    $"Nozzle exit/throat area ratio {rE:F4} is below the discharge coefficient {cd:F4}: with Cd applied at the throat, the diverging section cannot pass the corrected motive flow. Use a converging nozzle (ratio 1), a ratio of at least Cd, or a smaller Cd.");
            return null;
        }

        public void Calculate()
        {
            var sw = Stopwatch.StartNew();
            Diagnostics.Log("Calculate: entry");
            try
            {
                if (_motive.ConnectedObject == null || _suction.ConnectedObject == null || _outlet.ConnectedObject == null)
                    throw ComError.SolvingError("All three ports (Motive, Suction, Outlet) must be connected.");
                ApplyModes();
                string? geo = CheckGeometry();
                if (geo != null) throw ComError.SolvingError(geo);

                var mot = StreamAdapter.ReadInlet(_motive.ConnectedObject!, "Motive");
                var suc = StreamAdapter.ReadInlet(_suction.ConnectedObject!, "Suction");
                if (!mot.ComponentIds.SequenceEqual(suc.ComponentIds, StringComparer.OrdinalIgnoreCase))
                    throw ComError.SolvingError("Motive and suction streams must use the same compound list.");
                if (!(mot.MolarFlow > 0)) throw ComError.SolvingError("The motive flow must be positive.");
                if (suc.MolarFlow < 0) throw ComError.SolvingError("The suction flow must not be negative.");
                if (!(mot.P > suc.P)) throw ComError.SolvingError("The motive pressure must be above the suction pressure.");

                var ids = mot.ComponentIds;
                var mw = StreamAdapter.ReadMolecularWeights(_motive.ConnectedObject!, ids);
                double pTriple = ids.Length == 1 ? StreamAdapter.ReadTriplePointPressure(_motive.ConnectedObject!, ids[0]) : 0.0;
                object scratch = ((ICapeThermoMaterial)_motive.ConnectedObject!).CreateMaterial();
                using var flash = new PmeFlash(scratch, mw, pTriple);

                var eff = Efficiencies();
                var ej = new EjectorCore.Ejector(flash)
                {
                    EtaM = eff.EtaM, EtaS = eff.EtaS, EtaMix = eff.EtaMix, EtaD = eff.EtaD, CdM = eff.Cd,
                    MotiveModel = _motiveModel.ValueCore == "HNE" ? MotiveModel.HNE : MotiveModel.HEM,
                    PhiNe = _phi.ValueCore,
                    Mixing = eff.Mixing == Presets.ConstantPressure ? MixingModel.CPM : MixingModel.CAM,
                    PhiP = eff.PhiP,
                };
                double[]? zM = ids.Length > 1 ? mot.Z : null, zS = ids.Length > 1 ? suc.Z : null;
                var vap = flash.VapourPhases;
                var m0 = StreamAdapter.InletState(ej, mot, vap, zM);
                var s0 = StreamAdapter.InletState(ej, suc, vap, zS);
                double mM = mot.MolarFlow * flash.MolarMass(zM);
                double mS = suc.MolarFlow * flash.MolarMass(zS);
                Diagnostics.Log($"Calculate: m0 {m0}  s0 {s0}  mM={mM:G6} kg/s mS={mS:G6} kg/s");

                int nCurve = (int)Math.Round(_curvePoints.ValueCore);
                EjectorResult r;
                EjectorResult? curveSource = null;
                if (IsRating())
                {
                    double rE, rMix;
                    double? aT = null;
                    if (IsFixedSize())
                    {
                        rE = Math.Pow(_dE.ValueCore / _dT.ValueCore, 2);
                        rMix = Math.Pow(_dMix.ValueCore / _dT.ValueCore, 2);
                        aT = Geometry.Area(_dT.ValueCore);
                    }
                    else
                    {
                        rE = _rE.ValueCore;
                        rMix = _rMix.ValueCore;
                    }
                    try
                    {
                        r = ej.RateFlows(m0, s0, mM, mS, rE, rMix, aT, nCurve: nCurve);
                    }
                    catch (EntrainmentExceededException ex)
                    {
                        throw ComError.SolvingError(
                            FormattableString.Invariant($"The suction flow ({mS:G5} kg/s, ER {mS / mM:F4}) exceeds what this geometry can entrain: critical ER {ex.ERCritical:F4}, ") +
                            FormattableString.Invariant($"reached at an outlet pressure of {ex.PbCritical / 1e5:F3} bar. Reduce the suction flow or enlarge the mixing section."));
                    }
                    curveSource = r;
                }
                else
                {
                    double er = mS / mM;
                    if (er < 1e-6) er = 1e-6;   // a design needs a suction annulus
                    r = ej.Design(m0, s0, mM, er);
                    if (nCurve > 0)
                    {
                        try { curveSource = ej.RateFlows(m0, s0, mM, er * mM, r.Ae / r.At, r.Amix / r.At, null, nCurve: nCurve); }
                        catch (Exception ex) { Diagnostics.Log("Design curve failed: " + ex.Message); }
                    }
                }

                // outlet: pressure from the engine, enthalpy and composition from the mass and energy balances
                double[] zOut = ids.Length > 1 ? ej.MixedComposition(m0, s0, mM, mS)! : new[] { 1.0 };
                double nOut = mot.MolarFlow + suc.MolarFlow;
                double hOutMass = (mM * m0.H + mS * s0.H) / (mM + mS);
                double hOutMolar = hOutMass * flash.MolarMass(ids.Length > 1 ? zOut : null);
                StreamAdapter.WriteOutlet(_outlet.ConnectedObject!, zOut, nOut, r.Pout, hOutMolar, r.Out?.T ?? m0.T);

                PublishResults(r, curveSource, mM, mS, s0, ej.TriplePointPressure, flash.CalcEquilibriumCount, sw.Elapsed.TotalSeconds);
                _lastData = new ReportData(r, curveSource, mot, suc, mM, mS, m0, s0, _mode.ValueCore, _geometry.ValueCore,
                                           _preset.ValueCore, eff, flash.CalcEquilibriumCount, sw.Elapsed.TotalSeconds);
                _lastReport = HtmlReport.BuildText(_lastData);
                Diagnostics.Log($"Calculate -> OK: {r}  flashes={flash.CalcEquilibriumCount} t={sw.Elapsed.TotalSeconds:F2}s");
            }
            catch (CapeError ce) { _lastError = ce; throw; }
            catch (Exception ex)
            {
                Diagnostics.Log("Calculate failed: " + ex);
                _lastError = ComError.SolvingError("Ejector calculation failed: " + ex.Message);
                throw _lastError;
            }
        }

        private void PublishResults(EjectorResult r, EjectorResult? curve, double mM, double mS, FluidState s0,
                                    double pTriple, int nFlash, double seconds)
        {
            _pOut.SetInternal(r.Pout);
            _er.SetInternal(mS / mM);
            _lift.SetInternal(r.Pout / s0.P);
            _regime.value = r.Regime.ToString();
            _pt.SetInternal(r.Pt);
            _pe.SetInternal(r.Pe);
            _py.SetInternal(r.Py);
            _p3.SetInternal(r.P3);
            _ue.SetInternal(r.Ue);
            _u3.SetInternal(r.U3);
            _etaEj.SetInternal(r.EtaEjector);
            _solid.SetInternal(pTriple > 0 ? r.SolidMargin : 0.0);
            _erCrit.SetInternal(double.IsNaN(r.ERCritical) ? 0.0 : r.ERCritical);
            _pbCrit.SetInternal(double.IsNaN(r.PbCritical) ? 0.0 : r.PbCritical);
            _mCap.SetInternal(double.IsNaN(r.MotiveCapacity) ? 0.0 : r.MotiveCapacity);
            _mMis.SetInternal(double.IsNaN(r.MotiveMismatch) ? 0.0 : r.MotiveMismatch);
            _xOut.SetInternal(r.Out?.Q ?? double.NaN);
            _nFlash.SetInternal(nFlash);
            _tCalc.SetInternal(seconds);
            // geometry: computed in Design and FixedShape (sized to the motive flow); FixedSize keeps its inputs
            if (!(IsRating() && IsFixedSize()))
            {
                _dT.SetInternal(Geometry.Diameter(r.At));
                _dE.SetInternal(Geometry.Diameter(r.Ae));
                _dMix.SetInternal(Geometry.Diameter(r.Amix));
            }
            if (!IsRating())
            {
                _rE.SetInternal(r.Ae / r.At);
                _rMix.SetInternal(r.Amix / r.At);
            }
            else if (IsFixedSize())
            {
                _rE.SetInternal(Math.Pow(_dE.ValueCore / _dT.ValueCore, 2));
                _rMix.SetInternal(Math.Pow(_dMix.ValueCore / _dT.ValueCore, 2));
            }
            var c = curve?.Curve ?? new List<(double Pout, double ER)>();
            _curveP.SetInternal(c.Select(p => p.Pout).ToArray());
            _curveEr.SetInternal(c.Select(p => p.ER).ToArray());
            if (IsRating() && IsFixedSize() && Math.Abs(r.MotiveMismatch) > 0.02)
                Diagnostics.Log(FormattableString.Invariant($"FixedSize: motive capacity differs from the port flow by {r.MotiveMismatch:P1}"));
        }

        private EffSet Efficiencies()
        {
            if (Presets.Values.TryGetValue(_preset.ValueCore, out var v)) return v;
            return new EffSet(_etaM.ValueCore, _etaS.ValueCore, _etaMix.ValueCore, _etaD.ValueCore, _cd.ValueCore,
                              _mixing.ValueCore, _phiP.ValueCore);
        }

        private bool IsRating() => _mode.ValueCore == ModeRating;
        private bool IsFixedSize() => _geometry.ValueCore == SizeFixed;

        /// <summary>Input/output direction of the geometry and efficiency parameters follows Mode, GeometrySpec
        /// and EfficiencySet; preset values are shown in the (read-only) efficiency parameters.
        /// Returns true if any direction changed.</summary>
        internal bool ApplyModes()
        {
            bool changed = false;
            void Set(RealParameter p, bool input)
            {
                var m = input ? CapeParamMode.CAPE_INPUT : CapeParamMode.CAPE_OUTPUT;
                if (p.Mode != m) { p.Mode = m; changed = true; }
            }
            bool rating = IsRating(), size = IsFixedSize();
            Set(_rE, rating && !size);
            Set(_rMix, rating && !size);
            Set(_dT, rating && size);
            Set(_dE, rating && size);
            Set(_dMix, rating && size);
            var gm = rating ? CapeParamMode.CAPE_INPUT : CapeParamMode.CAPE_OUTPUT;
            if (_geometry.Mode != gm) { _geometry.Mode = gm; changed = true; }
            bool custom = _preset.ValueCore == Presets.Custom;
            foreach (var p in new[] { _etaM, _etaS, _etaMix, _etaD, _cd, _phiP }) Set(p, custom);
            var mm = custom ? CapeParamMode.CAPE_INPUT : CapeParamMode.CAPE_OUTPUT;
            if (_mixing.Mode != mm) { _mixing.Mode = mm; changed = true; }
            if (!custom && Presets.Values.TryGetValue(_preset.ValueCore, out var v))
            {
                _etaM.SetInternal(v.EtaM); _etaS.SetInternal(v.EtaS); _etaMix.SetInternal(v.EtaMix);
                _etaD.SetInternal(v.EtaD); _cd.SetInternal(v.Cd); _phiP.SetInternal(v.PhiP);
                _mixing.SetInternal(v.Mixing);
            }
            return changed;
        }

        // =================== ECapeRoot / ECapeUser ===================
        public string name => _lastError?.name ?? "ECapeUnknown";
        public int code => _lastError?.code ?? unchecked((int)0x80040501);
        public string description => _lastError?.description ?? string.Empty;
        public string scope => _lastError?.scope ?? string.Empty;
        public string interfaceName => _lastError?.interfaceName ?? "ICapeUnit";
        public string operation => _lastError?.operation ?? string.Empty;
        public string moreInfo => _lastError?.moreInfo ?? "Set EJECTOR_CAPEOPEN_LOG to write ejector_capeopen.log next to the DLL.";

        // =================== ICapeIdentification ===================
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }

        // =================== ICapeUtilities ===================
        public void Initialize() { }

        public void Terminate()
        {
            _motive.Disconnect();
            _suction.Disconnect();
            _outlet.Disconnect();
            if (_simulationContext != null && Marshal.IsComObject(_simulationContext))
                Marshal.ReleaseComObject(_simulationContext);
            _simulationContext = null;
        }

        /// <summary>No custom dialog yet: keep the parameter directions consistent and let the PME show its
        /// own parameter grid. S_OK (0) if a direction changed so the PME re-reads the parameters.</summary>
        public int Edit() => ApplyModes() ? 0 : 1;

        public object parameters => _params;

        public object simulationContext
        {
            get => _simulationContext!;
            set
            {
                if (!ReferenceEquals(_simulationContext, value) &&
                    _simulationContext != null && Marshal.IsComObject(_simulationContext))
                {
                    try { Marshal.ReleaseComObject(_simulationContext); } catch { }
                }
                _simulationContext = value;
            }
        }

        // =================== ICapeUnitReport ===================
        public object reports => new[] { "Ejector summary" };
        public string selectedReport { get; set; } = "Ejector summary";

        public void ProduceReport(ref string message)
        {
            try
            {
                if (_lastData != null && HtmlReportSupported())
                {
                    message = HtmlReport.BuildHtml(_lastData);
                    return;
                }
            }
            catch (Exception ex) { Diagnostics.Log("HTML report failed, using text: " + ex.Message); }
            message = _lastReport;
        }

        private bool HtmlReportSupported()
        {
            try
            {
                if (_simulationContext is ICapeCOSEUtilities u)
                {
                    object v = u.get_NamedValue("HTMLReportSupport");
                    return v != null && Convert.ToBoolean(v);
                }
            }
            catch { }
            return false;
        }

        /// <summary>HTML report with the operating-curve chart, for hosts that can show it (and for tests).</summary>
        internal string? LastHtmlReport => _lastData != null ? HtmlReport.BuildHtml(_lastData) : null;
        internal string LastTextReport => _lastReport;

        // =================== IPersistStreamInit ===================
        public void GetClassID(out Guid pClassID) => pClassID = new Guid(EjectorUnitIdentity.Clsid);
        public int IsDirty() => 0;   // always dirty: grid edits change parameters without notifying the unit
        public void GetSizeMax(out long pcbSize) => pcbSize = 256 * 1024;
        public void InitNew() { }

        private IEnumerable<RealParameter> PersistedReals() => new[]
        {
            _rE, _rMix, _dT, _dE, _dMix, _etaM, _etaS, _etaMix, _etaD, _cd, _phi, _phiP, _curvePoints,
            _pOut, _er, _lift, _pt, _pe, _py, _p3, _ue, _u3, _etaEj, _solid, _erCrit, _pbCrit, _mCap, _mMis, _xOut, _nFlash, _tCalc,
        };

        private IEnumerable<OptionParameter> PersistedOptions() => new[] { _mode, _geometry, _preset, _motiveModel, _mixing, _regime };

        public void Save(IStream pStm, bool fClearDirty)
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                w.Write(PersistVersion);
                var opts = PersistedOptions().ToList();
                w.Write(opts.Count);
                foreach (var o in opts) { w.Write(o.ComponentName); w.Write(o.ValueCore); }
                var reals = PersistedReals().ToList();
                w.Write(reals.Count);
                foreach (var p in reals) { w.Write(p.ComponentName); w.Write(p.ValueCore); }
                foreach (var a in new[] { _curveP, _curveEr })
                {
                    var v = a.GetInternal();
                    w.Write(v.Length);
                    foreach (var d in v) w.Write(d);
                }
            }
            StreamPersistence.WriteAll(pStm, ms.ToArray());
        }

        public void Load(IStream pStm)
        {
            byte[] data = StreamPersistence.ReadAll(pStm);
            if (data.Length == 0) return;
            try
            {
                using var ms = new MemoryStream(data);
                using var r = new BinaryReader(ms, Encoding.UTF8);
                int ver = r.ReadInt32();
                if (ver < 1) return;
                var opts = PersistedOptions().ToDictionary(o => o.ComponentName, StringComparer.Ordinal);
                int no = r.ReadInt32();
                for (int i = 0; i < no; i++)
                {
                    string n = r.ReadString(), v = r.ReadString();
                    if (opts.TryGetValue(n, out var o)) { try { o.value = v; } catch { /* unknown option value */ } }
                }
                var reals = PersistedReals().ToDictionary(p => p.ComponentName, StringComparer.Ordinal);
                int nr = r.ReadInt32();
                for (int i = 0; i < nr; i++)
                {
                    string n = r.ReadString();
                    double v = r.ReadDouble();
                    if (reals.TryGetValue(n, out var p)) p.SetInternal(v);
                }
                foreach (var a in new[] { _curveP, _curveEr })
                {
                    int len = r.ReadInt32();
                    var v = new double[len];
                    for (int i = 0; i < len; i++) v[i] = r.ReadDouble();
                    a.SetInternal(v);
                }
                ApplyModes();
            }
            catch (Exception ex) { Diagnostics.Log("Load failed: " + ex.Message); }
        }
    }
}
