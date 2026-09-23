using System.Collections.Generic;

namespace CRMS_Peguit.winforms.Models.Roles
{
    // CHILD CLASS - SalesStaff

    public class SalesStaff : User
    {
        public SalesStaff(
            string fullName,
            string email)
            : base(
                fullName,
                email,
                UserRole.SalesStaff)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "Dashboard",
                "Reports",
                "Analytics",
                "SupportTickets",
                "Leads",
                "Customers",
                "Properties",
                "Deals",
                "Activities",
                "TasksReminders",
                "Campaigns"
            };
        }

        public override string GetDashboardType()
        {
            return "SalesStaffDashboard";
        }
    }
}
