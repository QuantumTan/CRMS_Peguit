using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.Common;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Services
{
    /// <summary>
    /// Single central source of truth for all application branding.
    /// Replaces hardcoded strings across window titles, sidebar headers, reports, and emails.
    /// Supports online synchronization, offline caching, and graceful fallbacks.
    /// </summary>
    public static class BrandingService
    {
        private static readonly object _lock = new();
        private static readonly LocalAuthCache _cache = new();

        private static int _activeTenantId = 0;
        private static string _displayName = string.Empty;
        private static byte[]? _logoBytes;
        private static Icon? _cachedIcon;
        private static int _logoVersion = 1;
        private static string? _accentColorHex;
        private static Color? _cachedAccentColor;
        private static string? _contactEmail;
        private static string? _contactPhone;
        private static string? _address;
        private static bool _hidePoweredBy = false;
        private static DateTime _updatedAt = DateTime.MinValue;

        public const string DefaultPlatformName = "NEXA CRM SYSTEM";
        public const string PlatformBrand = "NEXA";

        public static event Action? BrandingChanged;

        public static int ActiveTenantId => _activeTenantId;
        public static int LogoVersion => _logoVersion;

        /// <summary>
        /// Gets the active tenant's display name with reliable fallbacks:
        /// 1. Custom DisplayName
        /// 2. CurrentSession.TenantName
        /// 3. "NEXA CRM SYSTEM"
        /// Never returns empty or blank.
        /// </summary>
        public static string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(_displayName))
                return _displayName.Trim();

            if (!string.IsNullOrWhiteSpace(CurrentSession.TenantName) &&
                !CurrentSession.TenantName.StartsWith("Tenant #", StringComparison.OrdinalIgnoreCase) &&
                !CurrentSession.TenantName.Contains("Master Platform", StringComparison.OrdinalIgnoreCase))
            {
                return CurrentSession.TenantName.Trim();
            }

            return DefaultPlatformName;
        }

        /// <summary>
        /// Gets the brand logo image as a fresh, independent Bitmap instance:
        /// 1. Custom tenant logo if uploaded and decoded
        /// 2. Platform default logo (AppBrand.Logo)
        /// </summary>
        public static Image? GetLogo()
        {
            lock (_lock)
            {
                if (_logoBytes != null && _logoBytes.Length > 0)
                {
                    var bmp = AppBrand.CreateBitmapFromBytes(_logoBytes);
                    if (bmp != null)
                        return bmp;
                }

                return AppBrand.Logo;
            }
        }

        /// <summary>
        /// Gets the window icon derived from the tenant logo, or default platform icon.
        /// </summary>
        public static Icon? GetAppIcon()
        {
            lock (_lock)
            {
                if (_cachedIcon != null)
                {
                    try
                    {
                        _ = _cachedIcon.Handle;
                        return (Icon)_cachedIcon.Clone();
                    }
                    catch
                    {
                        _cachedIcon = null;
                    }
                }

                using var logo = GetLogo();
                if (logo is Bitmap bmp)
                {
                    try
                    {
                        _cachedIcon = CreateIconFromBitmap(bmp);
                        if (_cachedIcon != null)
                        {
                            return (Icon)_cachedIcon.Clone();
                        }
                    }
                    catch { }
                }

                return AppBrand.AppIcon;
            }
        }

        /// <summary>
        /// Gets the brand color. Always returns the standard Theme.Primary default.
        /// </summary>
        public static Color GetAccentColor() => Theme.Primary;

        public static string? GetAccentColorHex() => null;

        public static string? GetContactEmail() => _contactEmail;
        public static string? GetContactPhone() => _contactPhone;
        public static string? GetAddress() => _address;

        /// <summary>
        /// Determines whether the "Powered by NEXA" footer should be displayed.
        /// Shown by default for Starter and Professional; hidden only if Enterprise tenant enabled HidePoweredBy.
        /// </summary>
        public static bool ShouldShowPoweredBy()
        {
            if (CurrentSession.CanHidePoweredBy && _hidePoweredBy)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Formats standard window and taskbar caption: "{DisplayName} — {User} ({Role})"
        /// </summary>
        public static string GetWindowCaption(string? userFullName, string? roleDisplay)
        {
            string name = GetDisplayName();
            string user = string.IsNullOrWhiteSpace(userFullName) ? "User" : userFullName.Trim();
            string role = string.IsNullOrWhiteSpace(roleDisplay) ? "Staff" : roleDisplay.Trim();

            return $"{name} — {user} ({role})";
        }

        /// <summary>
        /// Formats executive report headers.
        /// </summary>
        public static string GetReportHeaderTitle()
        {
            return $"{GetDisplayName()} — Executive Report";
        }

        /// <summary>
        /// Formats standard outbound email signature.
        /// </summary>
        public static string GetEmailSignature()
        {
            return $"{GetDisplayName()} Real Estate Advisory Team";
        }

        /// <summary>
        /// Loads tenant branding for the given tenant ID.
        /// First checks local cache for immediate display, then attempts refresh.
        /// </summary>
        public static void InitializeForTenant(int tenantId, string? fallbackCompanyName = null)
        {
            if (tenantId <= 0)
            {
                Clear();
                return;
            }

            lock (_lock)
            {
                _activeTenantId = tenantId;

                // 1. Immediate offline load from SQLite cache
                var cached = _cache.GetTenantBranding(tenantId);
                if (cached != null)
                {
                    ApplyState(
                        cached.DisplayName,
                        cached.LogoBytes,
                        cached.LogoVersion,
                        cached.AccentColor,
                        cached.ContactEmail,
                        cached.ContactPhone,
                        cached.Address,
                        cached.HidePoweredBy,
                        cached.UpdatedAt);
                }
                else
                {
                    _displayName = fallbackCompanyName ?? string.Empty;
                    _logoBytes = null;
                    _cachedIcon = null;
                    _logoVersion = 1;
                    _accentColorHex = null;
                    _cachedAccentColor = null;
                    _contactEmail = null;
                    _contactPhone = null;
                    _address = null;
                    _hidePoweredBy = false;
                    _updatedAt = DateTime.UtcNow;
                }
            }

            // 2. Asynchronously refresh from MasterDb or API
            _ = Task.Run(async () =>
            {
                await RefreshFromDataSourceAsync(tenantId);
            });
        }

        public static async Task RefreshFromDataSourceAsync(int tenantId)
        {
            if (tenantId <= 0) return;

            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var company = await masterDb.Companies
                    .Include(c => c.Branding)
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == tenantId);

                if (company != null)
                {
                    var branding = company.Branding;
                    string name = branding?.DisplayName ?? company.CompanyName;
                    byte[]? logo = branding?.LogoImage;
                    int version = branding?.LogoVersion ?? 1;
                    string? accent = branding?.AccentColor;
                    string? email = branding?.ContactEmail;
                    string? phone = branding?.ContactPhone;
                    string? addr = branding?.Address;
                    bool hideFooter = branding?.HidePoweredBy ?? false;
                    DateTime updated = branding?.UpdatedAt ?? company.CreatedAt;

                    // Update SQLite Cache
                    _cache.SaveTenantBranding(tenantId, name, logo, version, accent, email, phone, addr, hideFooter, updated);

                    // Update memory if this tenant is still active
                    lock (_lock)
                    {
                        if (_activeTenantId == tenantId)
                        {
                            ApplyState(name, logo, version, accent, email, phone, addr, hideFooter, updated);
                        }
                    }

                    NotifyChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrandingService.RefreshFromDataSourceAsync] Error: {ex.Message}");
            }
        }

        public static void ApplyDto(TenantBrandingDto dto, byte[]? logoBytes = null)
        {
            lock (_lock)
            {
                _activeTenantId = dto.CompanyId;
                byte[]? effectiveLogo = logoBytes ?? _logoBytes;

                ApplyState(
                    dto.DisplayName,
                    effectiveLogo,
                    dto.LogoVersion,
                    dto.AccentColor,
                    dto.ContactEmail,
                    dto.ContactPhone,
                    dto.Address,
                    dto.HidePoweredBy,
                    dto.UpdatedAt);

                _cache.SaveTenantBranding(
                    dto.CompanyId,
                    dto.DisplayName,
                    effectiveLogo,
                    dto.LogoVersion,
                    dto.AccentColor,
                    dto.ContactEmail,
                    dto.ContactPhone,
                    dto.Address,
                    dto.HidePoweredBy,
                    dto.UpdatedAt);
            }

            NotifyChanged();
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _activeTenantId = 0;
                _displayName = string.Empty;
                _logoBytes = null;
                try { _cachedIcon?.Dispose(); } catch { }
                _cachedIcon = null;
                _logoVersion = 1;
                _accentColorHex = null;
                _cachedAccentColor = null;
                _contactEmail = null;
                _contactPhone = null;
                _address = null;
                _hidePoweredBy = false;
                _updatedAt = DateTime.MinValue;
            }

            Theme.ApplyTenantAccent(null);
            NotifyChanged();
        }

        private static void ApplyState(
            string displayName,
            byte[]? logoBytes,
            int logoVersion,
            string? accentColor,
            string? contactEmail,
            string? contactPhone,
            string? address,
            bool hidePoweredBy,
            DateTime updatedAt)
        {
            _displayName = displayName;
            _logoBytes = logoBytes;

            try { _cachedIcon?.Dispose(); } catch { }
            _cachedIcon = null;

            if (_logoBytes != null && _logoBytes.Length > 0)
            {
                try
                {
                    using var bmp = AppBrand.CreateBitmapFromBytes(_logoBytes);
                    if (bmp != null)
                    {
                        _cachedIcon = CreateIconFromBitmap(bmp);
                    }
                }
                catch
                {
                    _cachedIcon = null;
                }
            }

            _logoVersion = logoVersion;
            _accentColorHex = accentColor;
            _cachedAccentColor = null;

            if (!string.IsNullOrWhiteSpace(accentColor) &&
                BrandingValidationRules.TryParseHexColor(accentColor, out int r, out int g, out int b))
            {
                _cachedAccentColor = Color.FromArgb(r, g, b);
            }

            _contactEmail = contactEmail;
            _contactPhone = contactPhone;
            _address = address;
            _hidePoweredBy = hidePoweredBy;
            _updatedAt = updatedAt;
        }

        private static void NotifyChanged()
        {
            try
            {
                Theme.ApplyTenantAccent(null);
                BrandingChanged?.Invoke();
            }
            catch { }
        }

        private static Icon? CreateIconFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                using var iconBmp = new Bitmap(48, 48);
                using (var g = Graphics.FromImage(iconBmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    float scale = Math.Min(48f / bitmap.Width, 48f / bitmap.Height);
                    int nw = Math.Max(1, (int)(bitmap.Width * scale));
                    int nh = Math.Max(1, (int)(bitmap.Height * scale));
                    int nx = (48 - nw) / 2;
                    int ny = (48 - nh) / 2;
                    g.DrawImage(bitmap, nx, ny, nw, nh);
                }

                // Pack into a valid, standalone Windows .ICO stream with PNG payload
                using var pngStream = new MemoryStream();
                iconBmp.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] pngBytes = pngStream.ToArray();

                using var icoStream = new MemoryStream();
                using var writer = new BinaryWriter(icoStream);

                // ICONDIR header (6 bytes)
                writer.Write((short)0); // Reserved. Must always be 0.
                writer.Write((short)1); // Specifies image type: 1 for icon (.ICO) image.
                writer.Write((short)1); // Specifies number of images in the file.

                // ICONDIRENTRY (16 bytes)
                writer.Write((byte)48); // Specifies image width in pixels.
                writer.Write((byte)48); // Specifies image height in pixels.
                writer.Write((byte)0);  // Specifies number of colors in the color palette (0 if no palette).
                writer.Write((byte)0);  // Reserved. Must be 0.
                writer.Write((short)1); // Specifies color planes. Should be 0 or 1.
                writer.Write((short)32);// Specifies bits per pixel.
                writer.Write((int)pngBytes.Length); // Specifies the size of the image's data in bytes.
                writer.Write((int)22);  // Specifies the offset of BMP/PNG data from the beginning of the ICO/CUR file (6 + 16 = 22).

                // Image Data (PNG bytes)
                writer.Write(pngBytes);
                writer.Flush();

                icoStream.Position = 0;
                return new Icon(icoStream);
            }
            catch
            {
                try
                {
                    IntPtr hIcon = bitmap.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
