using System;

namespace MembraneCore.Energy
{
    /// <summary>
    /// Adiabatic non-isothermal energy balance for a membrane module, layered on top of an already-solved
    /// isothermal separation. The separation is unaffected (permeance is temperature-independent and the
    /// driving force is partial pressure); this only computes outlet temperatures.
    ///
    /// Model (per the "separate temperatures, adiabatic, Joule–Thomson on the permeate" choice):
    ///   • Crossing the membrane is a throttling (isenthalpic) step, so the permeated gas keeps the molar
    ///     enthalpy it had leaving the high-pressure side but now sits at permeate pressure. Solving for the
    ///     temperature that restores that enthalpy at the lower pressure yields the Joule–Thomson change.
    ///   • The retentate outlet temperature follows from the overall adiabatic balance
    ///     H_feed = H_permeate + H_retentate, so energy is conserved by construction.
    ///
    /// All enthalpies come from <see cref="IEnthalpyProvider"/> (the PME's real EOS). For an ideal gas the
    /// enthalpy is pressure-independent, so both outlets return to the feed temperature — the correct
    /// no-Joule–Thomson limit.
    /// </summary>
    public static class NonIsothermalEnergy
    {
        private const double Tiny = 1e-12;

        /// <summary>
        /// Compute outlet temperatures (and optional temperature profiles) for the given isothermal separation
        /// under an adiabatic energy balance. Enthalpies are obtained from <paramref name="thermo"/>.
        /// </summary>
        public static EnergyResult Solve(
            IEnthalpyProvider thermo,
            double feedTemperatureK,
            double retentatePressurePa,
            double permeatePressurePa,
            double feedMolarFlow,
            double[] feedZ,
            MembraneResult isothermal,
            bool includeProfile = true)
        {
            if (thermo == null) throw new ArgumentNullException(nameof(thermo));
            if (isothermal == null) throw new ArgumentNullException(nameof(isothermal));

            double theta = Clamp01(isothermal.StageCut);
            double nP = feedMolarFlow * theta;
            double nR = feedMolarFlow * (1.0 - theta);
            double[] y = isothermal.PermeateComposition;
            double[] x = isothermal.RetentateComposition;

            double hFeed = thermo.MolarEnthalpy(feedTemperatureK, retentatePressurePa, feedZ);
            double totalFeedEnthalpy = feedMolarFlow * hFeed;

            // Permeate: isenthalpic expansion from retentate to permeate pressure.
            double permT = feedTemperatureK;
            double permCrossEnthalpy = hFeed; // per-mole, only used when there is permeate
            if (nP > Tiny)
            {
                permCrossEnthalpy = thermo.MolarEnthalpy(feedTemperatureK, retentatePressurePa, y);
                permT = SolveTemperatureForEnthalpy(thermo, permeatePressurePa, y, permCrossEnthalpy, feedTemperatureK);
            }

            // Retentate: whatever enthalpy the adiabatic balance leaves behind, at retentate pressure.
            double retT = feedTemperatureK;
            if (nR > Tiny)
            {
                double retEnthalpy = (totalFeedEnthalpy - nP * permCrossEnthalpy) / nR;
                retT = SolveTemperatureForEnthalpy(thermo, retentatePressurePa, x, retEnthalpy, feedTemperatureK);
            }

            // Closure check at the solved outlet states.
            double permCheck = nP > Tiny ? nP * thermo.MolarEnthalpy(permT, permeatePressurePa, y) : 0.0;
            double retCheck = nR > Tiny ? nR * thermo.MolarEnthalpy(retT, retentatePressurePa, x) : 0.0;
            double residual = Math.Abs(totalFeedEnthalpy - permCheck - retCheck)
                              / Math.Max(1e-30, Math.Abs(totalFeedEnthalpy));

            var result = new EnergyResult(retT, permT, feedTemperatureK, residual);

            if (includeProfile && isothermal.Profile != null)
                BuildProfiles(thermo, feedTemperatureK, retentatePressurePa, permeatePressurePa,
                    feedMolarFlow, totalFeedEnthalpy, isothermal.Profile, result);

            return result;
        }

        /// <summary>
        /// Temperature profiles along the module. At each position the retentate temperature follows from the
        /// adiabatic balance over the sub-module [0, position] (feed in, cumulative permeate out) and the
        /// permeate temperature from the isenthalpic expansion of the cumulative collected permeate. Endpoints
        /// coincide with the overall outlet temperatures.
        /// </summary>
        private static void BuildProfiles(
            IEnthalpyProvider thermo, double feedT, double retentateP, double permeateP,
            double feedMolarFlow, double totalFeedEnthalpy, MembraneProfile profile, EnergyResult result)
        {
            int pts = profile.Points, nc = profile.Components;
            var retProf = new double[pts];
            var permProf = new double[pts];
            var xk = new double[nc];
            var yk = new double[nc];
            double retGuess = feedT, permGuess = feedT;

            for (int k = 0; k < pts; k++)
            {
                double th = Clamp01(profile.StageCut[k]);
                double nPk = feedMolarFlow * th;
                double nRk = feedMolarFlow * (1.0 - th);
                for (int i = 0; i < nc; i++)
                {
                    xk[i] = profile.Retentate[i][k];
                    yk[i] = profile.PermeateCollected[i][k];
                }

                double crossEnthalpy = 0.0;
                if (nPk > Tiny)
                {
                    crossEnthalpy = thermo.MolarEnthalpy(feedT, retentateP, yk);
                    permProf[k] = SolveTemperatureForEnthalpy(thermo, permeateP, yk, crossEnthalpy, permGuess);
                    permGuess = permProf[k];
                }
                else
                {
                    permProf[k] = feedT;
                }

                if (nRk > Tiny)
                {
                    double retEnthalpy = (totalFeedEnthalpy - nPk * crossEnthalpy) / nRk;
                    retProf[k] = SolveTemperatureForEnthalpy(thermo, retentateP, xk, retEnthalpy, retGuess);
                    retGuess = retProf[k];
                }
                else
                {
                    retProf[k] = feedT;
                }
            }

            result.RetentateTemperatureProfile = retProf;
            result.PermeateTemperatureProfile = permProf;
        }

        /// <summary>
        /// Solve h(T, P, x) = target for T. Enthalpy is monotone increasing in T (Cp &gt; 0), so a bracketed
        /// bisection with a secant hint converges robustly regardless of the PME's enthalpy scale/offset.
        /// </summary>
        private static double SolveTemperatureForEnthalpy(
            IEnthalpyProvider thermo, double pressure, double[] x, double target, double guess)
        {
            const double span = 300.0;
            double lo = Math.Max(1.0, guess - span);
            double hi = guess + span;
            double flo = thermo.MolarEnthalpy(lo, pressure, x) - target;
            double fhi = thermo.MolarEnthalpy(hi, pressure, x) - target;

            // Expand the bracket until it straddles the root (or we hit the physical floor / a cap).
            int guard = 0;
            while (flo > 0.0 && lo > 1.0 && guard++ < 50)
            {
                lo = Math.Max(1.0, lo - span);
                flo = thermo.MolarEnthalpy(lo, pressure, x) - target;
            }
            guard = 0;
            while (fhi < 0.0 && guard++ < 50)
            {
                hi += span;
                fhi = thermo.MolarEnthalpy(hi, pressure, x) - target;
            }
            if (flo > 0.0 && fhi > 0.0) return lo;   // target below the coldest reachable enthalpy
            if (flo < 0.0 && fhi < 0.0) return hi;   // target above the hottest bracket

            double tol = 1e-6 * Math.Max(1.0, Math.Abs(target));
            for (int it = 0; it < 100; it++)
            {
                double denom = fhi - flo;
                double mid = Math.Abs(denom) > 1e-30 ? hi - fhi * (hi - lo) / denom : 0.5 * (lo + hi);
                if (mid <= lo || mid >= hi) mid = 0.5 * (lo + hi);

                double fm = thermo.MolarEnthalpy(mid, pressure, x) - target;
                if (Math.Abs(fm) <= tol || (hi - lo) <= 1e-6) return mid;
                if (fm < 0.0) { lo = mid; flo = fm; }
                else { hi = mid; fhi = fm; }
            }
            return 0.5 * (lo + hi);
        }

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);
    }
}
