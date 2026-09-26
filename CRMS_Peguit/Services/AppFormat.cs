using System;

namespace CRMS_Peguit.winforms.Services
{
    /// <summary>
    /// Centralized currency, compact currency, percent, and number formatting helpers.
    /// Ensures consistent monetary presentation across all modules, tables, and KPI cards.
    /// </summary>
    public static class AppFormat
    {
        public const string CurrencySymbol = "₱";

        /// <summary>
        /// Formats currency with standard two decimal places and commas: e.g. "₱1,245,600.00".
        /// </summary>
        public static string FormatCurrency(decimal amount)
        {
            return $"{CurrencySymbol}{amount:N2}";
        }

        /// <summary>
        /// Formats large currency values concisely for KPI cards and charts:
        /// e.g. ₱1,245,600 -> "₱1.2M", ₱45,000 -> "₱45.0K", ₱850 -> "₱850".
        /// </summary>
        public static string FormatCompactCurrency(decimal amount)
        {
            decimal abs = Math.Abs(amount);
            string sign = amount < 0 ? "-" : "";

            if (abs >= 1_000_000_000m)
                return $"{sign}{CurrencySymbol}{(abs / 1_000_000_000m):F1}B";
            if (abs >= 1_000_000m)
                return $"{sign}{CurrencySymbol}{(abs / 1_000_000m):F1}M";
            if (abs >= 10_000m)
                return $"{sign}{CurrencySymbol}{(abs / 1_000m):F1}K";
            if (abs >= 1_000m)
                return $"{sign}{CurrencySymbol}{(abs / 1_000m):F1}K";

            return $"{sign}{CurrencySymbol}{abs:N0}";
        }

        /// <summary>
        /// Formats standard percentage: e.g. "12.5%".
        /// </summary>
        public static string FormatPercent(double percent)
        {
            return $"{percent:F1}%";
        }

        /// <summary>
        /// Formats standard percentage: e.g. "12.5%".
        /// </summary>
        public static string FormatPercent(decimal percent)
        {
            return $"{percent:F1}%";
        }

        /// <summary>
        /// Formats standard date: e.g. "Oct 15, 2026".
        /// </summary>
        public static string FormatDate(DateTime? date, string fallback = "—")
        {
            return date.HasValue ? date.Value.ToLocalTime().ToString("MMM dd, yyyy") : fallback;
        }

        /// <summary>
        /// Formats date and time: e.g. "Oct 15, 2026 02:30 PM".
        /// </summary>
        public static string FormatDateTime(DateTime? dateTime, string fallback = "—")
        {
            return dateTime.HasValue ? dateTime.Value.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt") : fallback;
        }

        /// <summary>
        /// Formats short ISO date: e.g. "2026-10-15".
        /// </summary>
        public static string FormatShortDate(DateTime? date, string fallback = "—")
        {
            return date.HasValue ? date.Value.ToLocalTime().ToString("yyyy-MM-dd") : fallback;
        }
    }
}
