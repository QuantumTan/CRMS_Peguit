using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Centralized modern DataGridView styling helper for NEXA CRM.
    /// Provides consistent zebra striping, smooth row hover tracking,
    /// soft modern selection tints, and clean circular action button rendering.
    /// </summary>
    public static class UiGridHelper
    {
        public static readonly Color RowNormal = Color.White;
        public static readonly Color RowAlternate = Color.FromArgb(249, 250, 251); // #F9FAFB
        public static readonly Color RowHover = Color.FromArgb(241, 245, 249);     // #F1F5F9
        public static readonly Color SelectionBg = Color.FromArgb(238, 242, 255);  // #EEF2FF
        public static readonly Color TextDark = Color.FromArgb(15, 23, 42);        // #0F172A
        public static readonly Color HeaderBg = Color.FromArgb(248, 250, 252);     // #F8FAFC
        public static readonly Color HeaderText = Color.FromArgb(100, 116, 139);   // #64748B
        public static readonly Color GridBorder = Color.FromArgb(241, 245, 249);   // #F1F5F9

        public static void ApplyModernGridStyle(DataGridView grid, int rowHeight = 52)
        {
            if (grid is null) return;

            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.GridColor = GridBorder;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ShowCellToolTips = true;
            grid.RowTemplate.Height = rowHeight;
            grid.ColumnHeadersHeight = 46;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Column Header Styling (Uniform Subtle Surface #F8FAFC, 1px bottom border, zero blue highlight)
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeaderText;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = HeaderText;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Default Row Styling (Vertically Centered)
            grid.DefaultCellStyle.BackColor = RowNormal;
            grid.DefaultCellStyle.ForeColor = TextDark;
            grid.DefaultCellStyle.SelectionBackColor = SelectionBg;
            grid.DefaultCellStyle.SelectionForeColor = TextDark;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            grid.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Alternating Row Styling (Zebra Striping, Vertically Centered)
            grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextDark;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionBg;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextDark;
            grid.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.AlternatingRowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Smooth Row Hover Tracking
            int hoverRow = -1;
            int hoverCol = -1;

            // Cell cursor and tooltip for Actions
            grid.CellMouseEnter += (s, e) =>
            {
                hoverCol = e.ColumnIndex;
                if (e.RowIndex >= 0 && e.RowIndex < grid.RowCount && e.RowIndex != hoverRow)
                {
                    int old = hoverRow;
                    hoverRow = e.RowIndex;
                    if (old >= 0 && old < grid.RowCount && !grid.Rows[old].Selected)
                        grid.InvalidateRow(old);
                    if (hoverRow < grid.RowCount && !grid.Rows[hoverRow].Selected)
                        grid.InvalidateRow(hoverRow);
                }

                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && (grid.Columns[e.ColumnIndex].Name == "Actions" || grid.Columns[e.ColumnIndex] is Controls.ActionsColumn))
                {
                    grid.Cursor = Cursors.Hand;
                }
            };

            grid.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex == hoverRow)
                {
                    int old = hoverRow;
                    hoverRow = -1;
                    hoverCol = -1;
                    if (old < grid.RowCount && !grid.Rows[old].Selected)
                        grid.InvalidateRow(old);
                }

                if (e.ColumnIndex >= 0 && (grid.Columns[e.ColumnIndex].Name == "Actions" || grid.Columns[e.ColumnIndex] is Controls.ActionsColumn))
                {
                    grid.Cursor = Cursors.Default;
                }
            };

            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && (grid.Columns[e.ColumnIndex].Name == "Actions" || grid.Columns[e.ColumnIndex] is Controls.ActionsColumn))
                {
                    grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = "Options";
                }
            };

            grid.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < grid.RowCount && !grid.Rows[e.RowIndex].Selected)
                {
                    if (e.RowIndex == hoverRow)
                    {
                        using var brush = new SolidBrush(RowHover);
                        e.Graphics.FillRectangle(brush, e.RowBounds);
                        e.PaintParts &= ~DataGridViewPaintParts.Background;
                    }
                }
            };

            // Custom header bottom divider line
            grid.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, grid.ColumnHeadersHeight - 1, grid.Width, grid.ColumnHeadersHeight - 1);
            };

            // Cell and Header Painting
            grid.CellPainting += (s, e) =>
            {
                if (e.Graphics == null) return;

                // Uniform Header Painting: Guarantees NO blue fill on first cell
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    using (var hBrush = new SolidBrush(HeaderBg))
                    {
                        e.Graphics.FillRectangle(hBrush, e.CellBounds);
                    }

                    var col = grid.Columns[e.ColumnIndex];
                    var formatFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                    if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleRight)
                        formatFlags |= TextFormatFlags.Right;
                    else if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleCenter)
                        formatFlags |= TextFormatFlags.HorizontalCenter;
                    else
                        formatFlags |= TextFormatFlags.Left;

                    var headerTextRect = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 24, e.CellBounds.Height);
                    TextRenderer.DrawText(e.Graphics, col.HeaderText, grid.ColumnHeadersDefaultCellStyle.Font, headerTextRect, HeaderText, formatFlags);

                    // 1px subtle bottom border
                    using (var bPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        e.Graphics.DrawLine(bPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    e.Handled = true;
                    return;
                }

                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var column = grid.Columns[e.ColumnIndex];
                if (column.Name == "Actions" || column is Controls.ActionsColumn)
                {
                    Color cellBg = grid.Rows[e.RowIndex].Selected
                        ? SelectionBg
                        : (e.RowIndex == hoverRow ? RowHover : (e.RowIndex % 2 == 1 ? RowAlternate : RowNormal));
                    PaintActionCell(e, e.RowIndex == hoverRow && e.ColumnIndex == hoverCol, cellBg);
                }
            };
        }

        public static Color GetStatusColor(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Theme.StatusNeutral;
            string key = status.Trim().ToUpperInvariant();

            return key switch
            {
                // Green (#16A34A) - Success, positive completion, active, converted, won, sold
                "CONVERTED" or "ACTIVE" or "AVAILABLE" or "CLOSED" or "RESOLVED" or "APPROVED" or "COMPLETED" or "WON" or "SOLD" => Theme.StatusSuccess,

                // Amber (#D97706) - Pending, in-progress, awaiting action, triage
                "CONTACTED" or "PENDING REVIEW" or "PENDING_REVIEW" or "OFFER" or "CONTRACT" or "UNDER CONTRACT" or "PENDING" or "IN PROGRESS" or "IN_PROGRESS" or "FOLLOW UP" or "FOLLOW_UP" or "QUALIFIED" or "RESERVATION" or "UNDER REVIEW" or "TODAY" or "MEDIUM" => Theme.StatusPending,

                // Red (#DC2626) - Alert, critical, negative outcome, overdue, cancelled
                "INACTIVE" or "OVERDUE" or "LOST" or "URGENT" or "CRITICAL" or "HIGH" or "REJECTED" or "CANCELLED" or "CANCELED" or "FAILED" => Theme.StatusAlert,

                // Blue (#2563EB) - New, initial, prospect, open, upcoming
                "NEW" or "OPEN" or "PROSPECT" or "UPCOMING" => Theme.StatusInfo,

                // Muted Gray (#6B7280) - Neutral, unassigned, low, draft, archived
                "UNASSIGNED" or "LOW" or "DRAFT" or "ARCHIVED" or "UNKNOWN" or "-" => Theme.StatusNeutral,

                _ => Theme.StatusNeutral
            };
        }

        /// <summary>
        /// Renders status in table cell as bold colored TEXT only — strictly NO background pill shape, NO border, and NO container.
        /// Title Case text with spaces (no snake_case or raw DB enums).
        /// Shared 4-tier semantic color palette: Green (Won/Closed/Active), Amber (Pending/In-Progress), Red (Lost/Overdue), Gray (New/Unassigned).
        /// </summary>
        public static void PaintStatusText(DataGridView grid, DataGridViewCellPaintingEventArgs e, string rawStatus, bool center = false)
        {
            if (e.Graphics == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Paint base row background
            Color rowBg = grid.Rows[e.RowIndex].Selected
                ? SelectionBg
                : (e.RowIndex % 2 == 1 ? RowAlternate : RowNormal);

            using (var bgBrush = new SolidBrush(rowBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            string displayText = StatusColorHelper.ToTitleCase(rawStatus);
            Color textColor = StatusColorHelper.GetTextColor(rawStatus);

            using var font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            int leftPad = center ? 0 : 12;
            var textRect = new Rectangle(
                e.CellBounds.X + leftPad,
                e.CellBounds.Y,
                Math.Max(10, e.CellBounds.Width - leftPad - (center ? 0 : 8)),
                e.CellBounds.Height);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            flags |= center ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;

            TextRenderer.DrawText(e.Graphics, displayText, font, textRect, textColor, flags);

            // Bottom grid line
            using (var linePen = new Pen(GridBorder, 1f))
            {
                e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Status indicator alias: forwards to PaintStatusText to enforce APP-WIDE colored bold text standard.
        /// </summary>
        public static void PaintStatusBadge(DataGridView grid, DataGridViewCellPaintingEventArgs e, string rawStatus, bool center = false)
        {
            PaintStatusText(grid, e, rawStatus, center);
        }

        /// <summary>
        /// Legacy status indicator alias: forwards to PaintStatusText to enforce APP-WIDE colored bold text standard.
        /// </summary>
        public static void PaintStatusIndicator(DataGridView grid, DataGridViewCellPaintingEventArgs e, string status, bool center = false)
        {
            PaintStatusText(grid, e, status, center);
        }

        /// <summary>
        /// Paints cell text with uniform padding and 1px bottom border, preventing 0px margin jumps.
        /// </summary>
        public static void PaintTextCell(DataGridView grid, DataGridViewCellPaintingEventArgs e, string text, Font font, Color textColor, TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis, int leftPadding = 12, int rightPadding = 12)
        {
            if (e.Graphics == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            Color rowBg = grid.Rows[e.RowIndex].Selected
                ? SelectionBg
                : (e.RowIndex % 2 == 1 ? RowAlternate : RowNormal);

            using (var bgBrush = new SolidBrush(rowBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            var textRect = new Rectangle(
                e.CellBounds.X + leftPadding,
                e.CellBounds.Y,
                Math.Max(10, e.CellBounds.Width - leftPadding - rightPadding),
                e.CellBounds.Height);

            TextRenderer.DrawText(e.Graphics, text, font, textRect, textColor, flags);

            using (var linePen = new Pen(GridBorder, 1f))
            {
                e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Paints an avatar with initials at exact 12px inset followed by bold primary text.
        /// Derives initials and deterministic avatar color automatically if not provided.
        /// </summary>
        public static void PaintAvatarCell(DataGridView grid, DataGridViewCellPaintingEventArgs e, string name, string? initials = null, Color? avatarBg = null, Color? avatarText = null)
        {
            if (e.Graphics == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            Color rowBg = grid.Rows[e.RowIndex].Selected
                ? SelectionBg
                : (e.RowIndex % 2 == 1 ? RowAlternate : RowNormal);

            using (var bgBrush = new SolidBrush(rowBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            string safeName = string.IsNullOrWhiteSpace(name) ? "—" : name.Trim();
            string actualInitials = string.IsNullOrWhiteSpace(initials)
                ? Controls.AvatarLabel.GetInitials(safeName)
                : initials.Trim();

            var (defBg, defFg) = Controls.AvatarLabel.GetDeterministicAvatarColors(safeName);
            Color bg = (avatarBg.HasValue && !avatarBg.Value.IsEmpty) ? avatarBg.Value : defBg;
            Color fg = (avatarText.HasValue && !avatarText.Value.IsEmpty) ? avatarText.Value : defFg;

            int avatarSize = 28;
            int avatarX = e.CellBounds.X + 12;
            int avatarY = e.CellBounds.Y + (e.CellBounds.Height - avatarSize) / 2;
            var avatarRect = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillEllipse(brush, avatarRect);
            }

            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, actualInitials, font, avatarRect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int textX = avatarX + avatarSize + 10;
            var textRect = new Rectangle(textX, e.CellBounds.Y,
                Math.Max(10, e.CellBounds.Width - (textX - e.CellBounds.X) - 12), e.CellBounds.Height);

            using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, safeName, font, textRect, Theme.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            using (var linePen = new Pen(GridBorder, 1f))
            {
                e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Right-aligns column cells and column header with proper 14px right margin padding.
        /// </summary>
        public static void AlignNumericColumn(DataGridView grid, string? columnName)
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName)) return;
            if (!grid.Columns.Contains(columnName)) return;

            var col = grid.Columns[columnName];
            if (col == null) return;
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            col.DefaultCellStyle.Padding = new Padding(8, 0, 14, 0);
            col.HeaderCell.Style.Padding = new Padding(8, 0, 14, 0);
        }

        /// <summary>
        /// Automatically applies alignment standards across the grid:
        /// Right-aligns numbers/amounts/currencies and their headers, enforces Action button widths.
        /// </summary>
        public static void EnforceTableStandards(DataGridView grid)
        {
            if (grid == null) return;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                string header = col.HeaderText?.ToLowerInvariant() ?? "";
                string name = col.Name?.ToLowerInvariant() ?? "";

                bool isNumeric = header.Contains("value") || header.Contains("amount") ||
                                 header.Contains("commission") || header.Contains("price") ||
                                 header.Contains("balance") || header.Contains("total") ||
                                 header.Contains("budget") || header.Contains("rate") ||
                                 header.Contains("count") || header.Contains("score") ||
                                 name.Contains("value") || name.Contains("amount") ||
                                 name.Contains("commission") || name.Contains("price") ||
                                 name.Contains("balance") || name.Contains("total");

                if (isNumeric && !name.Contains("id") && !name.Contains("ref") && !name.Contains("number"))
                {
                    if (!string.IsNullOrEmpty(col.Name))
                        AlignNumericColumn(grid, col.Name);
                }
            }
        }

        public static Color GetRowBackgroundColor(DataGridView grid, int rowIndex, int hoveredRowIndex)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.RowCount) return RowNormal;
            if (grid.Rows[rowIndex].Selected) return SelectionBg;
            if (rowIndex == hoveredRowIndex) return RowHover;
            return (rowIndex % 2 == 1) ? RowAlternate : RowNormal;
        }

        public static void PaintActionCell(DataGridViewCellPaintingEventArgs e, bool isHovered, Color? background = null)
        {
            if (e.Graphics == null) return;
            
            Color rowBg = background ?? RowNormal;
            using (var rowBrush = new SolidBrush(rowBg))
            {
                e.Graphics.FillRectangle(rowBrush, e.CellBounds);
            }

            int btnSize = 28;
            int x = e.CellBounds.X + (e.CellBounds.Width - btnSize) / 2;
            int y = e.CellBounds.Y + (e.CellBounds.Height - btnSize) / 2;
            var rect = new Rectangle(x, y, btnSize, btnSize);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Soft circular pill background with hover state
            Color bg = isHovered ? Color.FromArgb(219, 234, 254) : Color.FromArgb(241, 245, 249); // #DBEAFE hover tint
            Color borderColor = isHovered ? Color.FromArgb(147, 197, 253) : Color.FromArgb(226, 232, 240);
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillEllipse(brush, rect);
            }

            using (var borderPen = new Pen(borderColor, 1f))
            {
                e.Graphics.DrawEllipse(borderPen, rect);
            }

            // Draw clean centered 3 vertical dots
            Color dotsColor = isHovered ? Color.FromArgb(29, 78, 216) : Color.FromArgb(71, 85, 105);
            using (var font = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(dotsColor))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString("⋮", font, textBrush, new RectangleF(x, y - 1, btnSize, btnSize), sf);
            }

            // Bottom border divider
            using (var pen = new Pen(GridBorder, 1f))
            {
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Adds a fixed-width Actions column that does NOT stretch with AutoSizeColumnsMode.Fill.
        /// Call this after setting all other column FillWeights.
        /// </summary>
        public static void AddActionsColumn(DataGridView grid, int fixedWidth = 64)
        {
            if (grid is null) return;

            // Remove any existing Actions column to avoid duplicates
            var existing = grid.Columns["Actions"];
            if (existing != null) grid.Columns.Remove(existing);

            var col = new Controls.ActionsColumn
            {
                Name = "Actions",
                HeaderText = "",
                Width = fixedWidth,
                MinimumWidth = fixedWidth,
                // Setting FillWeight to a very small value combined with Width freezes it visually
                FillWeight = 1,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };

            // Freeze the column width after Fill mode is applied
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            grid.Columns.Add(col);
        }

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

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
