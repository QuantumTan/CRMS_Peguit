using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CRMS_Peguit.api
{
    public static class ApiSecurityHelper
    {
        public static (int UserId, string Role, int TenantId) GetCurrentUserInfo(ClaimsPrincipal? user)
        {
            int userId = 0;
            string role = string.Empty;
            int tenantId = 0;

            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.FindFirst("nameid")?.Value
                    ?? user.FindFirst("UserId")?.Value;
                int.TryParse(idClaim, out userId);

                role = user.FindFirst(ClaimTypes.Role)?.Value
                    ?? user.FindFirst("role")?.Value
                    ?? user.FindFirst("Role")?.Value
                    ?? string.Empty;

                var tidClaim = user.FindFirst("tenantId")?.Value
                    ?? user.FindFirst("TenantId")?.Value;
                int.TryParse(tidClaim, out tenantId);
            }

            return (userId, role, tenantId);
        }

        public static (int UserId, string Role, int TenantId) GetCurrentUserInfo(HttpContext? context)
        {
            return GetCurrentUserInfo(context?.User);
        }

        public static int GetUserId(ClaimsPrincipal? user) => GetCurrentUserInfo(user).UserId;
        public static string GetRole(ClaimsPrincipal? user) => GetCurrentUserInfo(user).Role;
        public static int GetTenantId(ClaimsPrincipal? user) => GetCurrentUserInfo(user).TenantId;

        public static int GetUserId(HttpContext? context) => GetCurrentUserInfo(context).UserId;
        public static string GetRole(HttpContext? context) => GetCurrentUserInfo(context).Role;
        public static int GetTenantId(HttpContext? context) => GetCurrentUserInfo(context).TenantId;

        public static bool IsAgent(string role) =>
            !string.IsNullOrWhiteSpace(role) &&
            (role.Equals("Agent", StringComparison.OrdinalIgnoreCase) ||
             role.Equals("SalesStaff", StringComparison.OrdinalIgnoreCase) ||
             role.Equals("Sales Staff", StringComparison.OrdinalIgnoreCase));

        public static bool IsManager(string role) =>
            !string.IsNullOrWhiteSpace(role) &&
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        public static bool IsAdmin(string role) =>
            !string.IsNullOrWhiteSpace(role) &&
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        public static bool IsSuperAdmin(string role) =>
            !string.IsNullOrWhiteSpace(role) &&
            (role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
             role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

        public static bool HasFullOversight(string role) =>
            (IsManager(role) || IsAdmin(role)) && !IsSuperAdmin(role);

        public static bool CanAssignRecords(string role) =>
            (IsManager(role) || IsAdmin(role)) && !IsSuperAdmin(role);

        public static bool IsAgent(ClaimsPrincipal? user) => IsAgent(GetRole(user));
        public static bool IsManager(ClaimsPrincipal? user) => IsManager(GetRole(user));
        public static bool IsAdmin(ClaimsPrincipal? user) => IsAdmin(GetRole(user));
        public static bool IsSuperAdmin(ClaimsPrincipal? user) => IsSuperAdmin(GetRole(user));
        public static bool HasFullOversight(ClaimsPrincipal? user) => HasFullOversight(GetRole(user));
        public static bool CanAssignRecords(ClaimsPrincipal? user) => CanAssignRecords(GetRole(user));

        public static bool IsAgent(HttpContext? context) => IsAgent(GetRole(context));
        public static bool IsManager(HttpContext? context) => IsManager(GetRole(context));
        public static bool IsAdmin(HttpContext? context) => IsAdmin(GetRole(context));
        public static bool IsSuperAdmin(HttpContext? context) => IsSuperAdmin(GetRole(context));
        public static bool HasFullOversight(HttpContext? context) => HasFullOversight(GetRole(context));
        public static bool CanAssignRecords(HttpContext? context) => CanAssignRecords(GetRole(context));
    }
}
