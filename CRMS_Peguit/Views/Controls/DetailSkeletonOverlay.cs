using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Skeleton loading overlay for Detail forms and modals (CustomerDetailForm,
    /// LeadDetailForm, SupportTicketDetailForm, PropertyDetailForm, DealDetailForm,
    /// Retention Request dialogs).
    /// Renders realistic placeholder layout: large circular avatar block, bold title bars,
    /// field-label-to-value rows, and structured cards.
    /// </summary>
    public class DetailSkeletonOverlay : Control
    {
        private Control? _container;
        private bool _isAttached = false;

        public DetailSkeletonOverlay()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.FromArgb(248, 250, 252); // Slate 50 canvas background
            Visible = false;
        }

        public void AttachToContainer(Control container)
        {
            if (_container == container && _isAttached) return;

            _container = container ?? throw new ArgumentNullException(nameof(container));

            if (!_container.Controls.Contains(this))
            {
                _container.Controls.Add(this);
            }
            Dock = DockStyle.Fill;
            _isAttached = true;
        }

        public static DetailSkeletonOverlay CreateForContainer(Control container)
        {
            var overlay = new DetailSkeletonOverlay();
            overlay.AttachToContainer(container);
            return overlay;
        }

        public void ShowSkeleton()
        {
            if (_container != null && !_container.Controls.Contains(this))
            {
                _container.Controls.Add(this);
                Dock = DockStyle.Fill;
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

            // 1. Header Banner Card (Height = 88)
            var headerRect = new Rectangle(16, 12, w - 32, 88);
            DrawCard(g, headerRect);

            // Avatar circle
            int avatarSize = 52;
            var avatarRect = new Rectangle(headerRect.Left + 18, headerRect.Top + 18, avatarSize, avatarSize);
            SkeletonPulseHelper.DrawSkeletonCircle(g, avatarRect, w);

            // Title & Subtitle bars
            int titleX = avatarRect.Right + 16;
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(titleX, headerRect.Top + 22, 180, 18), 4, w);
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(titleX, headerRect.Top + 48, 120, 12), 3, w);

            // Right-aligned status pill & action button skeletons
            int rightX = headerRect.Right - 18;
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(rightX - 80, headerRect.Top + 22, 80, 24), 6, w);
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(rightX - 170, headerRect.Top + 22, 75, 24), 6, w);

            // 2. Field Detail Cards (2 Columns if wide enough, otherwise stacked)
            int bodyY = headerRect.Bottom + 16;
            int cardGap = 16;
            bool twoCols = w >= 650;
            int cardWidth = twoCols ? (w - 32 - cardGap) / 2 : w - 32;
            int cardHeight = 220;

            // Left Card: Core Information
            var leftCardRect = new Rectangle(16, bodyY, cardWidth, cardHeight);
            DrawFieldCard(g, leftCardRect, "CORE INFORMATION", new[]
            {
                ("Email Address", 180),
                ("Phone Number", 130),
                ("Category / Type", 100),
                ("Assigned To", 140)
            }, w);

            // Right Card: Additional Details
            int rightCardX = twoCols ? 16 + cardWidth + cardGap : 16;
            int rightCardY = twoCols ? bodyY : bodyY + cardHeight + cardGap;
            var rightCardRect = new Rectangle(rightCardX, rightCardY, cardWidth, cardHeight);
            DrawFieldCard(g, rightCardRect, "METRICS & TIMESTAMPS", new[]
            {
                ("Current Status", 90),
                ("Created Date", 120),
                ("Last Activity", 150),
                ("Estimated Value", 110)
            }, w);

            // 3. Bottom Activity / Timeline Card if space permits
            int bottomY = twoCols ? bodyY + cardHeight + cardGap : rightCardY + cardHeight + cardGap;
            if (bottomY + 120 <= h)
            {
                var timelineRect = new Rectangle(16, bottomY, w - 32, h - bottomY - 16);
                DrawTimelineSkeleton(g, timelineRect, w);
            }
        }

        private void DrawCard(Graphics g, Rectangle bounds)
        {
            using var bgBrush = new SolidBrush(Theme.Surface);
            using var path = UiRadiusHelper.CreateRoundedPath(bounds, 12);
            g.FillPath(bgBrush, path);
            using var borderPen = new Pen(Theme.Border, 1f);
            g.DrawPath(borderPen, path);
        }

        private void DrawFieldCard(Graphics g, Rectangle bounds, string sectionTitle, (string Label, int ValWidth)[] fields, int canvasW)
        {
            DrawCard(g, bounds);

            // Section title bar
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Left + 18, bounds.Top + 16, 120, 14), 4, canvasW);

            // Divider line below section title
            using (var linePen = new Pen(Theme.Border, 1f))
            {
                g.DrawLine(linePen, bounds.Left + 18, bounds.Top + 38, bounds.Right - 18, bounds.Top + 38);
            }

            // Field rows
            int rowY = bounds.Top + 48;
            int labelWidth = 100;

            foreach (var (_, valWidth) in fields)
            {
                // Field label bar
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Left + 18, rowY + 3, labelWidth, 11), 3, canvasW);

                // Field value bar
                int actualValW = Math.Min(valWidth, bounds.Right - bounds.Left - labelWidth - 54);
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Left + 18 + labelWidth + 14, rowY + 1, actualValW, 14), 4, canvasW);

                rowY += 38;
            }
        }

        private void DrawTimelineSkeleton(Graphics g, Rectangle bounds, int canvasW)
        {
            DrawCard(g, bounds);

            // Section header
            SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Left + 18, bounds.Top + 14, 130, 14), 4, canvasW);

            int itemY = bounds.Top + 40;
            for (int i = 0; i < 3 && itemY + 24 <= bounds.Bottom; i++)
            {
                // Timeline indicator dot
                SkeletonPulseHelper.DrawSkeletonCircle(g, new Rectangle(bounds.Left + 20, itemY + 4, 10, 10), canvasW);

                // Summary bar
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Left + 40, itemY + 2, Math.Min(220, bounds.Width - 160), 13), 4, canvasW);

                // Timestamp bar
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(bounds.Right - 90, itemY + 3, 70, 11), 3, canvasW);

                itemY += 30;
            }
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
