using System;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.domain.Common
{
    public class PlatformSnapshotDto
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiringThisMonth { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public decimal TotalMrr { get; set; }
        public int TenantACount { get; set; }
        public int TenantBCount { get; set; }
        public int TenantCCount { get; set; }
        public string LastBackupStatus { get; set; } = "None";
        public DateTime? LastBackupDate { get; set; }
    }

    public class AdminDto
    {
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SystemSettingDto
    {
        public int SettingId { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedByName { get; set; } = string.Empty;
    }

    public class BackupLogDto
    {
        public int BackupId { get; set; }
        public DateTime BackupDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FileLocation { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
    }

    public class CompanyDetailDto
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = string.Empty;
        public decimal BillingAmount { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }

    public class CompanyLookupDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
    }

    public class PlatformBiSummaryDto
    {
        public int TotalTenants { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiringSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public decimal TotalMrr { get; set; }
        public int TenantACount { get; set; }
        public int TenantBCount { get; set; }
        public int TenantCCount { get; set; }
    }

    public class TenantSubscriptionDto
    {
        public int SubscriptionId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public TenantTier Tier { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BillingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidThroughDate => EndDate;
    }

    public class CreateAdminRequest
    {
        public int TenantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RoleName { get; set; } = "Admin";
    }

    public class UpdateSettingRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateSubscriptionRequest
    {
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BillingAmount { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ChangeTierRequest
    {
        public string PlanName { get; set; } = string.Empty;
    }

    /// <summary>Audit log entry for display — platform actions only, never tenant CRM data.</summary>
    public class PlatformAuditLogDto
    {
        public int AuditLogId { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string? TargetCompanyName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Tenant grid row — Company metadata + subscription + admin info, never CRM data.</summary>
    public class TenantGridDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string PrimaryAdminName { get; set; } = string.Empty;
        public string TierLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
    }

    /// <summary>Sync health row — counts and timestamps ONLY, never sync queue content.</summary>
    public class SyncHealthDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime? LastSuccessfulSync { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public string SyncStatus { get; set; } = "Healthy";
    }

    /// <summary>Request to create a new tenant company.</summary>
    public class CreateTenantRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string AdminFirstName { get; set; } = string.Empty;
        public string AdminLastName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string TierLevel { get; set; } = "Tenant A";
    }

    public class PaymentRecordDto
    {
        public int PaymentRecordId { get; set; }
        public int SubscriptionId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodDisplay => PaymentMethod switch
        {
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.GCash => "GCash",
            PaymentMethod.Check => "Check",
            PaymentMethod.Cash => "Cash",
            _ => "Other"
        };
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public int RecordedByUserId { get; set; }
        public string RecordedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecordPaymentRequest
    {
        public int SubscriptionId { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
