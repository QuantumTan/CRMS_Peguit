using System;

namespace CRMS_Peguit.domain.entities
{
    public class EmailTemplate
    {
        public int TemplateId { get; set; }
        public int TenantId { get; set; } = 1;

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Equity Retention"; // Equity Retention, Listing CMA, Client Milestone, New Listing, Showing VIP
        public string TargetAudience { get; set; } = "All"; // All, Buyers, Sellers, Investors
        public string EmailFormat { get; set; } = "Html"; // Html or PlainText

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? CallToActionText { get; set; }
        public string? CallToActionUrl { get; set; }

        // RBAC & Governance
        public bool IsSystem { get; set; } = false; // Factory / standard system templates cannot be deleted
        public bool IsActive { get; set; } = true;
        public string CreatedByRole { get; set; } = "System"; // System, Admin, Manager, Agent
        public int? CreatedByUserId { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
