using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.Models;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Modern vertical-stacked KPI metric card following the Tailwind/Lucide design system:
    /// - Top row: Subdued uppercase title + rounded tinted icon container on the top-right
    /// - Main value: Prominent bold metric beneath the title (text-3xl font-bold text-slate-900)
    /// - Bottom row: Secondary metric / trend or status indicator
    /// - Clean white surface, rounded-xl (12px), subtle border, and soft elevation shadow
    /// </summary>
    public class KpiCard : Panel
    {
        private readonly Label _lblTitle;
        private readonly Label _lblValue;
        private readonly Label _lblSubtitle;

        public string FilterKey { get; }
        public bool IsSelected { get; private set; }

        private Color _accentColor;
        private Color _accentBgColor;
        private KpiIconType _iconType = KpiIconType.None;
        private bool _isHovered;
        private Action? _clickAction;

        private readonly ToolTip _toolTip = new ToolTip();
        private string _fullValueTooltip = string.Empty;

        // Visual layout metrics
        private const int CardRadius = 12;
        private const int IconSize = 34;
        private const int IconRadius = 8;
        private const int LeftPadding = 18;
        private const int RightPadding = 16;
        private const int TopPadding = 14;

        public KpiCard() : this("KPI", "all", AzureTints.SkylineBlue, KpiIconType.None)
        {
        }

        public KpiCard(string title, string filterKey, Color accentColor, KpiIconType icon = KpiIconType.None, string? subtitle = null)
        {
            FilterKey = filterKey;
            _accentColor = accentColor;
            _iconType = icon != KpiIconType.None ? icon : InferIconFromKey(filterKey, title);
            _accentBgColor = GetTintBackground(_accentColor);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            TabStop = true;
            Size = new Size(220, 104);
            BackColor = Color.White;
            Cursor = Cursors.Hand;

            // 1. Top Left: Subdued uppercase title
            _lblTitle = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139), // Slate 500 (#64748B)
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 2. Middle Left: Prominent bold metric (21pt Bold, Slate 900 #0F172A)
            _lblValue = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 21f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42), // Slate 900 (#0F172A)
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 3. Bottom Left: Small secondary metric or trend badge
            _lblSubtitle = new Label
            {
                Text = subtitle ?? InferDefaultSubtitle(filterKey),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate 400 (#94A3B8)
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_lblTitle);
            Controls.Add(_lblValue);
            Controls.Add(_lblSubtitle);

            // Forward child clicks to card
            _lblTitle.Click += (_, _) => OnClick(EventArgs.Empty);
            _lblValue.Click += (_, _) => OnClick(EventArgs.Empty);
            _lblSubtitle.Click += (_, _) => OnClick(EventArgs.Empty);

            // Forward hover states
            MouseEnter += (_, _) => SetHoverState(true);
            MouseLeave += (_, _) => SetHoverState(false);
            _lblTitle.MouseEnter += (_, _) => SetHoverState(true);
            _lblTitle.MouseLeave += (_, _) => SetHoverState(false);
            _lblValue.MouseEnter += (_, _) => SetHoverState(true);
            _lblValue.MouseLeave += (_, _) => SetHoverState(false);
            _lblSubtitle.MouseEnter += (_, _) => SetHoverState(true);
            _lblSubtitle.MouseLeave += (_, _) => SetHoverState(false);

            GotFocus += (_, _) => Invalidate();
            LostFocus += (_, _) => Invalidate();
            SizeChanged += (_, _) => LayoutCard();

            LayoutCard();
        }

        public void SetTitle(string title)
        {
            _lblTitle.Text = (title ?? string.Empty).ToUpperInvariant();
            UpdateTooltips();
            LayoutCard();
        }

        public void SetAction(Action? action)
        {
            _clickAction = action;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _clickAction?.Invoke();
        }

        public void SetIcon(KpiIconType icon, Color? accentColor = null, Color? accentBgColor = null)
        {
            _iconType = icon;
            if (accentColor.HasValue)
            {
                _accentColor = accentColor.Value;
                _accentBgColor = accentBgColor ?? GetTintBackground(_accentColor);
            }
            Invalidate();
        }

        public void SetValue(int value)
        {
            SetValue(value.ToString("N0"), value.ToString("N0"));
        }

        public void SetValue(string value, string? fullTooltipValue = null)
        {
            _lblValue.Text = value ?? "0";
            _fullValueTooltip = fullTooltipValue ?? value ?? string.Empty;
            UpdateTooltips();
            LayoutCard();
        }

        public void SetCurrencyValue(decimal amount, bool compact = true)
        {
            string compactText = compact ? AppFormat.FormatCompactCurrency(amount) : AppFormat.FormatCurrency(amount);
            string fullText = AppFormat.FormatCurrency(amount);
            SetValue(compactText, fullText);
        }

        public void SetValueColor(Color color)
        {
            _lblValue.ForeColor = color;
        }

        private void UpdateTooltips()
        {
            string tip = !string.IsNullOrWhiteSpace(_fullValueTooltip) ? _fullValueTooltip : _lblValue.Text;
            _toolTip.SetToolTip(this, tip);
            _toolTip.SetToolTip(_lblValue, tip);
            _toolTip.SetToolTip(_lblTitle, tip);
            _toolTip.SetToolTip(_lblSubtitle, tip);
        }

        public void SetSubtitle(string text, Color? textColor = null)
        {
            _lblSubtitle.Text = text;
            if (textColor.HasValue) _lblSubtitle.ForeColor = textColor.Value;
            _lblSubtitle.Visible = !string.IsNullOrWhiteSpace(text);
            LayoutCard();
        }

        public void SetSelected(bool selected)
        {
            if (IsSelected != selected)
            {
                IsSelected = selected;
                Invalidate();
            }
        }

        private void SetHoverState(bool hovered)
        {
            if (_isHovered != hovered)
            {
                _isHovered = hovered;
                Invalidate();
            }
        }

        private void LayoutCard()
        {
            if (Width <= 0 || Height <= 0) return;

            // Icon on top right
            int iconLeft = Width - RightPadding - IconSize;

            // Title on top left (restricted to not overlap icon)
            int titleWidth = Math.Max(40, iconLeft - LeftPadding - 8);
            _lblTitle.Location = new Point(LeftPadding, TopPadding + 2);
            _lblTitle.Size = new Size(titleWidth, 18);

            // Dynamic font auto-scaling for primary value
            int maxValWidth = Math.Max(50, iconLeft - LeftPadding - 4);
            float[] fontSizes = new float[] { 21f, 18f, 15f, 13f, 11f };
            Font? chosenFont = null;
            foreach (float sz in fontSizes)
            {
                var testFont = new Font("Segoe UI", sz, FontStyle.Bold);
                var measured = TextRenderer.MeasureText(_lblValue.Text, testFont);
                if (measured.Width <= maxValWidth || sz == fontSizes[^1])
                {
                    chosenFont = testFont;
                    break;
                }
                testFont.Dispose();
            }

            if (chosenFont != null && Math.Abs(_lblValue.Font.Size - chosenFont.Size) > 0.1f)
            {
                var oldFont = _lblValue.Font;
                _lblValue.Font = chosenFont;
                oldFont?.Dispose();
            }

            // Primary value beneath title
            int valueY = _lblTitle.Bottom + 2;
            _lblValue.Location = new Point(LeftPadding - 1, valueY);

            // Structured secondary metric / amount layout:
            // If primary value is compact, place secondary amount horizontally beside it with an 8px gap.
            // Otherwise, stack it cleanly below the value with guaranteed spacing without vertical collision.
            const int horizontalGap = 8;
            int availableWidth = Width - RightPadding;

            if (_lblValue.Right + horizontalGap + _lblSubtitle.PreferredWidth <= availableWidth)
            {
                // Position beside the value aligned near baseline
                int subY = Math.Max(_lblTitle.Bottom + 2, _lblValue.Bottom - _lblSubtitle.PreferredHeight - 4);
                _lblSubtitle.Location = new Point(_lblValue.Right + horizontalGap, subY);
            }
            else
            {
                // Stack below the value with guaranteed gap
                int subY = Math.Max(_lblValue.Bottom + 2, Height - _lblSubtitle.PreferredHeight - 6);
                _lblSubtitle.Location = new Point(LeftPadding, subY);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var cardRect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Gentle elevation shadow
            if (_isHovered)
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

            // 2. Clean card background fill
            Color bgColor = _isHovered ? Color.FromArgb(250, 252, 255) : Color.White;
            using (var bgBrush = new SolidBrush(bgColor))
            using (var bgPath = UiRadiusHelper.CreateRoundedPath(cardRect, CardRadius))
            {
                e.Graphics.FillPath(bgBrush, bgPath);
            }

            // 3. Subtle perimeter border
            Color borderColor;
            float borderWidth;
            if (IsSelected)
            {
                borderColor = Color.FromArgb(14, 165, 233); // Sky 500 (#0EA5E9)
                borderWidth = 1.5f;
            }
            else if (_isHovered)
            {
                borderColor = Color.FromArgb(203, 213, 225); // Slate 300 (#CBD5E1)
                borderWidth = 1f;
            }
            else
            {
                borderColor = Color.FromArgb(226, 232, 240); // Slate 200 (#E2E8F0)
                borderWidth = 1f;
            }

            using (var borderPen = new Pen(borderColor, borderWidth))
            using (var borderPath = UiRadiusHelper.CreateRoundedPath(cardRect, CardRadius))
            {
                e.Graphics.DrawPath(borderPen, borderPath);
            }

            // 4. Accessible focus ring
            if (Focused)
            {
                using var focusPen = new Pen(Theme.FocusBorder, 1.5f);
                using var focusPath = UiRadiusHelper.CreateRoundedPath(new Rectangle(2, 2, Width - 5, Height - 5), CardRadius - 2);
                e.Graphics.DrawPath(focusPen, focusPath);
            }

            // 5. Accent icon container on top-right
            if (_iconType != KpiIconType.None)
            {
                int iconX = Width - RightPadding - IconSize;
                int iconY = TopPadding;
                var iconBoxRect = new Rectangle(iconX, iconY, IconSize, IconSize);

                using (var iconBgBrush = new SolidBrush(_accentBgColor))
                using (var iconBgPath = UiRadiusHelper.CreateRoundedPath(iconBoxRect, IconRadius))
                {
                    e.Graphics.FillPath(iconBgBrush, iconBgPath);
                }

                // Inner vector icon
                int vectorSize = 18;
                int vectorX = iconX + (IconSize - vectorSize) / 2;
                int vectorY = iconY + (IconSize - vectorSize) / 2;
                var vectorRect = new Rectangle(vectorX, vectorY, vectorSize, vectorSize);

                UiIconHelper.DrawIcon(e.Graphics, _iconType, vectorRect, _accentColor);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private static KpiIconType InferIconFromKey(string filterKey, string title)
        {
            string k = (filterKey ?? "").Trim().ToLowerInvariant();
            string t = (title ?? "").Trim().ToLowerInvariant();

            if (k.Contains("customer") || t.Contains("customer") || k.Contains("agent") || t.Contains("agent"))
                return KpiIconType.Users;
            if (k.Contains("propert") || t.Contains("propert") || k.Contains("inventor") || t.Contains("inventor"))
                return KpiIconType.Building;
            if (k.Contains("lead") || t.Contains("lead"))
                return KpiIconType.Target;
            if (k.Contains("deal") || t.Contains("deal"))
                return KpiIconType.Briefcase;
            if (k.Contains("total") && (t.Contains("ticket") || k.Contains("ticket")))
                return KpiIconType.Ticket;
            if (k.Contains("open"))
                return KpiIconType.Clock;
            if (k.Contains("progress"))
                return KpiIconType.Refresh;
            if (k.Contains("overdue") || t.Contains("overdue") || t.Contains("sla"))
                return KpiIconType.AlertTriangle;
            if (k.Contains("volume") || t.Contains("volume") || k.Contains("pipeline") || t.Contains("pipeline"))
                return KpiIconType.Currency;

            return KpiIconType.None;
        }

        private static string? InferDefaultSubtitle(string filterKey)
        {
            string k = (filterKey ?? "").Trim().ToLowerInvariant();
            return k switch
            {
                "customers" => "Active accounts",
                "properties" => "Listed properties",
                "leads" => "Pipeline leads",
                "deals" => "Closed & active",
                "total" => "All registered",
                "open" => "Awaiting triage",
                "in_progress" => "In active resolution",
                "overdue" => "SLA threshold exceeded",
                _ => null
            };
        }

        private static Color GetTintBackground(Color accent)
        {
            // Calculate a soft, modern pastel wash (e.g. bg-blue-50, bg-emerald-50)
            int r = (int)(accent.R * 0.12f + 255 * 0.88f);
            int g = (int)(accent.G * 0.12f + 255 * 0.88f);
            int b = (int)(accent.B * 0.12f + 255 * 0.88f);
            return Color.FromArgb(Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }
    }
}
