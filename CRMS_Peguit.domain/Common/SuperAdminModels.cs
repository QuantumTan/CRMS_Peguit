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
}
