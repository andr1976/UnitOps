using System.Globalization;
using System.Text;
using MembraneCore;
using MembraneCore.Energy;
using MembraneCore.Models;

// Generates CSVs (and a stdout summary) for the technical-reference validation figures,
// by running the actual MembraneCore solvers. Run: dotnet run  (from this directory).

var ci = CultureInfo.InvariantCulture;
string dataDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data");
dataDir = Path.GetFullPath(dataDir);
Directory.CreateDirectory(dataDir);
void WriteCsv(string name, string header, IEnumerable<string> rows)
{
    var sb = new StringBuilder();
    sb.AppendLine(header);
    foreach (var r in rows) sb.AppendLine(r);
    File.WriteAllText(Path.Combine(dataDir, name), sb.ToString());
    Console.WriteLine($"  wrote {name}");
}
string F(double v) => v.ToString("G6", ci);

Console.WriteLine("=== Shindo/Dias Case 1: NH3/H2/N2 (polyethylene), gamma=0.13 ===");
double[] c1feed = { 0.45, 0.25, 0.30 };
double[] c1perm = { 2.63e-10, 8.35e-11, 1.72e-11 };
var c1cross = CrossFlowModel.SolveByStageCut(c1feed, c1perm, 0.13, 0.3726);
var c1counter = PlugFlowModel.SolveByStageCut(c1feed, c1perm, 0.13, FlowPattern.CounterCurrent, 0.3745);
Console.WriteLine($"  cross   theta=0.3726  perm=[{F(c1cross.PermeateComposition[0])},{F(c1cross.PermeateComposition[1])},{F(c1cross.PermeateComposition[2])}]  (Shindo 0.7338/0.2035/0.0627)");
Console.WriteLine($"  counter theta=0.3745  perm=[{F(c1counter.PermeateComposition[0])},{F(c1counter.PermeateComposition[1])},{F(c1counter.PermeateComposition[2])}]  (Shindo 0.7368/0.2010/0.0622)");

Console.WriteLine("=== Shindo/Dias Case 2: H2/CH4/CO/N2/CO2 (microporous glass), gamma=0.10, S_H2 typo-corrected ===");
double[] c2feed = { 0.30, 0.10, 0.25, 0.15, 0.20 };
double[] c2perm = { 4.80e-10, 1.91e-10, 1.40e-10, 1.38e-10, 1.48e-10 };
var c2counter = PlugFlowModel.SolveByStageCut(c2feed, c2perm, 0.10, FlowPattern.CounterCurrent, 0.4146);
Console.WriteLine($"  counter theta=0.4146  perm=[{string.Join("/", Array.ConvertAll(c2counter.PermeateComposition, F))}]");
Console.WriteLine($"          (Shindo 0.4742/0.0905/0.1793/0.1065/0.1495)");

Console.WriteLine("=== Geankoplis Ex 13.6-1: O2/N2 air separation cross-flow, alpha*=10, gamma=0.10, theta=0.20 ===");
double[] gfeed = { 0.209, 0.791 };
double[] gperm = { 5.0e-8, 5.0e-9 };            // alpha* = 10 (only the ratio matters for composition)
var gcross = CrossFlowModel.SolveByStageCut(gfeed, gperm, 0.10, 0.20);
Console.WriteLine($"  mixed permeate y_O2 = {F(gcross.PermeateComposition[0])}  (Geankoplis cross-flow 0.5690; complete-mixing 0.5067)");
Console.WriteLine($"  retentate  x_O2     = {F(gcross.RetentateComposition[0])}  (Geankoplis 0.1190)");

// Cross-flow profile for the Geankoplis case (local vs collected permeate along theta), vs Table 13.6-1.
double gPr = 1.0e6, gPp = 1.0e5, gFlow = 1.0;
double area = BisectAreaForTheta(gfeed, gFlow, gperm, gPr, gPp, 0.20);
var gprof = CrossFlowModel.SolveByArea(gfeed, gFlow, gperm, gPr, gPp, area, profilePoints: 100);
var pr = gprof.Profile!;
var rows = new List<string>();
for (int k = 0; k < pr.Points; k++)
    rows.Add($"{F(pr.StageCut[k])},{F(pr.Retentate[0][k])},{F(pr.Permeate[0][k])},{F(pr.PermeateCollected[0][k])}");
WriteCsv("geankoplis_crossflow_profile.csv", "theta_local,x_O2,y_local_O2,y_collected_O2", rows);

// Shindo Case 1 bar-chart data (mine vs reference).
WriteCsv("shindo_case1.csv", "component,mine_cross,ref_cross,mine_counter,ref_counter", new[]
{
    $"NH3,{F(c1cross.PermeateComposition[0])},0.7338,{F(c1counter.PermeateComposition[0])},0.7368",
    $"H2,{F(c1cross.PermeateComposition[1])},0.2035,{F(c1counter.PermeateComposition[1])},0.2010",
    $"N2,{F(c1cross.PermeateComposition[2])},0.0627,{F(c1counter.PermeateComposition[2])},0.0622",
});

Console.WriteLine("=== Non-isothermal energy layer: CO2/CH4, ideal-gas vs linear Joule-Thomson ===");
double[] efeed = { 0.10, 0.90 };
double[] eperm = { 3.0e-8, 1.5e-9 };
double ePr0 = 60e5, ePp = 2e5, eFlow = 1.0, eT = 313.15;
var idealTh = new IdealGas();
var jtTh = new JouleThomson();
var erows = new List<string>();
foreach (double prBar in new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0 })
{
    double ePr = prBar * 1e5;
    double a = BisectAreaForTheta(efeed, eFlow, eperm, ePr, ePp, 0.30);
    var sep = CrossFlowModel.SolveByArea(efeed, eFlow, eperm, ePr, ePp, a, profilePoints: 0);
    var eJt = NonIsothermalEnergy.Solve(jtTh, eT, ePr, ePp, eFlow, efeed, sep, includeProfile: false);
    var eId = NonIsothermalEnergy.Solve(idealTh, eT, ePr, ePp, eFlow, efeed, sep, includeProfile: false);
    erows.Add($"{F(prBar)},{F(eJt.PermeateTemperature)},{F(eJt.RetentateTemperature)},{F(eId.PermeateTemperature)},{F(eId.RetentateTemperature)}");
    if (Math.Abs(prBar - 60.0) < 1e-9)
        Console.WriteLine($"  Pr=60bar: JT  Tp={F(eJt.PermeateTemperature)}K Tr={F(eJt.RetentateTemperature)}K | ideal Tp={F(eId.PermeateTemperature)}K Tr={F(eId.RetentateTemperature)}K (feed {F(eT)}K)");
}
WriteCsv("energy_jt.csv", "Pr_bar,Tp_jt,Tr_jt,Tp_ideal,Tr_ideal", erows);

Console.WriteLine("=== MemPy N2/O2 air-separation experiment (12 points): cross-flow model vs measurement ===");
// DeJaco et al. Table 2. Retentate in SLPM, permeate in mL/min; O2 mole fractions measured.
double[] apF = { 206, 205, 205, 205, 205, 205, 274, 274, 274, 274, 274, 274 };        // kPa
double[] apRet = { 10.15, 7.482, 5.254, 3.077, 1.073, 0.54, 10.69, 7.551, 5.024, 3.035, 1.01, 0.52 }; // SLPM
double[] apPerm = { 452, 447, 448, 448, 446, 445, 810, 818, 824, 815, 816, 809 };       // mL/min
double[] apFeedO2 = { 0.210, 0.210, 0.210, 0.210, 0.210, 0.210, 0.207, 0.207, 0.207, 0.207, 0.207, 0.207 };
double[] apPermO2 = { 0.283, 0.281, 0.281, 0.278, 0.270, 0.263, 0.304, 0.302, 0.300, 0.296, 0.280, 0.268 };
double[] apRetO2 = { 0.206, 0.205, 0.203, 0.200, 0.186, 0.187, 0.202, 0.200, 0.195, 0.188, 0.163, 0.173 };
double apPp = 101325.0;
double[] apPermeance = { 145.0, 57.0 };   // O2, N2 (GPU mid-range; only the ratio, alpha*=2.54, matters here)
var aprows = new List<string>();
double worst = 0;
for (int i = 0; i < apF.Length; i++)
{
    double theta = apPerm[i] / (apRet[i] * 1000.0 + apPerm[i]);
    double gamma = apPp / (apF[i] * 1000.0);
    double[] fair = { apFeedO2[i], 1.0 - apFeedO2[i] };
    var r = CrossFlowModel.SolveByStageCut(fair, apPermeance, gamma, theta);
    double predPerm = r.PermeateComposition[0], predRet = r.RetentateComposition[0];
    worst = Math.Max(worst, Math.Abs(predPerm - apPermO2[i]));
    aprows.Add($"{i},{F(apF[i])},{F(theta)},{F(apPermO2[i])},{F(predPerm)},{F(apRetO2[i])},{F(predRet)}");
}
WriteCsv("mempy_airsep.csv", "point,Pfeed_kPa,theta,permO2_meas,permO2_pred,retO2_meas,retO2_pred", aprows);
Console.WriteLine($"  worst |permeate O2 pred - meas| over 12 points = {F(worst)}");

Console.WriteLine("=== Dias Case 4 (Polaris flue gas CO2/O2/N2): CO2-recovery design point ===");
double[] d4feed = { 0.20, 0.07, 0.73 };
double[] d4perm = { 3.89e-7, 3.11e-8, 1.44e-8 };
double d4gamma = 0.12;
double loT = 0.001, hiT = 0.99;
for (int it = 0; it < 100; it++)   // bisect stage cut for 90% CO2 recovery
{
    double mid = 0.5 * (loT + hiT);
    double rec = CrossFlowModel.SolveByStageCut(d4feed, d4perm, d4gamma, mid).ComponentRecovery[0];
    if (rec < 0.90) loT = mid; else hiT = mid;
}
double d4theta = 0.5 * (loT + hiT);
var d4 = CrossFlowModel.SolveByStageCut(d4feed, d4perm, d4gamma, d4theta);
Console.WriteLine($"  90% CO2 recovery at theta={F(d4theta)}; permeate CO2={F(d4.PermeateComposition[0])}; retentate CO2={F(d4.RetentateComposition[0])}");

Console.WriteLine("=== Dias Case 3 (cellulose acetate CO2/NG): leading-edge permeate CO2 (max purity) ===");
double[] d3feed = { 0.18, 0.64, 0.09, 0.06, 0.03 };
double[] d3perm = { 2.98e-8, 2.27e-10, 4.29e-10, 4.53e-10, 1.92e-8 };
var d3 = CrossFlowModel.SolveByStageCut(d3feed, d3perm, 0.04, 0.005);
Console.WriteLine($"  theta->0 permeate CO2 = {F(d3.PermeateComposition[0])} (feed 0.18)");

Console.WriteLine("=== Ideal-gas vs real-gas (fugacity) driving force: CO2/CH4 cross-flow, PR EOS ===");
double[] Co2Tc = { 304.13, 190.56 }, Co2Pc = { 73.77e5, 45.99e5 }, Co2W = { 0.2236, 0.0115 };  // CO2, CH4
double[] rgFeed = { 0.10, 0.90 };
double[] rgPerm = { 3.0e-8, 1.5e-9 };
double rgPp = 2e5, rgT = 313.15;
double rgArea = BisectAreaForTheta(rgFeed, 1.0, rgPerm, 40e5, rgPp, 0.30);   // fix area (IG theta=0.30 at 40 bar)
var rgRows = new List<string>();
foreach (double prBar in new[] { 10.0, 20, 30, 40, 50, 60, 70, 80, 90, 100 })
{
    double Pr = prBar * 1e5;
    var ig = CrossFlowModel.SolveByArea(rgFeed, 1.0, rgPerm, Pr, rgPp, rgArea);
    double[] aRet = PrPhi(rgT, Pr, rgFeed, Co2Tc, Co2Pc, Co2W, 0.0919);
    double[] bPerm = PrPhi(rgT, rgPp, rgFeed, Co2Tc, Co2Pc, Co2W, 0.0919);
    var rg = CrossFlowModel.SolveByArea(rgFeed, 1.0, rgPerm, Pr, rgPp, rgArea, a: aRet, b: bPerm);
    rgRows.Add($"{F(prBar)},{F(ig.StageCut)},{F(rg.StageCut)},{F(ig.PermeateComposition[0])},{F(rg.PermeateComposition[0])},{F(aRet[0])},{F(aRet[1])}");
    if (Math.Abs(prBar - 60) < 1e-9)
        Console.WriteLine($"  Pr=60bar: IG theta={F(ig.StageCut)}, RG theta={F(rg.StageCut)} ({F(100*(ig.StageCut-rg.StageCut)/rg.StageCut)}% high); phi_CO2={F(aRet[0])}, phi_CH4={F(aRet[1])}");
}
WriteCsv("ig_rg.csv", "Pr_bar,theta_ig,theta_rg,permCO2_ig,permCO2_rg,phiCO2,phiCH4", rgRows);

Console.WriteLine("=== IG vs RG on the MemPy propane/propylene system (MemPy's exact conditions) ===");
// DeJaco et al. run: T=296 K, 80% propylene, propylene/propane = 100/10 GPU, P_out=1.013 bar (fixed),
// feed 2.6 mol/s, fixed area L*H = 6.1237*12.2474 = 75 m^2, feed pressure swept 1.1..10 bar.
double GPU = 3.348e-10;
double[] c3Feed = { 0.80, 0.20 };                       // propylene, propane
double[] c3Perm = { 100 * GPU, 10 * GPU };
double[] c3Tc = { 364.9, 369.8 }, c3Pc = { 46.0e5, 42.5e5 }, c3W = { 0.142, 0.152 };  // C3H6, C3H8
double c3kij = 0.0089, c3T = 296.0, c3Pout = 1.01325e5, c3Area = 75.0, c3Flow = 2.6;
// MemPy's own 2D IG and PR stage cuts (results.csv) for overlay.
double[] mP = { 1.1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
double[] mIG = { 0.006082, 0.124949, 0.270040, 0.408375, 0.535873, 0.648201, 0.740268, 0.808352, 0.854295, 0.885600 };
double[] mRG = { 0.005772, 0.115243, 0.242216, 0.356798, 0.457757, 0.544875, 0.618279, 0.678459, 0.726358, 0.763385 };
var c3Rows = new List<string>();
for (int k = 0; k < mP.Length; k++)
{
    double Pf = mP[k] * 1e5;
    // Match MemPy's ideal-gas stage cut at this pressure (design mode) so only the EOS effect differs.
    double areaK = BisectAreaForTheta(c3Feed, c3Flow, c3Perm, Pf, c3Pout, mIG[k]);
    var ig = CrossFlowModel.SolveByArea(c3Feed, c3Flow, c3Perm, Pf, c3Pout, areaK);
    double[] a = PrPhi(c3T, Pf, c3Feed, c3Tc, c3Pc, c3W, c3kij);
    double[] bb = PrPhi(c3T, c3Pout, c3Feed, c3Tc, c3Pc, c3W, c3kij);
    var rg = CrossFlowModel.SolveByArea(c3Feed, c3Flow, c3Perm, Pf, c3Pout, areaK, a: a, b: bb);
    double ourErr = 100 * (ig.StageCut - rg.StageCut) / rg.StageCut;
    double memErr = 100 * (mIG[k] - mRG[k]) / mRG[k];
    c3Rows.Add($"{F(mP[k])},{F(ig.StageCut)},{F(rg.StageCut)},{F(ourErr)},{F(mIG[k])},{F(mRG[k])},{F(memErr)},{F(a[0])}");
    if (Math.Abs(mP[k] - 9) < 1e-9)
        Console.WriteLine($"  9 bar (theta matched to MemPy {F(mIG[k])}): ours RG={F(rg.StageCut)} over-pred={F(ourErr)}% | MemPy RG={F(mRG[k])} over-pred={F(memErr)}%; phi_C3H6={F(a[0])}");
}
WriteCsv("c3_ig_rg.csv", "Pf_bar,theta_ig_ours,theta_rg_ours,over_pred_ours,theta_ig_mempy,theta_rg_mempy,over_pred_mempy,phiC3H6", c3Rows);

Console.WriteLine("done.");

// ---- helpers ----
static double BisectAreaForTheta(double[] feed, double flow, double[] perm, double Pr, double Pp, double target)
{
    double lo = 0.0, hi = 1.0;
    while (CrossFlowModel.SolveByArea(feed, flow, perm, Pr, Pp, hi).StageCut < target && hi < 1e12) hi *= 4.0;
    for (int i = 0; i < 200; i++)
    {
        double mid = 0.5 * (lo + hi);
        double th = CrossFlowModel.SolveByArea(feed, flow, perm, Pr, Pp, mid).StageCut;
        if (Math.Abs(th - target) < 1e-7) return mid;
        if (th < target) lo = mid; else hi = mid;
    }
    return 0.5 * (lo + hi);
}

// Peng-Robinson vapour-phase fugacity coefficients for a binary mixture (illustration only; in the live
// unit these come from the PME's EOS). Component critical props Tc[K], Pc[Pa], acentric w, binary kij.
static double[] PrPhi(double T, double P, double[] x, double[] Tc, double[] Pc, double[] w, double kij)
{
    const double R = 8.314462618;
    int n = x.Length;
    var ai = new double[n]; var bi = new double[n];
    for (int i = 0; i < n; i++)
    {
        double kappa = 0.37464 + 1.54226 * w[i] - 0.26992 * w[i] * w[i];
        double alpha = Math.Pow(1 + kappa * (1 - Math.Sqrt(T / Tc[i])), 2);
        ai[i] = 0.45724 * R * R * Tc[i] * Tc[i] / Pc[i] * alpha;
        bi[i] = 0.07780 * R * Tc[i] / Pc[i];
    }
    var aij = new double[n, n];
    double amix = 0, bmix = 0;
    for (int i = 0; i < n; i++)
    {
        bmix += x[i] * bi[i];
        for (int j = 0; j < n; j++)
        {
            aij[i, j] = (1 - (i == j ? 0.0 : kij)) * Math.Sqrt(ai[i] * ai[j]);
            amix += x[i] * x[j] * aij[i, j];
        }
    }
    double A = amix * P / (R * R * T * T), B = bmix * P / (R * T);
    // Z^3 - (1-B)Z^2 + (A-3B^2-2B)Z - (AB-B^2-B^3) = 0; Newton from Z=1 -> vapour (largest) root.
    double c2 = -(1 - B), c1 = A - 3 * B * B - 2 * B, c0 = -(A * B - B * B - B * B * B);
    double Z = 1.0;
    for (int it = 0; it < 100; it++)
    {
        double f = Z * Z * Z + c2 * Z * Z + c1 * Z + c0;
        double df = 3 * Z * Z + 2 * c2 * Z + c1;
        double dz = f / df; Z -= dz; if (Math.Abs(dz) < 1e-13) break;
    }
    double sq2 = Math.Sqrt(2.0);
    var phi = new double[n];
    for (int i = 0; i < n; i++)
    {
        double sumj = 0; for (int j = 0; j < n; j++) sumj += x[j] * aij[i, j];
        double lnphi = bi[i] / bmix * (Z - 1) - Math.Log(Z - B)
            - A / (2 * sq2 * B) * (2 * sumj / amix - bi[i] / bmix)
              * Math.Log((Z + (1 + sq2) * B) / (Z + (1 - sq2) * B));
        phi[i] = Math.Exp(lnphi);
    }
    return phi;
}

sealed class IdealGas : IEnthalpyProvider
{
    static readonly double[] Cp = { 37.0, 36.0 };
    const double Tref = 298.15;
    public double MolarEnthalpy(double t, double p, double[] x)
    { double h = 0; for (int i = 0; i < x.Length; i++) h += x[i] * Cp[i] * (t - Tref); return h; }
}

sealed class JouleThomson : IEnthalpyProvider
{
    static readonly double[] Cp = { 37.0, 36.0 };
    const double Mu = 1.0e-6, Tref = 298.15, Pref = 101325.0;   // K/Pa
    public double MolarEnthalpy(double t, double p, double[] x)
    { double h = 0; for (int i = 0; i < x.Length; i++) h += x[i] * Cp[i] * ((t - Tref) - Mu * (p - Pref)); return h; }
}
