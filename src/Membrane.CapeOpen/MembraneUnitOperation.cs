using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CAPEOPEN;
using MembraneCore.Energy;
using MembraneCore.Fugacity;
using MembraneCore.Models;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// CAPE-OPEN 1.0 gas-permeation membrane unit operation (cross-flow, solution-diffusion, isothermal).
    /// One material inlet (Feed) and two material outlets (Retentate, Permeate). Physics is delegated to
    /// the validated <see cref="CrossFlowModel"/>; thermodynamics (flashes) are delegated to the PME's
    /// Material Objects. Per-component permeances and operating conditions are exposed as public unit
    /// parameters and edited through the flowsheet's parameter grid.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(MembraneUnitIdentity.Clsid)]
    [ProgId(MembraneUnitIdentity.ProgId)]
    [ComDefaultInterface(typeof(ICapeUnit))]
    public class MembraneUnitOperation :
        ICapeUnit, ICapeIdentification, ICapeUtilities, ICapeUnitReport, IPersistStreamInit
    {
        private const int PersistVersion = 7;   // v2 StageCut; v3 profiles; v4 collected; v5 SpecMode; v6 energy; v7 driving force
        private const string CrossFlow = "CrossFlow";
        private const string CounterCurrent = "CounterCurrent";
        private const string CoCurrent = "CoCurrent";
        private const string SpecArea = "Area";
        private const string SpecStageCut = "StageCut";
        private const string EnergyIsothermal = "Isothermal";
        private const string EnergyAdiabatic = "Adiabatic";
        private const string DrivingFugacity = "Fugacity";
        private const string DrivingPartial = "PartialPressure";
        private const string DrivingFugacityLocal = "FugacityLocal";

        // CAPE-OPEN dimensionality vectors, order [m, kg, s, A, K, mol] (spec: Watt = [2,1,-3,0,0,0]).
        private static readonly double[] DimPressure = { -1, 1, -2, 0, 0, 0 };            // Pa = kg·m⁻¹·s⁻²
        private static readonly double[] DimArea = { 2, 0, 0, 0, 0, 0 };                  // m²
        private static readonly double[] DimPermeance = { -1, -1, 1, 0, 0, 1 };           // mol·m⁻²·s⁻¹·Pa⁻¹
        private static readonly double[] DimTemperature = { 0, 0, 0, 0, 1, 0 };           // K

        // Ports.
        private readonly MaterialPort _feed = new MaterialPort("Feed", CapePortDirection.CAPE_INLET, "Feed gas stream");
        private readonly MaterialPort _retentate = new MaterialPort("Retentate", CapePortDirection.CAPE_OUTLET, "Retentate (residue) stream at feed pressure");
        private readonly MaterialPort _permeate = new MaterialPort("Permeate", CapePortDirection.CAPE_OUTLET, "Permeate stream at permeate pressure");
        private CapeCollection _ports = new CapeCollection();

        // Fixed parameters.
        private readonly RealParameter _permeatePressure =
            new RealParameter("PermeatePressure", "Permeate-side pressure", 1.0e5, 1.0, 5.0e7, CapeParamMode.CAPE_INPUT, DimPressure);
        private readonly RealParameter _membraneArea =
            new RealParameter("MembraneArea", "Total membrane area", 100.0, 1e-6, 1e9, CapeParamMode.CAPE_INPUT, DimArea);
        private readonly OptionParameter _flowPattern =
            new OptionParameter("FlowPattern",
                "Flow configuration: CrossFlow = realistic spiral-wound (default); CounterCurrent = best-case bound / hollow-fibre; CoCurrent = worst-case bound",
                CrossFlow, new[] { CrossFlow, CounterCurrent, CoCurrent }, CapeParamMode.CAPE_INPUT);
        private readonly OptionParameter _specMode =
            new OptionParameter("SpecMode",
                "Which variable is specified: Area = rating (given area -> compute stage cut); StageCut = design (given target stage cut -> compute required area). The computed one becomes read-only.",
                SpecArea, new[] { SpecArea, SpecStageCut }, CapeParamMode.CAPE_INPUT);
        private readonly RealParameter _stageCut =
            new RealParameter("StageCut", "Overall stage cut (permeate/feed)", 0.0, 0.0, 1.0, CapeParamMode.CAPE_OUTPUT);
        private readonly OptionParameter _energyMode =
            new OptionParameter("EnergyMode",
                "Energy balance: Isothermal = outlets at feed temperature (default); Adiabatic = PME-delegated enthalpy balance with Joule-Thomson cooling of the permeate (separation unchanged)",
                EnergyIsothermal, new[] { EnergyIsothermal, EnergyAdiabatic }, CapeParamMode.CAPE_INPUT);
        private readonly OptionParameter _drivingForce =
            new OptionParameter("DrivingForce",
                "Flux driving force: Fugacity = real-gas fugacity difference from the PME EOS (default; feed-evaluated coefficients, constant along the module); FugacityLocal = fugacity coefficients updated along the flow direction via a stage-cut table + interpolation (more PME calls, closer to a fully EOS-coupled model); PartialPressure = ideal-gas partial-pressure difference. Fugacity modes fall back to PartialPressure if the property package cannot supply fugacity coefficients.",
                DrivingFugacity, new[] { DrivingFugacity, DrivingFugacityLocal, DrivingPartial }, CapeParamMode.CAPE_INPUT);
        private readonly RealParameter _retentateTemperature =
            new RealParameter("RetentateTemperature", "Retentate outlet temperature", 0.0, 0.0, 1.0e5, CapeParamMode.CAPE_OUTPUT, DimTemperature);
        private readonly RealParameter _permeateTemperature =
            new RealParameter("PermeateTemperature", "Permeate outlet temperature", 0.0, 0.0, 1.0e5, CapeParamMode.CAPE_OUTPUT, DimTemperature);

        // Position-dependent profiles (array output parameters COFE can plot), sampled at ProfilePoints.
        private const int ProfilePoints = 100;
        private readonly ArrayParameter _positionProfile =
            new ArrayParameter("Profile_Position", "Membrane position (0 = feed end, 1 = retentate outlet)", ProfilePoints);
        private readonly ArrayParameter _stageCutProfile =
            new ArrayParameter("Profile_StageCut", "Cumulative stage cut vs position", ProfilePoints);
        private readonly ArrayParameter _retentateTempProfile =
            new ArrayParameter("Profile_RetentateTemperature", "Retentate temperature (K) vs position (Adiabatic mode)", ProfilePoints);
        private readonly ArrayParameter _permeateTempProfile =
            new ArrayParameter("Profile_PermeateTemperature", "Permeate temperature (K) vs position (Adiabatic mode)", ProfilePoints);
        private readonly Dictionary<string, ArrayParameter> _retentateProfileParams =
            new Dictionary<string, ArrayParameter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ArrayParameter> _permeateProfileParams =
            new Dictionary<string, ArrayParameter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ArrayParameter> _permeateCollectedProfileParams =
            new Dictionary<string, ArrayParameter>(StringComparer.OrdinalIgnoreCase);

        // Per-compound permeance parameters (mol·m⁻²·s⁻¹·Pa⁻¹), keyed by compound id, discovered from the feed.
        private readonly Dictionary<string, RealParameter> _permeanceParams =
            new Dictionary<string, RealParameter>(StringComparer.OrdinalIgnoreCase);
        // Permeance values restored from persistence before the compounds are known.
        private readonly Dictionary<string, double> _savedPermeances =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        // Restored profile arrays (per compound) applied when the profile parameters are (re)created.
        private readonly Dictionary<string, double[]> _savedRetentateProfile =
            new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double[]> _savedPermeateProfile =
            new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double[]> _savedPermeateCollectedProfile =
            new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);

        private CapeCollection _params = new CapeCollection();
        private object? _simulationContext;
        private CapeValidationStatus _valStatus = CapeValidationStatus.CAPE_NOT_VALIDATED;
        private bool _dirty;
        private string _lastReport = "No calculation has been performed yet.";
        private FeedState? _lastFeed;
        private MembraneCore.MembraneResult? _lastResult;
        private double _lastPr, _lastPp;
        private string _lastFlowPattern = CrossFlow;

        static MembraneUnitOperation()
        {
            // Ensure sibling managed DLLs (MembraneCore) resolve when hosted by a PME. The static ctor runs
            // before any instance is created and before Calculate() JITs its MembraneCore references.
            AssemblyResolver.Ensure();
        }

        public MembraneUnitOperation()
        {
            ComponentName = MembraneUnitIdentity.Name;
            ComponentDescription = MembraneUnitIdentity.Description;
            BuildPorts();
            RebuildParameters();
        }

        // =================== ICapeUnit ===================

        public object ports => _ports;

        public CapeValidationStatus ValStatus => _valStatus;

        public bool Validate(ref string message)
        {
            Diagnostics.Log("Validate: entry");
            try
            {
                // All material ports must be connected.
                if (_feed.ConnectedObject == null) return Invalid(ref message, "Feed port is not connected.");
                if (_retentate.ConnectedObject == null) return Invalid(ref message, "Retentate port is not connected.");
                if (_permeate.ConnectedObject == null) return Invalid(ref message, "Permeate port is not connected.");

                // Discover compounds from the feed and ensure a permeance parameter exists for each.
                // Read ONLY the compound list here — never stream state (T/P/composition), which may be
                // undefined until the flowsheet is solved. Reading state in Validate throws ECapeUnknown.
                var ids = MaterialObjectAdapter.ReadComponentIds(_feed.ConnectedObject!);
                EnsurePermeanceParameters(ids);
                ApplySpecMode();

                // Parameter checks.
                if (_permeatePressure.ValueCore <= 0.0) return Invalid(ref message, "Permeate pressure must be positive.");
                if (IsStageCutSpec())
                {
                    if (_stageCut.ValueCore <= 0.0 || _stageCut.ValueCore >= 1.0)
                        return Invalid(ref message, "In StageCut spec mode the target stage cut must be between 0 and 1.");
                }
                else if (_membraneArea.ValueCore <= 0.0)
                {
                    return Invalid(ref message, "Membrane area must be positive.");
                }

                bool anyPermeance = false;
                var seen = new StringBuilder("Validate: permeances ");
                foreach (var id in ids)
                {
                    if (!_permeanceParams.TryGetValue(id, out var p))
                        return Invalid(ref message, $"Missing permeance for compound '{id}'.");
                    if (p.ValueCore < 0.0) return Invalid(ref message, $"Permeance for '{id}' is negative.");
                    if (p.ValueCore > 0.0) anyPermeance = true;
                    seen.Append($"{id}={p.ValueCore:E3} ");
                }
                Diagnostics.Log(seen.ToString());
                if (!anyPermeance)
                    return Invalid(ref message, "At least one component permeance must be greater than zero (set them in the parameter grid).");

                message = string.Empty;
                _valStatus = CapeValidationStatus.CAPE_VALID;
                Diagnostics.Log("Validate -> VALID");
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

        public void Calculate()
        {
            Diagnostics.Log($"Calculate: entry (valStatus={_valStatus})");
            try
            {
                if (_feed.ConnectedObject == null || _retentate.ConnectedObject == null || _permeate.ConnectedObject == null)
                    throw ComError.SolvingError("All three material ports (Feed, Retentate, Permeate) must be connected.");

                var feed = MaterialObjectAdapter.ReadFeed(_feed.ConnectedObject!);
                Diagnostics.Log("Calculate: feed read OK");

                // Re-seed permeance parameters from the feed compounds (safe on a fresh/restored instance
                // that COFE may create for the solve without a preceding Edit/Validate).
                EnsurePermeanceParameters(feed.ComponentIds);

                double pr = feed.Pressure;
                double pp = _permeatePressure.ValueCore;
                if (pp >= pr)
                    throw ComError.SolvingError($"Permeate pressure ({pp:F0} Pa) must be below feed pressure ({pr:F0} Pa).");

                // Build the permeance array aligned to the feed's own component order (positional safety).
                var permeance = new double[feed.ComponentIds.Length];
                bool anyPermeance = false;
                for (int i = 0; i < feed.ComponentIds.Length; i++)
                {
                    if (!_permeanceParams.TryGetValue(feed.ComponentIds[i], out var p))
                        throw ComError.SolvingError($"No permeance configured for compound '{feed.ComponentIds[i]}'.");
                    permeance[i] = p.ValueCore;
                    if (p.ValueCore > 0.0) anyPermeance = true;
                }
                if (!anyPermeance)
                    throw ComError.SolvingError("All component permeances are zero — set them in the parameter grid.");
                Diagnostics.Log($"Calculate: pr={pr:F0}Pa pp={pp:F0}Pa area={_membraneArea.ValueCore:E3}m2 perm=[{string.Join(",", permeance)}]");

                string fp = _flowPattern.value?.ToString() ?? CrossFlow;

                // Real-gas driving force: feed-evaluated fugacity coefficients (null ⇒ ideal partial pressure);
                // in FugacityLocal mode also build a φ(θ) table for position-dependent coefficients (cross-flow).
                double[]? aRet = null, bPerm = null;
                FugacityTable? phiTable = null;
                if (IsAnyFugacity()) TryFugacityCoefficients(feed, pr, pp, out aRet, out bPerm);
                if (IsFugacityLocal())
                {
                    if (fp == CrossFlow)
                    {
                        phiTable = BuildLocalPhiTable(feed, permeance, pr, pp);
                        if (phiTable == null) Diagnostics.Log("FugacityLocal: table unavailable; using constant feed-phi.");
                    }
                    else Diagnostics.Log("FugacityLocal is CrossFlow-only; using constant feed-phi for " + fp + ".");
                }

                // Determine the membrane area from the spec mode (Area = rating; StageCut = design), then solve.
                double area;
                if (IsStageCutSpec())
                {
                    double target = _stageCut.ValueCore;
                    area = RequiredArea(feed, permeance, pr, pp, fp, target, aRet, bPerm, phiTable);
                    _membraneArea.SetInternal(area);   // computed output
                    Diagnostics.Log($"Calculate: StageCut spec, target theta={target:F4} -> required area={area:E4} m2");
                }
                else
                {
                    area = _membraneArea.ValueCore;
                }
                var result = SolveAt(feed, permeance, pr, pp, fp, area, ProfilePoints, aRet, bPerm, phiTable);
                Diagnostics.Log($"Calculate: [{fp}] area={area:E4}m2 solved theta={result.StageCut:F4} mbRes={result.MassBalanceResidual:E2}");

                // Optional adiabatic energy balance -> outlet temperatures. The separation is unchanged;
                // this only assigns temperatures (permeate carries the Joule-Thomson drop). Falls back to
                // isothermal (feed temperature) if the PME cannot deliver enthalpies.
                double retT = feed.Temperature, permT = feed.Temperature;
                EnergyResult? energy = null;
                if (IsAdiabatic())
                {
                    energy = TryComputeEnergy(feed, result, pr, pp);
                    if (energy != null) { retT = energy.RetentateTemperature; permT = energy.PermeateTemperature; }
                }

                // Write products: retentate at feed pressure, permeate at permeate pressure.
                double retFlow = feed.MolarFlow * (1.0 - result.StageCut);
                double permFlow = feed.MolarFlow * result.StageCut;
                MaterialObjectAdapter.WriteStream(_retentate.ConnectedObject!, result.RetentateComposition, retFlow, pr, retT);
                Diagnostics.Log("Calculate: retentate written");
                MaterialObjectAdapter.WriteStream(_permeate.ConnectedObject!, result.PermeateComposition, permFlow, pp, permT);
                Diagnostics.Log("Calculate: permeate written");

                _retentateTemperature.SetInternal(retT);
                _permeateTemperature.SetInternal(permT);

                if (!IsStageCutSpec()) _stageCut.SetInternal(result.StageCut);   // in StageCut spec it is the input target

                // Publish the position-dependent profiles into the (plottable) array output parameters.
                var prof = result.Profile;
                if (prof != null)
                {
                    _positionProfile.SetInternal(prof.Position);
                    _stageCutProfile.SetInternal(prof.StageCut);
                    for (int i = 0; i < feed.ComponentIds.Length; i++)
                    {
                        if (_retentateProfileParams.TryGetValue(feed.ComponentIds[i], out var rp)) rp.SetInternal(prof.Retentate[i]);
                        if (_permeateProfileParams.TryGetValue(feed.ComponentIds[i], out var pp2)) pp2.SetInternal(prof.Permeate[i]);
                        if (_permeateCollectedProfileParams.TryGetValue(feed.ComponentIds[i], out var cp)) cp.SetInternal(prof.PermeateCollected[i]);
                    }
                }

                // Temperature profiles (Adiabatic mode only; zero-filled otherwise).
                _retentateTempProfile.SetInternal(energy?.RetentateTemperatureProfile ?? new double[ProfilePoints]);
                _permeateTempProfile.SetInternal(energy?.PermeateTemperatureProfile ?? new double[ProfilePoints]);

                _lastFeed = feed; _lastResult = result; _lastPr = pr; _lastPp = pp; _lastFlowPattern = fp;
                _lastReport = BuildReport(feed, result, pr, pp);
                Diagnostics.Log($"Calculate -> OK: theta={result.StageCut:F4}");
            }
            catch (CapeError) { throw; }
            catch (Exception ex)
            {
                Diagnostics.Log("Calculate failed: " + ex);
                throw ComError.SolvingError("Membrane calculation failed: " + ex.Message);
            }
        }

        // =================== ICapeIdentification ===================

        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }

        // =================== ICapeUtilities ===================

        public void Initialize()
        {
            // Ports/parameters were built in the constructor; nothing external to acquire.
            Diagnostics.Log("Initialize");
        }

        public void Terminate()
        {
            // Release references to PME-owned objects.
            _feed.Disconnect();
            _retentate.Disconnect();
            _permeate.Disconnect();
            if (_simulationContext != null && Marshal.IsComObject(_simulationContext))
                Marshal.ReleaseComObject(_simulationContext);
            _simulationContext = null;
            Diagnostics.Log("Terminate");
        }

        public int Edit()
        {
            // No bespoke GUI yet; configuration is via the flowsheet parameter grid. But this is the
            // spec-compliant place (Edit/Initialize) to change the parameter collection — so if a feed is
            // connected, discover its compounds and expose a permeance parameter for each. Returning S_OK
            // when the collection changed prompts the PME to re-read the parameters.
            try
            {
                ApplySpecMode();   // reflect any SpecMode change in the Area/StageCut read/write modes
                if (_feed.ConnectedObject != null)
                    EnsurePermeanceParameters(MaterialObjectAdapter.ReadComponentIds(_feed.ConnectedObject));
                // Return S_OK so the PME re-reads the parameter collection and updated modes.
                return 0;
            }
            catch (Exception ex) { Diagnostics.Log("Edit discovery failed: " + ex.Message); return 1; }
        }

        public object parameters => _params;

        public object simulationContext
        {
            get => _simulationContext!;
            set
            {
                // Release the previously-held context before replacing (COM lifetime; Field Note #3).
                if (!ReferenceEquals(_simulationContext, value) &&
                    _simulationContext != null && Marshal.IsComObject(_simulationContext))
                {
                    try { Marshal.ReleaseComObject(_simulationContext); } catch { }
                }
                _simulationContext = value;
            }
        }

        // =================== ICapeUnitReport ===================

        public object reports => new[] { "Membrane summary" };
        public string selectedReport { get; set; } = "Membrane summary";

        public void ProduceReport(ref string message)
        {
            // Prefer a visual HTML report (with an SVG profile chart) when the PME renders HTML; else plain text.
            try
            {
                if (_lastResult?.Profile != null && _lastFeed != null && HtmlReportSupported())
                {
                    message = HtmlReport.Build(_lastFeed, _lastResult, _lastPr, _lastPp, _lastFlowPattern);
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
            catch { /* named value not available */ }
            return false;
        }

        // =================== spec-mode (rating vs design) helpers ===================

        private bool IsStageCutSpec() => (_specMode.value?.ToString() ?? SpecArea) == SpecStageCut;

        private bool IsAdiabatic() => (_energyMode.value?.ToString() ?? EnergyIsothermal) == EnergyAdiabatic;

        /// <summary>
        /// Adiabatic energy balance -> outlet temperatures, delegating enthalpy to the PME. Returns null
        /// (isothermal fallback) if the feed is not CO 1.1 or the property package cannot deliver enthalpies.
        /// </summary>
        private EnergyResult? TryComputeEnergy(FeedState feed, MembraneCore.MembraneResult result, double pr, double pp)
        {
            try
            {
                using var provider = EnthalpyProvider.Create(_feed.ConnectedObject!);
                if (provider == null)
                {
                    Diagnostics.Log("Energy: feed is not a CO 1.1 material; isothermal fallback.");
                    return null;
                }
                if (!provider.TryProbe(feed.Temperature, pr, feed.MoleFractions, out var reason))
                {
                    Diagnostics.Log("Energy: enthalpy probe failed (" + reason + "); isothermal fallback.");
                    return null;
                }
                var e = NonIsothermalEnergy.Solve(provider, feed.Temperature, pr, pp,
                    feed.MolarFlow, feed.MoleFractions, result, includeProfile: true);
                Diagnostics.Log($"Energy: Tr={e.RetentateTemperature:F2}K Tp={e.PermeateTemperature:F2}K resid={e.EnergyBalanceResidual:E2}");
                return e;
            }
            catch (Exception ex)
            {
                Diagnostics.Log("Energy computation failed (" + ex.Message + "); isothermal fallback.");
                return null;
            }
        }

        /// <summary>Sets Area/StageCut parameter modes so the PME greys out the computed one.</summary>
        private void ApplySpecMode()
        {
            bool sc = IsStageCutSpec();
            _membraneArea.Mode = sc ? CapeParamMode.CAPE_OUTPUT : CapeParamMode.CAPE_INPUT;
            _stageCut.Mode = sc ? CapeParamMode.CAPE_INPUT : CapeParamMode.CAPE_OUTPUT;
        }

        private string DrivingMode() => _drivingForce.value?.ToString() ?? DrivingFugacity;
        private bool IsAnyFugacity() { var m = DrivingMode(); return m == DrivingFugacity || m == DrivingFugacityLocal; }
        private bool IsFugacityLocal() => DrivingMode() == DrivingFugacityLocal;

        /// <summary>
        /// Builds a φ(θ) table for the local (position-dependent) fugacity driving force: a first cross-flow
        /// pass gives the composition trajectory x(θ), y_coll(θ) (area-independent), and the PME is evaluated
        /// at a coarse set of stage-cut breakpoints so the marching solver can interpolate φ cheaply at every
        /// step. Returns null (⇒ constant feed-φ fallback) if the property package cannot deliver φ.
        /// </summary>
        private FugacityTable? BuildLocalPhiTable(FeedState feed, double[] permeance, double pr, double pp)
        {
            try
            {
                using var provider = EnthalpyProvider.Create(_feed.ConnectedObject!);
                if (provider == null) return null;
                // Trajectory to a high stage cut (φ-vs-θ is area-independent for cross-flow), ideal φ is fine here.
                double areaHi = RequiredArea(feed, permeance, pr, pp, CrossFlow, 0.90);
                var prof = CrossFlowModel.SolveByArea(feed.MoleFractions, feed.MolarFlow, permeance, pr, pp,
                    areaHi, profilePoints: 40).Profile;
                if (prof == null) return null;

                int nc = feed.ComponentIds.Length;
                var thetas = new List<double>();
                var aRows = new List<double[]>();
                var bRows = new List<double[]>();
                double last = -1.0;
                for (int k = 0; k < prof.Points; k++)
                {
                    double th = prof.StageCut[k];
                    if (th <= last + 1e-4) continue;   // keep strictly ascending, coarse
                    var x = new double[nc];
                    var yc = new double[nc];
                    for (int i = 0; i < nc; i++) { x[i] = prof.Retentate[i][k]; yc[i] = prof.PermeateCollected[i][k]; }
                    if (!provider.TryFugacityCoefficients(feed.Temperature, pr, NormalizeFractions(x), out var aPhi)) return null;
                    if (!provider.TryFugacityCoefficients(feed.Temperature, pp, NormalizeFractions(yc), out var bPhi)) return null;
                    thetas.Add(th); aRows.Add(aPhi); bRows.Add(bPhi); last = th;
                }
                if (thetas.Count < 2) return null;
                Diagnostics.Log($"FugacityLocal: built phi(theta) table, {thetas.Count} breakpoints up to theta={last:F3}.");
                return new FugacityTable(thetas.ToArray(), aRows.ToArray(), bRows.ToArray());
            }
            catch (Exception ex) { Diagnostics.Log("FugacityLocal table build failed (" + ex.Message + "); constant-phi fallback."); return null; }
        }

        private static double[] NormalizeFractions(double[] v)
        {
            double s = 0.0; foreach (var t in v) s += t > 0.0 ? t : 0.0;
            var r = new double[v.Length];
            if (s <= 0.0) return r;
            for (int i = 0; i < v.Length; i++) r[i] = (v[i] > 0.0 ? v[i] : 0.0) / s;
            return r;
        }

        /// <summary>
        /// Feed-evaluated real-gas fugacity coefficients for the driving force: retentate-side a_i = φ_i(T,p_r,z)
        /// and permeate-side b_i = φ_i(T,p_p,z), held constant along the module (first-order correction).
        /// Leaves a/b null (⇒ ideal partial-pressure driving force) if the PME cannot deliver them.
        /// </summary>
        private void TryFugacityCoefficients(FeedState feed, double pr, double pp, out double[]? a, out double[]? b)
        {
            a = null; b = null;
            try
            {
                using var provider = EnthalpyProvider.Create(_feed.ConnectedObject!);
                if (provider == null) { Diagnostics.Log("Fugacity: feed not CO 1.1; partial-pressure fallback."); return; }
                if (provider.TryFugacityCoefficients(feed.Temperature, pr, feed.MoleFractions, out var pa) &&
                    provider.TryFugacityCoefficients(feed.Temperature, pp, feed.MoleFractions, out var pb))
                {
                    a = pa; b = pb;
                    Diagnostics.Log($"Fugacity coeffs: retentate=[{string.Join(",", pa)}] permeate=[{string.Join(",", pb)}]");
                }
                else Diagnostics.Log("Fugacity coefficients unavailable; partial-pressure fallback.");
            }
            catch (Exception ex) { Diagnostics.Log("Fugacity setup failed (" + ex.Message + "); partial-pressure fallback."); }
        }

        private MembraneCore.MembraneResult SolveAt(FeedState feed, double[] permeance, double pr, double pp,
                                                    string fp, double area, int profilePoints,
                                                    double[]? a = null, double[]? b = null, FugacityTable? phiTable = null)
        {
            if (fp == CounterCurrent)
                return PlugFlowModel.SolveByArea(feed.MoleFractions, feed.MolarFlow, permeance, pr, pp,
                    MembraneCore.FlowPattern.CounterCurrent, area, profilePoints: profilePoints, a: a, b: b);
            if (fp == CoCurrent)
                return PlugFlowModel.SolveByArea(feed.MoleFractions, feed.MolarFlow, permeance, pr, pp,
                    MembraneCore.FlowPattern.CoCurrent, area, profilePoints: profilePoints, a: a, b: b);
            return CrossFlowModel.SolveByArea(feed.MoleFractions, feed.MolarFlow, permeance, pr, pp,
                area, profilePoints: profilePoints, a: a, b: b, phiTable: phiTable);
        }

        /// <summary>Root-finds the membrane area that yields a target stage cut (θ(area) is monotone in area).</summary>
        private double RequiredArea(FeedState feed, double[] permeance, double pr, double pp, string fp,
                                    double targetTheta, double[]? a = null, double[]? b = null, FugacityTable? phiTable = null)
        {
            double lo = 0.0, hi = 1.0;
            double th = SolveAt(feed, permeance, pr, pp, fp, hi, 0, a, b, phiTable).StageCut;
            int guard = 0;
            while (th < targetTheta && guard++ < 100) { hi *= 4.0; th = SolveAt(feed, permeance, pr, pp, fp, hi, 0, a, b, phiTable).StageCut; }
            for (int it = 0; it < 200; it++)
            {
                double mid = 0.5 * (lo + hi);
                double t = SolveAt(feed, permeance, pr, pp, fp, mid, 0, a, b, phiTable).StageCut;
                if (Math.Abs(t - targetTheta) < 1e-6) return mid;
                if (t < targetTheta) lo = mid; else hi = mid;
            }
            return 0.5 * (lo + hi);
        }

        // =================== IPersistStreamInit ===================

        public void GetClassID(out Guid pClassID) => pClassID = new Guid(MembraneUnitIdentity.Clsid);
        // Always report dirty (S_OK) so the PME persists the current configuration — parameter values edited
        // in the PME's grid update the parameter objects directly without notifying the unit, so a
        // change-tracking flag would miss them and a fresh solve instance would load stale/zero permeances.
        public int IsDirty() => 0 /*S_OK = dirty*/;

        public void GetSizeMax(out long pcbSize) => pcbSize = 1024 * 1024; // ample for profiles

        public void InitNew() { _dirty = false; }

        private static void WriteDoubles(BinaryWriter w, double[] a)
        {
            w.Write(a.Length);
            for (int i = 0; i < a.Length; i++) w.Write(a[i]);
        }

        private static double[] ReadDoubles(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new double[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadDouble();
            return a;
        }

        public void Save(IStream pStm, bool fClearDirty)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(PersistVersion);
                w.Write(_permeatePressure.ValueCore);
                w.Write(_membraneArea.ValueCore);
                w.Write((string)(_flowPattern.value?.ToString() ?? CrossFlow));
                w.Write((string)(_specMode.value?.ToString() ?? SpecArea));   // v5
                w.Write(_permeanceParams.Count);
                foreach (var kv in _permeanceParams)
                {
                    w.Write(kv.Key);
                    w.Write(kv.Value.ValueCore);
                }
                w.Write(_stageCut.ValueCore);   // v2: persist computed output so it survives COFE's solve-instance hop
                // v3: persist the position profiles so they survive the same solve-instance hop.
                WriteDoubles(w, _positionProfile.GetInternal());
                WriteDoubles(w, _stageCutProfile.GetInternal());
                w.Write(_retentateProfileParams.Count);
                foreach (var kv in _retentateProfileParams)
                {
                    w.Write(kv.Key);
                    WriteDoubles(w, kv.Value.GetInternal());
                    WriteDoubles(w, _permeateProfileParams.TryGetValue(kv.Key, out var pp2) ? pp2.GetInternal() : new double[ProfilePoints]);
                    WriteDoubles(w, _permeateCollectedProfileParams.TryGetValue(kv.Key, out var cp2) ? cp2.GetInternal() : new double[ProfilePoints]);
                }
                // v6: energy mode, outlet temperatures, and temperature profiles.
                w.Write((string)(_energyMode.value?.ToString() ?? EnergyIsothermal));
                w.Write(_retentateTemperature.ValueCore);
                w.Write(_permeateTemperature.ValueCore);
                WriteDoubles(w, _retentateTempProfile.GetInternal());
                WriteDoubles(w, _permeateTempProfile.GetInternal());
                w.Write((string)(_drivingForce.value?.ToString() ?? DrivingFugacity));   // v7
                w.Flush();
                StreamPersistence.WriteAll(pStm, ms.ToArray());
            }
            if (fClearDirty) _dirty = false;
        }

        public void Load(IStream pStm)
        {
            byte[] data = StreamPersistence.ReadAll(pStm);
            if (data.Length == 0) return;
            try
            {
                using (var ms = new MemoryStream(data))
                using (var r = new BinaryReader(ms, Encoding.UTF8))
                {
                    int ver = r.ReadInt32();
                    if (ver < 1) return;
                    _permeatePressure.value = r.ReadDouble();
                    _membraneArea.value = r.ReadDouble();
                    string fp = r.ReadString();
                    if (Array.IndexOf(new[] { CrossFlow, CounterCurrent, CoCurrent }, fp) >= 0) _flowPattern.value = fp;
                    if (ver >= 5)
                    {
                        string sm = r.ReadString();
                        if (Array.IndexOf(new[] { SpecArea, SpecStageCut }, sm) >= 0) _specMode.value = sm;
                    }
                    int n = r.ReadInt32();
                    _savedPermeances.Clear();
                    for (int i = 0; i < n; i++)
                    {
                        string id = r.ReadString();
                        double val = r.ReadDouble();
                        _savedPermeances[id] = val;
                    }
                    if (ver >= 2) _stageCut.SetInternal(r.ReadDouble());   // restore last computed output
                    if (ver >= 3)
                    {
                        _positionProfile.SetInternal(ReadDoubles(r));
                        _stageCutProfile.SetInternal(ReadDoubles(r));
                        int pc = r.ReadInt32();
                        _savedRetentateProfile.Clear();
                        _savedPermeateProfile.Clear();
                        _savedPermeateCollectedProfile.Clear();
                        for (int i = 0; i < pc; i++)
                        {
                            string id = r.ReadString();
                            _savedRetentateProfile[id] = ReadDoubles(r);
                            _savedPermeateProfile[id] = ReadDoubles(r);
                            if (ver >= 4) _savedPermeateCollectedProfile[id] = ReadDoubles(r);
                        }
                    }
                    if (ver >= 6)
                    {
                        string em = r.ReadString();
                        if (Array.IndexOf(new[] { EnergyIsothermal, EnergyAdiabatic }, em) >= 0) _energyMode.value = em;
                        _retentateTemperature.SetInternal(r.ReadDouble());
                        _permeateTemperature.SetInternal(r.ReadDouble());
                        _retentateTempProfile.SetInternal(ReadDoubles(r));
                        _permeateTempProfile.SetInternal(ReadDoubles(r));
                    }
                    if (ver >= 7)
                    {
                        string df = r.ReadString();
                        if (Array.IndexOf(new[] { DrivingFugacity, DrivingFugacityLocal, DrivingPartial }, df) >= 0) _drivingForce.value = df;
                    }
                    // Seed any already-known permeance/profile params; the rest are seeded on discovery.
                    foreach (var kv in _savedPermeances)
                        if (_permeanceParams.TryGetValue(kv.Key, out var p)) p.value = kv.Value;
                    foreach (var kv in _savedRetentateProfile)
                        if (_retentateProfileParams.TryGetValue(kv.Key, out var rp)) rp.SetInternal(kv.Value);
                    foreach (var kv in _savedPermeateProfile)
                        if (_permeateProfileParams.TryGetValue(kv.Key, out var pp)) pp.SetInternal(kv.Value);
                    foreach (var kv in _savedPermeateCollectedProfile)
                        if (_permeateCollectedProfileParams.TryGetValue(kv.Key, out var cp)) cp.SetInternal(kv.Value);
                }
                ApplySpecMode();   // reflect restored spec mode in parameter read/write modes
                _dirty = false;
            }
            catch (Exception ex) { Diagnostics.Log("Load failed: " + ex.Message); }
        }

        // =================== internals ===================

        private void BuildPorts()
        {
            _ports = new CapeCollection();
            _ports.Add(_feed);
            _ports.Add(_retentate);
            _ports.Add(_permeate);
        }

        private void RebuildParameters()
        {
            var c = new CapeCollection();
            c.Add(_permeatePressure);
            c.Add(_membraneArea);
            c.Add(_flowPattern);
            c.Add(_specMode);
            c.Add(_energyMode);
            c.Add(_drivingForce);
            var ids = new List<string>(_permeanceParams.Keys);
            ids.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ids) c.Add(_permeanceParams[id]);
            c.Add(_stageCut);
            c.Add(_retentateTemperature);
            c.Add(_permeateTemperature);
            // Plottable profiles (array output params).
            c.Add(_positionProfile);
            c.Add(_stageCutProfile);
            c.Add(_retentateTempProfile);
            c.Add(_permeateTempProfile);
            foreach (var id in ids)
            {
                if (_retentateProfileParams.TryGetValue(id, out var rp)) c.Add(rp);
                if (_permeateProfileParams.TryGetValue(id, out var pp)) c.Add(pp);
                if (_permeateCollectedProfileParams.TryGetValue(id, out var cp)) c.Add(cp);
            }
            _params = c;
        }

        private void EnsurePermeanceParameters(string[] componentIds)
        {
            bool changed = false;
            foreach (var id in componentIds)
            {
                if (_permeanceParams.ContainsKey(id)) continue;
                double seed = _savedPermeances.TryGetValue(id, out var v) ? v : 0.0;
                _permeanceParams[id] = new RealParameter(
                    "Permeance_" + id, $"Permeance of {id}", seed, 0.0, 1.0, CapeParamMode.CAPE_INPUT, DimPermeance);
                _retentateProfileParams[id] = new ArrayParameter(
                    "Profile_Retentate_" + id, $"Retentate mole fraction of {id} vs position", ProfilePoints);
                _permeateProfileParams[id] = new ArrayParameter(
                    "Profile_Permeate_" + id, $"Local permeate mole fraction of {id} vs position", ProfilePoints);
                _permeateCollectedProfileParams[id] = new ArrayParameter(
                    "Profile_PermeateCollected_" + id, $"Cumulative collected permeate mole fraction of {id} (product so far) vs position", ProfilePoints);
                if (_savedRetentateProfile.TryGetValue(id, out var savedRet)) _retentateProfileParams[id].SetInternal(savedRet);
                if (_savedPermeateProfile.TryGetValue(id, out var savedPerm)) _permeateProfileParams[id].SetInternal(savedPerm);
                if (_savedPermeateCollectedProfile.TryGetValue(id, out var savedColl)) _permeateCollectedProfileParams[id].SetInternal(savedColl);
                changed = true;
            }
            if (changed) { RebuildParameters(); _dirty = true; }
        }

        private static string BuildReport(FeedState feed, MembraneCore.MembraneResult r, double pr, double pp)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ORS Membrane Unit Operation - Gas Permeation (cross-flow)");
            sb.AppendLine("==========================================================");
            sb.AppendLine();
            sb.AppendLine($"Feed pressure        : {pr / 1e5,10:F3} bar");
            sb.AppendLine($"Permeate pressure    : {pp / 1e5,10:F3} bar");
            sb.AppendLine($"Pressure ratio gamma : {pp / pr,10:F4}");
            sb.AppendLine($"Feed temperature     : {feed.Temperature,10:F2} K");
            sb.AppendLine($"Feed molar flow      : {feed.MolarFlow,10:E4} mol/s");
            sb.AppendLine($"Stage cut theta      : {r.StageCut,10:F4}");
            sb.AppendLine($"Mass-balance residual: {r.MassBalanceResidual,10:E2}");
            sb.AppendLine();
            sb.AppendLine("Compound        Feed x     Retentate x   Permeate y    Recovery");
            sb.AppendLine("----------------------------------------------------------------");
            for (int i = 0; i < feed.ComponentIds.Length; i++)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-14} {1,8:F5}   {2,10:F5}   {3,10:F5}   {4,8:F4}",
                    feed.ComponentIds[i], feed.MoleFractions[i],
                    r.RetentateComposition[i], r.PermeateComposition[i], r.ComponentRecovery[i]));
            }

            // Position-dependent profile (sampled) — also available as plottable array parameters.
            var pf = r.Profile;
            if (pf != null)
            {
                sb.AppendLine();
                sb.AppendLine("Position profile (retentate x_i / permeate y_i vs position, feed end -> retentate outlet):");
                var header = new StringBuilder();
                header.Append(string.Format("{0,-8}{1,-8}", "pos", "theta"));
                for (int i = 0; i < feed.ComponentIds.Length; i++)
                {
                    string id = feed.ComponentIds[i];
                    string tag = id.Length > 5 ? id.Substring(0, 5) : id;
                    header.Append(string.Format("{0,-9}{1,-9}", "x_" + tag, "y_" + tag));
                }
                sb.AppendLine(header.ToString());
                int pts = pf.Points;
                for (int k = 0; k < pts; k += Math.Max(1, pts / 10))
                {
                    var row = new StringBuilder();
                    row.Append(string.Format(CultureInfo.InvariantCulture, "{0,-8:F3}{1,-8:F4}", pf.Position[k], pf.StageCut[k]));
                    for (int i = 0; i < feed.ComponentIds.Length; i++)
                        row.Append(string.Format(CultureInfo.InvariantCulture, "{0,-9:F4}{1,-9:F4}", pf.Retentate[i][k], pf.Permeate[i][k]));
                    sb.AppendLine(row.ToString());
                }
            }
            return sb.ToString();
        }
    }
}
