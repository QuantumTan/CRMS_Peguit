using CRMS_Peguit.winforms.Models.Roles;

namespace CRMS_Peguit.winforms.Auth
{
    public static class RbacService
    {
        public static bool IsSuperAdmin =>
            CurrentSession.CurrentUser?.Role == UserRole.SuperAdmin;

        public static bool IsAdmin =>
            CurrentSession.CurrentUser?.Role == UserRole.Admin;

        public static bool IsManager =>
            CurrentSession.CurrentUser?.Role == UserRole.Manager;

        public static bool IsAgent =>
            CurrentSession.CurrentUser?.Role == UserRole.SalesStaff;

        // R26. Manager and Admin retain full oversight of tenant CRM data regardless of ownership.
        // Multi-tenant Security: SuperAdmin is platform-level ONLY and has ZERO access to tenant operational/business data.
        public static bool HasFullOversight =>
            (IsManager || IsAdmin) && !IsSuperAdmin;

        // R24. Only Manager or Admin may set or change ownership.
        public static bool CanAssignRecords =>
            (IsManager || IsAdmin) && !IsSuperAdmin;

        public static bool CanApproveAssignments =>
            IsManager && !IsSuperAdmin;

        // R23 (revised) & R25 (revised):
        // Visibility is scoped to exactly one Agent at a time:
        // - Creator while Pending / Unassigned
        // - Assignee once assigned
        // At no point is a Pending or Assigned record visible to an Agent who is neither its creator (while Pending) nor its assignee (once assigned).
        // Manager and Admin retain full oversight regardless of this (per R26).
        public static bool CanAgentViewRecord(int? assignedAgentId, int? createdByUserId)
        {
            if (IsSuperAdmin)
                return false;

            if (HasFullOversight)
                return true;

            if (!IsAgent)
                return false;

            int currentUserId = CurrentSession.UserId;
            if (currentUserId <= 0)
                return false;

            // Once assigned, visibility transfers: visible ONLY to the assigned Agent.
            if (assignedAgentId.HasValue && assignedAgentId.Value > 0)
            {
                return assignedAgentId.Value == currentUserId;
            }

            // While Pending / Unassigned, visible ONLY to its creator.
            return createdByUserId.HasValue && createdByUserId.Value == currentUserId;
        }

        // Operational sales records (Leads, Customers, Deals) are created by frontline Agents.
        // Managers supervise, approve, reassign, and archive.
        public static bool CanCreateSalesRecord =>
            IsAgent;

        public static bool CanExportData =>
            (IsAdmin || IsManager) && !IsSuperAdmin;

        public static bool CanViewBrokerageMargins =>
            IsAdmin && !IsSuperAdmin;

        public static bool CanExportFinancialSettlements =>
            IsAdmin && !IsSuperAdmin;

        public static bool CanEditRecord(int? assignedAgentId, int? createdByUserId, string? assignmentStatus = null)
        {
            if (IsSuperAdmin || IsAdmin)
                return false; // SuperAdmin has zero tenant access; Admin has oversight but does not directly manage records

            if (IsManager)
                return true;

            // While a record is pending review by management, operational editing is locked for agents
            if (string.Equals(assignmentStatus, "pending_review", System.StringComparison.OrdinalIgnoreCase))
                return false;

            return CanAgentViewRecord(assignedAgentId, createdByUserId);
        }

        public static bool CanArchiveRecord(int? assignedAgentId, int? createdByUserId)
        {
            if (IsSuperAdmin || IsAdmin)
                return false;

            if (IsManager)
                return true;

            return CanAgentViewRecord(assignedAgentId, createdByUserId);
        }

        public static bool CanEditAssignedRecord(int? assignedAgentId, int? createdByUserId = null) =>
            CanEditRecord(assignedAgentId, createdByUserId);

        public static bool CanArchiveAssignedRecord(int? assignedAgentId, int? createdByUserId = null) =>
            CanArchiveRecord(assignedAgentId, createdByUserId);

        // R23 (revised): Default state is Unassigned — a record created by an Agent starts Unassigned.
        // It is NEVER auto-assigned to its creator.
        public static bool ShouldAutoAssignCreatedRecord =>
            false;
    }
}
