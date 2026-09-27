using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Skeleton loading overlay for short list widgets (Today's Follow-Ups, Recent Activity,
    /// Pending Assignments, Platform Activity stream).
    /// Renders 3-5 placeholder list-item cards with avatar/icon circles, primary text bars,
    /// subtitle bars, and right-aligned timestamp badges.
    /// </summary>
    public class ListSkeletonOverlay : Control
    {
        private Panel? _container;
        private int _itemCount = 4;
        private int _itemHeight = 54;
        private bool _isAttached = false;

        public bool IsActive => Visible;

        public ListSkeletonOverlay()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Visible = false;
        }

        public void AttachToContainer(Panel container, int itemHeight = 54)
        {
            if (_container == container && _isAttached) return;

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _itemHeight = itemHeight;

            if (!_container.Controls.Contains(this))
            {
                _container.Controls.Add(this);
            }
            Dock = DockStyle.Fill;
            _isAttached = true;
        }

        public static ListSkeletonOverlay CreateForContainer(Panel container, int itemHeight = 54)
        {
            var overlay = new ListSkeletonOverlay();
            overlay.AttachToContainer(container, itemHeight);
            return overlay;
        }

        public void ShowSkeleton(int count = 4)
        {
            _itemCount = Math.Max(2, Math.Min(6, count));

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

            int padX = 8;
            int gapY = 8;
            int availableWidth = w - padX * 2;
            int curY = 6;

            for (int i = 0; i < _itemCount && curY + _itemHeight <= h + 20; i++)
            {
                var cardRect = new Rectangle(padX, curY, availableWidth, _itemHeight);

                // Card background and border
                using (var bgBrush = new SolidBrush(Theme.Surface))
                using (var path = UiRadiusHelper.CreateRoundedPath(cardRect, 8))
                {
                    g.FillPath(bgBrush, path);
                    using var borderPen = new Pen(Theme.Border, 1f);
                    g.DrawPath(borderPen, path);
                }

                // 1. Avatar / Icon circle on left
                int avatarSize = Math.Max(24, _itemHeight - 24);
                int avatarX = cardRect.Left + 12;
                int avatarY = cardRect.Top + (_itemHeight - avatarSize) / 2;
                SkeletonPulseHelper.DrawSkeletonCircle(g, new Rectangle(avatarX, avatarY, avatarSize, avatarSize), w);

                // 2. Title bar
                int textX = avatarX + avatarSize + 12;
                int textW = Math.Max(40, Math.Min(150 + (i % 3) * 20, cardRect.Right - textX - 90));
                int titleY = cardRect.Top + (_itemHeight >= 50 ? 11 : 8);
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(textX, titleY, textW, 13), 4, w);

                // 3. Subtitle bar
                int subW = Math.Max(30, Math.Min(100 + (i % 2) * 25, cardRect.Right - textX - 80));
                int subY = titleY + 18;
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(textX, subY, subW, 11), 3, w);

                // 4. Right timestamp / badge bar
                int badgeW = 60;
                int badgeX = cardRect.Right - 12 - badgeW;
                int badgeY = cardRect.Top + (_itemHeight - 14) / 2;
                SkeletonPulseHelper.DrawSkeletonBar(g, new Rectangle(badgeX, badgeY, badgeW, 14), 4, w);

                curY += _itemHeight + gapY;
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
