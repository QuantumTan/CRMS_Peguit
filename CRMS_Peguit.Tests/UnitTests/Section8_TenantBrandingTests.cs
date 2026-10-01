using System;
using System.Drawing;
using System.IO;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;
using FluentValidation;
using Xunit;

namespace CRMS_Peguit.Tests.UnitTests
{
    [Collection("FeatureGateAndBrandingTests")]
    public class Section8_TenantBrandingTests
    {
        #region 8.1 Branding Validation Rules & Security

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("Acme <img src=x onerror=alert(1)>")]
        [InlineData("<b>Bold</b> Properties")]
        [InlineData("<iframe src='evil.com'></iframe>")]
        [InlineData("Acme & <a href='javascript:void(0)'>Click</a>")]
        public void BrandingValidation_RejectsHtmlAndScriptTags(string dangerousName)
        {
            var req = new UpdateBrandingRequest
            {
                DisplayName = dangerousName
            };

            var validator = new UpdateBrandingRequestValidator();
            var result = validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.DisplayName));
        }

        [Theory]
        [InlineData("NEXA")]
        [InlineData("nexa")]
        [InlineData("Nexa CRM")]
        [InlineData("NEXA System")]
        [InlineData("Super Admin")]
        [InlineData("Platform Admin")]
        public void BrandingValidation_RejectsReservedPlatformNames(string reservedName)
        {
            var req = new UpdateBrandingRequest
            {
                DisplayName = reservedName
            };

            var validator = new UpdateBrandingRequestValidator();
            var result = validator.Validate(req);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(req.DisplayName));
        }

        [Theory]
        [InlineData("Acme Realty Corp.")]
        [InlineData("Beacon Real Estate Services")]
        [InlineData("Summit Peak Properties & Loans")]
        [InlineData("Green Valley Homes")]
        public void BrandingValidation_AcceptsLegitimateBusinessNames(string validName)
        {
            var req = new UpdateBrandingRequest
            {
                DisplayName = validName
            };

            var validator = new UpdateBrandingRequestValidator();
            var result = validator.Validate(req);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("#0284C7", true)]  // Valid 6-hex
        [InlineData("#38BDF8", true)]  // Valid 6-hex
        [InlineData("#8B5CF6", true)]  // Valid purple
        [InlineData("#2563EB", true)]  // Valid blue
        [InlineData("#123", true)]     // Valid 3-hex
        [InlineData("0284C7", false)]  // Missing hash
        [InlineData("#ZZZZZZ", false)] // Invalid chars
        [InlineData("#12345", false)]  // Invalid length
        [InlineData("#1234567", false)]// Invalid length
        public void BrandingValidation_HexColorFormat(string hex, bool expectedValid)
        {
            bool valid = BrandingValidationRules.IsValidHexColor(hex);
            Assert.Equal(expectedValid, valid);
        }

        [Fact]
        public void BrandingValidation_RejectsSemanticColorCollisions()
        {
            // Green #16A34A (Success)
            Assert.True(BrandingValidationRules.HasSemanticCollision("#16A34A"));

            // Amber #D97706 (Warning / In Progress)
            Assert.True(BrandingValidationRules.HasSemanticCollision("#D97706"));

            // Red #DC2626 (Danger / Alert)
            Assert.True(BrandingValidationRules.HasSemanticCollision("#DC2626"));

            // Allowed distinct brand colors (Sky Blue, Indigo, Purple, Teal)
            Assert.False(BrandingValidationRules.HasSemanticCollision("#4F46E5")); // Indigo
            Assert.False(BrandingValidationRules.HasSemanticCollision("#7C3AED")); // Purple
            Assert.False(BrandingValidationRules.HasSemanticCollision("#0D9488")); // Teal
        }

        [Fact]
        public void BrandingValidation_RejectsInsufficientContrastColors()
        {
            // Pure white on white background has 1:1 contrast (< 1.8 required against white)
            Assert.False(BrandingValidationRules.PassesContrastCheck("#FFFFFF", out string? errWhite));
            Assert.NotNull(errWhite);

            // Extremely pale grey (#F9F9F9)
            Assert.False(BrandingValidationRules.PassesContrastCheck("#F9F9F9", out string? errPale));
            Assert.NotNull(errPale);

            // Very dark black on dark sidebar background has < 1.25 contrast against #0F172A
            Assert.False(BrandingValidationRules.PassesContrastCheck("#0F172A", out string? errDark));
            Assert.NotNull(errDark);

            // Good readable contrast
            Assert.True(BrandingValidationRules.PassesContrastCheck("#38BDF8", out _));
        }

        #endregion

        #region 8.2 Logo Sanitization & Processing

        [Fact]
        public void LogoProcessor_RejectsNonPngJpgMagicBytes()
        {
            // Exe header MZ
            byte[] exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
            var (success1, _, err1) = LogoProcessor.ProcessAndNormalize(exeBytes);
            Assert.False(success1);
            Assert.Contains("PNG and JPG", err1);

            // Plain text
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes("Not an image file at all");
            var (success2, _, err2) = LogoProcessor.ProcessAndNormalize(textBytes);
            Assert.False(success2);
            Assert.Contains("PNG and JPG", err2);

            // Empty or null
            var (success3, _, _) = LogoProcessor.ProcessAndNormalize(Array.Empty<byte>());
            Assert.False(success3);
        }

        [Fact]
        public void LogoProcessor_RejectsOversizedFiles()
        {
            // 2MB + 1 byte
            byte[] oversized = new byte[2 * 1024 * 1024 + 1];
            // Fake PNG header
            oversized[0] = 0x89; oversized[1] = 0x50; oversized[2] = 0x4E; oversized[3] = 0x47;

            var (success, _, err) = LogoProcessor.ProcessAndNormalize(oversized);
            Assert.False(success);
            Assert.Contains("2 MB", err);
        }

        [Fact]
        public void LogoProcessor_NormalizesValidImageToPng()
        {
            // Create a 512x512 bitmap to test downscaling to max 256x256
            using var bmp = new Bitmap(512, 512);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.CornflowerBlue);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] rawPng = ms.ToArray();

            var (success, normalized, error) = LogoProcessor.ProcessAndNormalize(rawPng);
            Assert.True(success, error);
            Assert.NotNull(normalized);
            Assert.True(normalized!.Length > 0);

            // Verify magic bytes of normalized output are PNG
            Assert.Equal(0x89, normalized[0]);
            Assert.Equal(0x50, normalized[1]);
            Assert.Equal(0x4E, normalized[2]);
            Assert.Equal(0x47, normalized[3]);

            // Verify it was normalized down to 256x256 image
            using var loadMs = new MemoryStream(normalized);
            using var resultImg = Image.FromStream(loadMs);
            Assert.Equal(256, resultImg.Width);
            Assert.Equal(256, resultImg.Height);
        }

        #endregion

        #region 8.3 Tier Entitlement Matrix

        [Fact]
        public void TierGating_TenantA_AllowsLogo_BlocksAccentAndHidePoweredBy()
        {
            CurrentSession.Start(1, 1, "Tenant A Admin", "admin@a.com", "Admin", null, false, tier: TenantTier.TenantA);

            Assert.True(CurrentSession.CanUseCustomLogo);
            Assert.False(CurrentSession.CanUseAccentColor);
            Assert.False(CurrentSession.CanHidePoweredBy);

            Assert.True(FeatureGate.CanUseCustomLogo(TenantTier.TenantA));
            Assert.False(FeatureGate.CanUseAccentColor(TenantTier.TenantA));
            Assert.False(FeatureGate.CanHidePoweredBy(TenantTier.TenantA));
        }

        [Fact]
        public void TierGating_TenantB_AllowsLogo_BlocksAccentAndHidePoweredBy()
        {
            CurrentSession.Start(2, 2, "Tenant B Admin", "admin@b.com", "Admin", null, false, tier: TenantTier.TenantB);

            Assert.True(CurrentSession.CanUseCustomLogo);
            Assert.False(CurrentSession.CanUseAccentColor);
            Assert.False(CurrentSession.CanHidePoweredBy);

            Assert.True(FeatureGate.CanUseCustomLogo(TenantTier.TenantB));
            Assert.False(FeatureGate.CanUseAccentColor(TenantTier.TenantB));
            Assert.False(FeatureGate.CanHidePoweredBy(TenantTier.TenantB));
        }

        [Fact]
        public void TierGating_TenantC_AllowsLogo_Accent_AndHidePoweredBy()
        {
            CurrentSession.Start(3, 3, "Tenant C Admin", "admin@c.com", "Admin", null, false, tier: TenantTier.TenantC);

            Assert.True(CurrentSession.CanUseCustomLogo);
            Assert.True(CurrentSession.CanUseAccentColor);
            Assert.True(CurrentSession.CanHidePoweredBy);

            Assert.True(FeatureGate.CanUseCustomLogo(TenantTier.TenantC));
            Assert.True(FeatureGate.CanUseAccentColor(TenantTier.TenantC));
            Assert.True(FeatureGate.CanHidePoweredBy(TenantTier.TenantC));
        }

        #endregion

        #region 8.4 BrandingService Fallback & Formatting

        [Fact]
        public void BrandingService_FallbackCascade_WhenEmpty()
        {
            BrandingService.Clear();

            // When no branding loaded and no fallback, returns default
            Assert.Equal("NEXA CRM SYSTEM", BrandingService.GetDisplayName());

            // Window caption format
            string caption = BrandingService.GetWindowCaption("Jane Doe", "Agent");
            Assert.Equal("NEXA CRM SYSTEM — Jane Doe (Agent)", caption);

            // Report header format
            Assert.Equal("NEXA CRM SYSTEM — Executive Report", BrandingService.GetReportHeaderTitle());

            // Default powered-by is visible
            Assert.True(BrandingService.ShouldShowPoweredBy());
        }

        [Fact]
        public void BrandingService_AppliesTenantBrandingCorrectly()
        {
            FeatureGate.ClearOverrides();
            CurrentSession.Start(3, 3, "Enterprise Admin", "admin@enterprise.com", "Admin", null, false, tier: TenantTier.TenantC);

            var dto = new TenantBrandingDto
            {
                CompanyId = 3,
                CompanyName = "Prime Realty Inc.",
                DisplayName = "Prime Luxury Homes",
                AccentColor = "#7C3AED",
                HidePoweredBy = true,
                ContactEmail = "contact@primeluxury.com"
            };

            BrandingService.ApplyDto(dto);

            Assert.Equal("Prime Luxury Homes", BrandingService.GetDisplayName());
            Assert.Equal(Theme.Primary, BrandingService.GetAccentColor());
            Assert.Null(BrandingService.GetAccentColorHex());
            Assert.Equal("contact@primeluxury.com", BrandingService.GetContactEmail());

            // Enterprise can hide powered by
            Assert.False(BrandingService.ShouldShowPoweredBy());

            // Window caption reflects custom brand
            string caption = BrandingService.GetWindowCaption("Alex Cruz", "Broker");
            Assert.Equal("Prime Luxury Homes — Alex Cruz (Broker)", caption);

            BrandingService.Clear();
        }

        [Fact]
        public void BrandingService_NonEnterpriseCannotHidePoweredBy()
        {
            CurrentSession.Start(1, 1, "Starter Admin", "admin@starter.com", "Admin", null, false, tier: TenantTier.TenantA);

            var dto = new TenantBrandingDto
            {
                CompanyId = 1,
                CompanyName = "Starter Homes Ltd.",
                DisplayName = "Starter Realty",
                AccentColor = "#7C3AED", // Should be ignored
                HidePoweredBy = true     // Should be ignored for Tier A
            };

            BrandingService.ApplyDto(dto);

            Assert.Equal("Starter Realty", BrandingService.GetDisplayName());
            // Tier A cannot hide powered by, even if DB has true
            Assert.True(BrandingService.ShouldShowPoweredBy());
            // Tier A cannot use custom accent color
            Assert.Null(BrandingService.GetAccentColorHex());

            BrandingService.Clear();
        }

        [Fact]
        public void AppBrand_And_BrandingService_IconLifecycle_NeverDisposed()
        {
            // 1. AppBrand icon exists and has valid non-zero handle
            var appIcon = AppBrand.AppIcon;
            if (appIcon != null)
            {
                Assert.NotEqual(IntPtr.Zero, appIcon.Handle);

                // 2. Cloning produces valid handle
                using var clone = (Icon)appIcon.Clone();
                Assert.NotEqual(IntPtr.Zero, clone.Handle);

                // 3. Disposing the clone does not invalidate AppBrand.AppIcon
                clone.Dispose();
                var freshAppIcon = AppBrand.AppIcon;
                Assert.NotNull(freshAppIcon);
                Assert.NotEqual(IntPtr.Zero, freshAppIcon.Handle);
            }

            // 4. BrandingService.GetAppIcon returns valid icon
            var brandingIcon = BrandingService.GetAppIcon();
            if (brandingIcon != null)
            {
                Assert.NotEqual(IntPtr.Zero, brandingIcon.Handle);

                // 5. Assigning to a WinForms Form does not throw ObjectDisposedException
                using var form = new System.Windows.Forms.Form();
                form.Icon = brandingIcon;
                Assert.NotNull(form.Icon);
                Assert.NotEqual(IntPtr.Zero, form.Icon.Handle);

                // 6. Clearing BrandingService and disposing form does not corrupt subsequent calls
                BrandingService.Clear();
                form.Dispose();

                var nextIcon = BrandingService.GetAppIcon();
                if (nextIcon != null)
                {
                    Assert.NotEqual(IntPtr.Zero, nextIcon.Handle);
                }
            }
        }

        [Fact]
        public void Branding_Logo_AssignedToPictureBox_DoesNotThrow_OnVisibleChanged_Or_FrameDimensionsList()
        {
            // 1. AppBrand.Logo returns valid Image
            var logo = AppBrand.Logo;
            if (logo != null)
            {
                // Must be able to query FrameDimensionsList without GDI+ throwing ArgumentException: Parameter is not valid
                var dimensions = logo.FrameDimensionsList;
                Assert.NotNull(dimensions);
                Assert.NotEmpty(dimensions);

                // 2. PictureBox animation & visibility change does not throw
                using var picBox = new System.Windows.Forms.PictureBox();
                picBox.Image = logo;
                using var form = new System.Windows.Forms.Form();
                form.Controls.Add(picBox);

                // Changing visibility triggers PictureBox.Animate(true) -> ImageAnimator.CanAnimate
                picBox.Visible = true;
                Assert.NotNull(picBox.Image);

                // 3. Disposing the picturebox image does not corrupt subsequent calls to AppBrand.Logo
                picBox.Image.Dispose();
                var freshLogo = AppBrand.Logo;
                Assert.NotNull(freshLogo);
                var freshDims = freshLogo.FrameDimensionsList;
                Assert.NotNull(freshDims);
                freshLogo.Dispose();
            }

            // 4. Custom bytes with CreateBitmapFromBytes
            using var sampleBmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(sampleBmp))
            {
                g.Clear(Color.Red);
            }
            using var ms = new MemoryStream();
            sampleBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] customBytes = ms.ToArray();

            var (valid, normalized, _) = LogoProcessor.ProcessAndNormalize(customBytes);
            Assert.True(valid);
            Assert.NotNull(normalized);

            using var customLogo = AppBrand.CreateBitmapFromBytes(normalized);
            Assert.NotNull(customLogo);
            var customDims = customLogo.FrameDimensionsList;
            Assert.NotNull(customDims);
            Assert.NotEmpty(customDims);

            using var picCustom = new System.Windows.Forms.PictureBox();
            picCustom.Image = customLogo;
            picCustom.Visible = true;
            Assert.NotNull(picCustom.Image);
        }

        #endregion
    }
}
