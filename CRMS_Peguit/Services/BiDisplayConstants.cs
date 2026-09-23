using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ScottPlot;
using ScottPlot.WinForms;
using Color = System.Drawing.Color;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Shared visual constants, number/date formatters, semantic status colors,
    /// and ScottPlot charting configurations for NEXA Business Intelligence surfaces
    /// (DashboardView, AnalyticsView, ReportsView).
    /// </summary>
    public static class BiDisplayConstants
    {
        // =========================================================================
        // 1. SEMANTIC STATUS & OUTCOME PALETTE
        // =========================================================================
        public static readonly Color StatusWon = Theme.StatusSuccess;          // Emerald (#059669) - Closed, Won, Converted, Resolved, Active
        public static readonly Color StatusPending = Theme.StatusPending;      // Amber (#D97706) - Contacted, In Progress, Offer, Contract, Review
        public static readonly Color StatusLost = Theme.StatusAlert;           // Rose (#DC2626) - Lost, Overdue, Breached, High, Urgent, Critical
        public static readonly Color StatusNeutral = Theme.StatusNeutral;      // Slate (#475569) - New, Unassigned, Low, Medium, Prospect
        public static readonly Color PrimaryAccent = Theme.Primary;            // Skyline Blue (#25679C)
        public static readonly Color SecondaryAccent = Theme.PrimaryDark;      // Pressed Azure (#0C4D82)
        public static readonly Color HighlightAccent = Color.FromArgb(139, 92, 246); // Violet / Purple (#8B5CF6)
        public static readonly Color SkyAccent = Color.FromArgb(14, 165, 233);       // Sky (#0EA5E9)

        // Tint backgrounds for status pills/badges (WCAG contrast compliant with dark text)
        public static readonly Color StatusWonBg = Color.FromArgb(220, 252, 231);     // Emerald 100
        public static readonly Color StatusPendingBg = Color.FromArgb(254, 243, 199); // Amber 100
        public static readonly Color StatusLostBg = Color.FromArgb(254, 226, 226);    // Rose 100
        public static readonly Color StatusNeutralBg = Color.FromArgb(241, 245, 249); // Slate 100
        public static readonly Color PrimaryTintBg = Color.FromArgb(224, 242, 254);   // Sky 100

        public static Color GetStatusColor(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return StatusNeutral;
            string s = status.Trim().ToLowerInvariant();

            return s switch
            {
                "won" or "closed" or "converted" or "resolved" or "active" or "completed" or "available" or "yes" => StatusWon,
                "in progress" or "contacted" or "pending" or "pending review" or "offer" or "under contract" or "qualified" => StatusPending,
                "lost" or "overdue" or "breached" or "cancelled" or "critical" or "urgent" or "high" or "no" or "inactive" => StatusLost,
                _ => StatusNeutral
            };
        }

        public static Color GetStatusBgColor(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return StatusNeutralBg;
            string s = status.Trim().ToLowerInvariant();

            return s switch
            {
                "won" or "closed" or "converted" or "resolved" or "active" or "completed" or "available" or "yes" => StatusWonBg,
                "in progress" or "contacted" or "pending" or "pending review" or "offer" or "under contract" or "qualified" => StatusPendingBg,
                "lost" or "overdue" or "breached" or "cancelled" or "critical" or "urgent" or "high" or "no" or "inactive" => StatusLostBg,
                _ => StatusNeutralBg
            };
        }

        public static Color GetPriorityColor(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority)) return StatusNeutral;
            string p = priority.Trim().ToLowerInvariant();

            return p switch
            {
                "critical" or "urgent" or "high" => StatusLost,
                "medium" => StatusPending,
                "low" => StatusNeutral,
                _ => PrimaryAccent
            };
        }

        // =========================================================================
        // 2. UNIFIED NUMBER & DATE FORMATTING
        // =========================================================================
        public static string FormatCurrency(decimal amount) => $"₱{amount:N2}";
        public static string FormatCurrency(double amount) => $"₱{amount:N2}";

        public static string FormatCompactCurrency(decimal amount)
        {
            if (Math.Abs(amount) >= 1_000_000m)
                return $"₱{amount / 1_000_000m:N1}M";
            if (Math.Abs(amount) >= 1_000m)
                return $"₱{amount / 1_000m:N0}k";
            return $"₱{amount:N0}";
        }

        public static string FormatPercent(double percent) => $"{percent:F1}%";
        public static string FormatCount(int count) => count.ToString("N0");

        public static string FormatDate(DateTime date) => date.ToString("MMM dd, yyyy");
        public static string FormatDateTime(DateTime date) => date.ToString("MMM dd, yyyy h:mm tt");
        public static string FormatTime(DateTime date) => date.ToString("h:mm tt");

        // =========================================================================
        // 3. SCOTTPLOT 5 STANDARDIZED CHART HELPERS
        // =========================================================================
        public static void ConfigureStandardPlot(FormsPlot? plot)
        {
            if (plot == null) return;
            try
            {
                plot.UserInputProcessor.Disable();
                plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(Color.White);
                plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(Color.White);

                // Softer axis frame color (Slate 300)
                plot.Plot.Axes.Color(ScottPlot.Color.FromHex("#CBD5E1"));
                plot.Plot.Axes.Left.IsVisible = true;
                plot.Plot.Axes.Bottom.IsVisible = true;
                plot.Plot.Axes.Top.IsVisible = false;
                plot.Plot.Axes.Right.IsVisible = false;
                plot.Plot.Axes.Bottom.MinimumSize = 45;
                plot.Plot.Axes.Left.MinimumSize = 52;

                // Modern Segoe UI tick label styling
                plot.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#64748B");
                plot.Plot.Axes.Left.TickLabelStyle.FontName = "Segoe UI";
                plot.Plot.Axes.Left.TickLabelStyle.FontSize = 10;
                plot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#475569");
                plot.Plot.Axes.Bottom.TickLabelStyle.FontName = "Segoe UI";
                plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10;

                // Softer horizontal-only grid lines (Slate 100, minimalist modern)
                plot.Plot.Grid.XAxisStyle.IsVisible = false;
                plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#F1F5F9");
                plot.Plot.Grid.MajorLineWidth = 1;
                plot.Plot.ShowGrid();
            }
            catch { }
        }

        public static void ShowPlotEmpty(FormsPlot plot, string message)
        {
            plot.Plot.Clear();
            ConfigureStandardPlot(plot);
            var txt = plot.Plot.Add.Text($"📊  {message}", 0, 0);
            txt.LabelAlignment = Alignment.MiddleCenter;
            txt.LabelFontSize = 13.5f;
            txt.LabelFontName = "Segoe UI";
            txt.LabelFontColor = ScottPlot.Color.FromHex("#94A3B8");
            txt.LabelItalic = true;
            plot.Plot.Axes.Frameless();
            plot.Plot.HideGrid();
            plot.Plot.Axes.SetLimits(-1, 1, -1, 1);
            plot.Refresh();
        }

        private static string FormatTrendLabel(string rawLabel, bool simplifyYear = true)
        {
            if (string.IsNullOrWhiteSpace(rawLabel)) return "";
            if (simplifyYear && rawLabel.Length > 4 && char.IsDigit(rawLabel[^1]) && rawLabel.Contains(' '))
            {
                var parts = rawLabel.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return parts[0]; // e.g. "Jan"
                }
            }
            return rawLabel;
        }

        /// <summary>
        /// Renders a trend over time as a proper LINE chart with solid markers and data labels.
        /// </summary>
        public static void RenderTrendLinePlot(FormsPlot plot, List<(string label, double value)> data, Color? lineColor = null, Color? markerColor = null)
        {
            plot.Plot.Clear();
            ConfigureStandardPlot(plot);

            if (data == null || data.Count == 0)
            {
                ShowPlotEmpty(plot, "No trend data recorded for selected period");
                return;
            }

            plot.Plot.Axes.Left.IsVisible = true;
            plot.Plot.Axes.Bottom.IsVisible = true;
            plot.Plot.Axes.Top.IsVisible = false;
            plot.Plot.Axes.Right.IsVisible = false;
            plot.Plot.ShowGrid();
            plot.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();

            var lineClr = lineColor ?? PrimaryAccent;
            var markClr = markerColor ?? PrimaryAccent;

            double[] xs = data.Select((_, i) => (double)i).ToArray();
            double[] ys = data.Select(d => d.value).ToArray();

            var scatter = plot.Plot.Add.Scatter(xs, ys);
            scatter.Color = ScottPlot.Color.FromColor(lineClr);
            scatter.LineWidth = 2.5f;
            scatter.MarkerSize = 8f;
            scatter.MarkerShape = MarkerShape.FilledCircle;
            scatter.MarkerFillColor = ScottPlot.Color.FromColor(markClr);
            scatter.MarkerLineColor = ScottPlot.Color.FromColor(Color.White);
            scatter.MarkerLineWidth = 1.5f;

            // Subtle area tint under the trend curve
            scatter.FillY = true;
            scatter.FillYColor = ScottPlot.Color.FromColor(Color.FromArgb(20, lineClr.R, lineClr.G, lineClr.B));

            // Add clear data value label above each point (just like categorical bar charts)
            double maxY = ys.Length > 0 ? ys.Max() : 10;
            for (int i = 0; i < data.Count; i++)
            {
                double val = data[i].value;
                if (val > 0)
                {
                    string valDisplay = val >= 1_000 ? $"{val:N0}" : (val % 1 == 0 ? $"{val:N0}" : $"{val:N1}");
                    var txt = plot.Plot.Add.Text(valDisplay, i, val);
                    txt.LabelAlignment = Alignment.LowerCenter;
                    txt.LabelFontSize = 10;
                    txt.LabelFontName = "Segoe UI";
                    txt.LabelFontColor = ScottPlot.Color.FromHex("#0F172A"); // Slate 900
                    txt.LabelBold = true;
                }
            }

            // Simplify year if all ticks share the same year (e.g. "Jan 2026" -> "Jan")
            bool allSameYear = data.Count > 1 && data.All(d => d.label.Length >= 4 && char.IsDigit(d.label[^1]) && d.label[^4..] == data[0].label[^4..]);
            var ticks = data.Select((d, i) => new ScottPlot.Tick(i, allSameYear ? FormatTrendLabel(d.label, true) : d.label)).ToArray();
            var tickGen = new ScottPlot.TickGenerators.NumericManual(ticks);
            plot.Plot.Axes.Bottom.TickGenerator = tickGen;

            if (data.Count <= 12)
            {
                plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
                plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperCenter;
                plot.Plot.Axes.Bottom.MinimumSize = 36;
            }
            else
            {
                plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = -25;
                plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
                plot.Plot.Axes.Bottom.MinimumSize = 52;
            }

            plot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#475569");
            plot.Plot.Axes.Bottom.TickLabelStyle.FontName = "Segoe UI";
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
            plot.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#64748B");
            plot.Plot.Axes.Left.TickLabelStyle.FontName = "Segoe UI";
            plot.Plot.Axes.Left.TickLabelStyle.FontSize = 10;
            plot.Plot.Axes.Left.MinimumSize = 48;

            // Give balanced horizontal padding and enough top headroom for data labels
            double topHeadroom = maxY > 0 ? maxY * 1.25 : 10;
            plot.Plot.Axes.SetLimits(-0.5, xs.Length - 0.5, 0, topHeadroom);
            plot.Refresh();
        }

        private static string CleanDonutLabel(string rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawLabel)) return "";
            int parenIdx = rawLabel.IndexOf('(');
            return parenIdx > 0 ? rawLabel.Substring(0, parenIdx).Trim() : rawLabel.Trim();
        }

        /// <summary>
        /// Renders a part-of-whole Donut chart enforcing a max 5-slice limit with automatic "Other" bucket.
        /// </summary>
        public static void RenderDonutPlot(FormsPlot plot, IEnumerable<(string label, double value, Color color)> rawSlices, int maxSlices = 5)
        {
            plot.Plot.Clear();
            ConfigureStandardPlot(plot);

            var sliceList = rawSlices?.ToList() ?? new List<(string label, double value, Color color)>();
            if (sliceList.Count == 0 || sliceList.All(s => s.value <= 0.0001))
            {
                ShowPlotEmpty(plot, "No segmentation data in selected period");
                return;
            }

            var activeSlices = sliceList.Where(s => s.value > 0.0001).OrderByDescending(s => s.value).ToList();
            double totalSum = activeSlices.Sum(s => s.value);

            var slices = new List<PieSlice>();
            if (activeSlices.Count <= maxSlices)
            {
                foreach (var (lbl, val, col) in activeSlices)
                {
                    double pct = totalSum > 0 ? (val / totalSum) * 100.0 : 0;
                    string valStr = val >= 1_000 ? $"{val:N0}" : (val % 1 == 0 ? $"{val:N0}" : $"{val:N1}");
                    string clean = CleanDonutLabel(lbl);

                    slices.Add(new PieSlice
                    {
                        Value = val,
                        FillColor = ScottPlot.Color.FromColor(col),
                        Label = $"{clean}\n{valStr} ({pct:F0}%)"
                    });
                }
            }
            else
            {
                var top = activeSlices.Take(maxSlices - 1).ToList();
                var remaining = activeSlices.Skip(maxSlices - 1).ToList();
                double otherSum = remaining.Sum(r => r.value);

                foreach (var (lbl, val, col) in top)
                {
                    double pct = totalSum > 0 ? (val / totalSum) * 100.0 : 0;
                    string valStr = val >= 1_000 ? $"{val:N0}" : (val % 1 == 0 ? $"{val:N0}" : $"{val:N1}");
                    string clean = CleanDonutLabel(lbl);

                    slices.Add(new PieSlice
                    {
                        Value = val,
                        FillColor = ScottPlot.Color.FromColor(col),
                        Label = $"{clean}\n{valStr} ({pct:F0}%)"
                    });
                }

                if (otherSum > 0)
                {
                    double pct = totalSum > 0 ? (otherSum / totalSum) * 100.0 : 0;
                    string valStr = otherSum >= 1_000 ? $"{otherSum:N0}" : (otherSum % 1 == 0 ? $"{otherSum:N0}" : $"{otherSum:N1}");

                    slices.Add(new PieSlice
                    {
                        Value = otherSum,
                        FillColor = ScottPlot.Color.FromColor(StatusNeutral),
                        Label = $"Other\n{valStr} ({pct:F0}%)"
                    });
                }
            }

            var pie = plot.Plot.Add.Pie(slices);
            pie.DonutFraction = 0.68;
            pie.SliceLabelDistance = 1.35;

            plot.Plot.Axes.Frameless();
            plot.Plot.HideGrid();

            // Calculate responsive bounds to prevent circle distortion or label truncation
            double aspect = 1.6;
            if (plot.ClientSize.Width > 20 && plot.ClientSize.Height > 20)
            {
                aspect = (double)plot.ClientSize.Width / plot.ClientSize.Height;
            }

            double xLim, yLim;
            if (aspect >= 1.0)
            {
                yLim = 1.45;
                xLim = yLim * aspect;
            }
            else
            {
                xLim = 1.45;
                yLim = xLim / aspect;
            }

            plot.Plot.Axes.SetLimits(-xLim, xLim, -yLim, yLim);
            plot.Refresh();
        }

        /// <summary>
        /// Renders a categorical Bar comparison chart with consistent styling, visible axes, and data labels.
        /// </summary>
        public static void RenderBarPlot(FormsPlot plot, List<(string label, double value, Color color)> items, double rotation = 0)
        {
            plot.Plot.Clear();
            ConfigureStandardPlot(plot);

            if (items == null || items.Count == 0)
            {
                ShowPlotEmpty(plot, "No comparison data in selected period");
                return;
            }

            // Ensure axes and grid are fully visible (ShowPlotEmpty or Frameless could have disabled them)
            plot.Plot.Axes.Left.IsVisible = true;
            plot.Plot.Axes.Bottom.IsVisible = true;
            plot.Plot.Axes.Top.IsVisible = false;
            plot.Plot.Axes.Right.IsVisible = false;
            plot.Plot.ShowGrid();
            plot.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();

            var bars = new List<Bar>();
            var ticks = new List<ScottPlot.Tick>();

            // Adapt bar width so bars look substantial and fill the category spaces nicely
            double barSize = items.Count switch
            {
                1 => 0.40,
                2 => 0.52,
                3 => 0.62,
                4 => 0.68,
                _ => 0.72
            };

            double maxY = 0;

            for (int i = 0; i < items.Count; i++)
            {
                double val = items[i].value;
                if (val > maxY) maxY = val;

                bars.Add(new Bar
                {
                    Position = i,
                    Value = val,
                    Size = barSize,
                    FillColor = ScottPlot.Color.FromColor(items[i].color),
                    LineWidth = 0.5f,
                    LineColor = ScottPlot.Color.FromColor(Color.FromArgb(30, 0, 0, 0))
                });
                ticks.Add(new ScottPlot.Tick(i, items[i].label));

                // Add formatted value label above each bar (Segoe UI, Bold, Slate 900)
                if (val > 0)
                {
                    string valDisplay = val >= 1_000 ? $"{val:N0}" : (val % 1 == 0 ? $"{val:N0}" : $"{val:N1}");
                    var txt = plot.Plot.Add.Text(valDisplay, i, val);
                    txt.LabelAlignment = Alignment.LowerCenter;
                    txt.LabelFontSize = 11;
                    txt.LabelFontName = "Segoe UI";
                    txt.LabelFontColor = ScottPlot.Color.FromHex("#0F172A");
                    txt.LabelBold = true;
                }
            }

            plot.Plot.Add.Bars(bars);
            var tickGen = new ScottPlot.TickGenerators.NumericManual(ticks.ToArray());
            plot.Plot.Axes.Bottom.TickGenerator = tickGen;

            float effRotation = (float)rotation;
            if (Math.Abs(effRotation) < 0.01f && items.Count > 4)
            {
                effRotation = -25f;
            }

            if (Math.Abs(effRotation) > 0.01f)
            {
                plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = effRotation;
                plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
                plot.Plot.Axes.Bottom.MinimumSize = 58;
            }
            else
            {
                plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
                plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperCenter;
                plot.Plot.Axes.Bottom.MinimumSize = 40;
            }

            plot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#475569");
            plot.Plot.Axes.Bottom.TickLabelStyle.FontName = "Segoe UI";
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10.5f;
            plot.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#64748B");
            plot.Plot.Axes.Left.TickLabelStyle.FontName = "Segoe UI";
            plot.Plot.Axes.Left.TickLabelStyle.FontSize = 10.5f;
            plot.Plot.Axes.Left.MinimumSize = 50;

            // Frame chart nicely with optimized headroom for taller bars
            double topHeadroom = maxY > 0 ? maxY * 1.15 : 10;
            plot.Plot.Axes.SetLimits(-0.6, items.Count - 0.4, 0, topHeadroom);
            plot.Refresh();
        }

        public static void RenderBarPlot(FormsPlot plot, string[] labels, double[] values, Color[]? barColors = null, double rotation = -25)
        {
            var items = new List<(string label, double value, Color color)>();
            for (int i = 0; i < labels.Length && i < values.Length; i++)
            {
                Color col = barColors != null && i < barColors.Length ? barColors[i] : PrimaryAccent;
                items.Add((labels[i], values[i], col));
            }
            RenderBarPlot(plot, items, rotation);
        }
    }
}
