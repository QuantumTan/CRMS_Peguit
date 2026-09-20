using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Reusable WinForms control displaying a circular initials avatar paired with bold text.
    /// Avatar background color is deterministically derived from the person's name.
    /// </summary>
    public class AvatarLabel : Control
    {
        private string _title = string.Empty;
        private string _subtitle = string.Empty;
        private string _initials = string.Empty;
        private Color _avatarBg = Color.Empty;
        private Color _avatarFg = Color.White;
        private int _avatarSize = 32;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? string.Empty;
                UpdateInitials();
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle
        {
            get => _subtitle;
            set
            {
                _subtitle = value ?? string.Empty;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int AvatarSize
        {
            get => _avatarSize;
            set
            {
                _avatarSize = Math.Max(20, value);
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AvatarBackgroundColor
        {
            get => _avatarBg;
            set
            {
                _avatarBg = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AvatarForegroundColor
        {
            get => _avatarFg;
            set
            {
                _avatarFg = value;
                Invalidate();
            }
        }

        public AvatarLabel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.Transparent;
            Height = 36;
            Width = 180;
        }

        public AvatarLabel(string title, string subtitle = "") : this()
        {
            _title = title ?? string.Empty;
            _subtitle = subtitle ?? string.Empty;
            UpdateInitials();
        }

        private void UpdateInitials()
        {
            _initials = GetInitials(_title);
        }

        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpperInvariant() : parts[0].ToUpperInvariant();
            }
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpperInvariant();
        }

        public static (Color Background, Color Text) GetDeterministicAvatarColors(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (Color.FromArgb(226, 232, 240), Color.FromArgb(71, 85, 105));
            }

            // Modern accessible avatar palette (soft rich background + white text)
            Color[] palette = new[]
            {
                Color.FromArgb(37, 99, 235),   // Blue
                Color.FromArgb(13, 148, 136),  // Teal
                Color.FromArgb(124, 58, 237),  // Purple
                Color.FromArgb(217, 119, 6),   // Amber
                Color.FromArgb(225, 29, 72),   // Rose
                Color.FromArgb(5, 150, 105),   // Emerald
                Color.FromArgb(79, 70, 229),   // Indigo
                Color.FromArgb(234, 88, 12),   // Orange
                Color.FromArgb(8, 145, 178)    // Cyan
            };

            int hash = Math.Abs(name.Trim().ToLowerInvariant().GetHashCode());
            Color bg = palette[hash % palette.Length];
            return (bg, Color.White);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int size = Math.Min(_avatarSize, Height - 4);
            int avatarY = (Height - size) / 2;
            var avatarRect = new Rectangle(2, avatarY, size, size);

            var (defBg, defFg) = GetDeterministicAvatarColors(_title);
            Color bg = _avatarBg.IsEmpty ? defBg : _avatarBg;
            Color fg = _avatarFg.IsEmpty ? defFg : _avatarFg;

            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillEllipse(brush, avatarRect);
            }

            float fontSize = Math.Max(7.5f, size * 0.36f);
            using (var initFont = new Font("Segoe UI", fontSize, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, _initials, initFont, avatarRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int textX = avatarRect.Right + 8;
            int textWidth = Math.Max(10, Width - textX - 4);

            if (string.IsNullOrWhiteSpace(_subtitle))
            {
                var textRect = new Rectangle(textX, 0, textWidth, Height);
                using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, _title, font, textRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                int titleH = Height / 2;
                var titleRect = new Rectangle(textX, 2, textWidth, titleH);
                var subRect = new Rectangle(textX, titleH, textWidth, Height - titleH - 2);

                using var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var fontSub = new Font("Segoe UI", 8f, FontStyle.Regular);

                TextRenderer.DrawText(e.Graphics, _title, fontBold, titleRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, _subtitle, fontSub, subRect, Theme.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
