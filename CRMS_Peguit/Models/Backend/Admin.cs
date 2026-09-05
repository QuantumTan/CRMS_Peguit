using NEXA.Model;
using System.Collections.Generic;

namespace CRMS_Peguit.Models.Backend
{
    // CHILD CLASS - Admin

    public class Admin : User
    {
        public Admin(
            string fullName,
            string email)
            : base(
                fullName,
                email,
                UserRole.Admin)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "UserManagement",

                "Customers",
                "Leads",
                "Properties",
                "Deals",

                "Reports",
                "ImportExport",
                "Settings"
            };
        }

        public override string GetDashboardType()
        {
            return "AdminDashboard";
        }
    }
}