using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.winforms.Controllers;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.SuperAdmin
{
    public class SuperAdminTenantBrandingDialog : Form
    {
        private readonly int _companyId;
        private readonly string _companyName;
        private readonly SuperAdminTenantController _controller;

        private TextBox _txtDisplayName = null!;
        private CheckBox _chkOverrideReserved = null!;
        private CheckBox _chkHidePoweredBy = null!;
        private PictureBox _picLogoPreview = null!;
        private Button _btnUploadLogo = null!;
        private Button _btnResetLogo = null!;
        private Label _lblLogoStatus = null!;
        private TextBox _txtContactEmail = null!;
        private TextBox _txtContactPhone = null!;
        private TextBox _txtAddress = null!;
        private Button _btnSave = null!;
        private Button _btnResetAll = null!;
        private Label _lblDuplicateWarning = null!;

        private byte[]? _pendingLogoBytes;
        private bool _logoChanged = false;

        public bool ChangesMade { get; private set; }

        public SuperAdminTenantBrandingDialog(int companyId, string companyName, SuperAdminTenantController controller)
        {
            _companyId = companyId;
            _companyName = companyName;
            _controller = controller;

            Text = $"Tenant Branding Oversight — {_companyName}";
            Size = new Size(580, 690);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            UiRadiusHelper.StyleModal(this, 8);

            InitializeComponent();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28),
                AutoScroll = true
            };

            int y = 20;

            var lblHeader = new Label
            {
                Text = $"Tenant Branding: {_companyName}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnl.Controls.Add(lblHeader);
            y += 28;

            var lblSub = new Label
            {
                Text = "Super Admin platform branding oversight. Only branding identity metadata is accessed.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, y),
                AutoSize = true
            };
            pnl.Controls.Add(lblSub);
            y += 36;

            // Display Name
            pnl.Controls.Add(MakeLabel("Tenant Display Name:", y));
            y += 20;

            _txtDisplayName = MakeTextBox(y, 500);
            pnl.Controls.Add(_txtDisplayName);
            y += 36;

            _chkOverrideReserved = new CheckBox
            {
                Text = "Allow platform reserved name override (Super Admin privilege)",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnl.Controls.Add(_chkOverrideReserved);
            y += 28;

            _lblDuplicateWarning = new Label
            {
                Text = "⚠️ Note: Another tenant is currently using this display name.",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                Location = new Point(24, y),
                AutoSize = true,
                Visible = false
            };
            pnl.Controls.Add(_lblDuplicateWarning);
            y += 26;

            // Brand Logo Selection
            pnl.Controls.Add(MakeLabel("Brand Logo:", y));
            y += 20;

            _picLogoPreview = new PictureBox
            {
                Location = new Point(24, y),
                Size = new Size(84, 84),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnl.Controls.Add(_picLogoPreview);

            _btnUploadLogo = new Button
            {
                Text = "📁  Choose Logo Image...",
                Location = new Point(122, y),
                Size = new Size(196, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnUploadLogo, 6);
            _btnUploadLogo.Click += BtnUploadLogo_Click;
            pnl.Controls.Add(_btnUploadLogo);

            _btnResetLogo = new Button
            {
                Text = "🗑  Remove Logo",
                Location = new Point(328, y),
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnResetLogo, 6);
            _btnResetLogo.Click += BtnResetLogo_Click;
            pnl.Controls.Add(_btnResetLogo);

            _lblLogoStatus = new Label
            {
                Text = "Checking logo...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(122, y + 44),
                AutoSize = true
            };
            pnl.Controls.Add(_lblLogoStatus);
            y += 96;

            // White-Label Option
            _chkHidePoweredBy = new CheckBox
            {
                Text = "Hide 'Powered by NEXA' footer (Enterprise full white-label)",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary
            };
            pnl.Controls.Add(_chkHidePoweredBy);
            y += 36;

            // Contact
            pnl.Controls.Add(MakeLabel("Contact Email:", y));
            y += 20;
            _txtContactEmail = MakeTextBox(y, 500);
            pnl.Controls.Add(_txtContactEmail);
            y += 38;

            pnl.Controls.Add(MakeLabel("Contact Phone:", y));
            y += 20;
            _txtContactPhone = MakeTextBox(y, 500);
            pnl.Controls.Add(_txtContactPhone);
            y += 38;

            pnl.Controls.Add(MakeLabel("Office Address:", y));
            y += 20;
            _txtAddress = MakeTextBox(y, 500);
            pnl.Controls.Add(_txtAddress);
            y += 50;

            // Action Buttons
            _btnSave = new Button
            {
                Text = "💾  Save Branding",
                Location = new Point(24, y),
                Size = new Size(180, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnSave, 6);
            _btnSave.Click += BtnSave_Click;
            pnl.Controls.Add(_btnSave);

            _btnResetAll = new Button
            {
                Text = "🔄  Reset to Platform Default",
                Location = new Point(216, y),
                Size = new Size(200, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnResetAll, 6);
            _btnResetAll.Click += BtnResetAll_Click;
            pnl.Controls.Add(_btnResetAll);

            var btnCancel = new Button
            {
                Text = "Close",
                Location = new Point(426, y),
                Size = new Size(98, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(btnCancel, 6);
            btnCancel.Click += (_, _) => Close();
            pnl.Controls.Add(btnCancel);

            Controls.Add(pnl);
        }

        private async Task LoadDataAsync()
        {
            var dto = await _controller.GetTenantBrandingAsync(_companyId);
            var logoBytes = await _controller.GetTenantLogoAsync(_companyId);

            if (dto != null)
            {
                _txtDisplayName.Text = dto.DisplayName;
                _chkHidePoweredBy.Checked = dto.HidePoweredBy;
                _txtContactEmail.Text = dto.ContactEmail ?? "";
                _txtContactPhone.Text = dto.ContactPhone ?? "";
                _txtAddress.Text = dto.Address ?? "";
                _lblDuplicateWarning.Visible = dto.IsDuplicateName;
                _chkHidePoweredBy.Enabled = dto.CanHidePoweredBy;

                _pendingLogoBytes = logoBytes;
                _logoChanged = false;

                if (logoBytes != null && logoBytes.Length > 0)
                {
                    _picLogoPreview.Image = AppBrand.CreateBitmapFromBytes(logoBytes) ?? AppBrand.Logo;
                    _lblLogoStatus.Text = $"Custom Logo (v{dto.LogoVersion})";
                    _btnResetLogo.Enabled = true;
                }
                else
                {
                    _picLogoPreview.Image = AppBrand.Logo;
                    _lblLogoStatus.Text = "Platform Default Logo";
                    _btnResetLogo.Enabled = false;
                }
            }
        }

        private void BtnUploadLogo_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Choose Brand Logo Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(ofd.FileName);
                    var (success, normalized, error) = LogoProcessor.ProcessAndNormalize(bytes);

                    if (!success || normalized == null)
                    {
                        MessageBox.Show(error ?? "Invalid image file.", "Logo Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _pendingLogoBytes = normalized;
                    _logoChanged = true;

                    _picLogoPreview.Image = AppBrand.CreateBitmapFromBytes(normalized) ?? AppBrand.Logo;
                    _lblLogoStatus.Text = "New logo selected (unsaved)";
                    _btnResetLogo.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnResetLogo_Click(object? sender, EventArgs e)
        {
            var cfm = MessageBox.Show(
                "Remove custom logo for this tenant and revert to platform default?",
                "Confirm Logo Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (cfm == DialogResult.Yes)
            {
                _pendingLogoBytes = null;
                _logoChanged = true;
                _picLogoPreview.Image = AppBrand.Logo;
                _lblLogoStatus.Text = "Platform default (unsaved)";
                _btnResetLogo.Enabled = false;
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var req = new UpdateBrandingRequest
            {
                DisplayName = _txtDisplayName.Text.Trim(),
                AccentColor = null,
                ContactEmail = !string.IsNullOrWhiteSpace(_txtContactEmail.Text) ? _txtContactEmail.Text.Trim() : null,
                ContactPhone = !string.IsNullOrWhiteSpace(_txtContactPhone.Text) ? _txtContactPhone.Text.Trim() : null,
                Address = !string.IsNullOrWhiteSpace(_txtAddress.Text) ? _txtAddress.Text.Trim() : null,
                HidePoweredBy = _chkHidePoweredBy.Checked
            };

            var (success, error) = await _controller.UpdateTenantBrandingAsync(
                _companyId, 
                req, 
                _chkOverrideReserved.Checked,
                _pendingLogoBytes,
                _logoChanged);

            if (success)
            {
                ChangesMade = true;
                MessageBox.Show("Tenant branding updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show(error ?? "Failed to update branding.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnResetAll_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Reset branding for '{_companyName}' back to platform defaults?\nDisplay name will be reset to company name and any custom logo will be removed.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                var (success, error) = await _controller.ResetTenantBrandingAsync(_companyId);
                if (success)
                {
                    ChangesMade = true;
                    MessageBox.Show("Branding reset to platform defaults.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show(error ?? "Failed to reset branding.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static Label MakeLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(24, y),
                AutoSize = true
            };
        }

        private static TextBox MakeTextBox(int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(24, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiRadiusHelper.SetPadding(txt, 6, 6);
            return txt;
        }
    }
}
