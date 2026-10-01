using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Utility for applying smooth, modern rounded corner radiuses to WinForms controls
    /// (buttons, cards, panels, and badges) to eliminate harsh, edgy rectangles.
    /// </summary>
    public static class UiRadiusHelper
    {
        public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int d = radius * 2;
            if (d > bounds.Width) d = bounds.Width;
            if (d > bounds.Height) d = bounds.Height;

            var arc = new Rectangle(bounds.X, bounds.Y, d, d);

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static void ApplyRoundedCorners(Control control, int radius)
        {
            if (control is null) return;

            void UpdateRegion()
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using var path = CreateRoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius);
                control.Region = new Region(path);
            }

            UpdateRegion();
            control.SizeChanged += (_, _) => UpdateRegion();
        }

        public static void ApplyPillShape(Control control)
        {
            if (control is null) return;

            void UpdateRegion()
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                int radius = control.Height / 2;
                using var path = CreateRoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius);
                control.Region = new Region(path);
            }

            UpdateRegion();
            control.SizeChanged += (_, _) => UpdateRegion();
        }

        public static void StyleButton(Button button, int radius = 8, bool drawFocusRing = true)
        {
            if (button is null) return;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            ApplyRoundedCorners(button, radius);

            if (drawFocusRing)
            {
                // Accessible focus indicator (WCAG 2.4.7 Focus Visible)
                button.Paint += (s, e) =>
                {
                    if (button.Focused && button.Enabled)
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using var pen = new Pen(Theme.FocusBorder, 2.5f);
                        using var path = CreateRoundedPath(new Rectangle(1, 1, button.Width - 3, button.Height - 3), Math.Max(2, radius - 1));
                        e.Graphics.DrawPath(pen, path);
                    }
                };
                button.GotFocus += (_, _) => button.Invalidate();
                button.LostFocus += (_, _) => button.Invalidate();
            }
        }

        public static void AttachHoverFeedback(Button button, Color baseColor, Color hoverColor)
        {
            if (button is null) return;
            button.BackColor = baseColor;
            button.MouseEnter += (_, _) => { if (button.Enabled) button.BackColor = hoverColor; };
            button.MouseLeave += (_, _) => { if (button.Enabled) button.BackColor = baseColor; };
        }

        public static void StyleCard(Panel panel, int radius = 12, Color? borderColor = null)
        {
            if (panel is null) return;
            ApplyRoundedCorners(panel, radius);
            Color stroke = borderColor ?? Color.FromArgb(226, 232, 240); // Slate 200 hairline border
            panel.Paint += (s, e) =>
            {
                if (panel.Width <= 2 || panel.Height <= 2) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), radius);
                using var pen = new Pen(stroke, 1f);
                e.Graphics.DrawPath(pen, path);
            };
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int EM_SETMARGINS = 0xD3;
        private const int EC_LEFTMARGIN = 0x1;
        private const int EC_RIGHTMARGIN = 0x2;

        public static void SetPadding(TextBox textBox, int left = 10, int right = 10)
        {
            if (textBox is null) return;

            void ApplyMargins()
            {
                if (textBox.IsHandleCreated)
                {
                    SendMessage(textBox.Handle, EM_SETMARGINS, (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), (IntPtr)(left | (right << 16)));
                }
            }

            if (textBox.IsHandleCreated)
            {
                ApplyMargins();
            }
            else
            {
                textBox.HandleCreated += (_, _) => ApplyMargins();
            }
        }

        public static void MakeCircularAvatar(Label label)
        {
            if (label is null) return;
            label.Region = null; // Clear jagged 1-bit region mask

            label.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Color parentColor = label.Parent?.BackColor ?? Theme.Surface;
                using (var bgBrush = new SolidBrush(parentColor))
                {
                    e.Graphics.FillRectangle(bgBrush, label.ClientRectangle);
                }

                // Keep 4px padding so circle stroke is never clipped at outer bounds
                int size = Math.Max(10, Math.Min(label.Width, label.Height) - 4);
                int x = (label.Width - size) / 2;
                int y = (label.Height - size) / 2;

                using (var circleBrush = new SolidBrush(label.BackColor))
                {
                    e.Graphics.FillEllipse(circleBrush, x, y, size, size);
                }

                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    e.Graphics.DrawEllipse(pen, x, y, size, size);
                }

                if (!string.IsNullOrEmpty(label.Text))
                {
                    using var textBrush = new SolidBrush(label.ForeColor);
                    using var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(label.Text, label.Font, textBrush, new RectangleF(x, y, size, size), sf);
                }
            };
            label.Invalidate();
        }

        /// <summary>
        /// Standardizes filter pill buttons: 32px height, 16px horizontal padding,
        /// pill shape, and unified active/inactive brush states.
        /// </summary>
        public static void StyleFilterPill(Button btn, bool isSelected)
        {
            if (btn is null) return;
            btn.Height = 32;
            btn.Padding = new Padding(16, 0, 16, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            if (isSelected)
            {
                btn.BackColor = Theme.Primary;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else
            {
                btn.BackColor = Color.FromArgb(241, 245, 249);
                btn.ForeColor = Color.FromArgb(71, 85, 105);
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            }
            ApplyPillShape(btn);
        }

        public static void MakeStatusDot(Label label, Color? dotColor = null)
        {
            if (label is null) return;
            label.Text = string.Empty; // Prevent raw string painting

            Color color = dotColor ?? Color.FromArgb(34, 197, 94); // Crisp emerald green
            label.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Color parentColor = label.Parent?.BackColor ?? Theme.Surface;
                using (var bgBrush = new SolidBrush(parentColor))
                {
                    e.Graphics.FillRectangle(bgBrush, label.ClientRectangle);
                }

                int dotSize = 8;
                int x = (label.Width - dotSize) / 2;
                int y = (label.Height - dotSize) / 2;

                // Outer subtle glow ring
                using (var glowBrush = new SolidBrush(Color.FromArgb(45, color.R, color.G, color.B)))
                {
                    e.Graphics.FillEllipse(glowBrush, x - 2, y - 2, dotSize + 4, dotSize + 4);
                }

                // Main vibrant status circle
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillEllipse(brush, x, y, dotSize, dotSize);
                }
            };
            label.Invalidate();
        }

        public static void ApplyModernGridStyle(DataGridView grid, int rowHeight = 54)
        {
            UiGridHelper.ApplyModernGridStyle(grid, rowHeight);
        }

        /// <summary>
        /// Applies standardized modern styling to modal dialog windows:
        /// White background, 8px rounded corners, disabled maximize/minimize, centered.
        /// </summary>
        public static void StyleModal(Form form, int radius = 8)
        {
            if (form is null) return;
            form.BackColor = Color.White;
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ShowIcon = false;
            // Note: Do NOT set form.Region on top-level windows with OS borders/titlebars.
            // GDI+ regions clip OS title bars, bottom dialog action buttons, and disable Windows DWM anti-aliased shadows.
            if (form.FormBorderStyle == FormBorderStyle.None)
            {
                ApplyRoundedCorners(form, radius);
            }
        }

        /// <summary>
        /// Styles an input field (TextBox) with standardized Segoe UI font,
        /// white background, subtle border, and 10px inner horizontal margins.
        /// </summary>
        public static void StyleStandardInput(TextBox textBox, int leftPadding = 10, int rightPadding = 10)
        {
            if (textBox is null) return;
            textBox.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            textBox.BackColor = Color.White;
            textBox.ForeColor = Color.FromArgb(15, 23, 42);
            textBox.BorderStyle = BorderStyle.FixedSingle;
            SetPadding(textBox, leftPadding, rightPadding);
        }

        /// <summary>
        /// Styles a ComboBox with standardized Segoe UI font and clean dropdown styling.
        /// </summary>
        public static void StyleStandardComboBox(ComboBox comboBox)
        {
            if (comboBox is null) return;
            comboBox.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            comboBox.BackColor = Color.White;
            comboBox.ForeColor = Color.FromArgb(15, 23, 42);
            comboBox.FlatStyle = FlatStyle.System;
        }

        /// <summary>
        /// Styles a primary modal action button (filled accent, bold white text, 8px radius).
        /// </summary>
        public static void StylePrimaryButton(Button button, int radius = 8)
        {
            if (button is null) return;
            StyleButton(button, radius);
            button.BackColor = Theme.Primary;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            button.Height = 36;
        }

        /// <summary>
        /// Styles a secondary / cancel modal button (white background, subtle slate border, 8px radius).
        /// </summary>
        public static void StyleSecondaryButton(Button button, int radius = 8)
        {
            if (button is null) return;
            StyleButton(button, radius);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(51, 65, 85);
            button.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            button.Height = 36;
            button.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(Color.FromArgb(203, 213, 225), 1f);
                using var path = CreateRoundedPath(new Rectangle(0, 0, button.Width - 1, button.Height - 1), radius);
                e.Graphics.DrawPath(p, path);
            };
        }

        /// <summary>
        /// Styles a NumericUpDown with standardized Segoe UI font, white background, and subtle border.
        /// </summary>
        public static void StyleStandardNumericUpDown(NumericUpDown numericUpDown)
        {
            if (numericUpDown is null) return;
            numericUpDown.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            numericUpDown.BackColor = Color.White;
            numericUpDown.ForeColor = Color.FromArgb(15, 23, 42);
            numericUpDown.BorderStyle = BorderStyle.FixedSingle;
        }
    }
}
