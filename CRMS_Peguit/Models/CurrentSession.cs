using CRMS_Peguit.domain.entities;
using CRMS_Peguit.winforms.Models.Roles;
using System;

namespace CRMS_Peguit.winforms.Auth
{
    public static class CurrentSession
    {
        // ==============================================
        // CURRENT USER INFORMATION
        // ==============================================

        public static int UserId { get; private set; }
        public static int TenantId { get; private set; }
        public static string? TenantName { get; private set; }
        public static TenantTier TenantTier { get; private set; } = TenantTier.TenantA;
        public static int? ActiveBranchId { get; private set; }
        public static string? ActiveBranchName { get; private set; }
        public static string? JwtToken { get; private set; }
        public static CRMS_Peguit.winforms.Models.Roles.User? CurrentUser { get; private set; }
        public static bool IsOffline { get; private set; }

        // ==============================================
        // TIER CAPABILITY CHECKS (EXAM SPECIFICATION)
        // ==============================================

        public static bool CanAccessMainTransaction => true; // All tiers (Tenant A, B, C)
        public static bool CanAccessDataCollection => true; // All tiers (Tenant A, B, C)
        public static bool CanAccessBusinessIntelligence => TenantTier >= TenantTier.TenantB || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessActions => TenantTier >= TenantTier.TenantB || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessBranching => TenantTier >= TenantTier.TenantC || CurrentUser?.Role == UserRole.SuperAdmin;

        // ==============================================
        // START SESSION
        // ==============================================

        public static void Start(
            int userId,
            int tenantId,
            string fullName,
            string email,
            string roleName,
            string? jwtToken,
            bool isOffline,
            TenantTier tier = TenantTier.TenantA,
            string? tenantName = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("A valid UserId is required.", nameof(userId));
            }

            if (tenantId <= 0)
            {
                tenantId = 1;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = email;
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role is required.", nameof(roleName));
            }

            UserId = userId;
            TenantId = tenantId;
            JwtToken = jwtToken;
            IsOffline = isOffline;
            TenantTier = tier;
            TenantName = tenantName ?? $"Tenant #{tenantId}";

            var role = roleName.Trim().ToLowerInvariant();

            CurrentUser = role switch
            {
                "superadmin" or "super admin" or "super_admin" =>
                    new CRMS_Peguit.winforms.Models.Roles.SuperAdmin(fullName, email),

                "admin" =>
                    new Admin(fullName, email),

                "manager" =>
                    new Manager(fullName, email),

                "agent" =>
                    new SalesStaff(fullName, email),

                _ =>
                    throw new InvalidOperationException($"Unknown role '{roleName}' - cannot build a session user.")
            };

            if (CurrentUser is CRMS_Peguit.winforms.Models.Roles.SuperAdmin)
            {
                TenantTier = TenantTier.Master;
            }
        }

        public static void SetTenantTier(TenantTier tier, string? tenantName = null)
        {
            TenantTier = tier;
            if (!string.IsNullOrWhiteSpace(tenantName))
                TenantName = tenantName;
        }

        public static void SetActiveBranch(int? branchId, string? branchName)
        {
            ActiveBranchId = branchId;
            ActiveBranchName = branchName;
        }

        // ==============================================
        // CHECK MODULE ACCESS
        // ==============================================

        public static bool CanAccess(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return false;
            }

            // Gated by Tenant Tier requirements:
            if (moduleName.Equals("Analytics", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Reports", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("BusinessIntelligence", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessBusinessIntelligence) return false;
            }

            if (moduleName.Equals("Campaigns", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("TasksReminders", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("FollowUps", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Approvals", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Activities", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Actions", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessActions) return false;
            }

            if (moduleName.Equals("Branching", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessBranching) return false;
            }

            if (moduleName.Equals("AdminPanel", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Subscription", StringComparison.OrdinalIgnoreCase))
            {
                if (CurrentUser?.Role != UserRole.SuperAdmin) return false;
            }

            return CurrentUser?.GetAccessibleModules().Contains(moduleName) ?? false;
        }

        public static void SetOffline(bool isOffline)
        {
            IsOffline = isOffline;
        }

        // ==============================================
        // SIGN OUT
        // ==============================================

        public static void SignOut()
        {
            UserId = 0;
            TenantId = 0;
            TenantName = null;
            TenantTier = TenantTier.TenantA;
            ActiveBranchId = null;
            ActiveBranchName = null;
            JwtToken = null;
            CurrentUser = null;
            IsOffline = false;
        }
    }
}
