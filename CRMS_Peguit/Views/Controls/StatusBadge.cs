using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Shared WinForms control rendering a modern, rounded status badge / pill:
    /// - Converts any raw status or enum string to clean Title Case with spaces
    /// - Automatically looks up standard semantic colors (green, amber, red, blue, gray)
    /// - Renders a sleek anti-aliased pill capsule with soft background tint, border, and bold text
    /// </summary>
    public class StatusBadge : Control
    {
        private string _statusText = string.Empty;
        private string _displayText = string.Empty;
        private Color _bgColor;
        private Color _textColor;
        private Color _borderColor;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value ?? string.Empty;
                _displayText = StatusColorHelper.ToTitleCase(_statusText);
                var colors = StatusColorHelper.GetColors(_statusText);
                _bgColor = colors.Background;
                _textColor = colors.Text;
                _borderColor = colors.Border;
                UpdateBadgeSize();
                Invalidate();
            }
        }

        public StatusBadge() : this("Active")
        {
        }

        public StatusBadge(string status)
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);

            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            BackColor = Color.Transparent;
            Cursor = Cursors.Default;
            StatusText = status;
        }

        public void SetPriority(string priority)
        {
            _statusText = priority ?? string.Empty;
            _displayText = StatusColorHelper.ToTitleCase(_statusText);
            var colors = StatusColorHelper.GetPriorityColors(_statusText);
            _bgColor = colors.Background;
            _textColor = colors.Text;
            _borderColor = colors.Border;
            UpdateBadgeSize();
            Invalidate();
        }

        public void SetCustomColors(string text, Color bg, Color fg, Color? border = null)
        {
            _statusText = text ?? string.Empty;
            _displayText = StatusColorHelper.ToTitleCase(_statusText);
            _bgColor = bg;
            _textColor = fg;
            _borderColor = border ?? fg;
            UpdateBadgeSize();
            Invalidate();
        }

        private void UpdateBadgeSize()
        {
            using var g = CreateGraphics();
            var size = TextRenderer.MeasureText(_displayText, Font);
            // Pill with 14px horizontal padding, 24px height
            Width = Math.Max(64, size.Width + 18);
            Height = 24;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = rect.Height / 2;

            using (var path = UiRadiusHelper.CreateRoundedPath(rect, radius))
            {
                // 1. Fill pill background
                using (var bgBrush = new SolidBrush(_bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // 2. Draw subtle pill perimeter stroke
                using (var pen = new Pen(_borderColor, 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // 3. Render centered bold Title Case text
            TextRenderer.DrawText(
                e.Graphics,
                _displayText,
                Font,
                rect,
                _textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
