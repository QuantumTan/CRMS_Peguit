using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// High-performance Theme-matched skeleton loading overlay for DataGridView controls.
    /// Replaces blank flashes or unstyled spinners with 5-8 placeholder rows precisely matching
    /// the real grid's column layout (avatar circles, name bars, status pills, numeric alignments).
    /// Swept by a synchronized subtle shimmer wave derived from Theme.
    /// </summary>
    public class GridSkeletonOverlay : Control
    {
        private DataGridView? _grid;
        private int _requestedRowCount = 6;
        private bool _isAttached = false;

        public bool IsActive => Visible;

        public GridSkeletonOverlay()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.White;
            Visible = false;
        }

        /// <summary>
        /// Attaches this skeleton overlay to the specified DataGridView, automatically docking
        /// into the grid's parent container and keeping bounds synchronized.
        /// </summary>
        public void AttachToGrid(DataGridView grid)
        {
            if (_grid == grid && _isAttached) return;

            _grid = grid ?? throw new ArgumentNullException(nameof(grid));

            void SetupParent()
            {
                if (_grid.Parent != null && !_isAttached)
                {
                    _grid.Parent.Controls.Add(this);
                    Dock = _grid.Dock == DockStyle.Fill ? DockStyle.Fill : DockStyle.None;
                    if (Dock == DockStyle.None)
                    {
                        Bounds = _grid.Bounds;
                        _grid.LocationChanged += (_, _) => { if (Visible) Bounds = _grid.Bounds; };
                        _grid.SizeChanged += (_, _) => { if (Visible) Bounds = _grid.Bounds; };
                    }
                    BringToFront();
                    _isAttached = true;
                }
            }

            if (_grid.Parent != null)
            {
                SetupParent();
            }
            else
            {
                _grid.ParentChanged += (_, _) => SetupParent();
            }
        }

        public static GridSkeletonOverlay CreateForGrid(DataGridView grid)
        {
            var overlay = new GridSkeletonOverlay();
            overlay.AttachToGrid(grid);
            return overlay;
        }

        public void ShowSkeleton(int rowCount = 6)
        {
            _requestedRowCount = rowCount;

            if (_grid != null)
            {
                if (!_isAttached && _grid.Parent != null)
                {
                    AttachToGrid(_grid);
                }

                if (Dock == DockStyle.None)
                {
                    Bounds = _grid.Bounds;
                }

                _grid.Visible = false;
            }

            Visible = true;
            BringToFront();
            SkeletonPulseHelper.Register(this);
            Invalidate();
        }

        public void HideSkeleton()
        {
            SkeletonPulseHelper.Unregister(this);
            Visible = false;

            if (_grid != null && !_grid.IsDisposed)
            {
                _grid.Visible = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 10 || h <= 10) return;

            int headerHeight = _grid?.ColumnHeadersHeight > 0 ? _grid.ColumnHeadersHeight : 46;
            int rowHeight = _grid?.RowTemplate?.Height > 0 ? _grid.RowTemplate.Height : 52;

            // 1. Column Header Background & Dividers
            using (var headerBrush = new SolidBrush(UiGridHelper.HeaderBg))
            {
                g.FillRectangle(headerBrush, 0, 0, w, headerHeight);
            }

            using (var borderPen = new Pen(Theme.Border, 1f))
            {
                g.DrawLine(borderPen, 0, headerHeight - 1, w, headerHeight - 1);
            }

            // Draw header text or placeholder header bars
            if (_grid != null && _grid.Columns.Count > 0)
            {
                using var headerFont = _grid.ColumnHeadersDefaultCellStyle.Font ?? new Font("Segoe UI", 8.5f, FontStyle.Bold);
                foreach (DataGridViewColumn col in _grid.Columns)
                {
                    if (!col.Visible) continue;
                    var colRect = _grid.GetColumnDisplayRectangle(col.Index, false);
                    int colX = colRect.X >= 0 && colRect.Width > 0 ? colRect.X : GetFallbackColX(col.Index);
                    int colW = colRect.Width > 0 ? colRect.Width : col.Width;

                    if (!string.IsNullOrWhiteSpace(col.HeaderText))
                    {
                        var textRect = new Rectangle(colX + 12, 0, Math.Max(20, colW - 24), headerHeight);
                        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                        if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleRight)
                            flags |= TextFormatFlags.Right;
                        else if (col.HeaderCell.Style.Alignment == DataGridViewContentAlignment.MiddleCenter)
                            flags |= TextFormatFlags.HorizontalCenter;
                        else
                            flags |= TextFormatFlags.Left;

                        TextRenderer.DrawText(g, col.HeaderText, headerFont, textRect, UiGridHelper.HeaderText, flags);
                    }
                    else if (col.Name != "Actions" && !(col is ActionsColumn))
                    {
                        var headBarRect = new Rectangle(colX + 12, (headerHeight - 10) / 2, Math.Min(60, Math.Max(30, colW - 24)), 10);
                        SkeletonPulseHelper.DrawSkeletonBar(g, headBarRect, 3, w);
                    }
                }
            }
            else
            {
                // Fallback default column header labels
                int[] fallbackWidths = { 180, 150, 140, 120, 100 };
                string[] fallbackHeaders = { "NAME", "DETAILS", "CATEGORY", "DATE / VALUE", "STATUS" };
                int curX = 0;
                using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                for (int i = 0; i < fallbackWidths.Length && curX < w; i++)
                {
                    int fw = fallbackWidths[i];
                    var rect = new Rectangle(curX + 12, 0, fw - 24, headerHeight);
                    TextRenderer.DrawText(g, fallbackHeaders[i], font, rect, UiGridHelper.HeaderText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    curX += fw;
                }
            }

            // 2. Rows
            int availableHeight = h - headerHeight;
            int maxRows = Math.Max(4, Math.Min(10, availableHeight / rowHeight));
            int rowsToDraw = Math.Min(maxRows, Math.Max(_requestedRowCount, 5));

            for (int r = 0; r < rowsToDraw; r++)
            {
                int rowY = headerHeight + r * rowHeight;
                var rowRect = new Rectangle(0, rowY, w, rowHeight);

                Color rowBg = (r % 2 == 1) ? SkeletonPulseHelper.RowAlternate : SkeletonPulseHelper.RowNormal;
                using (var rowBrush = new SolidBrush(rowBg))
                {
                    g.FillRectangle(rowBrush, rowRect);
                }

                // Bottom row divider
                using (var linePen = new Pen(UiGridHelper.GridBorder, 1f))
                {
                    g.DrawLine(linePen, 0, rowY + rowHeight - 1, w, rowY + rowHeight - 1);
                }

                // Render columns within row
                if (_grid != null && _grid.Columns.Count > 0)
                {
                    foreach (DataGridViewColumn col in _grid.Columns)
                    {
                        if (!col.Visible) continue;
                        var colRect = _grid.GetColumnDisplayRectangle(col.Index, false);
                        int colX = colRect.X >= 0 && colRect.Width > 0 ? colRect.X : GetFallbackColX(col.Index);
                        int colW = colRect.Width > 0 ? colRect.Width : col.Width;

                        DrawCellSkeleton(g, col, colX, rowY, colW, rowHeight, w, r);
                    }
                }
                else
                {
                    DrawFallbackRowCells(g, rowY, rowHeight, w, r);
                }
            }
        }

        private int GetFallbackColX(int targetIndex)
        {
            if (_grid == null) return 0;
            int x = 0;
            for (int i = 0; i < targetIndex && i < _grid.Columns.Count; i++)
            {
                if (_grid.Columns[i].Visible)
                    x += _grid.Columns[i].Width;
            }
            return x;
        }

        private void DrawCellSkeleton(Graphics g, DataGridViewColumn col, int colX, int rowY, int colW, int rowH, int canvasW, int rowIndex)
        {
            string name = (col.Name ?? "").ToLowerInvariant();
            string header = (col.HeaderText ?? "").ToLowerInvariant();

            bool isAvatarCol = name.Contains("name") || name.Contains("customer") || name.Contains("lead") ||
                               name.Contains("client") || name.Contains("agent") || name.Contains("user") ||
                               name.Contains("admin") || name.Contains("actor") || name.Contains("tenant") ||
                               name.Contains("assigned") || header.Contains("name") || header.Contains("client");

            bool isStatusCol = name.Contains("status") || name.Contains("stage") || name.Contains("type") ||
                               name.Contains("tier") || name.Contains("plan") || name.Contains("role") ||
                               header.Contains("status") || header.Contains("stage") || header.Contains("tier");

            bool isActionCol = name.Contains("action") || col is ActionsColumn;

            bool isNumericCol = name.Contains("price") || name.Contains("amount") || name.Contains("value") ||
                                name.Contains("commission") || name.Contains("count") || name.Contains("revenue") ||
                                name.Contains("budget") || name.Contains("total") || col.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight;

            bool isDateCol = name.Contains("date") || name.Contains("time") || name.Contains("created") ||
                             name.Contains("close") || header.Contains("date");

            if (isAvatarCol)
            {
                // Avatar circle (28x28) + text bar
                int avatarSize = 28;
                int avatarY = rowY + (rowH - avatarSize) / 2;
                var circleRect = new Rectangle(colX + 12, avatarY, avatarSize, avatarSize);
                SkeletonPulseHelper.DrawSkeletonCircle(g, circleRect, canvasW);

                int textW = Math.Max(40, Math.Min(130, colW - avatarSize - 36));
                int textY = rowY + (rowH - 14) / 2;
                var textRect = new Rectangle(colX + 12 + avatarSize + 10, textY, textW, 14);
                SkeletonPulseHelper.DrawSkeletonBar(g, textRect, 4, canvasW);
            }
            else if (isActionCol)
            {
                // Circular action button outline
                int btnSize = 24;
                int btnX = colX + (colW - btnSize) / 2;
                int btnY = rowY + (rowH - btnSize) / 2;
                SkeletonPulseHelper.DrawSkeletonCircle(g, new Rectangle(btnX, btnY, btnSize, btnSize), canvasW);
            }
            else if (isStatusCol)
            {
                // Status indicator bar
                int barW = Math.Min(65, Math.Max(40, colW - 24));
                int barY = rowY + (rowH - 14) / 2;
                var barRect = new Rectangle(colX + 12, barY, barW, 14);
                SkeletonPulseHelper.DrawSkeletonBar(g, barRect, 4, canvasW);
            }
            else if (isNumericCol)
            {
                // Right-aligned numeric bar
                int barW = Math.Min(80, Math.Max(40, colW - 24));
                int barX = colX + colW - 12 - barW;
                int barY = rowY + (rowH - 14) / 2;
                var barRect = new Rectangle(barX, barY, barW, 14);
                SkeletonPulseHelper.DrawSkeletonBar(g, barRect, 4, canvasW);
            }
            else if (isDateCol)
            {
                // Date bar (~75px)
                int barW = Math.Min(85, Math.Max(40, colW - 24));
                int barY = rowY + (rowH - 12) / 2;
                var barRect = new Rectangle(colX + 12, barY, barW, 12);
                SkeletonPulseHelper.DrawSkeletonBar(g, barRect, 4, canvasW);
            }
            else
            {
                // General text bar (varying slightly per row for realistic look)
                int varOffset = (rowIndex % 3) * 15;
                int barW = Math.Max(35, Math.Min(colW - 28 - varOffset, 160));
                int barY = rowY + (rowH - 14) / 2;
                var barRect = new Rectangle(colX + 12, barY, barW, 14);
                SkeletonPulseHelper.DrawSkeletonBar(g, barRect, 4, canvasW);
            }
        }

        private void DrawFallbackRowCells(Graphics g, int rowY, int rowH, int canvasW, int rowIndex)
        {
            // Col 1: Avatar + Name
            int avatarSize = 28;
            int avatarY = rowY + (rowH - avatarSize) / 2;
            SkeletonPulseHelper.DrawSkeletonCircle(g, new Rectangle(12, avatarY, avatarSize, avatarSize), canvasW);
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(48, rowY + (rowH - 14) / 2, 110, 14), 4, canvasW);

            // Col 2: Details / Email
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(192, rowY + (rowH - 13) / 2, 120, 13), 4, canvasW);

            // Col 3: Category
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(342, rowY + (rowH - 13) / 2, 90, 13), 4, canvasW);

            // Col 4: Date / Amount
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(482, rowY + (rowH - 13) / 2, 80, 13), 4, canvasW);

            // Col 5: Status
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(602, rowY + (rowH - 14) / 2, 60, 14), 4, canvasW);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SkeletonPulseHelper.Unregister(this);
            }
            base.Dispose(disposing);
        }
    }
}
