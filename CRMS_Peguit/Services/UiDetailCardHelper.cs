using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Reusable UI builder helper for record detail dialogs (Customer, Lead, Property).
    /// Enforces consistent modern card styling, crisp borders, WCAG-compliant pill badges,
    /// and clean typography.
    /// </summary>
    public static class UiDetailCardHelper
    {
        public static readonly Color CardBg = Color.White;
        public static readonly Color BorderColor = Color.FromArgb(226, 232, 240);       // #E2E8F0
        public static readonly Color SectionTitleColor = Color.FromArgb(30, 41, 59);    // #1E293B
        public static readonly Color LabelMutedColor = Color.FromArgb(100, 116, 139);   // #64748B
        public static readonly Color ValueTextColor = Color.FromArgb(15, 23, 42);       // #0F172A
        public static readonly Color TileBg = Color.FromArgb(248, 250, 252);            // #F8FAFC
        public static readonly Color DividerColor = Color.FromArgb(241, 245, 249);      // #F1F5F9

        /// <summary>
        /// Creates a card panel with anti-aliased rounded corners and subtle border.
        /// </summary>
        public static Panel CreateCard(int radius = 10)
        {
            var panel = new Panel
            {
                BackColor = CardBg,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(18, 16, 18, 18),
                Width = 660,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = UiRadiusHelper.CreateRoundedPath(rect, radius);
                using var pen = new Pen(BorderColor, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            UiRadiusHelper.ApplyRoundedCorners(panel, radius);
            return panel;
        }

        /// <summary>
        /// Adds a child control to a card panel docked to Top and calls BringToFront()
        /// so that Top-docked controls stack in natural top-to-bottom order.
        /// </summary>
        public static void AddControl(Panel card, Control control)
        {
            if (card is null || control is null) return;
            card.Controls.Add(control);
            control.BringToFront();
        }

        /// <summary>
        /// Creates a card header with section title and optional count badge.
        /// </summary>
        public static Panel CreateCardHeader(string title, string? countText = null)
        {
            var header = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = SectionTitleColor,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            header.Controls.Add(lblTitle);

            if (!string.IsNullOrWhiteSpace(countText))
            {
                var lblBadge = CreatePillBadge(countText, Color.FromArgb(241, 245, 249), LabelMutedColor);
                lblBadge.Location = new Point(lblTitle.Right + 10, 2);
                header.Controls.Add(lblBadge);
            }

            return header;
        }

        /// <summary>
        /// Creates a subtle horizontal divider line.
        /// </summary>
        public static Panel CreateDivider()
        {
            return new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = DividerColor,
                Margin = new Padding(0, 4, 0, 12)
            };
        }

        /// <summary>
        /// Creates a 2-column or 1-column key-value row.
        /// </summary>
        public static Panel CreateKeyValueRow(string label1, string value1, string? label2 = null, string? value2 = null)
        {
            var row = new Panel
            {
                Height = 52,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Left Column
            var lblCaption1 = new Label
            {
                Text = label1.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = LabelMutedColor,
                Location = new Point(0, 4),
                AutoSize = true
            };
            var lblVal1 = new Label
            {
                Text = string.IsNullOrWhiteSpace(value1) ? "—" : value1,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = ValueTextColor,
                Location = new Point(0, 24),
                AutoSize = true,
                MaximumSize = new Size(300, 24)
            };
            row.Controls.Add(lblCaption1);
            row.Controls.Add(lblVal1);

            // Right Column (if present)
            if (label2 is not null)
            {
                var lblCaption2 = new Label
                {
                    Text = label2.ToUpperInvariant(),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = LabelMutedColor,
                    Location = new Point(330, 4),
                    AutoSize = true
                };
                var lblVal2 = new Label
                {
                    Text = string.IsNullOrWhiteSpace(value2) ? "—" : value2,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = ValueTextColor,
                    Location = new Point(330, 24),
                    AutoSize = true,
                    MaximumSize = new Size(300, 24)
                };
                row.Controls.Add(lblCaption2);
                row.Controls.Add(lblVal2);
            }

            return row;
        }

        /// <summary>
        /// Creates a clean, modern status indicator with semantic dot and colored typography (no heavy pill badge).
        /// </summary>
        public static Label CreatePillBadge(string text, Color bg, Color fg, Color? border = null)
        {
            return CreateStatusIndicator(text, fg);
        }

        /// <summary>
        /// Creates an inline semantic status dot + colored typography without pill borders or heavy backgrounds.
        /// </summary>
        public static Label CreateStatusIndicator(string text, Color color)
        {
            string formattedText = ToTitleCase(text);
            var badge = new Label
            {
                Text = "    " + formattedText,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = color,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Height = 24,
                Cursor = Cursors.Default
            };

            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int dotSize = 6;
                int dotY = (badge.Height - dotSize) / 2;
                using var dotBrush = new SolidBrush(color);
                e.Graphics.FillEllipse(dotBrush, 2, dotY, dotSize, dotSize);
            };

            return badge;
        }

        /// <summary>
        /// Properly capitalizes names and enum values to Title Case (e.g., "john sin" -> "John Sin").
        /// </summary>
        public static string ToTitleCase(string? text)
        {
            return StatusColorHelper.ToTitleCase(text);
        }

        /// <summary>
        /// Cleans raw string keys in section headings (replaces underscores with spaces).
        /// </summary>
        public static string FormatHeaderString(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Replace('_', ' ').Trim();
        }

        /// <summary>
        /// Returns standard background and foreground colors for status badges.
        /// </summary>
        public static (Color bg, Color fg, Color border) GetStatusColors(string? status)
        {
            return StatusColorHelper.GetColors(status);
        }

        /// <summary>
        /// Returns standard colors for Priority badges.
        /// </summary>
        public static (Color bg, Color fg, Color border) GetPriorityColors(string? priority)
        {
            var p = (priority ?? string.Empty).Trim().ToLowerInvariant();
            switch (p)
            {
                case "high":
                case "urgent":
                    return (Color.FromArgb(254, 226, 226), Color.FromArgb(220, 38, 38), Color.FromArgb(252, 165, 165));
                case "medium":
                    return (Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6), Color.FromArgb(253, 230, 138));
                case "low":
                default:
                    return (Color.FromArgb(241, 245, 249), Color.FromArgb(107, 114, 128), Color.FromArgb(203, 213, 225));
            }
        }

        /// <summary>
        /// Extracts clean 1-2 character initials from a full name.
        /// </summary>
        public static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Length >= 2
                    ? parts[0].Substring(0, 2).ToUpperInvariant()
                    : parts[0].ToUpperInvariant();
            }
            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}
