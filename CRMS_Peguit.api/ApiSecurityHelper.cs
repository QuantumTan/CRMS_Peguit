using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CRMS_Peguit.api
{
    public static class ApiSecurityHelper
    {
        public static (int UserId, string Role, int TenantId) GetCurrentUserInfo(HttpContext context)
        {
            int userId = 0;
            string role = "Agent";
            int tenantId = 0;

            if (context?.User?.Identity?.IsAuthenticated == true || context?.User != null)
            {
                var info = GetCurrentUserInfo(context.User);
                userId = info.UserId;
                role = info.Role;
                tenantId = info.TenantId;
            }

            if (userId <= 0 && context != null)
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(authHeader.Substring(7).Trim());
                        var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub")?.Value;
                        int.TryParse(idClaim, out userId);

                        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                        if (!string.IsNullOrWhiteSpace(roleClaim))
                            role = roleClaim;

                        var tidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;
                        int.TryParse(tidClaim, out tenantId);
                    }
                    catch { }
                }
            }

            // Fallback headers
            if (context != null)
            {
                if (userId <= 0 && int.TryParse(context.Request.Headers["X-User-Id"].ToString(), out var hdrUid) && hdrUid > 0)
                {
                    userId = hdrUid;
                }

                var hdrRole = context.Request.Headers["X-User-Role"].ToString();
                if (!string.IsNullOrWhiteSpace(hdrRole))
                {
                    role = hdrRole;
                }

                if (tenantId <= 0 && int.TryParse(context.Request.Headers["X-Tenant-Id"].ToString(), out var hdrTid) && hdrTid > 0)
                {
                    tenantId = hdrTid;
                }
            }

            return (userId, role, tenantId);
        }

        public static (int UserId, string Role, int TenantId) GetCurrentUserInfo(ClaimsPrincipal? user)
        {
            int userId = 0;
            string role = "Agent";
            int tenantId = 0;

            if (user != null)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.FindFirst("nameid")?.Value
                    ?? user.FindFirst("UserId")?.Value;
                int.TryParse(idClaim, out userId);

                role = user.FindFirst(ClaimTypes.Role)?.Value
                    ?? user.FindFirst("role")?.Value
                    ?? user.FindFirst("Role")?.Value
                    ?? "Agent";

                var tidClaim = user.FindFirst("tenantId")?.Value
                    ?? user.FindFirst("TenantId")?.Value;
                int.TryParse(tidClaim, out tenantId);
            }

            return (userId, role, tenantId);
        }

        public static int GetUserId(ClaimsPrincipal? user) => GetCurrentUserInfo(user).UserId;
        public static string GetRole(ClaimsPrincipal? user) => GetCurrentUserInfo(user).Role;
        public static int GetTenantId(ClaimsPrincipal? user) => GetCurrentUserInfo(user).TenantId;

        public static int GetUserId(HttpContext? context) => context != null ? GetCurrentUserInfo(context).UserId : 0;
        public static string GetRole(HttpContext? context) => context != null ? GetCurrentUserInfo(context).Role : "Agent";
        public static int GetTenantId(HttpContext? context) => context != null ? GetCurrentUserInfo(context).TenantId : 0;

        public static bool IsAgent(string role) =>
            role.Equals("Agent", StringComparison.OrdinalIgnoreCase) || role.Equals("SalesStaff", StringComparison.OrdinalIgnoreCase);

        public static bool IsManager(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        public static bool IsAdmin(string role) =>
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        public static bool IsSuperAdmin(string role) =>
            role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);

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
