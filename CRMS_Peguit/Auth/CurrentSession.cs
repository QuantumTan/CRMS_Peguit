using CRMS_Peguit.Models.Backend;
using NEXA.Model;
using System;

namespace CRMS_Peguit.winforms.Auth
{
    public static class CurrentSession
    {
        // ==============================================
        // CURRENT USER INFORMATION
        // ==============================================

        public static int UserId
        {
            get;
            private set;
        }

        public static string? JwtToken
        {
            get;
            private set;
        }

        public static User? CurrentUser
        {
            get;
            private set;
        }

        public static bool IsOffline
        {
            get;
            private set;
        }

        // ==============================================
        // START SESSION
        // ==============================================

        public static void Start(
            int userId,
            string fullName,
            string email,
            string roleName,
            string? jwtToken,
            bool isOffline)
        {
            UserId = userId;

            JwtToken = jwtToken;

            IsOffline = isOffline;

            // Normalize role name.
            var role = roleName
                .Trim()
                .ToLowerInvariant();

            CurrentUser = role switch
            {
                "admin" =>
                    new Admin(
                        fullName,
                        email
                    ),

                "manager" =>
                    new Manager(
                        fullName,
                        email
                    ),

                "agent" =>
                    new SalesStaff(
                        fullName,
                        email
                    ),

                _ =>
                    throw new InvalidOperationException(
                        $"Unknown role '{roleName}' - cannot build a session user."
                    )
            };
        }

        // ==============================================
        // CHECK MODULE ACCESS
        // ==============================================

        public static bool CanAccess(
            string moduleName)
        {
            return CurrentUser?
                .GetAccessibleModules()
                .Contains(moduleName)
                ?? false;
        }

        // ==============================================
        // SIGN OUT
        // ==============================================

        public static void SignOut()
        {
            UserId = 0;

            JwtToken = null;

            CurrentUser = null;

            IsOffline = false;
        }
    }
}