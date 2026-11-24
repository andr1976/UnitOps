using System;
using System.Collections.Generic;
using System.Linq;

namespace EjectorCore
{
    /// <summary>Motive-nozzle flashing model.</summary>
    public enum MotiveModel
    {
        /// <summary>Homogeneous equilibrium.</summary>
        HEM,
        /// <summary>Homogeneous non-equilibrium: x = phi * x_eq in the density.</summary>
        HNE,
    }

    /// <summary>Mixing-section model (techref sec-mixing-models).</summary>
    public enum MixingModel
    {
        /// <summary>Constant-area mixing: mass, momentum and energy over the mixing section.</summary>
        CAM,
        /// <summary>Constant-pressure mixing at a hypothetical throat (Huang et al. 1999), then a normal shock.</summary>
        CPM,
    }

    /// <summary>
    /// One-dimensional constant-area-mixing ejector (techref chapters 2-5).
    /// Port of ejector/prototype/ejector1d.py (class Ejector); the Python code is the
    /// reference and the regression tests compare the two on identical thermodynamics.
    /// Not thread-safe (the flash wrapper counts flashes).
    /// </summary>
    public sealed class Ejector
    {
        private readonly Thermo _th;

        /// <param name="flash">Property model.</param>
        /// <param name="solidCheck">Raise <see cref="SolidPhaseRiskException"/> below the triple point of a pure fluid.</param>
        public Ejector(IFlash flash, bool solidCheck = true)
        {
            _th = new Thermo(flash, solidCheck);
        }

        /// <summary>Motive-nozzle isentropic efficiency (acts beyond the throat).</summary>
        public double EtaM { get; set; } = 0.90;
        /// <summary>Suction isentropic efficiency.</summary>
        public double EtaS { get; set; } = 0.85;
        /// <summary>Mixing efficiency on the inlet momentum flux.</summary>
        public double EtaMix { get; set; } = 0.90;
        /// <summary>Diffuser isentropic efficiency.</summary>
        public double EtaD { get; set; } = 0.80;
        /// <summary>Motive flashing model.</summary>
        public MotiveModel MotiveModel { get; set; } = MotiveModel.HEM;
        /// <summary>HNE flashing factor (used when <see cref="MotiveModel"/> is HNE).</summary>
        public double PhiNe { get; set; } = 1.0;
        /// <summary>Motive discharge coefficient on the isentropic critical flux.</summary>
        public double CdM { get; set; } = 1.0;
        /// <summary>Mixing-section model.</summary>
        public MixingModel Mixing { get; set; } = MixingModel.CAM;
        /// <summary>CPM: primary-core area coefficient at the hypothetical throat (Huang's phi_p).</summary>
        public double PhiP { get; set; } = 1.0;

        /// <summary>Total flashes since construction.</summary>
        public int FlashCount => _th.FlashCount;
        /// <summary>Molar mass [kg/mol] at composition z (from the property model).</summary>
        public double MolarMass(double[]? z) => _th.MolarMass(z);

        /// <summary>Triple-point pressure used by the solid guard (0 if none).</summary>
        public double TriplePointPressure => _th.PTriple;

        // ------------------------------------------------------------------ inlets
        /// <summary>Stagnation inlet from pressure and temperature.</summary>
        public FluidState InletPT(double P, double T, double[]? z = null) => _th.PT(P, T, z);
        /// <summary>Stagnation inlet from pressure and molar vapour fraction (saturated states).</summary>
        public FluidState InletPQ(double P, double q, double[]? z = null) => _th.PQ(P, q, z);
        /// <summary>Stagnation inlet from pressure and specific enthalpy (what a flowsheet stream carries).</summary>
        public FluidState InletPH(double P, double h, double[]? z = null) => _th.PH(P, h, z);

        private double PLow(FluidState m0, FluidState s0) =>
            Math.Max(_th.PTriple * 1.001, 0.01 * Math.Min(s0.P, m0.P));

        // ------------------------------------------------------------------ motive nozzle
        /// <summary>Motive nozzle: isentropic to the throat, EtaM beyond it.
        /// Returns (exit-path expansion, throat pressure, CdM * G*).</summary>
        private (Expansion Exp, double Pt, double Gt) Motive(FluidState m0, double pLow)
        {
            double phi = MotiveModel == MotiveModel.HEM ? 1.0 : PhiNe;
            var (pt, g) = new Expansion(_th, m0, 1.0, phi).Critical(pLow);
            var exp = new Expansion(_th, m0, EtaM, phi, pt);
            return (exp, pt, g * CdM);
        }

        /// <summary>
        /// Throat pressure and discharge-corrected critical flux CdM*G* [kg/(m2 s)] of the
        /// motive nozzle (isentropic to the throat), searched down to <paramref name="pLow"/>.
        /// If the flux still rises at pLow the nozzle does not choke above it and (pLow, flux) is returned.
        /// </summary>
        public (double Pt, double Gstar) MotiveThroat(FluidState m0, double pLow)
        {
            var (_, pt, gt) = Motive(m0, Math.Max(pLow, _th.PTriple * 1.001));
            return (pt, gt);
        }

        // ------------------------------------------------------------------ mixing
        /// <summary>Constant-area section: subsonic root of mass, momentum and energy. If the residual
        /// starts to fall without turning positive, its maximum is refined between the grid points
        /// before the section is declared choked. eta overrides EtaMix (1 = loss-free normal shock).</summary>
        private (double P3, double U3, FluidState St3)? Mix(double aMix, IList<(double PA, double MU)> streams,
                                                              double mT, double h0T, double[]? zT, double? eta = null)
        {
            double fIn = streams.Sum(s => s.PA) + (eta ?? EtaMix) * streams.Sum(s => s.MU);

            (double R, double U3, FluidState? St) Resid(double p3)
            {
                double u3 = (fIn - p3 * aMix) / mT;
                if (u3 <= 0) return (-1.0, u3, null);
                double h3 = h0T - 0.5 * u3 * u3;
                var st = _th.PH(p3, h3, zT);
                return (st.Rho * u3 * aMix / mT - 1.0, u3, st);
            }

            (double, double, FluidState) Root(double lo, double hi)
            {
                double p3 = Numerics.Brentq(x => Resid(x).R, lo, hi, 1e-6 * lo);
                var res = Resid(p3);
                return (p3, res.U3, res.St!);
            }

            double pHi = fIn / aMix * (1 - 1e-9);
            double pLo = Math.Max(_th.PTriple * 1.0001, 1e3);
            double pPrev2 = pHi, pPrev = pHi, rPrev = -1.0, p = pHi;
            while (p > pLo)
            {
                p = Math.Max(p * 0.96, pLo);
                double r;
                try
                {
                    r = Resid(p).R;
                }
                catch (Exception ex) when (IsValueError(ex) || ex is SolidPhaseRiskException)
                {
                    return null;
                }
                if (r > 0) return Root(p, pPrev);
                if (r < rPrev && rPrev > -0.999)
                {
                    // past the residual maximum on the grid: refine it between the grid points
                    double Neg(double x)
                    {
                        try { return -Resid(x).R; }
                        catch (Exception ex) when (IsValueError(ex) || ex is SolidPhaseRiskException) { return 1.0; }
                    }
                    var m = Numerics.Fminbound(Neg, p, pPrev2, 1e-7 * p);
                    if (-m.F > 0) return Root(m.X, pPrev2);   // narrow positive peak between grid points
                    return null;
                }
                pPrev2 = pPrev;
                pPrev = p;
                rPrev = r;
            }
            return null;   // mixing section cannot pass the flow
        }

        // ------------------------------------------------------------------ diffuser
        private FluidState Diffuser(FluidState st3, double u3, double h0)
        {
            double target = st3.H + EtaD * 0.5 * u3 * u3;
            double F(double P) => _th.PS(P, st3.S, st3.Z).H - target;
            double pHi = st3.P * 1.5;
            while (F(pHi) < 0) pHi *= 1.5;
            double pOut = Numerics.Brentq(F, st3.P, pHi, 1e-7 * st3.P);
            return _th.PH(pOut, h0, st3.Z);
        }

        private double EtaEjector(FluidState m0, FluidState s0, FluidState outSt, double er)
        {
            try
            {
                double wRec = _th.PS(outSt.P, s0.S, s0.Z).H - s0.H;
                double wAvail = m0.H - _th.PS(outSt.P, m0.S, m0.Z).H;
                return er * wRec / wAvail;
            }
            catch (Exception ex) when (IsValueError(ex) || ex is SolidPhaseRiskException)
            {
                return double.NaN;
            }
        }

        /// <summary>Overall mole fractions of the mixed stream (null for a pure fluid).</summary>
        public double[]? MixedComposition(FluidState m0, FluidState s0, double mM, double mS)
        {
            if (!_th.IsMixture) return null;
            if (m0.Z == null || s0.Z == null) throw new ArgumentException("mixture inlets need compositions");
            double nM = mM / _th.MolarMass(m0.Z);
            double nS = mS / _th.MolarMass(s0.Z);
            var z = new double[m0.Z.Length];
            for (int i = 0; i < z.Length; i++) z[i] = (nM * m0.Z[i] + nS * s0.Z[i]) / (nM + nS);
            return z;
        }

        /// <summary>Python ValueError equivalents: flash failure, empty bracket, bad geometry.</summary>
        private static bool IsValueError(Exception ex) =>
            ex is FlashException || ex is RootBracketException || ex is ArgumentException;

        // ------------------------------------------------------------------ one pass at P_y
        private sealed class Pass
        {
            public double Ms, Uy, P3, U3, Mm, Pt, Pe, Ue, At, Ae, Amix, ER;
            public FluidState StY = null!, St3 = null!, Out = null!, StE = null!;
        }

        private Pass? DoPass(FluidState m0, FluidState s0, double mM, double pE, FluidState stE, double uE,
                             double aE, double aMix, double pY, Expansion sexp, double gSMax,
                             Expansion? mexp = null)
        {
            if (Mixing == MixingModel.CPM)
                return DoPassCpm(m0, s0, mM, pE, stE, uE, aMix, pY, sexp, gSMax, mexp);
            double aSy = aMix - aE;
            if (aSy <= 0) throw new ArgumentException("nozzle exit area >= mixing area");
            var (stY, uY) = sexp.At(pY);
            double gS = Math.Min(stY.Rho * uY, gSMax);
            double mS = gS * aSy;
            double mT = mM + mS;
            double h0T = (mM * m0.H + mS * s0.H) / mT;
            var zT = MixedComposition(m0, s0, mM, mS);
            var mix = Mix(aMix, new[] { (pE * aE, mM * uE), (pY * aSy, mS * uY) }, mT, h0T, zT);
            if (mix == null) return null;
            var (p3, u3, st3) = mix.Value;
            var outSt = Diffuser(st3, u3, h0T);
            return new Pass { Ms = mS, StY = stY, Uy = uY, P3 = p3, U3 = u3, St3 = st3, Out = outSt };
        }

        /// <summary>
        /// Constant-pressure mixing (Huang et al. 1999, real fluid) at mixing pressure pY: the motive
        /// core, carried along the motive path from the nozzle exit to pY, takes PhiP*mM/(rho u) of
        /// the section aMix at the hypothetical throat; the suction stream passes the rest. The
        /// streams mix at constant pY (EtaMix on the momentum flux); the mixed stream passes a
        /// loss-free normal shock (none if subsonic) before the diffuser. Port of _pass_cpm.
        /// </summary>
        private Pass? DoPassCpm(FluidState m0, FluidState s0, double mM, double pE, FluidState stE, double uE,
                                double aMix, double pY, Expansion sexp, double gSMax, Expansion? mexp)
        {
            FluidState stP; double uP;
            if (Math.Abs(pE - pY) <= 1e-9 * pY || mexp == null) { stP = stE; uP = uE; }
            else (stP, uP) = mexp.At(pY);
            double aSy = aMix - PhiP * mM / (stP.Rho * uP);
            if (aSy <= 0.0) return null;                       // the motive core fills the section
            var (stY, uY) = sexp.At(pY);
            double gS = Math.Min(stY.Rho * uY, gSMax);
            double mS = gS * aSy;
            double mT = mM + mS;
            double h0T = (mM * m0.H + mS * s0.H) / mT;
            var zT = MixedComposition(m0, s0, mM, mS);
            double uM = EtaMix * (mM * uP + mS * uY) / mT;
            var stM = _th.PH(pY, h0T - 0.5 * uM * uM, zT);
            double aM = mT / (stM.Rho * uM);
            var shock = Mix(aM, new[] { (pY * aM, mT * uM) }, mT, h0T, zT, eta: 1.0);
            if (shock == null) return null;
            var (p3, u3, st3) = shock.Value;
            var outSt = Diffuser(st3, u3, h0T);
            return new Pass { Ms = mS, StY = stY, Uy = uY, P3 = p3, U3 = u3, St3 = st3, Out = outSt };
        }

        /// <summary>
        /// Index of the most-entraining feasible scan point, made exact in place (port of
        /// _critical_point). CAM: bisect the feasibility boundary. CPM: the entrainment maximum
        /// (compound choking), refined with a bounded search; points below it are dropped.
        /// </summary>
        private int CriticalPoint(IList<double> pys, IList<Pass?> rows, Func<double, Pass?> run, FluidState s0)
        {
            if (rows.All(r => r == null))
                throw new EjectorException("no feasible mixing solution (mixing section too small?)");
            if (Mixing == MixingModel.CPM)
            {
                int k = 0;
                double best = double.NegativeInfinity;
                for (int j = 0; j < rows.Count; j++)
                {
                    double e = rows[j] != null ? rows[j]!.Ms / rows[j]!.Mm : double.NegativeInfinity;
                    if (e > best) { best = e; k = j; }
                }
                double lo = pys[Math.Max(k - 1, 0)], hi = pys[Math.Min(k + 1, pys.Count - 1)];
                double Neg(double p)
                {
                    var r = run(p);
                    return r != null ? -r.Ms / r.Mm : 1.0;
                }
                var m = Numerics.Fminbound(Neg, lo, hi, 1e-7 * s0.P);
                if (-m.F > best) { pys[k] = m.X; rows[k] = run(m.X); }
                for (int j = 0; j < k; j++) rows[j] = null;
                return k;
            }
            int k0 = 0;
            while (rows[k0] == null) k0++;
            if (k0 > 0)
            {
                // feasibility boundary (mixed flow sonic at the end of the mixing section) lies
                // between grid points k0-1 and k0: bisect it, otherwise the critical ER depends on
                // the scan resolution.
                double lo = pys[k0 - 1], hi = pys[k0];
                Pass rHi = rows[k0]!;
                while (hi - lo > 1e-6 * s0.P)
                {
                    double mid = 0.5 * (lo + hi);
                    var r = run(mid);
                    if (r != null) { hi = mid; rHi = r; } else lo = mid;
                }
                pys[k0] = hi;
                rows[k0] = rHi;
            }
            return k0;
        }

        /// <summary>Motive flow, throat/exit pressures and exit state for mixing-inlet pressure pY.</summary>
        /// <summary>Exit conditions of a converging-diverging motive nozzle (see <see cref="CdExit"/>).</summary>
        private sealed class CdExitInfo
        {
            public double Ge, PSup, USup, PFc, PSe;
            public FluidState StSup = null!;
        }

        /// <summary>
        /// Converging-diverging nozzle, Ae/At = rE &gt; 1. Cd acts at the throat only; the diverging section
        /// carries the HEM flux, so a choked nozzle has the exit mass flux Ge = Cd*G*/rE, which needs
        /// rE &gt;= Cd (else <see cref="ArgumentException"/>, no silent clamp). PSup: supersonic exit; PFc:
        /// first-critical back pressure (choked below it); PSe: back pressure with a normal shock at the exit.
        /// Depends on rE only, so it is cached per rE.
        /// </summary>
        private CdExitInfo CdExit(Expansion mexp, double pT, double gT, double rE, double pLow,
                                  Dictionary<double, CdExitInfo> cache)
        {
            double key = Math.Round(rE, 12);
            if (cache.TryGetValue(key, out var hit)) return hit;
            double gStar = mexp.Flux(pT);
            double ge = gT / rE;
            if (ge > gStar * (1 + 1e-9))
                throw new ArgumentException(FormattableString.Invariant(
                    $"nozzle exit/throat area ratio {rE:F4} is below the discharge coefficient {CdM:F4}: with Cd applied at the throat, the equilibrium flow in the diverging section cannot pass the corrected motive flow"));
            double pSup;
            try
            {
                pSup = Numerics.Brentq(P => mexp.Flux(P) - ge, pLow, pT, 1e-6 * pT);
            }
            catch (RootBracketException)
            {
                throw new ArgumentException(FormattableString.Invariant(
                    $"motive nozzle over-expands below the search floor ({pLow / 1e5:F3} bar) at Ae/At = {rE:F3}"));
            }
            double pFc = Numerics.Brentq(P => mexp.Flux(P) - ge, pT, mexp.Inlet.P * (1 - 1e-12), 1e-7 * pT);
            var (stSup, uSup) = mexp.At(pSup);
            var shock = Mix(1.0, new[] { (pSup, ge * uSup) }, ge, mexp.Inlet.H, mexp.Inlet.Z, eta: 1.0);
            double pSe = shock.HasValue ? Math.Min(shock.Value.P3, pFc) : pFc;
            var ex = new CdExitInfo { Ge = ge, PSup = pSup, StSup = stSup, USup = uSup, PFc = pFc, PSe = pSe };
            cache[key] = ex;
            return ex;
        }

        /// <summary>
        /// Motive flow, throat pressure, exit pressure, exit state and jet velocity at mixing-inlet pressure pY.
        /// Converging nozzle (Ae &lt;= 1.0001 At): choked below Pt (exit = throat), else subsonic at pY.
        /// Converging-diverging: choked below PFc, with the supersonic exit state below PSe and a subsonic
        /// post-shock exit at pY between PSe and PFc; exit-controlled flow G(pY)*Ae above PFc.
        /// </summary>
        private (double Mm, double Pt, double Pe, FluidState StE, double Ue) MotiveState(
            Expansion mexp, double pT, double gT, double aT, double aE, double pY, double pLow,
            Dictionary<double, CdExitInfo>? cache = null)
        {
            double rE = aE / aT;
            if (rE <= 1.0001)
            {
                if (pY >= pT)
                {
                    double g = mexp.Flux(pY) * CdM;
                    var (stE0, uE0) = mexp.At(pY);
                    return (g * aT, pY, pY, stE0, uE0);
                }
                var (stT, uT) = mexp.At(pT);
                return (gT * aT, pT, pT, stT, uT);
            }
            var ex = CdExit(mexp, pT, gT, rE, pLow, cache ?? new Dictionary<double, CdExitInfo>());
            if (pY >= ex.PFc)
            {
                var (stU, uU) = mexp.At(pY);                    // unchoked: the exit controls the flow
                return (stU.Rho * uU * aE, pY, pY, stU, uU);
            }
            double mM = gT * aT;
            if (pY < ex.PSe) return (mM, pT, ex.PSup, ex.StSup, ex.USup);
            // normal shock inside the diverging section: subsonic exit at pY, same h0 and mass flux
            double h0 = mexp.Inlet.H;
            double[]? z = mexp.Inlet.Z;
            double F(double h) => _th.PH(pY, h, z).Rho * Math.Sqrt(Math.Max(2.0 * (h0 - h), 0.0)) - ex.Ge;
            double hIsen = mexp.At(pY).State.H;
            double scale = Math.Max(Math.Abs(h0), 1.0);
            double hSol = Numerics.Brentq(F, hIsen, h0 - 1e-9 * scale, 1e-9 * scale);
            var stE = _th.PH(pY, hSol, z);
            return (mM, pT, pY, stE, Math.Sqrt(2.0 * (h0 - hSol)));
        }

        /// <summary>Motive flow per unit throat area at pY (sizing a fixed shape to a given motive flow).</summary>
        private double ThroatFlux(Expansion mexp, double pT, double gT, double rE, double pY, double pLow,
                                  Dictionary<double, CdExitInfo> cache)
        {
            if (rE <= 1.0001) return pY < pT ? gT : mexp.Flux(pY) * CdM;
            var ex = CdExit(mexp, pT, gT, rE, pLow, cache);
            return pY < ex.PFc ? gT : mexp.Flux(pY) * rE;
        }

        private EjectorResult Make(Regime regime, double pY, Pass r, FluidState m0, FluidState s0, double er,
                                   FluidState? outSt = null)
        {
            var o = outSt ?? r.Out;
            return new EjectorResult
            {
                Regime = regime, Mm = r.Mm, Ms = r.Ms, ER = er, Pout = o.P, Pt = r.Pt, Pe = r.Pe, Py = pY,
                P3 = r.P3, At = r.At, Ae = r.Ae, Amix = r.Amix, Ue = r.Ue, Uy = r.Uy, U3 = r.U3, Out = o,
                EtaEjector = EtaEjector(m0, s0, o, er),
                StateM0 = m0, StateS0 = s0, StateE = r.StE, StateY = r.StY, StateMix = r.St3,
            }.CheckSolid(_th.PTriple);
        }

        // ================================================================== RATING
        /// <summary>
        /// Off-design performance of a given geometry at a given outlet pressure
        /// (techref sec-rating, sec-rating-algorithm).
        /// </summary>
        public EjectorResult Rate(FluidState m0, FluidState s0, double aT, double aE, double aMix,
                                  double pOut, int nScan = 40)
        {
            int n0 = _th.FlashCount;
            double pLow = PLow(m0, s0);
            var (mexp, pT, gT) = Motive(m0, pLow);
            var sexp = new Expansion(_th, s0, EtaS);
            var (pYs, gSMax) = sexp.Critical(pLow);
            var cache = new Dictionary<double, CdExitInfo>();

            Pass? Run(double py)
            {
                var (mM, pt, pe, stE, uE) = MotiveState(mexp, pT, gT, aT, aE, py, pLow, cache);
                var r = DoPass(m0, s0, mM, pe, stE, uE, aE, aMix, py, sexp, gSMax, mexp);
                if (r != null)
                {
                    r.Mm = mM; r.Pt = pt; r.Pe = pe; r.StE = stE; r.Ue = uE;
                    r.At = aT; r.Ae = aE; r.Amix = aMix; r.ER = r.Ms / mM;
                }
                return r;
            }

            var pys = Numerics.Linspace(pYs, s0.P * 0.99999, nScan);
            var rows = pys.Select(Run).ToArray();
            int k0 = CriticalPoint(pys, rows, Run, s0);    // most-entraining feasible point
            double erC = rows[k0]!.Ms / rows[k0]!.Mm, pbC = rows[k0]!.Out.P;
            EjectorResult? res = null;
            if (pOut <= pbC)
            {
                // critical: flows as at the critical point; the excess lift is lost in shocks
                // downstream, so the outlet is the mixed stagnation enthalpy at the specified pOut
                var rc = rows[k0]!;
                var outC = _th.PH(pOut, rc.Out.H, MixedComposition(m0, s0, rc.Mm, rc.Ms));
                res = Make(Regime.Critical, pys[k0], rc, m0, s0, rc.ER, outC);
            }
            else
            {
                for (int k = k0; k < pys.Length - 1; k++)
                {
                    var a = rows[k];
                    var b = rows[k + 1];
                    if (a != null && b != null && (a.Out.P - pOut) * (b.Out.P - pOut) <= 0)
                    {
                        double py = Numerics.Brentq(p => Run(p)!.Out.P - pOut, pys[k], pys[k + 1], 1e-6 * s0.P);
                        var r = Run(py)!;
                        if (Math.Abs(r.Out.P - pOut) > 1e-4 * pOut)
                            throw new EjectorException(
                                FormattableString.Invariant($"specified outlet pressure {pOut / 1e5:F4} bar falls in a discontinuity of the characteristic between {a.Out.P / 1e5:F4} and {b.Out.P / 1e5:F4} bar ")
                                + FormattableString.Invariant($"(motive nozzle regime change near P_y = {py / 1e5:F4} bar); computed {r.Out.P / 1e5:F4} bar"));
                        res = Make(Regime.Subcritical, py, r, m0, s0, r.ER);
                        break;
                    }
                }
                if (res == null)
                {
                    // backflow: no entrainment; the outlet is the motive stream at pOut
                    var last = rows[rows.Length - 1]!;
                    var outB = _th.PH(pOut, m0.H, m0.Z);
                    res = Make(Regime.Backflow, pys[pys.Length - 1], last, m0, s0, 0.0, outB);
                    res.Ms = 0.0;
                    res.Uy = 0.0;
                }
            }
            res.ERCritical = erC;
            res.PbCritical = pbC;
            res.FlashCount = _th.FlashCount - n0;
            return res;
        }

        // ================================================================== RATING, FLOWS GIVEN
        /// <summary>
        /// Inverse rating: both inlet flows given, outlet pressure computed (the
        /// flowsheet mode; techref sec-flowsheet-specs). The geometry is a fixed SHAPE
        /// (<paramref name="rE"/> = Ae/At, <paramref name="rMix"/> = Amix/At) sized so the
        /// nozzle passes <paramref name="mM"/>; P_y follows from ER(P_y) = mS/mM between the
        /// most-entraining feasible point and zero entrainment.
        /// Fixed SIZE: pass the actual throat area <paramref name="aT"/>; the result then
        /// reports the throat's capacity Cd*G*At and its mismatch with mM.
        /// </summary>
        /// <exception cref="EntrainmentExceededException">mS/mM above the critical ER.</exception>
        public EjectorResult RateFlows(FluidState m0, FluidState s0, double mM, double mS, double rE,
                                       double rMix, double? aT = null, int nScan = 40, int nCurve = 0)
        {
            if (!(rE >= 1.0 && rE < rMix)) throw new ArgumentException("need 1 <= Ae/At < Amix/At");
            int n0 = _th.FlashCount;
            double erReq = mS / mM;
            double pLow = PLow(m0, s0);
            var (mexp, pT, gT) = Motive(m0, pLow);
            var sexp = new Expansion(_th, s0, EtaS);
            var (pYs, gSMax) = sexp.Critical(pLow);

            var cache = new Dictionary<double, CdExitInfo>();

            Pass? Run(double py)
            {
                double at = mM / ThroatFlux(mexp, pT, gT, rE, py, pLow, cache);
                double ae = rE * at, amix = rMix * at;
                var (mm, pt, pe, stE, uE) = MotiveState(mexp, pT, gT, at, ae, py, pLow, cache);
                var r = DoPass(m0, s0, mm, pe, stE, uE, ae, amix, py, sexp, gSMax, mexp);
                if (r != null)
                {
                    r.Mm = mm; r.Pt = pt; r.Pe = pe; r.StE = stE; r.Ue = uE;
                    r.At = at; r.Ae = ae; r.Amix = amix; r.ER = r.Ms / mm;
                }
                return r;
            }

            var pys = Numerics.Linspace(pYs, s0.P * 0.99999, nScan).ToList();
            var rows = pys.Select(Run).ToList();
            int k0 = CriticalPoint(pys, rows, Run, s0);    // most-entraining feasible point
            double erC = rows[k0]!.ER, pbC = rows[k0]!.Out.P;
            var curve = new List<(double, double)>();
            if (nCurve > 0)
            {
                foreach (double py in Numerics.Linspace(pys[k0], s0.P, nCurve))
                {
                    var r = Run(py);
                    if (r != null) curve.Add((r.Out.P, r.ER));
                }
            }
            else
            {
                for (int k = k0; k < rows.Count; k++)
                    if (rows[k] != null) curve.Add((rows[k]!.Out.P, rows[k]!.ER));
            }

            EjectorResult Finish(Regime regime, double py, Pass r)
            {
                var res = Make(regime, py, r, m0, s0, r.ER);
                res.ERCritical = erC;
                res.PbCritical = pbC;
                res.Curve = curve;
                if (aT.HasValue)
                {
                    res.MotiveCapacity = mM * aT.Value / r.At;
                    res.MotiveMismatch = aT.Value / r.At - 1.0;
                }
                res.FlashCount = _th.FlashCount - n0;
                return res;
            }

            if (erReq > erC * (1 + 1e-6))
                throw new EntrainmentExceededException(FormattableString.Invariant(
                    $"suction flow {mS:G4} kg/s (ER {erReq:F4}) exceeds the critical entrainment of this geometry, ER* = {erC:F4}"),
                    erC, pbC);
            if (erReq >= erC * (1 - 1e-6)) return Finish(Regime.Critical, pys[k0], rows[k0]!);

            var ers = rows.Select(r => r != null ? r.ER : double.NaN).ToList();
            pys.Add(s0.P);            // zero entrainment at P_y = P_s0
            ers.Add(0.0);
            for (int k = k0; k < pys.Count - 1; k++)
            {
                double a = ers[k], b = ers[k + 1];
                if (!double.IsNaN(a) && !double.IsNaN(b) && (a - erReq) * (b - erReq) <= 0)
                {
                    double py = erReq <= 1e-9
                        ? s0.P
                        : Numerics.Brentq(p => Run(p)!.ER - erReq, pys[k], pys[k + 1], 1e-7 * s0.P);
                    return Finish(Regime.Subcritical, py, Run(py)!);
                }
            }
            throw new EjectorException("RateFlows: no bracket for the requested entrainment");
        }

        // ================================================================== DESIGN
        /// <summary>
        /// Size an ejector for motive flow mM and entrainment ratio er (techref sec-design):
        /// motive nozzle ideally expanded to P_y; if <paramref name="pY"/> is null, P_y is
        /// chosen to maximise the outlet pressure.
        /// </summary>
        public EjectorResult Design(FluidState m0, FluidState s0, double mM, double er, double? pY = null)
        {
            int n0 = _th.FlashCount;
            double pLow = PLow(m0, s0);
            var (mexp, pT, gT) = Motive(m0, pLow);
            double aT = double.NaN;
            var sexp = new Expansion(_th, s0, EtaS);
            var (pYs, gSMax) = sexp.Critical(pLow);
            double mS = er * mM;

            (Pass? R, double Ae, double Amix, double Ue, FluidState StE) Build(double py)
            {
                double g = py < pT ? gT : mexp.Flux(py) * CdM;
                aT = mM / g;
                var (stE, uE) = mexp.At(py);                // ideally expanded to P_y
                double aE = Math.Max(mM / (stE.Rho * uE), aT);
                var (stY, uY) = sexp.At(py);
                double aSy = mS > 0 ? mS / Math.Max(stY.Rho * uY, 1e-12) : 0.0;
                // CPM: hypothetical throat = PhiP * motive core + suction passage
                double aMix = Mixing == MixingModel.CPM ? PhiP * mM / (stE.Rho * uE) + aSy : aE + aSy;
                var r = DoPass(m0, s0, mM, py, stE, uE, aE, aMix, py, sexp, gSMax, mexp);
                return (r, aE, aMix, uE, stE);
            }

            double NegPout(double py)
            {
                try
                {
                    var r = Build(py).R;
                    return r != null ? -r.Out.P : 0.0;
                }
                catch (Exception ex) when (IsValueError(ex) || ex is SolidPhaseRiskException)
                {
                    return 0.0;
                }
            }

            double py0;
            if (pY.HasValue)
            {
                py0 = pY.Value;
            }
            else
            {
                // 30 linear points plus a log-spaced cluster just below P_s0: at small ER the feasible
                // window is a narrow band there (the suction annulus is otherwise too small)
                var cluster = Numerics.Geomspace(4e-3, 1e-4, 10).Select(e => s0.P * (1.0 - e));
                var pys = Numerics.Linspace(Math.Max(pYs, pLow), s0.P * 0.995, 30).Concat(cluster)
                                  .Distinct().OrderBy(v => v).ToArray();
                var vals = pys.Select(NegPout).ToArray();
                int k = 0;
                for (int j = 1; j < vals.Length; j++) if (vals[j] < vals[k]) k = j;
                double lo = pys[Math.Max(k - 1, 0)], hi = pys[Math.Min(k + 1, pys.Length - 1)];
                py0 = Numerics.Fminbound(NegPout, lo, hi, 1e-5 * s0.P).X;
            }
            var (rr, ae, amix, ue, ste) = Build(py0);
            if (rr == null) throw new EjectorException("design infeasible: mixing cannot pass the flow");
            var res = new EjectorResult
            {
                Regime = Regime.Design, Mm = mM, Ms = mS, ER = er, Pout = rr.Out.P, Pt = Math.Max(pT, py0),
                Pe = py0, Py = py0, P3 = rr.P3, At = aT, Ae = ae, Amix = amix, Ue = ue, Uy = rr.Uy, U3 = rr.U3,
                Out = rr.Out, EtaEjector = EtaEjector(m0, s0, rr.Out, er),
                StateM0 = m0, StateS0 = s0, StateE = ste, StateY = rr.StY, StateMix = rr.St3,
            }.CheckSolid(_th.PTriple);
            res.FlashCount = _th.FlashCount - n0;
            return res;
        }

        /// <summary>
        /// Largest ER whose optimum design still reaches pOut (techref: design for lift). The optimum-design
        /// outlet pressure is not monotone in ER (at very small ER the constant-area section can hardly pass
        /// the flow), so its maximum is located first; pOut is unreachable only above that maximum, and the
        /// answer is the root on the falling branch.
        /// </summary>
        public EjectorResult DesignForLift(FluidState m0, FluidState s0, double mM, double pOut, double erMax = 20.0)
        {
            double POpt(double er)
            {
                try { return Design(m0, s0, mM, er).Pout; }
                catch (Exception ex) when (ex is EjectorException || IsValueError(ex) || ex is SolidPhaseRiskException)
                {
                    return double.NegativeInfinity;
                }
            }

            var grid = new[] { 1e-4, 3e-4, 1e-3, 2e-3, 5e-3, 0.01, 0.02, 0.05, 0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 6.4, 12.8, erMax };
            var vals = new List<double>();
            int kBest = 0;
            for (int k = 0; k < grid.Length; k++)
            {
                vals.Add(POpt(grid[k]));
                if (vals[k] > vals[kBest]) kBest = k;
                if (k > kBest && vals[k] < pOut && pOut <= vals[kBest]) break;
            }
            double lo = grid[Math.Max(kBest - 1, 0)], hi = grid[Math.Min(kBest + 1, vals.Count - 1)];
            double erBest = grid[kBest], pBest = vals[kBest];
            if (lo < hi)
            {
                var m = Numerics.Fminbound(e => -POpt(e), lo, hi, 1e-6);
                if (-m.F > pBest) { erBest = m.X; pBest = -m.F; }
            }
            if (pBest < pOut)
                throw new EjectorException(FormattableString.Invariant(
                    $"required outlet pressure {pOut / 1e5:F3} bar not reachable: the highest optimum-design outlet pressure is {pBest / 1e5:F3} bar (at ER {erBest:G4})"));
            int kHi = -1;
            for (int k = kBest + 1; k < vals.Count; k++) if (vals[k] < pOut) { kHi = k; break; }
            if (kHi < 0)
                throw new EjectorException(FormattableString.Invariant($"required outlet pressure {pOut / 1e5:F3} bar is reached up to ER_max = {erMax}"));
            double erSol = Numerics.Brentq(e => POpt(e) - pOut, Math.Max(erBest, grid[kHi - 1]), grid[kHi], 1e-5);
            return Design(m0, s0, mM, erSol);
        }
    }
}
