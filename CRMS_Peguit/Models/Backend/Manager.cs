using NEXA.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.Models.Backend
{
    // CHILD CLASS - Manager

    public class Manager : User
    {
        public Manager(string fullName, string email)
            : base(fullName, email, UserRole.Manager)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "TeamDashboard", "Reports", "CustomerAssignments",
                "Customers", "Leads", "Deals"
            };
        }

        public override string GetDashboardType()
        {
            return "ManagerDashboard";
        }
    }
}
