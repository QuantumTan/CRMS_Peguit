using System.Collections.Generic;

namespace CRMS_Peguit.winforms.Models.Roles
{
    // CHILD CLASS - SuperAdmin

    public class SuperAdmin : User
    {
        public SuperAdmin(
            string fullName,
            string email)
            : base(
                fullName,
                email,
                UserRole.SuperAdmin)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "Dashboard",
                "AdminPanel",
                "BusinessIntelligence",
                "Reports",
                "Analytics",
                "Subscription",
                "Branching",
                "Administrators",
                "Roles",
                "SystemAccess",
                "SystemDataBackup",
                "SystemSettings",
                "Policies"
            };
        }

        public override string GetDashboardType()
        {
            return "SuperAdminDashboard";
        }
    }
}
