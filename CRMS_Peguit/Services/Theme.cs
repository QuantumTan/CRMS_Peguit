using System.Drawing;
using CRMS_Peguit.Models;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Central design system for the NEXA CRM application,
    /// optimized for a crisp, high-contrast, modern Light SaaS aesthetic.
    /// </summary>
    public static class Theme
    {
        // Core Palette (Modern Light SaaS: Slate & Sky)
        public static Color Primary = Color.FromArgb(2, 132, 199);          // Sky 600 (#0284C7)
        public static Color PrimaryDark = Color.FromArgb(3, 105, 161);      // Sky 700 (#0369A1)
        public static Color PrimaryLight = Color.FromArgb(224, 242, 254);   // Sky 100 (#E0F2FE)
        public static Color PrimaryBadgeText = Color.FromArgb(3, 105, 161); // Sky 700 (#0369A1)
        public static Color Surface = Color.FromArgb(255, 255, 255);        // Pure White (#FFFFFF)
        public static Color Background = Color.FromArgb(248, 250, 252);     // Slate 50 (#F8FAFC)
        public static Color TextPrimary = Color.FromArgb(15, 23, 42);       // Slate 900 (#0F172A)
        public static Color TextSecondary = Color.FromArgb(100, 116, 139);  // Slate 500 (#64748B)
        public static Color Border = Color.FromArgb(226, 232, 240);        // Slate 200 (#E2E8F0)
        public static Color Success = Color.FromArgb(22, 163, 74);          // Green 600 (#16A34A)
        public static Color Danger = Color.FromArgb(220, 38, 38);           // Red 600 (#DC2626)

        // Sidebar & Hero Palette (Modern Dark Slate #0F172A)
        public static Color SidebarBackground = Color.FromArgb(15, 23, 42);   // Slate 900 (#0F172A)
        public static Color SidebarSelected = Color.FromArgb(30, 41, 59);     // Slate 800 (#1E293B)
        public static Color SidebarHover = Color.FromArgb(30, 41, 59);        // Slate 800 (#1E293B)
        public static Color SidebarText = Color.FromArgb(148, 163, 184);      // Slate 400 (#94A3B8)
        public static Color SidebarTextActive = Color.White;
        public static Color SidebarTextMuted = Color.FromArgb(100, 116, 139); // Slate 500 (#64748B)
        public static Color SidebarProfileCard = Color.FromArgb(30, 41, 59);  // Slate 800 (#1E293B)
        public static Color SidebarAccent = Color.FromArgb(56, 189, 248);     // Sky 400 (#38BDF8)

        // Top Header (Crisp Light Surface)
        public static Color HeaderBackground = Color.FromArgb(255, 255, 255); // Pure White
        public static Color HeaderBorder = Color.FromArgb(226, 232, 240);     // Slate 200 (#E2E8F0)

        // Accessible UI Borders (≥ 3:1 against white/surface)
        public static Color BorderAccessible = Color.FromArgb(203, 213, 225); // Slate 300 (#CBD5E1)

        // Visible Focus Ring for keyboard navigation (Fitts's / WCAG 2.4.7)
        public static Color FocusBorder = Color.FromArgb(14, 165, 233);       // Sky 500 (#0EA5E9)

        // Minimalist Status Palette (Colored bold text standard)
        public static Color StatusSuccess = Color.FromArgb(22, 163, 74);      // Green (#16A34A) - Converted, Active, Available, Closed, Resolved, Sold, Won
        public static Color StatusPending = Color.FromArgb(217, 119, 6);      // Amber (#D97706) - Contacted, Pending Review, Offer, Contract, In Progress
        public static Color StatusAlert = Color.FromArgb(220, 38, 38);        // Red (#DC2626) - Inactive, Overdue, Lost, Urgent, Critical, High, Rejected
        public static Color StatusInfo = Color.FromArgb(37, 99, 235);         // Blue (#2563EB) - New, Open, Prospect, Upcoming
        public static Color StatusNeutral = Color.FromArgb(100, 116, 139);    // Slate 500 (#64748B) - Unassigned, Low, Draft, Archived, None

        // Legacy Status Badge Palette (retained for backward compatibility)
        public static Color StatusActiveBg = Color.FromArgb(220, 252, 231);
        public static Color StatusActiveText = Color.FromArgb(22, 101, 52);
        public static Color StatusFollowUpBg = Color.FromArgb(254, 243, 199);
        public static Color StatusFollowUpText = Color.FromArgb(180, 83, 9);
        public static Color StatusInactiveBg = Color.FromArgb(254, 226, 226);
        public static Color StatusInactiveText = Color.FromArgb(153, 27, 27);

        // OS High-Contrast Mode Awareness
        public static bool IsHighContrast => System.Windows.Forms.SystemInformation.HighContrast;

        public static Color GetEffectiveTextColor(Color defaultColor)
        {
            return IsHighContrast ? System.Drawing.SystemColors.WindowText : defaultColor;
        }

        public static Color GetEffectiveSurfaceColor(Color defaultColor)
        {
            return IsHighContrast ? System.Drawing.SystemColors.Window : defaultColor;
        }
    }
}