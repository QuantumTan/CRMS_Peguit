using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Shared interaction utilities enforcing the NEXA Click Behavior Standard:
    /// - IsCleanClick: Drag-threshold gate so a failed pan/zoom gesture never misfires as a click.
    /// - CreateHoverDebounceTimer: 75ms debounce timer for tooltip/hover repaints.
    /// Used identically by KpiCard, ChartWrapperControl, and every chart across the system.
    /// </summary>
    public static class InteractionHelper
    {
        /// <summary>
        /// Default drag-threshold in pixels. If the cursor moves more than this distance
        /// between MouseDown and MouseUp, the gesture is a drag, not a click.
        /// </summary>
        private const int DefaultDragThreshold = 4;

        /// <summary>
        /// Default hover debounce interval in milliseconds (50-100ms range per standard).
        /// </summary>
        private const int DefaultDebounceMs = 75;

        /// <summary>
        /// Determines whether a mouse gesture qualifies as a "clean click" (not a drag).
        /// Uses Manhattan distance for fast, predictable hit detection.
        /// 
        /// Standard requirement: "Click-triggered logic wired to MouseUp (or the library's
        /// equivalent 'click' event), never MouseDown, and gated by a clean-click drag-threshold
        /// check (a few pixels) so a failed pan/zoom gesture never misfires as a filter/navigation click."
        /// </summary>
        /// <param name="mouseDownPoint">The Point where MouseDown occurred (client coordinates).</param>
        /// <param name="mouseUpPoint">The Point where MouseUp/Click occurred (client coordinates).</param>
        /// <param name="threshold">Maximum Manhattan distance (px) to qualify as a click. Default: 4px.</param>
        /// <returns>True if the gesture is a clean click; false if it's a drag.</returns>
        public static bool IsCleanClick(Point mouseDownPoint, Point mouseUpPoint, int threshold = DefaultDragThreshold)
        {
            int dx = Math.Abs(mouseUpPoint.X - mouseDownPoint.X);
            int dy = Math.Abs(mouseUpPoint.Y - mouseDownPoint.Y);
            return (dx + dy) <= threshold;
        }

        /// <summary>
        /// Creates a pre-configured debounce timer for hover/tooltip logic.
        /// 
        /// Standard requirement: "Hover logic is debounced (50-100ms) on the pointer-move event
        /// so rapid cursor movement across a dense chart doesn't thrash the UI thread with tooltip
        /// repaints; the click behavior itself needs no debounce."
        /// 
        /// Usage pattern:
        ///   var timer = InteractionHelper.CreateHoverDebounceTimer(() => { /* hit-test + tooltip update */ });
        ///   // In MouseMove handler:
        ///   timer.Stop(); timer.Start();  // restart debounce on each move
        ///   // In MouseLeave handler:
        ///   timer.Stop(); /* clear tooltip immediately */
        /// </summary>
        /// <param name="onTick">Action to execute after the debounce interval elapses.</param>
        /// <param name="intervalMs">Debounce interval in milliseconds. Default: 75ms.</param>
        /// <returns>A configured but stopped Timer instance. Caller is responsible for disposal.</returns>
        public static System.Windows.Forms.Timer CreateHoverDebounceTimer(Action onTick, int intervalMs = DefaultDebounceMs)
        {
            var timer = new System.Windows.Forms.Timer
            {
                Interval = intervalMs,
                Enabled = false
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                onTick();
            };
            return timer;
        }

        /// <summary>
        /// Tracks MouseDown position for clean-click validation.
        /// Attach to a control's MouseDown event to record the press point,
        /// then call IsCleanClick in the Click/MouseUp handler.
        /// </summary>
        public sealed class ClickTracker
        {
            private Point _mouseDownPoint;

            /// <summary>
            /// Records the MouseDown position. Call this from the MouseDown event handler.
            /// </summary>
            public void RecordMouseDown(MouseEventArgs e)
            {
                _mouseDownPoint = e.Location;
            }

            /// <summary>
            /// Validates that the click is clean (not a drag) relative to the recorded MouseDown point.
            /// Call this from the Click or MouseUp event handler.
            /// </summary>
            /// <param name="currentClientPoint">Current mouse position in client coordinates.</param>
            /// <param name="threshold">Maximum Manhattan distance to qualify as a click.</param>
            /// <returns>True if the gesture is a clean click.</returns>
            public bool Validate(Point currentClientPoint, int threshold = DefaultDragThreshold)
            {
                return IsCleanClick(_mouseDownPoint, currentClientPoint, threshold);
            }

            /// <summary>
            /// Validates using MouseEventArgs directly.
            /// </summary>
            public bool Validate(MouseEventArgs e, int threshold = DefaultDragThreshold)
            {
                return IsCleanClick(_mouseDownPoint, e.Location, threshold);
            }
        }
    }
}
