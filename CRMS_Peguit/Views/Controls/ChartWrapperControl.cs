using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.WinForms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Represents a discrete segment, bar, or slice within an interactive chart.
    /// </summary>
    public class ChartSegment
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public Color Color { get; set; }
        public string? FullTooltip { get; set; }

        // Bar geometry
        public int BarIndex { get; set; } = -1;
        public double BarPosition { get; set; }
        public double BarWidth { get; set; } = 0.6;

        // Donut / Pie geometry
        public double StartAngleDeg { get; set; }
        public double SweepAngleDeg { get; set; }
    }

    /// <summary>
    /// Standardized wrapper control for ScottPlot charting that strictly enforces the NEXA Click Behavior Standard:
    /// - CASE 1 (InPlaceFilter): Screen has a grid -> clicking a segment/bar filters that grid in-place.
    /// - CASE 2 (Navigate): Screen has no grid (Dashboard) -> clicking navigates to target module with filter pre-applied.
    ///
    /// Universal requirements:
    /// - Pointer cursor + visible hover state on all clickable charts.
    /// - In Navigate mode, displays subtle "View full report →" affordance so users know it navigates away.
    /// - Hovering a chart segment shows exact value/label tooltip BEFORE clicking.
    /// - Active in-place filter shows persistent highlighted visual state (accent border + active filter badge).
    /// - Clicking the same segment again clears the filter and returns grid to unfiltered.
    /// </summary>
    public class ChartWrapperControl : Panel
    {
        private readonly Panel _pnlHeader;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private readonly Label _lblActionHint;
        private readonly FlowLayoutPanel _pnlLegend;
        private readonly FormsPlot _plot;
        private readonly ToolTip _toolTip = new();

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public FormsPlot PlotControl => _plot;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ScottPlot.Plot Plot => _plot.Plot;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public KpiClickMode Mode { get; private set; } = KpiClickMode.InPlaceFilter;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? NavigationTarget { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? DefaultNavigationFilter { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string? ActiveSegmentKey { get; private set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsFilterActive => !string.IsNullOrEmpty(ActiveSegmentKey);

        public event Action<string?>? InPlaceFilterChanged;
        public event Action<string, string?>? NavigateRequested;

        private readonly List<ChartSegment> _segments = new();
        private ChartSegment? _hoveredSegment;
        private bool _isCardHovered;
        private Action? _externalNavigationAction;

        private const int CardRadius = 12;
        private const int HeaderHeight = 56;
        private const int LegendMinHeight = 32;

        public ChartWrapperControl() : this("Chart Title", "Chart subtitle", KpiClickMode.InPlaceFilter)
        {
        }

        public ChartWrapperControl(string title, string subtitle, KpiClickMode mode = KpiClickMode.InPlaceFilter)
        {
            Mode = mode;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            Padding = new Padding(14, 12, 14, 12);
            Size = new Size(400, 300);

            // 1. Header panel with title, subtitle, and action hint
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = Color.Transparent
            };

            _lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblSubtitle = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                Location = new Point(1, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblActionHint = new Label
            {
                Text = Mode == KpiClickMode.Navigate ? "View full report →" : "",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Visible = Mode == KpiClickMode.Navigate
            };

            _lblActionHint.Click += (_, _) => OnActionHintClick();

            _pnlHeader.Controls.Add(_lblTitle);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(_lblActionHint);

            // 2. Interactive Legend / Chip Bar
            _pnlLegend = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = LegendMinHeight,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 4, 0, 0)
            };

            // 3. Embedded ScottPlot Control
            _plot = new FormsPlot
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            BiDisplayConstants.ConfigureStandardPlot(_plot);

            Controls.Add(_plot);
            Controls.Add(_pnlLegend);
            Controls.Add(_pnlHeader);

            // Wire hover and interaction
            _plot.MouseMove += Plot_MouseMove;
            _plot.MouseLeave += (_, _) => SetHoveredSegment(null);
            _plot.MouseClick += Plot_MouseClick;

            MouseEnter += (_, _) => SetCardHover(true);
            MouseLeave += (_, _) => SetCardHover(false);
            _pnlHeader.MouseEnter += (_, _) => SetCardHover(true);
            _pnlHeader.MouseLeave += (_, _) => SetCardHover(false);

            if (Mode == KpiClickMode.Navigate)
            {
                Cursor = Cursors.Hand;
                _pnlHeader.Cursor = Cursors.Hand;
                _lblTitle.Cursor = Cursors.Hand;
                _lblSubtitle.Cursor = Cursors.Hand;

                _pnlHeader.Click += (_, _) => TriggerNavigation(null);
                _lblTitle.Click += (_, _) => TriggerNavigation(null);
                _lblSubtitle.Click += (_, _) => TriggerNavigation(null);
            }

            SizeChanged += (_, _) => PositionActionHint();
            PositionActionHint();
        }

        public void SetMode(KpiClickMode mode)
        {
            Mode = mode;
            if (Mode == KpiClickMode.Navigate)
            {
                Cursor = Cursors.Hand;
                _lblActionHint.Text = "View full report →";
                _lblActionHint.Visible = true;
                _lblActionHint.ForeColor = Theme.Primary;
                _pnlHeader.Cursor = Cursors.Hand;
                _lblTitle.Cursor = Cursors.Hand;
                _lblSubtitle.Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
                _pnlHeader.Cursor = Cursors.Default;
                _lblTitle.Cursor = Cursors.Default;
                _lblSubtitle.Cursor = Cursors.Default;
                UpdateActionHintForInPlace();
            }
            PositionActionHint();
            Invalidate();
        }

        public void SetHeader(string title, string subtitle)
        {
            _lblTitle.Text = title;
            _lblSubtitle.Text = subtitle;
            PositionActionHint();
        }

        public void SetNavigationAction(Action action)
        {
            _externalNavigationAction = action;
        }

        public void ClearSegments()
        {
            _segments.Clear();
            _pnlLegend.Controls.Clear();
            _pnlLegend.Visible = false;
        }

        public void RegisterSegment(ChartSegment segment)
        {
            _segments.Add(segment);
        }

        public void ClearFilter()
        {
            if (ActiveSegmentKey != null)
            {
                ActiveSegmentKey = null;
                UpdateActionHintForInPlace();
                UpdateLegendChipSelection();
                Invalidate();
            }
        }

        public void SetActiveFilter(string? key)
        {
            ActiveSegmentKey = key;
            UpdateActionHintForInPlace();
            UpdateLegendChipSelection();
            Invalidate();
        }

        // =========================================================================
        // HIGH-LEVEL CHART RENDERING WITH HIT-TEST REGISTRATION
        // =========================================================================

        public void RenderDonutPlot(IEnumerable<(string label, double value, Color color, string? key)> items, int maxSlices = 5)
        {
            ClearSegments();
            _plot.Plot.Clear();
            BiDisplayConstants.ConfigureStandardPlot(_plot);

            var list = items?.Where(x => x.value > 0.0001).OrderByDescending(x => x.value).ToList() ?? new();
            if (list.Count == 0)
            {
                BiDisplayConstants.ShowPlotEmpty(_plot, "No data recorded in selected period");
                return;
            }

            double totalSum = list.Sum(x => x.value);

            // Calculate angular boundaries for hit testing
            double currentAngle = 0;
            var pieSlices = new List<PieSlice>();

            foreach (var item in list.Take(maxSlices))
            {
                double sweep = totalSum > 0 ? (item.value / totalSum) * 360.0 : 0;
                string segKey = item.key ?? item.label.Trim();

                var seg = new ChartSegment
                {
                    Key = segKey,
                    Label = item.label,
                    Value = item.value,
                    Color = item.color,
                    StartAngleDeg = currentAngle,
                    SweepAngleDeg = sweep,
                    FullTooltip = $"{item.label}: {item.value:N0} ({(totalSum > 0 ? (item.value / totalSum) * 100 : 0):F1}%)"
                };
                RegisterSegment(seg);

                pieSlices.Add(new PieSlice
                {
                    Value = item.value,
                    FillColor = ScottPlot.Color.FromColor(item.color),
                    Label = $"{item.label}\n{item.value:N0}"
                });

                currentAngle += sweep;
            }

            // Handle "Other" slice if exceeding maxSlices
            if (list.Count > maxSlices)
            {
                var others = list.Skip(maxSlices).ToList();
                double otherSum = others.Sum(x => x.value);
                double sweep = (otherSum / totalSum) * 360.0;

                var seg = new ChartSegment
                {
                    Key = "Other",
                    Label = "Other",
                    Value = otherSum,
                    Color = BiDisplayConstants.StatusNeutral,
                    StartAngleDeg = currentAngle,
                    SweepAngleDeg = sweep,
                    FullTooltip = $"Other: {otherSum:N0} ({(otherSum / totalSum) * 100:F1}%)"
                };
                RegisterSegment(seg);

                pieSlices.Add(new PieSlice
                {
                    Value = otherSum,
                    FillColor = ScottPlot.Color.FromColor(BiDisplayConstants.StatusNeutral),
                    Label = $"Other\n{otherSum:N0}"
                });
            }

            var pie = _plot.Plot.Add.Pie(pieSlices);
            pie.DonutFraction = 0.65;
            pie.SliceLabelDistance = 1.35;

            _plot.Plot.Axes.Frameless();
            _plot.Plot.HideGrid();

            double aspect = 1.6;
            if (_plot.ClientSize.Width > 20 && _plot.ClientSize.Height > 20)
                aspect = (double)_plot.ClientSize.Width / _plot.ClientSize.Height;

            double yLim = 1.45;
            double xLim = yLim * Math.Max(1.0, aspect);
            _plot.Plot.Axes.SetLimits(-xLim, xLim, -yLim, yLim);
            _plot.Refresh();

            BuildLegendChips();
        }

        public void RenderBarPlot(IEnumerable<(string label, double value, Color color, string? key)> items, double rotation = 0)
        {
            ClearSegments();
            _plot.Plot.Clear();
            BiDisplayConstants.ConfigureStandardPlot(_plot);

            var list = items?.ToList() ?? new();
            if (list.Count == 0 || list.All(x => x.value <= 0.0001))
            {
                BiDisplayConstants.ShowPlotEmpty(_plot, "No data recorded in selected period");
                return;
            }

            _plot.Plot.Axes.Left.IsVisible = true;
            _plot.Plot.Axes.Bottom.IsVisible = true;
            _plot.Plot.Axes.Top.IsVisible = false;
            _plot.Plot.Axes.Right.IsVisible = false;
            _plot.Plot.ShowGrid();
            _plot.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();

            var bars = new List<ScottPlot.Bar>();
            var ticks = new List<ScottPlot.Tick>();
            double barWidth = list.Count switch { 1 => 0.40, 2 => 0.52, 3 => 0.62, _ => 0.72 };
            double maxY = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it.value > maxY) maxY = it.value;

                string segKey = it.key ?? it.label.Trim();
                var seg = new ChartSegment
                {
                    Key = segKey,
                    Label = it.label,
                    Value = it.value,
                    Color = it.color,
                    BarIndex = i,
                    BarPosition = i,
                    BarWidth = barWidth,
                    FullTooltip = $"{it.label}: {it.value:N0}"
                };
                RegisterSegment(seg);

                bars.Add(new ScottPlot.Bar
                {
                    Position = i,
                    Value = it.value,
                    Size = barWidth,
                    FillColor = ScottPlot.Color.FromColor(it.color),
                    LineWidth = 0.5f,
                    LineColor = ScottPlot.Color.FromColor(Color.FromArgb(30, 0, 0, 0))
                });
                ticks.Add(new ScottPlot.Tick(i, it.label));

                if (it.value > 0)
                {
                    string valDisplay = it.value >= 1_000 ? $"{it.value:N0}" : (it.value % 1 == 0 ? $"{it.value:N0}" : $"{it.value:N1}");
                    var txt = _plot.Plot.Add.Text(valDisplay, i, it.value);
                    txt.LabelAlignment = Alignment.LowerCenter;
                    txt.LabelFontSize = 10.5f;
                    txt.LabelFontName = "Segoe UI";
                    txt.LabelFontColor = ScottPlot.Color.FromHex("#0F172A");
                    txt.LabelBold = true;
                }
            }

            _plot.Plot.Add.Bars(bars);
            _plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks.ToArray());

            float effRotation = (float)rotation;
            if (Math.Abs(effRotation) < 0.01f && list.Count > 4) effRotation = -25f;

            _plot.Plot.Axes.Bottom.TickLabelStyle.Rotation = effRotation;
            _plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = effRotation != 0 ? Alignment.MiddleRight : Alignment.UpperCenter;
            _plot.Plot.Axes.Bottom.MinimumSize = effRotation != 0 ? 55 : 40;
            _plot.Plot.Axes.Left.MinimumSize = 50;

            double topHeadroom = maxY > 0 ? maxY * 1.20 : 10;
            _plot.Plot.Axes.SetLimits(-0.6, list.Count - 0.4, 0, topHeadroom);
            _plot.Refresh();

            BuildLegendChips();
        }

        // =========================================================================
        // INTERACTIVE LEGEND CHIPS (Accessible, 100% reliable click targets)
        // =========================================================================

        private void BuildLegendChips()
        {
            _pnlLegend.Controls.Clear();
            if (_segments.Count == 0)
            {
                _pnlLegend.Visible = false;
                return;
            }

            _pnlLegend.Visible = true;

            foreach (var seg in _segments)
            {
                var chip = CreateLegendChip(seg);
                _pnlLegend.Controls.Add(chip);
            }
        }

        private Panel CreateLegendChip(ChartSegment seg)
        {
            var chip = new Panel
            {
                Height = 26,
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 2),
                Tag = seg.Key
            };
            UiRadiusHelper.ApplyRoundedCorners(chip, 6);

            var dot = new Panel
            {
                Size = new Size(9, 9),
                Location = new Point(8, 8),
                BackColor = seg.Color
            };
            UiRadiusHelper.ApplyRoundedCorners(dot, 4);

            string valText = seg.Value >= 1_000_000 ? $"{seg.Value / 1_000_000:N1}M" : (seg.Value >= 1_000 ? $"{seg.Value / 1_000:N0}k" : $"{seg.Value:N0}");
            var lbl = new Label
            {
                Text = $"{seg.Label} ({valText})",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(22, 5),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            chip.Controls.Add(dot);
            chip.Controls.Add(lbl);
            chip.Width = lbl.Right + 10;

            void SetChipHover(bool hovered)
            {
                bool isSelected = string.Equals(ActiveSegmentKey, seg.Key, StringComparison.OrdinalIgnoreCase);
                if (isSelected)
                {
                    chip.BackColor = Color.FromArgb(224, 242, 254);
                }
                else
                {
                    chip.BackColor = hovered ? Color.FromArgb(241, 245, 249) : Color.FromArgb(248, 250, 252);
                }
            }

            chip.MouseEnter += (_, _) => SetChipHover(true);
            chip.MouseLeave += (_, _) => SetChipHover(false);
            lbl.MouseEnter += (_, _) => SetChipHover(true);
            lbl.MouseLeave += (_, _) => SetChipHover(false);
            dot.MouseEnter += (_, _) => SetChipHover(true);
            dot.MouseLeave += (_, _) => SetChipHover(false);

            Action onChipClick = () => HandleSegmentClick(seg);
            chip.Click += (_, _) => onChipClick();
            lbl.Click += (_, _) => onChipClick();
            dot.Click += (_, _) => onChipClick();

            _toolTip.SetToolTip(chip, Mode == KpiClickMode.Navigate ? $"{seg.FullTooltip} · View full report →" : $"{seg.FullTooltip} · Click to filter in-place");
            _toolTip.SetToolTip(lbl, _toolTip.GetToolTip(chip));

            return chip;
        }

        private void UpdateLegendChipSelection()
        {
            foreach (Control c in _pnlLegend.Controls)
            {
                if (c is Panel chip && chip.Tag is string key)
                {
                    bool isSelected = string.Equals(ActiveSegmentKey, key, StringComparison.OrdinalIgnoreCase);
                    chip.BackColor = isSelected ? Color.FromArgb(224, 242, 254) : Color.FromArgb(248, 250, 252);
                    if (chip.Controls.OfType<Label>().FirstOrDefault() is Label lbl)
                    {
                        lbl.Font = new Font("Segoe UI", 8.25f, isSelected ? FontStyle.Bold : FontStyle.Regular);
                        lbl.ForeColor = isSelected ? Color.FromArgb(14, 116, 144) : Color.FromArgb(71, 85, 105);
                    }
                }
            }
        }

        // =========================================================================
        // HIT-TESTING & EVENT HANDLING
        // =========================================================================

        private void Plot_MouseMove(object? sender, MouseEventArgs e)
        {
            var hit = HitTestSegment(e.Location);
            SetHoveredSegment(hit);

            if (hit != null)
            {
                _plot.Cursor = Cursors.Hand;
                string hint = Mode == KpiClickMode.Navigate ? " · View full report →" : " · Click to filter in-place";
                _toolTip.SetToolTip(_plot, $"{hit.FullTooltip ?? hit.Label}{hint}");
            }
            else
            {
                if (Mode == KpiClickMode.Navigate)
                {
                    _plot.Cursor = Cursors.Hand;
                    _toolTip.SetToolTip(_plot, "Click to view full report →");
                }
                else
                {
                    _plot.Cursor = Cursors.Default;
                    _toolTip.SetToolTip(_plot, null);
                }
            }
        }

        private void Plot_MouseClick(object? sender, MouseEventArgs e)
        {
            var hit = HitTestSegment(e.Location);
            if (hit != null)
            {
                HandleSegmentClick(hit);
            }
            else if (Mode == KpiClickMode.Navigate)
            {
                TriggerNavigation(null);
            }
        }

        private ChartSegment? HitTestSegment(Point pixelPt)
        {
            if (_segments.Count == 0) return null;

            try
            {
                var coords = _plot.Plot.GetCoordinates(new Pixel(pixelPt.X, pixelPt.Y));

                // 1. Bar chart hit-testing
                if (_segments.Any(s => s.BarIndex >= 0))
                {
                    foreach (var s in _segments)
                    {
                        if (s.BarIndex >= 0)
                        {
                            double half = s.BarWidth / 2.0;
                            if (coords.X >= s.BarPosition - half && coords.X <= s.BarPosition + half &&
                                coords.Y >= 0 && coords.Y <= s.Value * 1.05)
                            {
                                return s;
                            }
                        }
                    }
                }

                // 2. Donut / Pie chart hit-testing (centered at 0,0)
                if (_segments.Any(s => s.SweepAngleDeg > 0))
                {
                    double dist = Math.Sqrt(coords.X * coords.X + coords.Y * coords.Y);
                    if (dist >= 0.45 && dist <= 1.35)
                    {
                        // In cartesian coordinates: angle counter-clockwise from positive X
                        double angleRad = Math.Atan2(coords.Y, coords.X);
                        double angleDeg = angleRad * 180.0 / Math.PI;
                        if (angleDeg < 0) angleDeg += 360.0;

                        foreach (var s in _segments)
                        {
                            double start = s.StartAngleDeg % 360.0;
                            double end = (s.StartAngleDeg + s.SweepAngleDeg) % 360.0;

                            if (s.SweepAngleDeg >= 360.0) return s;

                            if (start < end)
                            {
                                if (angleDeg >= start && angleDeg <= end) return s;
                            }
                            else
                            {
                                // Wraps around 360
                                if (angleDeg >= start || angleDeg <= end) return s;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private void HandleSegmentClick(ChartSegment seg)
        {
            if (Mode == KpiClickMode.InPlaceFilter)
            {
                if (string.Equals(ActiveSegmentKey, seg.Key, StringComparison.OrdinalIgnoreCase))
                {
                    // Clicked the SAME segment again -> CLEAR filter!
                    ClearFilter();
                    InPlaceFilterChanged?.Invoke(null);
                }
                else
                {
                    // Clicked a DIFFERENT segment -> apply filter!
                    SetActiveFilter(seg.Key);
                    InPlaceFilterChanged?.Invoke(seg.Key);
                }
            }
            else // Mode == KpiClickMode.Navigate
            {
                TriggerNavigation(seg.Key);
            }
        }

        private void TriggerNavigation(string? segmentFilter)
        {
            string target = NavigationTarget ?? "Analytics";
            string? filter = segmentFilter ?? DefaultNavigationFilter;

            if (_externalNavigationAction != null)
            {
                _externalNavigationAction();
            }
            else
            {
                NavigateRequested?.Invoke(target, filter);
            }
        }

        private void OnActionHintClick()
        {
            if (Mode == KpiClickMode.InPlaceFilter && IsFilterActive)
            {
                ClearFilter();
                InPlaceFilterChanged?.Invoke(null);
            }
            else if (Mode == KpiClickMode.Navigate)
            {
                TriggerNavigation(null);
            }
        }

        private void SetHoveredSegment(ChartSegment? seg)
        {
            if (_hoveredSegment != seg)
            {
                _hoveredSegment = seg;
            }
        }

        private void SetCardHover(bool hovered)
        {
            if (_isCardHovered != hovered)
            {
                _isCardHovered = hovered;
                if (Mode == KpiClickMode.Navigate)
                {
                    _lblActionHint.ForeColor = hovered ? Theme.PrimaryDark : Theme.Primary;
                }
                Invalidate();
            }
        }

        private void UpdateActionHintForInPlace()
        {
            if (Mode == KpiClickMode.InPlaceFilter)
            {
                if (IsFilterActive)
                {
                    _lblActionHint.Text = $"Filtered: {ActiveSegmentKey} (✕ Clear)";
                    _lblActionHint.ForeColor = Color.FromArgb(14, 165, 233); // Sky 500
                    _lblActionHint.Visible = true;
                }
                else
                {
                    _lblActionHint.Visible = false;
                }
            }
            PositionActionHint();
        }

        private void PositionActionHint()
        {
            if (_lblActionHint.Visible && Width > 0)
            {
                _lblActionHint.Location = new Point(Width - Padding.Right - _lblActionHint.PreferredWidth - 2, 6);
            }
        }

        // =========================================================================
        // VISUAL CARD PAINTING & ACCESSIBLE BORDERS
        // =========================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var cardRect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Soft elevation shadow
            if (_isCardHovered)
            {
                using var shadowPen = new Pen(Color.FromArgb(18, 15, 23, 42), 2f);
                using var shadowPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(1, 2, Width - 3, Height - 3), CardRadius);
                e.Graphics.DrawPath(shadowPen, shadowPath);
            }
            else
            {
                using var shadowPen = new Pen(Color.FromArgb(8, 15, 23, 42), 1f);
                using var shadowPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(0, 1, Width - 1, Height - 1), CardRadius);
                e.Graphics.DrawPath(shadowPen, shadowPath);
            }

            // 2. Perimeter border
            Color borderColor;
            float borderWidth;

            if (IsFilterActive && Mode == KpiClickMode.InPlaceFilter)
            {
                // Persistent highlighted visual state when an in-place filter is active
                borderColor = Color.FromArgb(14, 165, 233); // Sky 500 (#0EA5E9)
                borderWidth = 2.0f;
            }
            else if (_isCardHovered)
            {
                if (Mode == KpiClickMode.Navigate)
                {
                    borderColor = Theme.Primary;
                    borderWidth = 1.5f;
                }
                else
                {
                    borderColor = Color.FromArgb(203, 213, 225); // Slate 300
                    borderWidth = 1.0f;
                }
            }
            else
            {
                borderColor = Color.FromArgb(226, 232, 240); // Slate 200
                borderWidth = 1.0f;
            }

            using (var borderPen = new Pen(borderColor, borderWidth))
            using (var borderPath = UiRadiusHelper.CreateRoundedPath(cardRect, CardRadius))
            {
                e.Graphics.DrawPath(borderPen, borderPath);
            }
        }
    }
}
