using NEXA.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS_Peguit.Models.Backend
{
    // CHILD CLASS - SalesStaff

    public class SalesStaff : User
    {
        public SalesStaff(string fullName, string email)
            : base(fullName, email, UserRole.SalesStaff)
        {
        }

        public override List<string> GetAccessibleModules()
        {
            return new List<string>
            {
                "Customers", "Leads", "Deals", "Activities", "TasksReminders"
            };
        }

        public override string GetDashboardType()
        {
            return "SalesStaffDashboard";
        }
    }
}
