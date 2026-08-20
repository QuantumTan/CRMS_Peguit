using NEXA.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.Models.Backend
{
    // CHILD CLASS - Admin

    public class Admin : User
    {
        public Admin(string fullName, string email)
            : base(fullName, email, UserRole.Admin)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "UserManagement", "Customers", "Leads", "Deals",
                "Reports", "ImportExport", "Settings"
            };
        }

        public override string GetDashboardType()
        {// 
            return "AdminDashboard";
        }
    }
}
