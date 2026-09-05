using CRMS_Peguit.Models.Backend;
using NEXA.Model;
using System;

namespace CRMS_Peguit.winforms.Auth
{
    // The single source of truth for "who is logged in right now".
    // This is the bridge between the real DB-backed login (email/hash/role
    // string from the API or the offline cache) and your NEXA.Model OOP
    // hierarchy - so GetAccessibleModules()/GetDashboardType() actually
    // drive the running app instead of sitting unused.
    public static class CurrentSession
    {
        public static int UserId { get; private set; }
        public static string? JwtToken { get; private set; }
        public static User? CurrentUser { get; private set; } // Admin / Manager / Agent instance
        public static bool IsOffline { get; private set; }

        public static void Start(int userId, string fullName, string email, string roleName, string? jwtToken, bool isOffline)
        {
            UserId = userId;
            JwtToken = jwtToken;
            IsOffline = isOffline;

            CurrentUser = roleName switch
            {
                "Admin" => new Admin(fullName, email),
                "Manager" => new Manager(fullName, email),
                "Agent" => new SalesStaff(fullName, email),
                _ => throw new InvalidOperationException($"Unknown role '{roleName}' - cannot build a session user.")
            };
        }

        public static bool CanAccess(string moduleName)
        {
            return CurrentUser?.GetAccessibleModules().Contains(moduleName) ?? false;
        }

        public static void SignOut()
        {
            UserId = 0;
            JwtToken = null;
            CurrentUser = null;
            IsOffline = false;
        }
    }
}