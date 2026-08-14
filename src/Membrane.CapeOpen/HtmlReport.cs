using System.Globalization;
using System.Text;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// Renders the membrane result as a self-contained HTML report with an inline SVG profile chart.
    /// COFE detects HTML reports by the leading &lt;!DOCTYPE html&gt; and renders them (errata §2.6), giving a
    /// visual position-profile plot without relying on the PME's array-parameter plotting.
    /// </summary>
    internal static class HtmlReport
    {
        // Report palette.
        private const string Navy = "#1F3A5F", Red = "#C0392B", Amber = "#D68910", Slate = "#7F8C8D", DarkGrey = "#34495E";
        private static readonly string[] SeriesColors = { Red, Navy, Amber, Slate, DarkGrey };

        public static string Build(FeedState feed, MembraneCore.MembraneResult r, double pr, double pp, string flowPattern)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\"><style>");
            sb.AppendLine("body{font-family:Arial,Helvetica,sans-serif;color:#34495E;margin:12px;}");
            sb.AppendLine("h2{color:#1F3A5F;margin:0 0 4px;} h3{color:#1F3A5F;margin:14px 0 4px;}");
            sb.AppendLine("table{border-collapse:collapse;margin:6px 0;} th,td{border:1px solid #7F8C8D;padding:3px 8px;text-align:right;}");
            sb.AppendLine("th{background:#1F3A5F;color:#fff;} td.l,th.l{text-align:left;}");
            sb.AppendLine(".k{color:#7F8C8D;}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h2>Membrane — Gas Permeation</h2>");
            sb.AppendFormat(ci, "<div class='k'>Flow pattern: <b>{0}</b> &nbsp; Feed {1:F2} K, {2:F2} bar &rarr; permeate {3:F2} bar (γ={4:F3}) &nbsp; Feed {5:E3} mol/s</div>",
                flowPattern, feed.Temperature, pr / 1e5, pp / 1e5, pp / pr, feed.MolarFlow);
            sb.AppendFormat(ci, "<div class='k'>Stage cut θ = <b>{0:F4}</b> &nbsp; mass-balance residual {1:E2}</div>", r.StageCut, r.MassBalanceResidual);

            // Results table.
            sb.AppendLine("<h3>Streams</h3><table>");
            sb.AppendLine("<tr><th class='l'>Compound</th><th>Feed x</th><th>Retentate x</th><th>Permeate y</th><th>Recovery</th></tr>");
            for (int i = 0; i < feed.ComponentIds.Length; i++)
                sb.AppendFormat(ci, "<tr><td class='l'>{0}</td><td>{1:F4}</td><td>{2:F4}</td><td>{3:F4}</td><td>{4:F4}</td></tr>",
                    feed.ComponentIds[i], feed.MoleFractions[i], r.RetentateComposition[i], r.PermeateComposition[i], r.ComponentRecovery[i]);
            sb.AppendLine("</table>");

            if (r.Profile != null)
            {
                sb.AppendLine("<h3>Position profiles (feed end &rarr; retentate outlet)</h3>");
                sb.Append(Svg(feed, r.Profile));
                sb.AppendLine("<div class='k'>Solid = retentate mole fraction, dashed = permeate mole fraction, dotted grey = cumulative stage cut. x-axis = membrane position (0..1).</div>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Svg(FeedState feed, MembraneCore.MembraneProfile p)
        {
            var ci = CultureInfo.InvariantCulture;
            const int W = 680, H = 380, L = 60, R = 150, T = 20, B = 40;
            double plotW = W - L - R, plotH = H - T - B;

            double X(double pos) => L + pos * plotW;
            double Y(double frac) => T + (1.0 - frac) * plotH;   // fraction 0..1

            var s = new StringBuilder();
            s.AppendFormat(ci, "<svg xmlns='http://www.w3.org/2000/svg' width='{0}' height='{1}' viewBox='0 0 {0} {1}'>", W, H);
            s.Append("<rect width='100%' height='100%' fill='white'/>");

            // Axes + gridlines + ticks (0,0.25,0.5,0.75,1 both axes).
            for (int t = 0; t <= 4; t++)
            {
                double f = t / 4.0;
                double gy = Y(f), gx = X(f);
                s.AppendFormat(ci, "<line x1='{0:F1}' y1='{1:F1}' x2='{2:F1}' y2='{1:F1}' stroke='#e6eaec'/>", L, gy, L + plotW);
                s.AppendFormat(ci, "<text x='{0:F1}' y='{1:F1}' font-size='10' fill='#34495E' text-anchor='end'>{2:F2}</text>", L - 6, gy + 3, f);
                s.AppendFormat(ci, "<text x='{0:F1}' y='{1:F1}' font-size='10' fill='#34495E' text-anchor='middle'>{2:F2}</text>", gx, T + plotH + 16, f);
            }
            s.AppendFormat(ci, "<line x1='{0}' y1='{1}' x2='{0}' y2='{2}' stroke='#34495E'/>", L, T, T + plotH);
            s.AppendFormat(ci, "<line x1='{0}' y1='{1}' x2='{2}' y2='{1}' stroke='#34495E'/>", L, T + plotH, L + plotW);
            s.AppendFormat(ci, "<text x='{0:F1}' y='{1}' font-size='11' fill='#1F3A5F' text-anchor='middle'>membrane position</text>", L + plotW / 2, H - 6);
            s.AppendFormat(ci, "<text x='14' y='{0:F1}' font-size='11' fill='#1F3A5F' text-anchor='middle' transform='rotate(-90 14 {0:F1})'>mole fraction / stage cut</text>", T + plotH / 2);

            // Component series: retentate (solid) + permeate (dashed).
            int legendY = T + 4;
            for (int i = 0; i < feed.ComponentIds.Length; i++)
            {
                string color = SeriesColors[i % SeriesColors.Length];
                s.AppendFormat(ci, "<polyline fill='none' stroke='{0}' stroke-width='2' points='{1}'/>", color, Points(p.Position, p.Retentate[i], X, Y));
                s.AppendFormat(ci, "<polyline fill='none' stroke='{0}' stroke-width='2' stroke-dasharray='5,4' points='{1}'/>", color, Points(p.Position, p.Permeate[i], X, Y));
                s.AppendFormat(ci, "<rect x='{0}' y='{1}' width='12' height='3' fill='{2}'/><text x='{3}' y='{4}' font-size='11' fill='#34495E'>{5}</text>",
                    L + plotW + 12, legendY, color, L + plotW + 28, legendY + 4, feed.ComponentIds[i]);
                legendY += 18;
            }
            // Stage cut (dotted grey).
            s.AppendFormat(ci, "<polyline fill='none' stroke='{0}' stroke-width='2' stroke-dasharray='2,3' points='{1}'/>", Slate, Points(p.Position, p.StageCut, X, Y));
            s.AppendFormat(ci, "<text x='{0}' y='{1}' font-size='11' fill='{2}'>stage cut</text>", L + plotW + 12, legendY + 4, Slate);

            s.Append("</svg>");
            return s.ToString();
        }

        private static string Points(double[] xs, double[] ys, System.Func<double, double> X, System.Func<double, double> Y)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            for (int k = 0; k < xs.Length; k++)
                sb.AppendFormat(ci, "{0:F1},{1:F1} ", X(xs[k]), Y(ys[k]));
            return sb.ToString();
        }
    }
}
