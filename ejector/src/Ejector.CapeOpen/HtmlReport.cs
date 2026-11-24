using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EjectorCore;

namespace Ejector.CapeOpen
{
    /// <summary>Everything a report needs from one calculation.</summary>
    internal sealed class ReportData
    {
        public ReportData(EjectorResult r, EjectorResult? curve, InletStream mot, InletStream suc, double mM, double mS,
                          FluidState m0, FluidState s0, string mode, string geometry, string preset,
                          EffSet eff, int flashes, double seconds)
        {
            R = r; CurveSource = curve; Mot = mot; Suc = suc; MM = mM; MS = mS; M0 = m0; S0 = s0;
            Mode = mode; GeometrySpec = geometry; Preset = preset; Eff = eff; Flashes = flashes; Seconds = seconds;
        }

        public EjectorResult R { get; }
        public EjectorResult? CurveSource { get; }
        public InletStream Mot { get; }
        public InletStream Suc { get; }
        public double MM { get; }
        public double MS { get; }
        public FluidState M0 { get; }
        public FluidState S0 { get; }
        public string Mode { get; }
        public string GeometrySpec { get; }
        public string Preset { get; }
        public EffSet Eff { get; }
        public int Flashes { get; }
        public double Seconds { get; }
    }

    /// <summary>Plain-text and HTML (inline SVG chart) result reports.</summary>
    internal static class HtmlReport
    {
        private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;
        private const string Navy = "#1F3A5F", Red = "#C0392B", Amber = "#D68910", Slate = "#7F8C8D", Grey = "#34495E";

        private static List<(string Label, string Value)> Rows(ReportData d)
        {
            var r = d.R;
            string F(double v, string fmt) => double.IsNaN(v) ? "n/a" : v.ToString(fmt, Ci);
            string Bar(double p) => F(p / 1e5, "F4") + " bar";
            string Mm(double a) => F(Geometry.Diameter(a) * 1e3, "F3") + " mm";
            var rows = new List<(string, string)>
            {
                ("Mode", d.Mode == EjectorUnitOperation.ModeRating ? $"Rating ({d.GeometrySpec})" : "Design (optimum geometry)"),
                ("Regime", r.Regime.ToString()),
                ("Motive inlet", $"{Bar(d.M0.P)}, {F(d.M0.T - 273.15, "F2")} °C, {F(d.MM, "G6")} kg/s"),
                ("Suction inlet", $"{Bar(d.S0.P)}, {F(d.S0.T - 273.15, "F2")} °C, {F(d.MS, "G6")} kg/s"),
                ("Entrainment ratio", F(d.MS / d.MM, "F5")),
                ("Outlet pressure", Bar(r.Pout)),
                ("Pressure lift Pout/Ps", F(r.Pout / d.S0.P, "F4")),
                ("Outlet temperature", F((r.Out?.T ?? double.NaN) - 273.15, "F2") + " °C"),
                ("Outlet vapour mass fraction", F(r.Out?.Q ?? double.NaN, "F4") + "  (-1 liquid, 2 vapour)"),
                ("Throat / exit / suction static / mixing-end pressure", $"{Bar(r.Pt)} / {Bar(r.Pe)} / {Bar(r.Py)} / {Bar(r.P3)}"),
                ("Jet velocity / mixing-end velocity", $"{F(r.Ue, "F1")} / {F(r.U3, "F1")} m/s"),
                ("Throat / exit / mixing diameter", $"{Mm(r.At)} / {Mm(r.Ae)} / {Mm(r.Amix)}"),
                ("Area ratios Ae/At, Amix/At", $"{F(r.Ae / r.At, "F4")}, {F(r.Amix / r.At, "F4")}"),
                ("Ejector efficiency", F(r.EtaEjector, "F4")),
                ("Solid margin (Pmin / Ptriple)", double.IsInfinity(r.SolidMargin) ? "n/a" : F(r.SolidMargin, "F3")),
                ("Efficiencies (m / s / mix / d), Cd", $"{F(d.Eff.EtaM, "F3")} / {F(d.Eff.EtaS, "F3")} / {F(d.Eff.EtaMix, "F3")} / {F(d.Eff.EtaD, "F3")}, {F(d.Eff.Cd, "F3")}  [{d.Preset}]"),
                ("Mixing model", d.Eff.Mixing == Presets.ConstantPressure
                    ? $"constant pressure (hypothetical throat), phi_p {F(d.Eff.PhiP, "F3")}" : "constant area"),
            };
            if (!double.IsNaN(r.ERCritical))
                rows.Add(("Critical ER / critical back pressure", $"{F(r.ERCritical, "F5")} / {Bar(r.PbCritical)}"));
            if (!double.IsNaN(r.MotiveMismatch))
                rows.Add(("Motive capacity of the given throat", $"{F(r.MotiveCapacity, "G6")} kg/s ({F(100 * r.MotiveMismatch, "+0.00;-0.00")} % vs port flow)"));
            rows.Add(("Flashes / time", $"{d.Flashes} / {F(d.Seconds, "F2")} s"));
            return rows;
        }

        public static string BuildText(ReportData d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ejector unit operation - one-dimensional real-fluid model");
            sb.AppendLine("=========================================================");
            foreach (var (l, v) in Rows(d)) sb.AppendLine($"{l,-52}: {v}");
            var c = d.CurveSource?.Curve;
            if (c != null && c.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Operating curve (critical point -> zero entrainment)");
                sb.AppendLine("  P_out [bar]      ER");
                foreach (var (p, er) in c) sb.AppendLine(string.Format(Ci, "  {0,10:F4}  {1,10:F5}", p / 1e5, er));
            }
            if (!double.IsNaN(d.R.MotiveMismatch) && Math.Abs(d.R.MotiveMismatch) > 0.02)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format(Ci,
                    "NOTE: the given throat passes {0:P1} of the motive port flow; adjust the motive pressure or flow upstream to close it.",
                    1.0 + d.R.MotiveMismatch));
            }
            return sb.ToString();
        }

        public static string BuildHtml(ReportData d)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset=\"utf-8\"><style>body{font-family:Arial,sans-serif;color:")
              .Append(Grey).Append(";font-size:12px}h2{color:").Append(Navy)
              .Append("}table{border-collapse:collapse}td{padding:2px 10px;border-bottom:1px solid #ddd}td:first-child{color:")
              .Append(Slate).Append("}</style></head><body><h2>Ejector unit operation</h2><table>");
            foreach (var (l, v) in Rows(d))
                sb.Append("<tr><td>").Append(Esc(l)).Append("</td><td>").Append(Esc(v)).Append("</td></tr>");
            sb.Append("</table>");
            var c = d.CurveSource?.Curve;
            if (c != null && c.Count > 1) sb.Append(Svg(c, d.R.Pout, d.MS / d.MM, d.R.Regime));
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>Operating curve ER(P_out) with the critical plateau and the operating point.</summary>
        private static string Svg(List<(double Pout, double ER)> curve, double pOp, double erOp, Regime regime)
        {
            const int W = 520, H = 320, L = 60, R = 20, T = 20, B = 45;
            var pts = curve.Select(q => (X: q.Pout / 1e5, Y: q.ER)).ToList();
            double xMin = Math.Min(pts.Min(p => p.X), pOp / 1e5), xMax = Math.Max(pts.Max(p => p.X), pOp / 1e5);
            double xPad = 0.08 * (xMax - xMin + 1e-9);
            xMin -= xPad; xMax += xPad;
            double yMax = 1.1 * Math.Max(pts.Max(p => p.Y), erOp) + 1e-12, yMin = 0.0;
            double Sx(double x) => L + (x - xMin) / (xMax - xMin) * (W - L - R);
            double Sy(double y) => H - B - (y - yMin) / (yMax - yMin) * (H - T - B);
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\" font-family=\"Arial\" font-size=\"11\">");
            sb.Append($"<rect x=\"{L}\" y=\"{T}\" width=\"{W - L - R}\" height=\"{H - T - B}\" fill=\"none\" stroke=\"{Grey}\"/>");
            for (int i = 0; i <= 4; i++)
            {
                double x = xMin + i * (xMax - xMin) / 4, y = yMin + i * (yMax - yMin) / 4;
                sb.Append(string.Format(Ci, "<text x=\"{0:F1}\" y=\"{1}\" text-anchor=\"middle\" fill=\"{2}\">{3:F2}</text>", Sx(x), H - B + 15, Grey, x));
                sb.Append(string.Format(Ci, "<text x=\"{0}\" y=\"{1:F1}\" text-anchor=\"end\" fill=\"{2}\">{3:F3}</text>", L - 5, Sy(y) + 4, Grey, y));
            }
            sb.Append(string.Format(Ci, "<text x=\"{0}\" y=\"{1}\" text-anchor=\"middle\" fill=\"{2}\">outlet pressure [bar]</text>", (L + W - R) / 2, H - 8, Grey));
            sb.Append(string.Format(Ci, "<text x=\"14\" y=\"{0}\" text-anchor=\"middle\" fill=\"{1}\" transform=\"rotate(-90 14 {0})\">entrainment ratio [-]</text>", (T + H - B) / 2, Grey));
            // critical plateau: constant ER from the left edge to the critical point
            var c0 = pts[0];
            sb.Append(string.Format(Ci, "<line x1=\"{0:F1}\" y1=\"{1:F1}\" x2=\"{2:F1}\" y2=\"{1:F1}\" stroke=\"{3}\" stroke-width=\"2.5\"/>", Sx(xMin), Sy(c0.Y), Sx(c0.X), Navy));
            var path = string.Join(" ", pts.Select((p, i) => string.Format(Ci, "{0}{1:F1},{2:F1}", i == 0 ? "M" : "L", Sx(p.X), Sy(p.Y))));
            sb.Append($"<path d=\"{path}\" fill=\"none\" stroke=\"{Red}\" stroke-width=\"2.5\"/>");
            sb.Append(string.Format(Ci, "<circle cx=\"{0:F1}\" cy=\"{1:F1}\" r=\"5\" fill=\"{2}\" stroke=\"{3}\"/>", Sx(pOp / 1e5), Sy(erOp), Amber, Grey));
            sb.Append(string.Format(Ci, "<text x=\"{0:F1}\" y=\"{1:F1}\" fill=\"{2}\">operating point ({3})</text>", Sx(pOp / 1e5) + 8, Sy(erOp) - 8, Grey, regime));
            sb.Append(string.Format(Ci, "<text x=\"{0}\" y=\"{1}\" fill=\"{2}\">critical plateau</text>", L + 6, Sy(c0.Y) - 6, Navy));
            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
