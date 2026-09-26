using System;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Services
{
    /// <summary>
    /// Centralized UI/UX design tokens and style constants for NEXA CRM.
    /// Establishes the uniform typography scale, spacing units, component dimensions,
    /// and standard dialog confirmation wording across all screens.
    /// </summary>
    public static class UiStyleConstants
    {
        // =========================================================================
        // 1. TYPOGRAPHY SCALE (Segoe UI family everywhere)
        // =========================================================================
        public const string FontFamily = "Segoe UI";

        /// <summary>Page headline: 20pt Bold</summary>
        public static readonly Font PageTitleFont = new Font(FontFamily, 20f, FontStyle.Bold);

        /// <summary>Dialog / Form header title: 16pt Bold</summary>
        public static readonly Font FormTitleFont = new Font(FontFamily, 16f, FontStyle.Bold);

        /// <summary>Card header / Section title: 14pt Bold</summary>
        public static readonly Font SectionTitleFont = new Font(FontFamily, 14f, FontStyle.Bold);

        /// <summary>Subsection title / Card group title: 11pt Bold</summary>
        public static readonly Font SubSectionTitleFont = new Font(FontFamily, 11f, FontStyle.Bold);

        /// <summary>Standard body text: 10pt Regular</summary>
        public static readonly Font BodyFont = new Font(FontFamily, 10f, FontStyle.Regular);

        /// <summary>Bold body text / Grid data: 9.5pt Bold</summary>
        public static readonly Font BodyBoldFont = new Font(FontFamily, 9.5f, FontStyle.Bold);

        /// <summary>Grid cell standard text: 9.5pt Regular</summary>
        public static readonly Font GridCellFont = new Font(FontFamily, 9.5f, FontStyle.Regular);

        /// <summary>Grid column header text: 8.5pt Bold</summary>
        public static readonly Font GridHeaderFont = new Font(FontFamily, 8.5f, FontStyle.Bold);

        /// <summary>Helper / Subtitle / Caption text: 9.5pt Regular</summary>
        public static readonly Font SubtitleFont = new Font(FontFamily, 9.5f, FontStyle.Regular);

        /// <summary>Small caption / Footnote: 9pt Regular</summary>
        public static readonly Font CaptionFont = new Font(FontFamily, 9f, FontStyle.Regular);

        /// <summary>Avatar / Badge initials: 8.5pt Bold</summary>
        public static readonly Font BadgeFont = new Font(FontFamily, 8.5f, FontStyle.Bold);

        // =========================================================================
        // 2. SPACING SCALE (Multiples of 8px / 10px / 12px)
        // =========================================================================
        public const int PageMarginLeft = 30;
        public const int PageMarginTop = 20;
        public const int PageMarginRight = 30;
        public const int PageMarginBottom = 24;

        public const int SpacingXs = 4;
        public const int SpacingSm = 8;
        public const int SpacingMd = 12;
        public const int SpacingLg = 16;
        public const int SpacingXl = 24;
        public const int SpacingXxl = 32;

        public static readonly Padding PagePadding = new Padding(PageMarginLeft, PageMarginTop, PageMarginRight, PageMarginBottom);

        // =========================================================================
        // 3. COMPONENT DIMENSIONS & GEOMETRY
        // =========================================================================
        public const int KpiRowHeight = 88;
        public const int ToolbarRowHeight = 36;
        public const int SearchBoxWidth = 320;
        public const int TableRowHeight = 52;
        public const int TableHeaderHeight = 46;
        public const int ActionsColumnWidth = 64;

        public const int CardCornerRadius = 12;
        public const int ButtonCornerRadius = 8;
        public const int BadgeCornerRadius = 6;

        // =========================================================================
        // 4. CONFIRMATION DIALOG TEMPLATES
        // =========================================================================
        /// <summary>
        /// Standardized confirmation message template: "Are you sure you want to {action} '{name}'?"
        /// </summary>
        public static string FormatConfirmMessage(string action, string name)
        {
            return $"Are you sure you want to {action} '{name}'?";
        }

        /// <summary>
        /// Displays standard confirmation dialog with Yes/No buttons and uniform phrasing.
        /// </summary>
        public static bool ConfirmAction(IWin32Window? owner, string action, string name, string title = "Confirm Action")
        {
            string message = FormatConfirmMessage(action, name);
            var result = MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Standard delete confirmation dialog.
        /// </summary>
        public static bool ConfirmDelete(IWin32Window? owner, string name, string recordType = "record")
        {
            return ConfirmAction(owner, $"delete this {recordType}", name, "Confirm Delete");
        }

        /// <summary>
        /// Standard archive confirmation dialog.
        /// </summary>
        public static bool ConfirmArchive(IWin32Window? owner, string name, string recordType = "record")
        {
            string action = recordType == "record" ? "archive" : $"archive this {recordType}";
            return ConfirmAction(owner, action, name, "Confirm Archive");
        }

        /// <summary>
        /// Standard restore confirmation dialog.
        /// </summary>
        public static bool ConfirmRestore(IWin32Window? owner, string name)
        {
            return ConfirmAction(owner, "restore", name, "Confirm Restore");
        }
    }
}
