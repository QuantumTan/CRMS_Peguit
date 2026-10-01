using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;

namespace CRMS_Peguit.domain.Common
{
    public class TenantBrandingDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool HasCustomLogo { get; set; }
        public int LogoVersion { get; set; } = 1;
        public string? AccentColor { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool HidePoweredBy { get; set; }
        public bool CanCustomizeAccent { get; set; }
        public bool CanHidePoweredBy { get; set; }
        public bool IsDuplicateName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateBrandingRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? AccentColor { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool HidePoweredBy { get; set; }
    }

    public class UploadLogoRequest
    {
        public byte[]? LogoBytes { get; set; }
        public string? ContentType { get; set; }
    }

    public class SuperAdminUpdateBrandingRequest : UpdateBrandingRequest
    {
        public bool OverrideReservedName { get; set; }
    }

    public class BrandingValidationRules
    {
        public static readonly string[] ReservedNames =
        {
            "NEXA",
            "NEXA CRM",
            "NEXA CRM SYSTEM",
            "NEXA Support",
            "Admin",
            "Super Admin",
            "SuperAdmin",
            "Support",
            "Platform Admin",
            "Platform Administration"
        };

        // Letters, numbers, spaces, and common business punctuation: & . , ' -
        public static readonly Regex AllowedDisplayNameRegex = new(
            @"^[\p{L}0-9\s&.',\-]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static readonly Regex HexColorRegex = new(
            @"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$",
            RegexOptions.Compiled);

        // Disallowed semantic colors (green, amber/orange, red, neutral gray) to protect status indicators
        public static readonly (int R, int G, int B)[] SemanticStatusColors =
        {
            (22, 163, 74),   // Green #16A34A (Active / Success)
            (217, 119, 6),   // Amber #D97706 (Warning / Expiring)
            (220, 38, 38),   // Red #DC2626 (Danger / Error / Expired)
            (100, 116, 139)  // Slate/Gray #64748B (Inactive / Neutral)
        };

        public static bool ContainsHtmlOrScript(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains('<', StringComparison.Ordinal) ||
                   text.Contains('>', StringComparison.Ordinal) ||
                   text.IndexOf("script", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsReservedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string trimmed = name.Trim();
            if (ReservedNames.Any(r => string.Equals(r, trimmed, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (trimmed.Equals("NEXA", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("NEXA ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static bool HasSemanticCollision(string hex)
        {
            if (!TryParseHexColor(hex, out var r, out var g, out var b)) return false;

            foreach (var sc in SemanticStatusColors)
            {
                // Euclidean distance in RGB color space
                double dist = Math.Sqrt(Math.Pow(r - sc.R, 2) + Math.Pow(g - sc.G, 2) + Math.Pow(b - sc.B, 2));
                if (dist < 40.0) // Too close to semantic color
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PassesContrastCheck(string hex, out string? reason)
        {
            reason = null;
            if (!TryParseHexColor(hex, out var r, out var g, out var b))
            {
                reason = "Invalid hex color format. Expected #RRGGBB or #RGB.";
                return false;
            }

            // Calculate relative luminance according to WCAG 2.1
            double lum = CalculateRelativeLuminance(r, g, b);

            // Contrast against pure white background (#FFFFFF, L=1.0)
            double contrastOnWhite = (1.0 + 0.05) / (lum + 0.05);

            // Contrast against dark chrome background (#0F172A, L ~ 0.01)
            double darkChromeLum = CalculateRelativeLuminance(15, 23, 42);
            double contrastOnDark = (lum + 0.05) / (darkChromeLum + 0.05);

            // The accent color must not be blindingly light (invisible on white: contrast < 2.0)
            if (contrastOnWhite < 1.8)
            {
                reason = "Accent color is too light and does not meet readable UI contrast requirements.";
                return false;
            }

            // The accent color must not be indistinguishable from pitch black (invisible against dark sidebar: contrast < 1.3)
            if (contrastOnDark < 1.25)
            {
                reason = "Accent color is too dark to be distinguished against the dark sidebar chrome.";
                return false;
            }

            return true;
        }

        public static bool IsValidHexColor(string? hex) => TryParseHexColor(hex, out _, out _, out _);

        public static bool TryParseHexColor(string? hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex) || !HexColorRegex.IsMatch(hex.Trim())) return false;

            string raw = hex.Trim().TrimStart('#');
            try
            {
                if (raw.Length == 3)
                {
                    r = int.Parse(new string(raw[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    g = int.Parse(new string(raw[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    b = int.Parse(new string(raw[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return true;
                }
                else if (raw.Length == 6)
                {
                    r = int.Parse(raw.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    g = int.Parse(raw.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    b = int.Parse(raw.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private static double CalculateRelativeLuminance(int r, int g, int b)
        {
            double sr = r / 255.0;
            double sg = g / 255.0;
            double sb = b / 255.0;

            double rLin = sr <= 0.03928 ? sr / 12.92 : Math.Pow((sr + 0.055) / 1.055, 2.4);
            double gLin = sg <= 0.03928 ? sg / 12.92 : Math.Pow((sg + 0.055) / 1.055, 2.4);
            double bLin = sb <= 0.03928 ? sb / 12.92 : Math.Pow((sb + 0.055) / 1.055, 2.4);

            return 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;
        }
    }

    public class UpdateBrandingRequestValidator : AbstractValidator<UpdateBrandingRequest>
    {
        private readonly bool _allowReservedOverride;

        public UpdateBrandingRequestValidator(bool allowReservedOverride = false)
        {
            _allowReservedOverride = allowReservedOverride;

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display Name is required.")
                .Length(2, 100).WithMessage("Display Name must be between 2 and 100 characters.")
                .Must(name => !BrandingValidationRules.ContainsHtmlOrScript(name))
                .WithMessage("Display Name cannot contain HTML or script markup.")
                .Must(name => string.IsNullOrWhiteSpace(name) || BrandingValidationRules.AllowedDisplayNameRegex.IsMatch(name.Trim()))
                .WithMessage("Display Name can only contain letters, numbers, spaces, and punctuation (& . , ' -).")
                .Must(name => _allowReservedOverride || !BrandingValidationRules.IsReservedName(name))
                .WithMessage("This name is reserved by the platform and cannot be used as a tenant display name.");

            RuleFor(x => x.AccentColor)
                .Must(color => string.IsNullOrWhiteSpace(color) || BrandingValidationRules.HexColorRegex.IsMatch(color.Trim()))
                .WithMessage("Accent color must be a valid hex color code (e.g. #0284C7).")
                .Must(color => string.IsNullOrWhiteSpace(color) || !BrandingValidationRules.HasSemanticCollision(color.Trim()))
                .WithMessage("Accent color cannot collide with semantic status indicators (green, amber, red, gray).")
                .Must(color =>
                {
                    if (string.IsNullOrWhiteSpace(color)) return true;
                    return BrandingValidationRules.PassesContrastCheck(color.Trim(), out _);
                })
                .WithMessage(x =>
                {
                    BrandingValidationRules.PassesContrastCheck(x.AccentColor?.Trim() ?? "", out var reason);
                    return reason ?? "Accent color failed UI contrast checks.";
                });

            RuleFor(x => x.ContactEmail)
                .MaximumLength(255).WithMessage("Contact Email cannot exceed 255 characters.")
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail))
                .WithMessage("Please enter a valid email address.");

            RuleFor(x => x.ContactPhone)
                .MaximumLength(50).WithMessage("Contact Phone cannot exceed 50 characters.")
                .Must(p => string.IsNullOrWhiteSpace(p) || !BrandingValidationRules.ContainsHtmlOrScript(p))
                .WithMessage("Contact Phone cannot contain HTML or script markup.");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.")
                .Must(a => string.IsNullOrWhiteSpace(a) || !BrandingValidationRules.ContainsHtmlOrScript(a))
                .WithMessage("Address cannot contain HTML or script markup.");
        }
    }
}
