using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Controls
{
    public enum ChartSkeletonType
    {
        Bars,
        Donut,
        Line
    }

    /// <summary>
    /// Animated loading skeleton placeholder shown while chart data is being fetched.
    /// Replaces blank white or frozen panels with a subtle pulsing shimmer effect
    /// across bar-shaped, donut-shaped, or line-shaped outlines in Theme.Border colors.
    /// </summary>
    public class ChartSkeletonOverlay : Control
    {
        private ChartSkeletonType _skeletonType = ChartSkeletonType.Bars;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ChartSkeletonType SkeletonType
        {
            get => _skeletonType;
            set
            {
                _skeletonType = value;
                Invalidate();
            }
        }

        public ChartSkeletonOverlay()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.White;
            Visible = false;
        }

        public void ShowSkeleton(ChartSkeletonType type)
        {
            _skeletonType = type;
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

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 10 || h <= 10) return;

            // Compute moving shimmer highlight gradient using synchronized SkeletonPulseHelper phase
            float shimmerCenter = SkeletonPulseHelper.Phase * (w * 1.5f) - (w * 0.25f);
            int shimmerWidth = Math.Max(60, (int)(w * 0.35f));
            var shimmerRect = new Rectangle((int)shimmerCenter, 0, shimmerWidth, h);

            Color baseColor = SkeletonPulseHelper.BaseColor;
            Color shimmerColor = SkeletonPulseHelper.HighlightColor;

            using var baseBrush = new SolidBrush(baseColor);
            using var borderPen = new Pen(SkeletonPulseHelper.BorderColor, 1f);

            switch (_skeletonType)
            {
                case ChartSkeletonType.Donut:
                    DrawDonutSkeleton(g, w, h, baseBrush, shimmerCenter, shimmerWidth);
                    break;
                case ChartSkeletonType.Line:
                    DrawLineSkeleton(g, w, h, baseBrush, shimmerCenter, shimmerWidth);
                    break;
                case ChartSkeletonType.Bars:
                default:
                    DrawBarsSkeleton(g, w, h, baseBrush, shimmerCenter, shimmerWidth);
                    break;
            }
        }

        private void DrawBarsSkeleton(Graphics g, int w, int h, Brush baseBrush, float shimmerCenter, int shimmerWidth)
        {
            int barCount = Math.Max(4, Math.Min(7, w / 60));
            int bottomMargin = 30;
            int topMargin = 20;
            int availableH = h - bottomMargin - topMargin;
            int paddingX = 24;
            int totalBarWidth = w - paddingX * 2;
            int gap = 14;
            int barWidth = Math.Max(18, (totalBarWidth - (gap * (barCount - 1))) / barCount);

            // Realistic placeholder heights pattern
            float[] relativeHeights = new float[] { 0.45f, 0.78f, 0.55f, 0.90f, 0.65f, 0.82f, 0.40f };

            for (int i = 0; i < barCount; i++)
            {
                int x = paddingX + i * (barWidth + gap);
                float relH = relativeHeights[i % relativeHeights.Length];
                int barH = (int)(availableH * relH);
                int y = h - bottomMargin - barH;

                var rect = new Rectangle(x, y, barWidth, barH);
                using (var path = UiRadiusHelper.CreateRoundedPath(rect, 6))
                {
                    g.FillPath(baseBrush, path);

                    // Apply shimmer highlight to overlapping bars
                    if (rect.Right >= shimmerCenter && rect.Left <= shimmerCenter + shimmerWidth)
                    {
                        using var shimmerBrush = new LinearGradientBrush(
                            new Point((int)shimmerCenter, 0),
                            new Point((int)(shimmerCenter + shimmerWidth), 0),
                            Color.FromArgb(0, 255, 255, 255),
                            Color.FromArgb(160, 255, 255, 255));
                        g.FillPath(shimmerBrush, path);
                    }
                }

                // X-axis label skeleton line
                var labelRect = new Rectangle(x + (barWidth - 24) / 2, h - bottomMargin + 10, 24, 6);
                using (var labelPath = UiRadiusHelper.CreateRoundedPath(labelRect, 3))
                {
                    g.FillPath(baseBrush, labelPath);
                }
            }
        }

        private void DrawDonutSkeleton(Graphics g, int w, int h, Brush baseBrush, float shimmerCenter, int shimmerWidth)
        {
            int size = Math.Min(w, h) - 40;
            if (size <= 20) return;

            int cx = w / 2;
            int cy = h / 2;
            int outerR = size / 2;
            int innerR = (int)(outerR * 0.62f);

            var outerRect = new Rectangle(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            var innerRect = new Rectangle(cx - innerR, cy - innerR, innerR * 2, innerR * 2);

            using var donutPath = new GraphicsPath();
            donutPath.AddEllipse(outerRect);
            donutPath.AddEllipse(innerRect);

            g.FillPath(baseBrush, donutPath);

            // Shimmer over donut
            if (outerRect.Right >= shimmerCenter && outerRect.Left <= shimmerCenter + shimmerWidth)
            {
                using var shimmerBrush = new LinearGradientBrush(
                    new Point((int)shimmerCenter, 0),
                    new Point((int)(shimmerCenter + shimmerWidth), 0),
                    Color.FromArgb(0, 255, 255, 255),
                    Color.FromArgb(160, 255, 255, 255));
                g.FillPath(shimmerBrush, donutPath);
            }
        }

        private void DrawLineSkeleton(Graphics g, int w, int h, Brush baseBrush, float shimmerCenter, int shimmerWidth)
        {
            int pointCount = 6;
            int paddingX = 24;
            int availableW = w - paddingX * 2;
            int bottomMargin = 30;
            int topMargin = 20;
            int availableH = h - bottomMargin - topMargin;

            float[] relativeYs = new float[] { 0.60f, 0.45f, 0.70f, 0.35f, 0.50f, 0.25f };
            PointF[] points = new PointF[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float x = paddingX + ((float)i / (pointCount - 1)) * availableW;
                float y = topMargin + relativeYs[i % relativeYs.Length] * availableH;
                points[i] = new PointF(x, y);
            }

            using var linePen = new Pen(Color.FromArgb(226, 232, 240), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawCurve(linePen, points, 0.4f);

            foreach (var pt in points)
            {
                var circleRect = new RectangleF(pt.X - 5, pt.Y - 5, 10, 10);
                g.FillEllipse(baseBrush, circleRect);
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
