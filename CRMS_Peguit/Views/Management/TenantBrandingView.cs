using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Controls;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Views.Management
{
    public class TenantBrandingView : UserControl
    {
        private Panel _pnlHeader = null!;
        private Panel _pnlContentWrapper = null!;
        private Panel _pnlCard = null!;

        // Fields
        private TextBox _txtDisplayName = null!;
        private Label _lblDisplayNameError = null!;

        private PictureBox _picLogoPreview = null!;
        private Button _btnUploadLogo = null!;
        private Button _btnRemoveLogo = null!;
        private byte[]? _pendingLogoBytes;
        private bool _logoChanged = false;

        private TextBox _txtContactEmail = null!;
        private TextBox _txtContactPhone = null!;
        private TextBox _txtAddress = null!;

        private CheckBox _chkHidePoweredBy = null!;
        private Label _lblHidePoweredByLocked = null!;

        private Button _btnSave = null!;
        private Button _btnReset = null!;

        private TenantBrandingDto? _currentBranding;

        public TenantBrandingView()
        {
            InitializeComponent();
            _ = LoadBrandingAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            AutoScroll = true;

            // 1. Top Page Header
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                Padding = new Padding(28, 18, 28, 0),
                BackColor = Theme.Surface
            };
            _pnlHeader.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.Border, 1f);
                e.Graphics.DrawLine(p, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "Tenant Branding & White-Label Settings",
                Font = UiStyleConstants.PageTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, 18)
            };

            var lblSubtitle = new Label
            {
                Text = "Customize your business identity, logo, client correspondence details, and visual styling.",
                Font = UiStyleConstants.SubtitleFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(28, 52)
            };

            _pnlHeader.Controls.Add(lblTitle);
            _pnlHeader.Controls.Add(lblSubtitle);
            ResponsiveLayout.BindHeader(_pnlHeader, lblTitle, lblSubtitle);

            // 2. Content Card Wrapper
            _pnlContentWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 28),
                BackColor = Theme.Background,
                AutoScroll = true
            };

            _pnlCard = new Panel
            {
                BackColor = Color.White,
                Padding = new Padding(32),
                Width = 840,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            UiRadiusHelper.StyleCard(_pnlCard, 8);

            int y = 20;

            // Section 1: Business Display Name
            var lblSec1 = MakeSectionHeader("ORGANIZATION DISPLAY IDENTITY", y);
            _pnlCard.Controls.Add(lblSec1);
            y += 28;

            _pnlCard.Controls.Add(MakeLabel("Business Display Name *", y));
            y += 20;

            _txtDisplayName = MakeTextBox(y, 450);
            _txtDisplayName.TextChanged += (s, e) => ValidateDisplayName();
            _pnlCard.Controls.Add(_txtDisplayName);

            var lblNameHint = new Label
            {
                Text = "This name replaces 'NEXA' on window title bars, sidebar navigation, reports, and emails.",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Location = new Point(24, y + 36),
                AutoSize = true
            };
            _pnlCard.Controls.Add(lblNameHint);

            _lblDisplayNameError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(24, y + 54),
                AutoSize = true,
                Visible = false
            };
            _pnlCard.Controls.Add(_lblDisplayNameError);
            y += 82;

            // Section 2: Logo Management
            var lblSec2 = MakeSectionHeader("BRAND LOGO", y);
            _pnlCard.Controls.Add(lblSec2);
            y += 28;

            _picLogoPreview = new PictureBox
            {
                Location = new Point(24, y),
                Size = new Size(110, 110),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _pnlCard.Controls.Add(_picLogoPreview);

            _btnUploadLogo = new Button
            {
                Text = "📁  Upload New Logo",
                Location = new Point(148, y + 10),
                Size = new Size(170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnUploadLogo, 6);
            _btnUploadLogo.Click += BtnUploadLogo_Click;
            _pnlCard.Controls.Add(_btnUploadLogo);

            _btnRemoveLogo = new Button
            {
                Text = "🗑  Remove Logo",
                Location = new Point(148, y + 56),
                Size = new Size(170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnRemoveLogo, 6);
            _btnRemoveLogo.Click += BtnRemoveLogo_Click;
            _pnlCard.Controls.Add(_btnRemoveLogo);

            var lblLogoHint = new Label
            {
                Text = "PNG or JPG, max 2 MB. Automatically sanitized and scaled to high-DPI 256x256 icon standard.",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Theme.TextSecondary,
                Location = new Point(148, y + 96),
                AutoSize = true
            };
            _pnlCard.Controls.Add(lblLogoHint);
            y += 136;

            // Section 3: Contact & Outreach Details
            var lblSec3 = MakeSectionHeader("CONTACT & FOOTER CORRESPONDENCE", y);
            _pnlCard.Controls.Add(lblSec3);
            y += 28;

            _pnlCard.Controls.Add(MakeLabel("Public Contact Email", y));
            var lblPhone = MakeLabel("Public Contact Phone", y);
            lblPhone.Location = new Point(410, y);
            _pnlCard.Controls.Add(lblPhone);
            y += 20;

            _txtContactEmail = MakeTextBox(y, 360);
            _pnlCard.Controls.Add(_txtContactEmail);

            _txtContactPhone = MakeTextBox(y, 360);
            _txtContactPhone.Location = new Point(410, y);
            _pnlCard.Controls.Add(_txtContactPhone);
            y += 44;

            _pnlCard.Controls.Add(MakeLabel("Office Address", y));
            y += 20;
            _txtAddress = MakeTextBox(y, 746);
            _pnlCard.Controls.Add(_txtAddress);
            y += 56;

            // Section 4: Enterprise Tier White-Labeling (Feature Gated)
            var lblSec4 = MakeSectionHeader("WHITE-LABEL BRANDING (ENTERPRISE)", y);
            _pnlCard.Controls.Add(lblSec4);
            y += 28;

            _chkHidePoweredBy = new CheckBox
            {
                Text = "Hide 'Powered by NEXA' footer (Full White-Label)",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Cursor = Cursors.Hand
            };
            _pnlCard.Controls.Add(_chkHidePoweredBy);

            _lblHidePoweredByLocked = new Label
            {
                Text = "⭐ Enterprise Tier Feature — Full white-labeling is reserved for Enterprise tier.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                Location = new Point(390, y + 2),
                AutoSize = true,
                Visible = false
            };
            _pnlCard.Controls.Add(_lblHidePoweredByLocked);
            y += 50;

            // Action Buttons
            var divider = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(746, 1),
                BackColor = Theme.Border
            };
            _pnlCard.Controls.Add(divider);
            y += 24;

            _btnSave = new Button
            {
                Text = "💾  Save Branding Changes",
                Location = new Point(24, y),
                Size = new Size(220, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnSave, 6);
            _btnSave.Click += BtnSave_Click;
            _pnlCard.Controls.Add(_btnSave);

            _btnReset = new Button
            {
                Text = "🔄  Reset to Default",
                Location = new Point(256, y),
                Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = Color.FromArgb(220, 38, 38),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            UiRadiusHelper.StyleButton(_btnReset, 6);
            _btnReset.Click += BtnReset_Click;
            _pnlCard.Controls.Add(_btnReset);
            y += 60;

            _pnlCard.Height = y;

            _pnlContentWrapper.Controls.Add(_pnlCard);
            Controls.Add(_pnlContentWrapper);
            Controls.Add(_pnlHeader);
            // The editor has deliberate two-column fields: allow scrolling rather than clipping them.
            _pnlCard.AutoSize = false;
            _pnlCard.MinimumSize = new Size(820, y);
            void LayoutBrandingCard()
            {
                _pnlCard.Location = new Point(_pnlContentWrapper.Padding.Left, _pnlContentWrapper.Padding.Top);
                _pnlCard.Width = Math.Max(820, _pnlContentWrapper.ClientSize.Width - _pnlContentWrapper.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
                _pnlContentWrapper.AutoScrollMinSize = new Size(_pnlCard.Width + _pnlContentWrapper.Padding.Horizontal,
                    _pnlCard.Height + _pnlContentWrapper.Padding.Vertical);
            }
            _pnlContentWrapper.SizeChanged += (_, _) => LayoutBrandingCard();
            LayoutBrandingCard();
        }

        private async Task LoadBrandingAsync()
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == tenantId);

                if (company != null)
                {
                    var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                    var tier = sub?.Tier ?? CurrentSession.TenantTier;
                    var branding = company.Branding;

                    _currentBranding = new TenantBrandingDto
                    {
                        CompanyId = company.CompanyId,
                        CompanyName = company.CompanyName,
                        DisplayName = branding?.DisplayName ?? company.CompanyName,
                        HasCustomLogo = branding?.LogoImage != null && branding.LogoImage.Length > 0,
                        LogoVersion = branding?.LogoVersion ?? 1,
                        AccentColor = branding?.AccentColor,
                        ContactEmail = branding?.ContactEmail,
                        ContactPhone = branding?.ContactPhone,
                        Address = branding?.Address,
                        HidePoweredBy = branding?.HidePoweredBy ?? false,
                        CanCustomizeAccent = FeatureGate.CanUseAccentColor(tier),
                        CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier)
                    };

                    _pendingLogoBytes = branding?.LogoImage;
                    _logoChanged = false;

                    PopulateFields(_currentBranding);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load branding settings: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateFields(TenantBrandingDto dto)
        {
            _txtDisplayName.Text = dto.DisplayName;
            _txtContactEmail.Text = dto.ContactEmail ?? "";
            _txtContactPhone.Text = dto.ContactPhone ?? "";
            _txtAddress.Text = dto.Address ?? "";
            _chkHidePoweredBy.Checked = dto.HidePoweredBy;

            // Logo preview
            if (_pendingLogoBytes != null && _pendingLogoBytes.Length > 0)
            {
                _picLogoPreview.Image = AppBrand.CreateBitmapFromBytes(_pendingLogoBytes) ?? AppBrand.Logo;
            }
            else
            {
                _picLogoPreview.Image = AppBrand.Logo;
            }

            // Gating permissions
            bool canHide = dto.CanHidePoweredBy;
            _chkHidePoweredBy.Enabled = canHide;
            _lblHidePoweredByLocked.Visible = !canHide;
        }

        private void ValidateDisplayName()
        {
            string name = _txtDisplayName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                _lblDisplayNameError.Text = "Display Name is required.";
                _lblDisplayNameError.Visible = true;
                return;
            }

            if (name.Length < 2 || name.Length > 100)
            {
                _lblDisplayNameError.Text = "Display Name must be between 2 and 100 characters.";
                _lblDisplayNameError.Visible = true;
                return;
            }

            if (BrandingValidationRules.ContainsHtmlOrScript(name))
            {
                _lblDisplayNameError.Text = "Display Name cannot contain HTML or script markup.";
                _lblDisplayNameError.Visible = true;
                return;
            }

            if (!BrandingValidationRules.AllowedDisplayNameRegex.IsMatch(name))
            {
                _lblDisplayNameError.Text = "Display Name contains invalid characters. Use letters, numbers, spaces, and & . , ' - only.";
                _lblDisplayNameError.Visible = true;
                return;
            }

            if (BrandingValidationRules.IsReservedName(name))
            {
                _lblDisplayNameError.Text = "This name is reserved by the platform and cannot be used.";
                _lblDisplayNameError.Visible = true;
                return;
            }

            _lblDisplayNameError.Visible = false;
        }

        private void BtnUploadLogo_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Brand Logo Image",
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRemoveLogo_Click(object? sender, EventArgs e)
        {
            var res = MessageBox.Show(
                "Remove custom tenant logo and revert to platform default?",
                "Remove Logo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _pendingLogoBytes = null;
                _logoChanged = true;
                _picLogoPreview.Image = AppBrand.Logo;
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            ValidateDisplayName();
            if (_lblDisplayNameError.Visible)
            {
                _txtDisplayName.Focus();
                return;
            }

            var confirm = MessageBox.Show(
                $"Apply these branding changes for your tenant organization?\n\nDisplay Name: '{_txtDisplayName.Text.Trim()}'\nAll users in your organization will see the updated brand.",
                "Confirm Branding Update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _btnSave.Enabled = false;
            _btnSave.Text = "Saving...";

            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .Include(c => c.Subscriptions)
                    .FirstOrDefaultAsync(c => c.CompanyId == tenantId);

                if (company == null)
                {
                    MessageBox.Show("Tenant organization record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var sub = company.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
                var tier = sub?.Tier ?? CurrentSession.TenantTier;

                var req = new UpdateBrandingRequest
                {
                    DisplayName = _txtDisplayName.Text.Trim(),
                    AccentColor = null,
                    ContactEmail = !string.IsNullOrWhiteSpace(_txtContactEmail.Text) ? _txtContactEmail.Text.Trim() : null,
                    ContactPhone = !string.IsNullOrWhiteSpace(_txtContactPhone.Text) ? _txtContactPhone.Text.Trim() : null,
                    Address = !string.IsNullOrWhiteSpace(_txtAddress.Text) ? _txtAddress.Text.Trim() : null,
                    HidePoweredBy = _chkHidePoweredBy.Checked
                };

                var validator = new UpdateBrandingRequestValidator(allowReservedOverride: false);
                var valResult = await validator.ValidateAsync(req);
                if (!valResult.IsValid)
                {
                    MessageBox.Show(valResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (req.HidePoweredBy && !FeatureGate.CanHidePoweredBy(tier))
                {
                    MessageBox.Show("White-label footer removal is locked for your plan tier.", "Feature Gated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var branding = company.Branding;
                if (branding == null)
                {
                    branding = new TenantBranding
                    {
                        CompanyId = tenantId,
                        DisplayName = req.DisplayName,
                        LogoVersion = 1,
                        UpdatedAt = DateTime.UtcNow
                    };
                    masterDb.TenantBrandings.Add(branding);
                }

                branding.DisplayName = req.DisplayName;
                branding.AccentColor = null;
                branding.HidePoweredBy = FeatureGate.CanHidePoweredBy(tier) && req.HidePoweredBy;
                branding.ContactEmail = req.ContactEmail;
                branding.ContactPhone = req.ContactPhone;
                branding.Address = req.Address;
                branding.UpdatedAt = DateTime.UtcNow;
                branding.UpdatedByUserId = CurrentSession.UserId;

                if (_logoChanged)
                {
                    branding.LogoImage = _pendingLogoBytes;
                    branding.LogoVersion++;
                    _logoChanged = false;
                }

                await masterDb.SaveChangesAsync();

                // Audit in tenant DB
                try
                {
                    using var tenantDb = LocalDb.CreateContext(tenantId);
                    tenantDb.RetentionAuditLogs.Add(new RetentionAuditLog
                    {
                        PerformedByUserId = CurrentSession.UserId,
                        ActionType = "TenantBrandingUpdated",
                        Detail = $"DisplayName='{branding.DisplayName}', LogoVersion={branding.LogoVersion}",
                        CreatedAt = DateTime.UtcNow
                    });
                    await tenantDb.SaveChangesAsync();
                }
                catch { }

                // Live propagation to central BrandingService
                BrandingService.ApplyDto(new TenantBrandingDto
                {
                    CompanyId = tenantId,
                    CompanyName = company.CompanyName,
                    DisplayName = branding.DisplayName,
                    HasCustomLogo = branding.LogoImage != null && branding.LogoImage.Length > 0,
                    LogoVersion = branding.LogoVersion,
                    AccentColor = null,
                    ContactEmail = branding.ContactEmail,
                    ContactPhone = branding.ContactPhone,
                    Address = branding.Address,
                    HidePoweredBy = branding.HidePoweredBy,
                    CanCustomizeAccent = false,
                    CanHidePoweredBy = FeatureGate.CanHidePoweredBy(tier),
                    UpdatedAt = branding.UpdatedAt
                }, branding.LogoImage);

                MessageBox.Show("Branding configuration updated successfully!\nAll window titles and sidebar chrome have been updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save branding: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "💾  Save Branding Changes";
            }
        }

        private async void BtnReset_Click(object? sender, EventArgs e)
        {
            var res = MessageBox.Show(
                "Reset all branding settings back to platform defaults?\nThis will revert your Display Name to the registered Company Name and remove custom logo and accent colors.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .FirstOrDefaultAsync(c => c.CompanyId == tenantId);

                if (company == null) return;

                var branding = company.Branding;
                if (branding != null)
                {
                    branding.DisplayName = company.CompanyName;
                    branding.LogoImage = null;
                    branding.LogoVersion++;
                    branding.AccentColor = null;
                    branding.HidePoweredBy = false;
                    branding.ContactEmail = null;
                    branding.ContactPhone = null;
                    branding.Address = null;
                    branding.UpdatedAt = DateTime.UtcNow;
                    branding.UpdatedByUserId = CurrentSession.UserId;
                }

                await masterDb.SaveChangesAsync();

                // Live propagation to central BrandingService
                BrandingService.InitializeForTenant(tenantId, company.CompanyName);

                await LoadBrandingAsync();

                MessageBox.Show("Branding reset to default platform values.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label MakeSectionHeader(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, y),
                AutoSize = true
            };
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
                Size = new Size(width, 32),
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
