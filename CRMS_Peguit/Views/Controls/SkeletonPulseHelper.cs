using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Central animation manager and Theme-matched color system for NEXA skeleton loading states.
    /// Manages a single lightweight ~30 FPS timer that runs ONLY when skeleton placeholders are active,
    /// driving a synchronized shimmer wave and opacity pulse across all loading components.
    /// </summary>
    public static class SkeletonPulseHelper
    {
        private static readonly System.Windows.Forms.Timer _timer;
        private static readonly HashSet<Control> _subscribers = new();
        private static float _phase = 0f; // 0.0 to 1.0 cycle over ~1.2s

        public static float Phase => _phase;

        // Theme-derived color definitions (Slate/Sky Light SaaS)
        public static Color BaseColor => Color.FromArgb(241, 245, 249);       // Slate 100
        public static Color HighlightColor => Theme.Border;                    // Slate 200 (#E2E8F0)
        public static Color BorderColor => Color.FromArgb(241, 245, 249);     // Slate 100 hairline
        public static Color RowNormal => Theme.Surface;                        // Pure White
        public static Color RowAlternate => Color.FromArgb(249, 250, 251);    // Slate 50 (#F9FAFB)

        static SkeletonPulseHelper()
        {
            _timer = new System.Windows.Forms.Timer { Interval = 35 }; // ~30 FPS
            _timer.Tick += OnTick;
        }

        public static void Register(Control control)
        {
            if (control == null || control.IsDisposed) return;
            if (_subscribers.Add(control))
            {
                if (!_timer.Enabled && _subscribers.Count > 0)
                {
                    _phase = 0f;
                    _timer.Start();
                }
            }
        }

        public static void Unregister(Control control)
        {
            if (control == null) return;
            _subscribers.Remove(control);
            if (_subscribers.Count == 0 && _timer.Enabled)
            {
                _timer.Stop();
            }
        }

        private static void OnTick(object? sender, EventArgs e)
        {
            _phase += 0.030f;
            if (_phase > 1f) _phase -= 1f;

            if (_subscribers.Count == 0)
            {
                _timer.Stop();
                return;
            }

            var toRemove = new List<Control>();
            foreach (var ctrl in _subscribers)
            {
                if (ctrl == null || ctrl.IsDisposed || !ctrl.Visible)
                {
                    if (ctrl != null) toRemove.Add(ctrl);
                }
                else
                {
                    ctrl.Invalidate();
                }
            }

            foreach (var r in toRemove)
            {
                _subscribers.Remove(r);
            }

            if (_subscribers.Count == 0)
            {
                _timer.Stop();
            }
        }

        /// <summary>
        /// Computes a gentle opacity multiplier oscillating between 0.50 and 0.95 in a 1.2s cycle.
        /// </summary>
        public static float GetPulseAlpha()
        {
            return 0.72f + 0.23f * (float)Math.Sin(_phase * 2.0 * Math.PI);
        }

        /// <summary>
        /// Paints a rounded placeholder bar with base color and moving shimmer highlight.
        /// </summary>
        public static void DrawSkeletonBar(Graphics g, Rectangle bounds, int radius = 4, int canvasWidth = 0)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            int cw = canvasWidth > 0 ? canvasWidth : bounds.Width;
            float shimmerCenter = _phase * (cw * 1.5f) - (cw * 0.25f);
            int shimmerWidth = Math.Max(60, (int)(cw * 0.35f));

            using var baseBrush = new SolidBrush(BaseColor);
            using var path = UiRadiusHelper.CreateRoundedPath(bounds, radius);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPath(baseBrush, path);

            // Shimmer highlight pass if overlapping
            if (bounds.Right >= shimmerCenter && bounds.Left <= shimmerCenter + shimmerWidth)
            {
                int left = Math.Max(bounds.Left, (int)shimmerCenter);
                int right = Math.Min(bounds.Right, (int)(shimmerCenter + shimmerWidth));
                if (right > left)
                {
                    using var shimmerBrush = new LinearGradientBrush(
                        new Point((int)shimmerCenter, 0),
                        new Point((int)(shimmerCenter + shimmerWidth), 0),
                        Color.FromArgb(0, 255, 255, 255),
                        Color.FromArgb(170, 255, 255, 255));
                    g.FillPath(shimmerBrush, path);
                }
            }
        }

        /// <summary>
        /// Paints a circular skeleton placeholder (e.g. for Avatar or Icon).
        /// </summary>
        public static void DrawSkeletonCircle(Graphics g, Rectangle bounds, int canvasWidth = 0)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            int cw = canvasWidth > 0 ? canvasWidth : bounds.Width;
            float shimmerCenter = _phase * (cw * 1.5f) - (cw * 0.25f);
            int shimmerWidth = Math.Max(60, (int)(cw * 0.35f));

            using var baseBrush = new SolidBrush(BaseColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillEllipse(baseBrush, bounds);

            if (bounds.Right >= shimmerCenter && bounds.Left <= shimmerCenter + shimmerWidth)
            {
                using var shimmerBrush = new LinearGradientBrush(
                    new Point((int)shimmerCenter, 0),
                    new Point((int)(shimmerCenter + shimmerWidth), 0),
                    Color.FromArgb(0, 255, 255, 255),
                    Color.FromArgb(170, 255, 255, 255));
                g.FillEllipse(shimmerBrush, bounds);
            }
        }
    }
}
