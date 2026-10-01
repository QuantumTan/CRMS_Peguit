using CRMS_Peguit.domain.entities;
using CRMS_Peguit.domain.Common;
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
        public static int? AssignedBranchId { get; private set; }
        public static string? AssignedBranchName { get; private set; }
        public static string? JwtToken { get; private set; }
        public static CRMS_Peguit.winforms.Models.Roles.User? CurrentUser { get; private set; }
        public static bool IsOffline { get; private set; }

        // ==============================================
        // TIER CAPABILITY CHECKS (EXAM SPECIFICATION)
        // ==============================================

        public static bool CanAccessMainTransaction => FeatureGate.CanAccessDealsPipeline(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessDataCollection => FeatureGate.CanAccessDataCollection(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessBusinessIntelligence => FeatureGate.CanAccessBusinessIntelligence(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessActions => FeatureGate.CanAccessActions(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessBranching => FeatureGate.CanAccessBranching(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanSwitchBranch
        {
            get
            {
                if (!CanAccessBranching) return false;
                // Platform SuperAdmin and Tenant Admin have company-wide scope and can switch branches freely
                if (CurrentUser?.Role == UserRole.SuperAdmin || CurrentUser?.Role == UserRole.Admin)
                    return true;

                // Staff / Agents / Managers assigned to a specific branch are locked to that branch;
                // only staff assigned to all branches (no specific branch assigned) can switch branches.
                return !AssignedBranchId.HasValue || AssignedBranchId.Value <= 0;
            }
        }
        public static bool CanUseCustomLogo => FeatureGate.CanUseCustomLogo(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanUseAccentColor => FeatureGate.CanUseAccentColor(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanHidePoweredBy => FeatureGate.CanHidePoweredBy(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanExportData => FeatureGate.CanExportData(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessDisasterRecovery => FeatureGate.CanAccessDisasterRecovery(TenantTier) || CurrentUser?.Role == UserRole.SuperAdmin;
        public static bool CanAccessFeature(string featureKey) => FeatureGate.IsFeatureEnabled(TenantTier, featureKey) || CurrentUser?.Role == UserRole.SuperAdmin;

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
            string? tenantName = null,
            int? assignedBranchId = null,
            string? assignedBranchName = null)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("A valid UserId is required.", nameof(userId));
            }

            if (tenantId < 0)
            {
                tenantId = 0;
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
            TenantName = tenantName ?? (tenantId == 0 ? "Master Platform Administration" : $"Tenant #{tenantId}");

            AssignedBranchId = (assignedBranchId.HasValue && assignedBranchId.Value > 0) ? assignedBranchId.Value : null;
            AssignedBranchName = assignedBranchName;
            ActiveBranchId = AssignedBranchId;
            ActiveBranchName = AssignedBranchName;

            var role = roleName.Trim().ToLowerInvariant();

            CurrentUser = role switch
            {
                "superadmin" or "super admin" or "super_admin" =>
                    new CRMS_Peguit.winforms.Models.Roles.SuperAdmin(fullName, email),

                "admin" =>
                    new Admin(fullName, email),

                "manager" =>
                    new Manager(fullName, email),

                "agent" or "salesstaff" or "sales staff" or "sales_staff" =>
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
            // Staff assigned to a specific branch cannot switch branch context
            if (!CanSwitchBranch && AssignedBranchId.HasValue)
            {
                return;
            }

            ActiveBranchId = branchId;
            ActiveBranchName = branchName;
        }

        public static void SetJwtToken(string? token)
        {
            JwtToken = token;
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

            if (moduleName.Equals("Branching", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Branches", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessBranching) return false;
            }

            if (moduleName.Equals("Deals", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessMainTransaction) return false;
            }

            if (moduleName.Equals("Customers", StringComparison.OrdinalIgnoreCase) ||
                moduleName.Equals("Leads", StringComparison.OrdinalIgnoreCase))
            {
                if (!CanAccessDataCollection) return false;
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
            CRMS_Peguit.winforms.Services.BrandingService.Clear();
        }
    }
}
