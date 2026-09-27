using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Modern web-app-grade floating tooltip card for interactive charts:
    /// - Rich presentation: uppercase category title, large bold formatted metric,
    ///   contextual comparison badge (percentage of whole, % change vs prior), and action hint.
    /// - Theme-matched styling: crisp white surface, Slate 200 perimeter border, 8px rounded corners,
    ///   soft ambient drop-shadow. Replaces WinForms 1990s flat yellow/gray tooltip.
    /// - Smooth hover transitions: 100-150ms alpha fade-in on enter, smooth position tracking,
    ///   and 80ms alpha fade-out on leave.
    /// - Clamped positioning: keeps tooltip fully within the host plot bounds without clipping.
    /// </summary>
    public class ModernChartTooltip : Control
    {
        private string _categoryTitle = string.Empty;
        private string _primaryValue = string.Empty;
        private string _comparisonBadgeText = string.Empty;
        private Color _comparisonBadgeBg = Color.FromArgb(224, 242, 254);   // Sky 100
        private Color _comparisonBadgeFg = Color.FromArgb(3, 105, 161);    // Sky 700
        private string _actionHint = string.Empty;
        private Color _accentColor = Theme.Primary;

        // Smooth fade-in / fade-out animation
        private float _alpha = 0f;
        private float _targetAlpha = 0f;
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private const float FadeInStep = 0.18f;   // ~6 frames @ 16ms = ~100ms
        private const float FadeOutStep = 0.22f;  // ~5 frames @ 16ms = ~80ms

        private const int CornerRadius = 8;
        private const int PaddingHoriz = 12;
        private const int PaddingVert = 10;

        public ModernChartTooltip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Visible = false;
            Size = new Size(180, 84);

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _fadeTimer.Tick += OnFadeTick;
        }

        /// <summary>
        /// Displays the rich tooltip at the specified host coordinates with smooth fade-in.
        /// </summary>
        public void ShowTooltip(Point hostPoint, string category, string value,
                                string? comparisonText = null, Color? badgeColor = null,
                                string? actionHint = null, Color? accentColor = null)
        {
            _categoryTitle = (category ?? string.Empty).ToUpperInvariant();
            _primaryValue = value ?? string.Empty;
            _comparisonBadgeText = comparisonText ?? string.Empty;
            _actionHint = actionHint ?? string.Empty;
            _accentColor = accentColor ?? Theme.Primary;

            if (badgeColor.HasValue)
            {
                _comparisonBadgeFg = badgeColor.Value;
                _comparisonBadgeBg = Color.FromArgb(28, badgeColor.Value.R, badgeColor.Value.G, badgeColor.Value.B);
            }
            else
            {
                _comparisonBadgeBg = Color.FromArgb(224, 242, 254);
                _comparisonBadgeFg = Color.FromArgb(3, 105, 161);
            }

            // Dynamically compute required card size
            ComputeSize();

            // Position tooltip offset to top-right of cursor, clamped to parent bounds
            PositionAt(hostPoint);

            // Bring to front and start smooth fade-in
            BringToFront();
            if (!Visible)
            {
                _alpha = 0f;
                Visible = true;
            }

            _targetAlpha = 1f;
            _fadeTimer.Start();
            Invalidate();
        }

        /// <summary>
        /// Initiates smooth fade-out and hides the tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (!Visible) return;
            _targetAlpha = 0f;
            _fadeTimer.Start();
        }

        /// <summary>
        /// Immediately hides the tooltip without waiting for fade-out (used on mouse leave or plot clear).
        /// </summary>
        public void HideImmediate()
        {
            _fadeTimer.Stop();
            _alpha = 0f;
            _targetAlpha = 0f;
            Visible = false;
        }

        private void OnFadeTick(object? sender, EventArgs e)
        {
            if (_targetAlpha > _alpha)
            {
                _alpha = Math.Min(1f, _alpha + FadeInStep);
                if (_alpha >= 1f) _fadeTimer.Stop();
            }
            else if (_targetAlpha < _alpha)
            {
                _alpha = Math.Max(0f, _alpha - FadeOutStep);
                if (_alpha <= 0f)
                {
                    _fadeTimer.Stop();
                    Visible = false;
                }
            }
            else
            {
                _fadeTimer.Stop();
            }

            Invalidate();
        }

        private void ComputeSize()
        {
            using var g = CreateGraphics();
            using var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var fontVal = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            using var fontBadge = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var fontHint = new Font("Segoe UI", 7.5f, FontStyle.Regular);

            var sizeTitle = TextRenderer.MeasureText(g, _categoryTitle, fontTitle);
            var sizeVal = TextRenderer.MeasureText(g, _primaryValue, fontVal);
            var sizeBadge = !string.IsNullOrEmpty(_comparisonBadgeText)
                ? TextRenderer.MeasureText(g, _comparisonBadgeText, fontBadge)
                : Size.Empty;
            var sizeHint = !string.IsNullOrEmpty(_actionHint)
                ? TextRenderer.MeasureText(g, _actionHint, fontHint)
                : Size.Empty;

            int contentWidth = Math.Max(sizeTitle.Width, sizeVal.Width + (sizeBadge.IsEmpty ? 0 : sizeBadge.Width + 8));
            contentWidth = Math.Max(contentWidth, sizeHint.Width);
            int cardW = Math.Max(160, contentWidth + PaddingHoriz * 2 + 10);

            int cardH = PaddingVert * 2 + sizeTitle.Height + sizeVal.Height + 4;
            if (!string.IsNullOrEmpty(_actionHint))
            {
                cardH += sizeHint.Height + 4;
            }

            Size = new Size(cardW, cardH);
        }

        private void PositionAt(Point pt)
        {
            if (Parent == null) return;

            // Ideal position: 16px to right, centered vertically with cursor
            int x = pt.X + 16;
            int y = pt.Y - Height / 2;

            // Clamp inside parent control boundaries
            int maxX = Parent.ClientSize.Width - Width - 8;
            int maxY = Parent.ClientSize.Height - Height - 8;

            if (x > maxX)
            {
                // Flip to left of cursor if overflowing right edge
                x = pt.X - Width - 16;
            }

            x = Math.Max(8, Math.Min(maxX, x));
            y = Math.Max(8, Math.Min(maxY, y));

            Location = new Point(x, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_alpha <= 0.01f) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int alphaByte = (int)(_alpha * 255);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Soft ambient elevation shadow (modern web-app aesthetic)
            Color shadowColor = Color.FromArgb((int)(16 * _alpha), 15, 23, 42);
            using (var shadowPen = new Pen(shadowColor, 2f))
            using (var shadowPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(1, 2, Width - 3, Height - 3), CornerRadius))
            {
                g.DrawPath(shadowPen, shadowPath);
            }

            // 2. Card background fill (Theme.Surface #FFFFFF with alpha)
            Color bgColor = Color.FromArgb(alphaByte, 255, 255, 255);
            using (var bgBrush = new SolidBrush(bgColor))
            using (var bgPath = UiRadiusHelper.CreateRoundedPath(rect, CornerRadius))
            {
                g.FillPath(bgBrush, bgPath);
            }

            // 3. Subtle crisp border (Slate 200 with alpha)
            Color borderColor = Color.FromArgb((int)(220 * _alpha), 226, 232, 240);
            using (var borderPen = new Pen(borderColor, 1f))
            using (var borderPath = UiRadiusHelper.CreateRoundedPath(rect, CornerRadius))
            {
                g.DrawPath(borderPen, borderPath);
            }

            // 4. Accent left bar indicator
            using (var accentBrush = new SolidBrush(Color.FromArgb(alphaByte, _accentColor)))
            using (var accentPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(2, 6, 3, Height - 12), 2))
            {
                g.FillPath(accentBrush, accentPath);
            }

            // 5. Typography rendering
            int curY = PaddingVert;
            int textX = PaddingHoriz + 2;

            // 5a. Uppercase Category Title
            using (var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(alphaByte, 100, 116, 139))) // Slate 500
            {
                g.DrawString(_categoryTitle, fontTitle, titleBrush, new PointF(textX, curY));
                curY += (int)g.MeasureString(_categoryTitle, fontTitle).Height + 2;
            }

            // 5b. Primary Metric Value
            using (var fontVal = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(Color.FromArgb(alphaByte, 15, 23, 42))) // Slate 900
            {
                var valSize = g.MeasureString(_primaryValue, fontVal);
                g.DrawString(_primaryValue, fontVal, valBrush, new PointF(textX, curY));

                // 5c. Contextual Comparison Badge (e.g. "34% of whole" or "+12.5% vs prior")
                if (!string.IsNullOrEmpty(_comparisonBadgeText))
                {
                    int badgeX = textX + (int)valSize.Width + 6;
                    int badgeY = curY + 2;
                    using var fontBadge = new Font("Segoe UI", 7.25f, FontStyle.Bold);
                    var badgeTextSize = g.MeasureString(_comparisonBadgeText, fontBadge);
                    var badgeRect = new Rectangle(badgeX, badgeY, (int)badgeTextSize.Width + 8, (int)badgeTextSize.Height + 2);

                    Color bgPill = Color.FromArgb((int)(_comparisonBadgeBg.A * _alpha), _comparisonBadgeBg.R, _comparisonBadgeBg.G, _comparisonBadgeBg.B);
                    using (var pillBrush = new SolidBrush(bgPill))
                    using (var pillPath = UiRadiusHelper.CreateRoundedPath(badgeRect, 4))
                    {
                        g.FillPath(pillBrush, pillPath);
                    }

                    Color fgPill = Color.FromArgb(alphaByte, _comparisonBadgeFg);
                    using (var pillTextBrush = new SolidBrush(fgPill))
                    {
                        g.DrawString(_comparisonBadgeText, fontBadge, pillTextBrush, new PointF(badgeX + 4, badgeY + 1));
                    }
                }

                curY += (int)valSize.Height + 3;
            }

            // 5d. Interactive Action Hint (Case 1 / Case 2 guidance)
            if (!string.IsNullOrEmpty(_actionHint))
            {
                using var fontHint = new Font("Segoe UI", 7.25f, FontStyle.Bold);
                Color hintColor = Color.FromArgb(alphaByte, _accentColor);
                using var hintBrush = new SolidBrush(hintColor);
                g.DrawString(_actionHint, fontHint, hintBrush, new PointF(textX, curY));
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;
            if (m.Msg == WM_NCHITTEST)
            {
                // Make tooltip completely transparent to mouse events so it never interferes with chart hovering or clicks
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
