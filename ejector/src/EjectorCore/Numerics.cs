using System;

namespace EjectorCore
{
    /// <summary>
    /// Scalar root finding and bounded minimisation, ported from SciPy so the C#
    /// engine follows the Python reference implementation step for step:
    /// <see cref="Brentq"/> = scipy.optimize.brentq (Brent 1973, zeros.c),
    /// <see cref="Fminbound"/> = scipy.optimize.minimize_scalar(method="bounded").
    /// </summary>
    public static class Numerics
    {
        private const double Eps = 2.220446049250313e-16;

        /// <summary>Root of f in [a, b]; f(a) and f(b) must differ in sign.</summary>
        /// <exception cref="RootBracketException">f(a), f(b) of the same sign.</exception>
        public static double Brentq(Func<double, double> f, double a, double b, double xtol,
                                    double rtol = 4 * Eps, int maxIter = 100)
        {
            double xpre = a, xcur = b, xblk = 0.0, fblk = 0.0, spre = 0.0, scur = 0.0;
            double fpre = f(xpre), fcur = f(xcur);
            if (fpre == 0.0) return xpre;
            if (fcur == 0.0) return xcur;
            if (double.IsNaN(fpre) || double.IsNaN(fcur) || (fpre < 0) == (fcur < 0))
                throw new RootBracketException("f(a) and f(b) must have different signs");
            for (int i = 0; i < maxIter; i++)
            {
                if (fpre != 0.0 && fcur != 0.0 && (fpre < 0) != (fcur < 0))
                {
                    xblk = xpre; fblk = fpre;
                    spre = scur = xcur - xpre;
                }
                if (Math.Abs(fblk) < Math.Abs(fcur))
                {
                    xpre = xcur; xcur = xblk; xblk = xpre;
                    fpre = fcur; fcur = fblk; fblk = fpre;
                }
                double delta = (xtol + rtol * Math.Abs(xcur)) / 2.0;
                double sbis = (xblk - xcur) / 2.0;
                if (fcur == 0.0 || Math.Abs(sbis) < delta) return xcur;
                if (Math.Abs(spre) > delta && Math.Abs(fcur) < Math.Abs(fpre))
                {
                    double stry;
                    if (xpre == xblk)
                    {
                        stry = -fcur * (xcur - xpre) / (fcur - fpre);           // interpolate
                    }
                    else
                    {                                                            // extrapolate
                        double dpre = (fpre - fcur) / (xpre - xcur);
                        double dblk = (fblk - fcur) / (xblk - xcur);
                        stry = -fcur * (fblk * dblk - fpre * dpre) / (dblk * dpre * (fblk - fpre));
                    }
                    if (2 * Math.Abs(stry) < Math.Min(Math.Abs(spre), 3 * Math.Abs(sbis) - delta))
                    {
                        spre = scur; scur = stry;                                // good short step
                    }
                    else
                    {
                        spre = sbis; scur = sbis;                                // bisect
                    }
                }
                else
                {
                    spre = sbis; scur = sbis;
                }
                xpre = xcur; fpre = fcur;
                if (Math.Abs(scur) > delta) xcur += scur;
                else xcur += sbis > 0 ? delta : -delta;
                fcur = f(xcur);
            }
            return xcur;   // SciPy warns (RuntimeWarning) and returns the last iterate
        }

        /// <summary>Bounded scalar minimisation (Brent's golden-section/parabolic method).</summary>
        public static (double X, double F) Fminbound(Func<double, double> func, double x1, double x2,
                                                     double xatol = 1e-5, int maxIter = 500)
        {
            double sqrtEps = Math.Sqrt(2.2e-16);
            double goldenMean = 0.5 * (3.0 - Math.Sqrt(5.0));
            double a = x1, b = x2;
            double fulc = a + goldenMean * (b - a);
            double nfc = fulc, xf = fulc;
            double rat = 0.0, e = 0.0;
            double x = xf;
            double fx = func(x);
            int num = 1;
            double ffulc = fx, fnfc = fx;
            double xm = 0.5 * (a + b);
            double tol1 = sqrtEps * Math.Abs(xf) + xatol / 3.0;
            double tol2 = 2.0 * tol1;
            while (Math.Abs(xf - xm) > (tol2 - 0.5 * (b - a)))
            {
                bool golden = true;
                if (Math.Abs(e) > tol1)
                {                                                     // parabolic fit
                    golden = false;
                    double r = (xf - nfc) * (fx - ffulc);
                    double q = (xf - fulc) * (fx - fnfc);
                    double p = (xf - fulc) * q - (xf - nfc) * r;
                    q = 2.0 * (q - r);
                    if (q > 0.0) p = -p;
                    q = Math.Abs(q);
                    r = e;
                    e = rat;
                    if (Math.Abs(p) < Math.Abs(0.5 * q * r) && p > q * (a - xf) && p < q * (b - xf))
                    {
                        rat = (p + 0.0) / q;
                        x = xf + rat;
                        if ((x - a) < tol2 || (b - x) < tol2)
                        {
                            double si0 = Sgn(xm - xf) + ((xm - xf) == 0 ? 1 : 0);
                            rat = tol1 * si0;
                        }
                    }
                    else
                    {
                        golden = true;
                    }
                }
                if (golden)
                {
                    e = xf >= xm ? a - xf : b - xf;
                    rat = goldenMean * e;
                }
                double si = Sgn(rat) + (rat == 0 ? 1 : 0);
                x = xf + si * Math.Max(Math.Abs(rat), tol1);
                double fu = func(x);
                num++;
                if (fu <= fx)
                {
                    if (x >= xf) a = xf; else b = xf;
                    fulc = nfc; ffulc = fnfc;
                    nfc = xf; fnfc = fx;
                    xf = x; fx = fu;
                }
                else
                {
                    if (x < xf) a = x; else b = x;
                    if (fu <= fnfc || nfc == xf)
                    {
                        fulc = nfc; ffulc = fnfc;
                        nfc = x; fnfc = fu;
                    }
                    else if (fu <= ffulc || fulc == xf || fulc == nfc)
                    {
                        fulc = x; ffulc = fu;
                    }
                }
                xm = 0.5 * (a + b);
                tol1 = sqrtEps * Math.Abs(xf) + xatol / 3.0;
                tol2 = 2.0 * tol1;
                if (num >= maxIter) break;
            }
            return (xf, fx);
        }

        /// <summary>numpy.sign without the NaN exception of Math.Sign (NaN gives 0).</summary>
        private static double Sgn(double x) => x > 0 ? 1.0 : x < 0 ? -1.0 : 0.0;

        /// <summary>numpy.linspace (endpoint included).</summary>
        public static double[] Linspace(double a, double b, int n)
        {
            var v = new double[n];
            if (n == 1) { v[0] = a; return v; }
            double step = (b - a) / (n - 1);
            for (int i = 0; i < n; i++) v[i] = a + i * step;
            v[n - 1] = b;
            return v;
        }

        /// <summary>numpy.geomspace (endpoints exact).</summary>
        public static double[] Geomspace(double a, double b, int n)
        {
            var v = new double[n];
            double la = Math.Log10(a), lb = Math.Log10(b);
            double step = n > 1 ? (lb - la) / (n - 1) : 0.0;
            for (int i = 0; i < n; i++) v[i] = Math.Pow(10.0, la + i * step);
            v[0] = a;
            if (n > 1) v[n - 1] = b;
            return v;
        }
    }
}
