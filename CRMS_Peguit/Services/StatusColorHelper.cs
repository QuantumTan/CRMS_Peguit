using System;
using System.Drawing;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Shared status color helper and Title Case converter for NEXA CRM.
    /// Provides unified semantic color mappings across all modules, grids, detail forms, and cards:
    /// - Green (Positive/Complete): Active, Won, Resolved, Closed, Closed-Won, Available, Approved, Converted, Completed, Sold, Online, Secure, Yes
    /// - Amber (In-Progress/Pending): Pending, In Progress, Pending Review, Under Review, Contacted, Qualified, Offer, Contract, Reserved, Under Contract, Follow Up, Medium, Normal, Today
    /// - Red (Negative/Blocked): Lost, Overdue, Inactive, Urgent, Critical, High, Rejected, Cancelled, Failed, No, Breached
    /// - Blue (Info/New): New, Open, Prospect, Upcoming
    /// - Slate Gray (Neutral/Unassigned): Unassigned, Low, Draft, Archived, Unknown, None, Nominal, "-"
    /// </summary>
    public static class StatusColorHelper
    {
        // ── Standard Semantic Palettes (Light pastel background, dark high-contrast foreground, subtle border) ──
        
        // Green / Success (WCAG AA compliant contrast ratio >= 4.5:1)
        public static readonly Color GreenBg = Color.FromArgb(220, 252, 231);     // Emerald 100 (#DCFCE7)
        public static readonly Color GreenFg = Color.FromArgb(22, 101, 52);       // Emerald 800 (#166534)
        public static readonly Color GreenBorder = Color.FromArgb(134, 239, 172); // Emerald 300 (#86EFAC)

        // Amber / Pending
        public static readonly Color AmberBg = Color.FromArgb(254, 243, 199);     // Amber 100 (#FEF3C7)
        public static readonly Color AmberFg = Color.FromArgb(146, 64, 14);       // Amber 800 (#92400E)
        public static readonly Color AmberBorder = Color.FromArgb(253, 230, 138); // Amber 300 (#FDE68A)

        // Red / Alert / Negative
        public static readonly Color RedBg = Color.FromArgb(254, 226, 226);       // Rose 100 (#FEE2E2)
        public static readonly Color RedFg = Color.FromArgb(153, 27, 27);         // Rose 800 (#991B1B)
        public static readonly Color RedBorder = Color.FromArgb(252, 165, 165);   // Rose 300 (#FCA5A5)

        // Blue / Info
        public static readonly Color BlueBg = Color.FromArgb(239, 246, 255);      // Blue 50 (#EFF6FF)
        public static readonly Color BlueFg = Color.FromArgb(29, 78, 216);        // Blue 700 (#1D4ED8)
        public static readonly Color BlueBorder = Color.FromArgb(191, 219, 254);  // Blue 200 (#BFDBFE)

        // Slate / Neutral
        public static readonly Color GrayBg = Color.FromArgb(241, 245, 249);      // Slate 100 (#F1F5F9)
        public static readonly Color GrayFg = Color.FromArgb(71, 85, 105);        // Slate 600 (#475569)
        public static readonly Color GrayBorder = Color.FromArgb(203, 213, 225);  // Slate 300 (#CBD5E1)

        /// <summary>
        /// Converts raw enum, snake_case, or uppercase strings to clean human-readable Title Case with spaces.
        /// E.g. "pending_review" -> "Pending Review", "in_progress" -> "In Progress", "CONTRACTSIGNED" -> "Contract Signed".
        /// </summary>
        public static string ToTitleCase(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "—";

            string trimmed = raw.Trim();

            // Handle common concatenated enum values
            if (string.Equals(trimmed, "CONTRACTSIGNED", StringComparison.OrdinalIgnoreCase))
                return "Contract Signed";
            if (string.Equals(trimmed, "CLOSEDWON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "CLOSED_WON", StringComparison.OrdinalIgnoreCase))
                return "Closed Won";
            if (string.Equals(trimmed, "CLOSEDLOST", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "CLOSED_LOST", StringComparison.OrdinalIgnoreCase))
                return "Closed Lost";
            if (string.Equals(trimmed, "PENDINGREVIEW", StringComparison.OrdinalIgnoreCase))
                return "Pending Review";
            if (string.Equals(trimmed, "UNDERCONTRACT", StringComparison.OrdinalIgnoreCase))
                return "Under Contract";
            if (string.Equals(trimmed, "INPROGRESS", StringComparison.OrdinalIgnoreCase))
                return "In Progress";
            if (string.Equals(trimmed, "FOLLOWUP", StringComparison.OrdinalIgnoreCase))
                return "Follow Up";

            // Split on underscore, space, or hyphen
            var tokens = trimmed.Replace('_', ' ').Replace('-', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t.Length == 1)
                {
                    tokens[i] = char.ToUpperInvariant(t[0]).ToString();
                }
                else
                {
                    tokens[i] = char.ToUpperInvariant(t[0]) + t.Substring(1).ToLowerInvariant();
                }
            }

            return string.Join(" ", tokens);
        }

        /// <summary>
        /// Maps any status string to its standard (Background, Text, Border) color tuple.
        /// </summary>
        public static (Color Background, Color Text, Color Border) GetColors(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return (GrayBg, GrayFg, GrayBorder);

            string key = status.Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToUpperInvariant();

            return key switch
            {
                // Green (Positive / Complete)
                "ACTIVE" or "AVAILABLE" or "CONVERTED" or "APPROVED" or "COMPLETED" or "WON" or "SOLD"
                or "RESOLVED" or "CLOSED" or "CLOSEDWON" or "ONLINE" or "SECURE" or "YES" => (GreenBg, GreenFg, GreenBorder),

                // Amber (Pending / In-Progress / Awaiting Action)
                "CONTACTED" or "QUALIFIED" or "PENDING" or "PENDINGREVIEW" or "UNDERREVIEW" or "INPROGRESS"
                or "OFFER" or "CONTRACT" or "CONTRACTSIGNED" or "RESERVED" or "RESERVATION" or "UNDERCONTRACT"
                or "FOLLOWUP" or "TODAY" or "MEDIUM" or "NORMAL" or "AWAITING" => (AmberBg, AmberFg, AmberBorder),

                // Red (Negative / Blocked / SLA Breach / Urgent)
                "INACTIVE" or "LOST" or "CLOSEDLOST" or "OVERDUE" or "URGENT" or "CRITICAL" or "HIGH"
                or "REJECTED" or "CANCELLED" or "CANCELED" or "FAILED" or "NO" or "BREACHED" => (RedBg, RedFg, RedBorder),

                // Blue / Info
                "OPEN" or "PROSPECT" or "UPCOMING" => (BlueBg, BlueFg, BlueBorder),

                // Gray / Neutral / New / Unstarted
                "NEW" or "UNASSIGNED" or "UNSTARTED" or "DRAFT" or "ARCHIVED" or "UNKNOWN" or "-" => (GrayBg, GrayFg, GrayBorder),
                _ => (GrayBg, GrayFg, GrayBorder)
            };
        }

        /// <summary>
        /// Returns the single bold foreground text color for a status string.
        /// </summary>
        public static Color GetTextColor(string? status)
        {
            return GetColors(status).Text;
        }

        /// <summary>
        /// Maps ticket/task priority levels to standard color tuple.
        /// </summary>
        public static (Color Background, Color Text, Color Border) GetPriorityColors(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority)) return (GrayBg, GrayFg, GrayBorder);

            string p = priority.Trim().ToUpperInvariant();
            return p switch
            {
                "URGENT" or "CRITICAL" or "HIGH" => (RedBg, RedFg, RedBorder),
                "MEDIUM" or "NORMAL" => (AmberBg, AmberFg, AmberBorder),
                "LOW" => (GrayBg, GrayFg, GrayBorder),
                _ => (GrayBg, GrayFg, GrayBorder)
            };
        }
    }
}
