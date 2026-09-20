using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Controls
{
    /// <summary>
    /// Shared status presentation control:
    /// Renders status as bold colored TEXT only — strictly NO background pill shape, NO border, and NO pill container.
    /// Automatically converts raw enums and snake_case strings to clean Title Case.
    /// Shared 4-tier semantic color mapping:
    /// - Green = Positive / Complete (Active, Won, Resolved, Closed, Approved, Converted, Sold)
    /// - Amber = Pending / In-Progress (Pending, In Progress, Contacted, Qualified, Under Review)
    /// - Red   = Negative / Blocked (Lost, Overdue, Inactive, Urgent, Critical)
    /// - Gray  = New / Unstarted (New, Unassigned, Draft, Unknown)
    /// </summary>
    [ToolboxItem(true)]
    public class StatusText : Label
    {
        private string _rawStatus = string.Empty;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Status
        {
            get => _rawStatus;
            set
            {
                _rawStatus = value ?? string.Empty;
                UpdateDisplay();
            }
        }

        public StatusText()
        {
            AutoSize = true;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            TextAlign = ContentAlignment.MiddleLeft;
            Margin = new Padding(0);
            Padding = new Padding(0);
            UseMnemonic = false;
        }

        public StatusText(string rawStatus) : this()
        {
            SetStatus(rawStatus);
        }

        public void SetStatus(string rawStatus)
        {
            _rawStatus = rawStatus ?? string.Empty;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            string displayText = StatusColorHelper.ToTitleCase(_rawStatus);
            Text = displayText;
            ForeColor = StatusColorHelper.GetTextColor(_rawStatus);
            Invalidate();
        }
    }
}
