using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Views.Shared
{
    public class LogActivityDialog : Form
    {
        private readonly ComboBox _cmbRelatedType;
        private readonly ComboBox _cmbRecord;
        private readonly ComboBox _cmbActivityType;
        private readonly Label _lblOutcome;
        private readonly ComboBox _cmbOutcome;
        private readonly Label _lblDuration;
        private readonly NumericUpDown _numDuration;
        private readonly Label _lblDateTime;
        private readonly DateTimePicker _dtpDate;
        private readonly DateTimePicker _dtpTime;
        private readonly TextBox _txtNotes;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;
        private readonly Label _lblError;

        private readonly List<Customer> _assignedCustomers;
        private readonly List<Lead> _assignedLeads;
        private readonly int? _preselectedCustomerId;
        private readonly int? _preselectedLeadId;

        public LogActivityDialog(int? preselectedCustomerId = null, int? preselectedLeadId = null)
        {
            _preselectedCustomerId = preselectedCustomerId;
            _preselectedLeadId = preselectedLeadId;

            Text = "Log Interaction";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 520);
            AutoScroll = false;
            MinimumSize = new Size(520, 500);
            BackColor = Color.White;

            // Load records assigned to current agent
            using var fuController = new FollowUpController();
            _assignedCustomers = fuController.GetAssignedCustomers(_preselectedCustomerId);
            _assignedLeads = fuController.GetAssignedLeads(_preselectedLeadId);

            // Header panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblHeaderTitle = new Label
            {
                Text = "⚡ Log Client Interaction",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 116, 144),
                Location = new Point(20, 10),
                AutoSize = true
            };

            var lblHeaderSub = new Label
            {
                Text = "Record a completed call, email, or meeting into the contact's unified timeline.",
                Font = new Font("Segoe UI", 8.75f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(22, 36),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);

            // Content Panel (Scrollable)
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            // --- Row 1: Contact Type & Record Picker ---
            var lblType = new Label
            {
                Text = "LOG FOR",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 16),
                AutoSize = true
            };

            _cmbRelatedType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 36),
                Size = new Size(130, 28)
            };
            _cmbRelatedType.Items.AddRange(new object[] { "Customer", "Lead" });
            _cmbRelatedType.SelectedIndex = _preselectedLeadId.HasValue ? 1 : 0;
            _cmbRelatedType.SelectedIndexChanged += (_, _) => PopulateRecords();

            var lblRecord = new Label
            {
                Text = "SELECT CONTACT",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(164, 16),
                AutoSize = true
            };

            _cmbRecord = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(164, 36),
                Size = new Size(322, 28)
            };

            // If pre-selected from a detail form, lock selection
            if (_preselectedCustomerId.HasValue || _preselectedLeadId.HasValue)
            {
                _cmbRelatedType.Enabled = false;
                _cmbRecord.Enabled = false;
            }

            // --- Row 2: Activity Type & Outcome ---
            var lblActType = new Label
            {
                Text = "ACTIVITY TYPE",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 76),
                AutoSize = true
            };

            _cmbActivityType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 96),
                Size = new Size(210, 28)
            };
            _cmbActivityType.Items.AddRange(new object[] { "Call", "Email", "Meeting", "Note" });
            _cmbActivityType.SelectedIndex = 0;
            _cmbActivityType.SelectedIndexChanged += (_, _) => UpdateDynamicFieldVisibility();

            _lblOutcome = new Label
            {
                Text = "CALL RESULT / DISPOSITION",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(248, 76),
                AutoSize = true
            };

            _cmbOutcome = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(248, 96),
                Size = new Size(238, 28)
            };
            _cmbOutcome.Items.Add(new ComboBoxItem<CallOutcome>(CallOutcome.Connected, "Connected"));
            _cmbOutcome.Items.Add(new ComboBoxItem<CallOutcome>(CallOutcome.LeftVoicemail, "Left Voicemail"));
            _cmbOutcome.Items.Add(new ComboBoxItem<CallOutcome>(CallOutcome.NoAnswer, "No Answer"));
            _cmbOutcome.Items.Add(new ComboBoxItem<CallOutcome>(CallOutcome.Busy, "Busy"));
            _cmbOutcome.Items.Add(new ComboBoxItem<CallOutcome>(CallOutcome.WrongNumber, "Wrong Number"));
            _cmbOutcome.SelectedIndex = 0;

            // --- Row 3: Duration & Date/Time ---
            _lblDuration = new Label
            {
                Text = "DURATION (MINUTES)",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 136),
                AutoSize = true
            };

            _numDuration = new NumericUpDown
            {
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 156),
                Size = new Size(130, 28),
                Minimum = 0,
                Maximum = 600,
                Value = 15
            };

            _lblDateTime = new Label
            {
                Text = "DATE & TIME",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(164, 136),
                AutoSize = true
            };

            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(164, 156),
                Size = new Size(170, 28),
                MinDate = DateTime.Today,
                Value = DateTime.Now.Date
            };

            _dtpTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(342, 156),
                Size = new Size(144, 28),
                Value = DateTime.Now
            };

            // --- Row 4: Notes ---
            var lblNotes = new Label
            {
                Text = "ACTIVITY DETAILS & NOTES",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 196),
                AutoSize = true
            };

            _txtNotes = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(24, 216),
                Size = new Size(462, 110)
            };

            // Error Label
            _lblError = new Label
            {
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(24, 336),
                Size = new Size(462, 20),
                Visible = false
            };

            pnlContent.Controls.Add(lblType);
            pnlContent.Controls.Add(_cmbRelatedType);
            pnlContent.Controls.Add(lblRecord);
            pnlContent.Controls.Add(_cmbRecord);
            pnlContent.Controls.Add(lblActType);
            pnlContent.Controls.Add(_cmbActivityType);
            pnlContent.Controls.Add(_lblOutcome);
            pnlContent.Controls.Add(_cmbOutcome);
            pnlContent.Controls.Add(_lblDuration);
            pnlContent.Controls.Add(_numDuration);
            pnlContent.Controls.Add(_lblDateTime);
            pnlContent.Controls.Add(_dtpDate);
            pnlContent.Controls.Add(_dtpTime);
            pnlContent.Controls.Add(lblNotes);
            pnlContent.Controls.Add(_txtNotes);
            pnlContent.Controls.Add(_lblError);

            // Footer Panel (Docked Bottom)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

            _btnSave = new Button
            {
                Text = "Save Activity",
                Size = new Size(110, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 91, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSaveClick;

            UiRadiusHelper.StyleButton(_btnSave, 8);
            UiRadiusHelper.StyleButton(_btnCancel, 8);

            pnlFooter.Resize += (_, _) =>
            {
                _btnSave.Location = new Point(pnlFooter.Width - _btnSave.Width - 24, 12);
                _btnCancel.Location = new Point(_btnSave.Left - _btnCancel.Width - 12, 12);
            };

            pnlFooter.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(_btnSave);

            Controls.Add(pnlContent);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);

            PopulateRecords();
            UpdateDynamicFieldVisibility();
        }

        private void UpdateDynamicFieldVisibility()
        {
            string actType = _cmbActivityType.SelectedItem?.ToString() ?? "Call";
            bool isCall = actType.Equals("Call", StringComparison.OrdinalIgnoreCase);
            bool isMeeting = actType.Equals("Meeting", StringComparison.OrdinalIgnoreCase);

            // Outcome: Call only
            _lblOutcome.Visible = isCall;
            _cmbOutcome.Visible = isCall;

            // Duration: Call or Meeting
            bool hasDuration = isCall || isMeeting;
            _lblDuration.Visible = hasDuration;
            _numDuration.Visible = hasDuration;

            // If no duration, shift DateTime picker left to fill the row
            if (hasDuration)
            {
                _lblDateTime.Location = new Point(164, 136);
                _dtpDate.Location = new Point(164, 156);
                _dtpDate.Size = new Size(170, 28);
                _dtpTime.Location = new Point(342, 156);
                _dtpTime.Size = new Size(144, 28);
            }
            else
            {
                _lblDateTime.Location = new Point(24, 136);
                _dtpDate.Location = new Point(24, 156);
                _dtpDate.Size = new Size(240, 28);
                _dtpTime.Location = new Point(272, 156);
                _dtpTime.Size = new Size(214, 28);
            }
        }

        private void PopulateRecords()
        {
            _cmbRecord.Items.Clear();

            if (_cmbRelatedType.SelectedIndex == 0) // Customer
            {
                foreach (var c in _assignedCustomers)
                {
                    _cmbRecord.Items.Add(new ComboBoxItem<int>(c.CustomerId, $"{c.FullName} ({c.Email ?? "No email"})"));
                }
            }
            else // Lead
            {
                foreach (var l in _assignedLeads)
                {
                    _cmbRecord.Items.Add(new ComboBoxItem<int>(l.LeadId, $"{l.FullName} ({l.Source ?? "Lead"})"));
                }
            }

            // Select pre-selected contact or first in list
            if (_preselectedCustomerId.HasValue && _cmbRelatedType.SelectedIndex == 0)
            {
                for (int i = 0; i < _cmbRecord.Items.Count; i++)
                {
                    if (_cmbRecord.Items[i] is ComboBoxItem<int> item && item.Value == _preselectedCustomerId.Value)
                    {
                        _cmbRecord.SelectedIndex = i;
                        return;
                    }
                }
            }
            else if (_preselectedLeadId.HasValue && _cmbRelatedType.SelectedIndex == 1)
            {
                for (int i = 0; i < _cmbRecord.Items.Count; i++)
                {
                    if (_cmbRecord.Items[i] is ComboBoxItem<int> item && item.Value == _preselectedLeadId.Value)
                    {
                        _cmbRecord.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (_cmbRecord.Items.Count > 0)
            {
                _cmbRecord.SelectedIndex = 0;
            }
        }

        private void BtnSaveClick(object? sender, EventArgs e)
        {
            if (_cmbRecord.SelectedItem is not ComboBoxItem<int> selectedItem)
            {
                _lblError.Text = "Please select a Customer or Lead.";
                _lblError.Visible = true;
                return;
            }

            string notes = _txtNotes.Text.Trim();
            if (string.IsNullOrWhiteSpace(notes))
            {
                _lblError.Text = "Please enter activity details/notes.";
                _lblError.Visible = true;
                return;
            }

            string actType = _cmbActivityType.SelectedItem?.ToString() ?? "Call";
            bool isCustomer = _cmbRelatedType.SelectedIndex == 0;

            // Combine selected date and time
            DateTime localCombined = _dtpDate.Value.Date + _dtpTime.Value.TimeOfDay;
            DateTime utcTimestamp = localCombined.ToUniversalTime();

            // Disallow past date
            if (_dtpDate.Value.Date < DateTime.Today)
            {
                _lblError.Text = "Activity date cannot be in the past. Please select today's date.";
                _lblError.Visible = true;
                return;
            }

            // Disallow future logging beyond 5 min
            if (utcTimestamp > DateTime.UtcNow.AddMinutes(5))
            {
                _lblError.Text = "Activity date/time cannot be in the future.";
                _lblError.Visible = true;
                return;
            }

            CallOutcome? outcome = null;
            if (actType.Equals("Call", StringComparison.OrdinalIgnoreCase) &&
                _cmbOutcome.SelectedItem is ComboBoxItem<CallOutcome> outcomeItem)
            {
                outcome = outcomeItem.Value;
            }

            int? duration = null;
            if (actType.Equals("Call", StringComparison.OrdinalIgnoreCase) ||
                actType.Equals("Meeting", StringComparison.OrdinalIgnoreCase))
            {
                duration = (int)_numDuration.Value;
            }

            try
            {
                using var actCtrl = new ActivityController();
                var activity = new Activity
                {
                    Type = actType,
                    RelatedCustomerId = isCustomer ? selectedItem.Value : null,
                    RelatedLeadId = isCustomer ? null : selectedItem.Value,
                    Notes = notes,
                    ActivityDate = utcTimestamp,
                    Outcome = outcome,
                    DurationMinutes = duration
                };

                actCtrl.LogActivity(activity);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = $"Failed to save activity: {ex.Message}";
                _lblError.Visible = true;
            }
        }

        private sealed class ComboBoxItem<T>
        {
            public T Value { get; }
            public string Display { get; }

            public ComboBoxItem(T value, string display)
            {
                Value = value;
                Display = display;
            }

            public override string ToString() => Display;
        }
    }
}
