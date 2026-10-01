using System;

namespace CRMS_Peguit.domain.entities
{
    /// <summary>
    /// Tenant-level branding metadata (1:1 with Company).
    /// Stores identity and presentation configuration.
    /// Strictly non-operational data — accessible by Tenant Admin and platform Super Admin oversight.
    /// </summary>
    public class TenantBranding
    {
        public int CompanyId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public byte[]? LogoImage { get; set; }

        public int LogoVersion { get; set; } = 1;

        public string? AccentColor { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }

        public string? Address { get; set; }

        public bool HidePoweredBy { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedByUserId { get; set; }

        // Navigation
        public virtual Company Company { get; set; } = null!;
    }
}
