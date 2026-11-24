using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CAPEOPEN;
using EjectorCore;

namespace Ejector.CapeOpen
{
    /// <summary>
    /// <see cref="IFlash"/> on the PME's property package (techref sec-flash-port): every flash is a
    /// CAPE-OPEN 1.1 <c>CalcEquilibrium</c> on a private scratch material object (created with
    /// <c>CreateMaterial</c> from an inlet, so the PME's own streams are never touched), followed by phase
    /// densities, enthalpies and entropies from <c>CalcSinglePhaseProp</c>. Mixture (homogeneous) properties
    /// are assembled on a mass basis. CO 1.1 works in molar units; the conversion uses the compound molecular
    /// weights. If the package rejects a PS or PH specification, the state is found by a temperature search
    /// with TP flashes, with saturated end points for a pure fluid inside the two-phase region.
    /// </summary>
    internal sealed class PmeFlash : IFlash, IDisposable
    {
        private const string Mole = "mole";
        private readonly object _scratch;
        private readonly ICapeThermoMaterial _m;
        private readonly ICapeThermoEquilibriumRoutine _eq;
        private readonly ICapeThermoPropertyRoutine _props;
        private readonly double[] _mw;                    // kg/mol per compound, PME order
        private readonly string[] _phaseLabels;
        private readonly HashSet<string> _vapourPhases;
        private readonly string? _vapourLabel;
        private bool _psSpecWorks = true, _phSpecWorks = true;
        private double _lastT = double.NaN;

        /// <param name="scratch">A CO 1.1 material object the unit owns (from CreateMaterial).</param>
        /// <param name="molecularWeights">Compound molecular weights [kg/mol] in the PME compound order.</param>
        /// <param name="triplePointPressure">Triple-point pressure [Pa] of a pure fluid, else 0.</param>
        public PmeFlash(object scratch, double[] molecularWeights, double triplePointPressure)
        {
            _scratch = scratch;
            _m = scratch as ICapeThermoMaterial
                 ?? throw new FlashException("The scratch material does not implement ICapeThermoMaterial (CO 1.1 required).");
            _eq = scratch as ICapeThermoEquilibriumRoutine
                  ?? throw new FlashException("The material object does not implement ICapeThermoEquilibriumRoutine (flashes are required).");
            _props = scratch as ICapeThermoPropertyRoutine
                     ?? throw new FlashException("The material object does not implement ICapeThermoPropertyRoutine (phase properties are required).");
            _mw = molecularWeights;
            IsMixture = molecularWeights.Length > 1;
            TriplePointPressure = IsMixture ? 0.0 : triplePointPressure;

            // Phase list: labels and state of aggregation (vapour vs condensed).
            var labels = new List<string>();
            _vapourPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (scratch is ICapeThermoPhases ph)
            {
                object l = null!, agg = null!, key = null!;
                ph.GetPhaseList(ref l, ref agg, ref key);
                var ls = StreamAdapter.ToStringArray(l);
                var ag = StreamAdapter.ToStringArray(agg);
                for (int i = 0; i < ls.Length; i++)
                {
                    labels.Add(ls[i]);
                    if (i < ag.Length && ag[i].StartsWith("Vap", StringComparison.OrdinalIgnoreCase)) _vapourPhases.Add(ls[i]);
                }
            }
            _phaseLabels = labels.ToArray();
            foreach (var l in _phaseLabels) if (_vapourPhases.Contains(l)) { _vapourLabel = l; break; }
            Diagnostics.Log($"PmeFlash: phases=[{string.Join(",", _phaseLabels)}] vapour=[{string.Join(",", _vapourPhases)}] mw=[{string.Join(",", _mw)}]");
        }

        public bool IsMixture { get; }
        public double TriplePointPressure { get; }
        /// <summary>Labels of the vapour phases of the property package.</summary>
        public ISet<string> VapourPhases => _vapourPhases;
        /// <summary>Number of CalcEquilibrium calls made.</summary>
        public int CalcEquilibriumCount { get; private set; }

        public double MolarMass(double[]? z)
        {
            var x = Comp(z);
            double m = 0.0;
            for (int i = 0; i < x.Length; i++) m += x[i] * _mw[i];
            return m;
        }

        private double[] Comp(double[]? z)
        {
            if (z == null)
            {
                if (_mw.Length != 1) throw new FlashException("composition required for a mixture");
                return new[] { 1.0 };
            }
            if (z.Length != _mw.Length) throw new FlashException("composition length does not match the compound list");
            return z;
        }

        // ------------------------------------------------------------------ IFlash
        public FluidState PT(double P, double T, double[]? z)
        {
            var x = Comp(z);
            Prepare(P, x);
            _m.SetOverallProp("temperature", null, new[] { T });
            Equilibrium(new[] { "temperature", null!, "Overall" });
            return Read(P, x, z, null);
        }

        public FluidState PH(double P, double h, double[]? z)
        {
            var x = Comp(z);
            if (_phSpecWorks)
            {
                try
                {
                    Prepare(P, x);
                    _m.SetOverallProp("enthalpy", Mole, new[] { h * MolarMass(x) });
                    Equilibrium(new[] { "enthalpy", null!, "Overall" });
                    return Read(P, x, z, h);
                }
                catch (FlashException ex)
                {
                    Diagnostics.Log("PmeFlash: PH specification failed (" + ex.Message + "); temperature search");
                    var st = ByTemperature(P, x, z, h, isEnthalpy: true);
                    if (++_phRescued >= 3) _phSpecWorks = false;   // the package does not do PH: stop asking
                    return st;
                }
            }
            return ByTemperature(P, x, z, h, isEnthalpy: true);
        }

        public FluidState PS(double P, double s, double[]? z)
        {
            var x = Comp(z);
            if (_psSpecWorks)
            {
                try
                {
                    Prepare(P, x);
                    _m.SetOverallProp("entropy", Mole, new[] { s * MolarMass(x) });
                    Equilibrium(new[] { "entropy", null!, "Overall" });
                    return Read(P, x, z, null);
                }
                catch (FlashException ex)
                {
                    Diagnostics.Log("PmeFlash: PS specification failed (" + ex.Message + "); temperature search");
                    var st = ByTemperature(P, x, z, s, isEnthalpy: false);
                    if (++_psRescued >= 3) _psSpecWorks = false;
                    return st;
                }
            }
            return ByTemperature(P, x, z, s, isEnthalpy: false);
        }

        public FluidState PQ(double P, double q, double[]? z)
        {
            var x = Comp(z);
            if (_vapourLabel == null) throw new FlashException("the property package reports no vapour phase (PQ flash impossible)");
            Prepare(P, x);
            _m.SetSinglePhaseProp("phaseFraction", _vapourLabel, Mole, new[] { q });
            Equilibrium(new[] { "phaseFraction", Mole, _vapourLabel });
            return Read(P, x, z, null);
        }

        // ------------------------------------------------------------------ internals
        private int _phRescued, _psRescued;

        private void Prepare(double P, double[] x)
        {
            _m.ClearAllProps();
            if (_phaseLabels.Length > 0)
            {
                try
                {
                    var status = new int[_phaseLabels.Length];   // CAPE_UNKNOWNPHASESTATUS: let the flash decide
                    _m.SetPresentPhases(_phaseLabels, status);
                }
                catch (Exception ex) { Diagnostics.Log("PmeFlash: SetPresentPhases failed: " + ex.Message); }
            }
            _m.SetOverallProp("pressure", null, new[] { P });
            _m.SetOverallProp("fraction", Mole, x);
            _m.SetOverallProp("totalFlow", Mole, new[] { 1.0 });
        }

        private void Equilibrium(string[] spec2)
        {
            CalcEquilibriumCount++;
            try
            {
                _eq.CalcEquilibrium(new[] { "pressure", null!, "Overall" }, spec2, "Unspecified");
            }
            catch (Exception ex)
            {
                throw new FlashException($"CalcEquilibrium(pressure, {spec2[0]}) failed: {ex.Message}", ex);
            }
        }

        /// <summary>Homogeneous mixture state from the phases present after a flash.</summary>
        private FluidState Read(double P, double[] x, double[]? z, double? hGiven)
        {
            try
            {
                double t = 0.0, p = 0.0; object comp = null!;
                _m.GetOverallTPFraction(ref t, ref p, ref comp);
                object labelsObj = null!, statusObj = null!;
                _m.GetPresentPhases(ref labelsObj, ref statusObj);
                var phases = StreamAdapter.ToStringArray(labelsObj);
                if (phases.Length == 0) throw new FlashException("no phases present after the flash");

                double mOv = 0.0, hOv = 0.0, sOv = 0.0;
                var beta = new double[phases.Length];
                var mPh = new double[phases.Length];
                var rhoMass = new double[phases.Length];
                for (int k = 0; k < phases.Length; k++)
                {
                    string ph = phases[k];
                    beta[k] = 1.0;
                    if (phases.Length > 1)
                    {
                        object b = null!;
                        _m.GetSinglePhaseProp("phaseFraction", ph, Mole, ref b);
                        beta[k] = StreamAdapter.First(b);
                    }
                    double[] xk = x;
                    if (_mw.Length > 1)
                    {
                        object xo = null!;
                        _m.GetSinglePhaseProp("fraction", ph, Mole, ref xo);
                        xk = StreamAdapter.ToDoubleArray(xo);
                    }
                    for (int i = 0; i < xk.Length; i++) mPh[k] += xk[i] * _mw[i];
                    if (beta[k] <= 0.0) continue;
                    _props.CalcSinglePhaseProp(new[] { "density", "enthalpy", "entropy" }, ph);
                    object d = null!, hh = null!, ss = null!;
                    _m.GetSinglePhaseProp("density", ph, Mole, ref d);
                    _m.GetSinglePhaseProp("enthalpy", ph, Mole, ref hh);
                    _m.GetSinglePhaseProp("entropy", ph, Mole, ref ss);
                    rhoMass[k] = StreamAdapter.First(d) * mPh[k];
                    mOv += beta[k] * mPh[k];
                    hOv += beta[k] * StreamAdapter.First(hh);
                    sOv += beta[k] * StreamAdapter.First(ss);
                }
                if (!(mOv > 0)) throw new FlashException("phase fractions sum to zero");

                double vOv = 0.0, wV = 0.0, vV = 0.0, wL = 0.0, vL = 0.0;
                for (int k = 0; k < phases.Length; k++)
                {
                    if (beta[k] <= 0.0) continue;
                    double w = beta[k] * mPh[k] / mOv;
                    vOv += w / rhoMass[k];
                    if (_vapourPhases.Contains(phases[k])) { wV += w; vV += w / rhoMass[k]; }
                    else { wL += w; vL += w / rhoMass[k]; }
                }
                var st = new FluidState
                {
                    P = P, T = t, H = hGiven ?? hOv / mOv, S = sOv / mOv, Rho = 1.0 / vOv, Z = z,
                };
                if (wV > 0 && wL > 0)
                {
                    st.Q = wV;
                    st.RhoV = wV / vV;
                    st.RhoL = wL / vL;
                }
                else
                {
                    bool vap = false;
                    foreach (var ph in phases) if (_vapourPhases.Contains(ph)) vap = true;
                    st.Q = vap ? 2.0 : -1.0;
                }
                _lastT = t;
                return st;
            }
            catch (FlashException) { throw; }
            catch (Exception ex) { throw new FlashException("reading the flash result failed: " + ex.Message, ex); }
        }

        /// <summary>
        /// PH/PS without the PME specification: find T with h(T,P) (or s) equal to the target by TP flashes.
        /// For a pure fluid the property jumps at the saturation temperature; a target inside the jump is a
        /// two-phase state, built from the saturated end points (PQ flashes) on a mass basis.
        /// </summary>
        private FluidState ByTemperature(double P, double[] x, double[]? z, double target, bool isEnthalpy)
        {
            double Prop(FluidState s) => isEnthalpy ? s.H : s.S;
            if (!IsMixture && _vapourLabel != null)
            {
                FluidState? l = null, v = null;
                try { l = PQ(P, 0.0, z); v = PQ(P, 1.0, z); } catch (FlashException) { /* above critical */ }
                if (l != null && v != null && Prop(l) < Prop(v) && target >= Prop(l) && target <= Prop(v))
                {
                    double w = (target - Prop(l)) / (Prop(v) - Prop(l));
                    double vl = 1.0 / l.Rho, vv = 1.0 / v.Rho;
                    return new FluidState
                    {
                        P = P, T = l.T, Z = z, Q = w, RhoL = l.Rho, RhoV = v.Rho,
                        H = isEnthalpy ? target : l.H + w * (v.H - l.H),
                        S = isEnthalpy ? l.S + w * (v.S - l.S) : target,
                        Rho = 1.0 / ((1 - w) * vl + w * vv),
                    };
                }
            }
            double t0 = double.IsNaN(_lastT) ? 300.0 : _lastT;
            double F(double t) => Prop(PT(P, t, z)) - target;
            double lo = t0, hi = t0, flo = F(lo), fhi = flo;
            double step = 10.0;
            for (int i = 0; i < 60 && flo * fhi > 0; i++)
            {
                if (flo > 0) { hi = lo; fhi = flo; lo = Math.Max(lo - step, 1.0); flo = F(lo); }
                else { lo = hi; flo = fhi; hi += step; fhi = F(hi); }
                step *= 1.6;
            }
            if (flo * fhi > 0) throw new FlashException("temperature search could not bracket the " + (isEnthalpy ? "PH" : "PS") + " state");
            double tSol = Numerics.Brentq(F, lo, hi, 1e-8);
            var st = PT(P, tSol, z);
            if (isEnthalpy) st.H = target;
            return st;
        }

        public void Dispose()
        {
            if (Marshal.IsComObject(_scratch))
            {
                try { Marshal.ReleaseComObject(_scratch); } catch { }
            }
        }
    }
}
