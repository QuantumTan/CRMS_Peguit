using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.Security;
using CRMS_Peguit.winforms.Models.Services;
using CRMS_Peguit.winforms.Services;

namespace CRMS_Peguit.winforms.Auth
{
    public class AuthResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public bool WasOffline { get; init; }
    }

    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalAuthCache _localCache;

        // Point this at your monsterASP-hosted API, e.g. "https://your-app.runasp.net/"
        public AuthService(string apiBaseUrl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (message.RequestUri?.IsLoopback == true)
                        return true;
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(3)
            };
            _localCache = new LocalAuthCache();
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            return await Task.Run(async () =>
            {
                var timer = Stopwatch.StartNew();
                // Architecture: Local -> Cloud then Sync.
                // 1. Fast path: Authenticate against Local DB first (~150ms) to ensure instant responsiveness.
                var localAttempt = TryLocalDbLogin(email, password);
                if (localAttempt.Success && !localAttempt.WasOffline)
                {
                    Debug.WriteLine($"[Auth.Performance] Local sign-in completed in {timer.ElapsedMilliseconds} ms.");
                    // Asynchronously acquire JWT token in background if API is reachable (non-blocking)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                            var resp = await _httpClient.PostAsJsonAsync("api/auth/login", new
                            {
                                email = email.Trim(),
                                password
                            }, cts.Token).ConfigureAwait(false);

                            if (resp.IsSuccessStatusCode)
                            {
                                var apiRes = await resp.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: cts.Token).ConfigureAwait(false);
                                if (apiRes != null && !string.IsNullOrWhiteSpace(apiRes.Token))
                                {
                                    CurrentSession.SetJwtToken(apiRes.Token);
                                }
                            }
                        }
                        catch
                        {
                            // Background API probe failed; local session continues normally
                        }
                    });

                    return localAttempt;
                }

                // If local attempt encountered an active account restriction (suspended/inactive), return immediately.
                if (!string.IsNullOrWhiteSpace(localAttempt.ErrorMessage) &&
                    (localAttempt.ErrorMessage.StartsWith("Access Suspended", StringComparison.OrdinalIgnoreCase) ||
                     localAttempt.ErrorMessage.Contains("currently", StringComparison.OrdinalIgnoreCase)))
                {
                    return localAttempt;
                }

                // 2. Fallback path: If user was not in local DB (or password may have been updated on cloud),
                // check the API with a strict 2.5s cancellation timeout so the UI never hangs.
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                    var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
                    {
                        email = email.Trim(),
                        password
                    }, cts.Token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: cts.Token).ConfigureAwait(false);
                        if (result is null)
                            return new AuthResult { Success = false, ErrorMessage = "Unexpected response from server." };

                        var localHash = PasswordHasher.Hash(password);
                        _localCache.SaveSuccessfulLogin(result.TenantId, result.UserId, result.FullName, result.Email, localHash, result.RoleName, result.BranchId, result.BranchName);

                        int effectiveUserId = EnsureLocalUser(result.UserId, result.TenantId, result.FullName, result.Email, localHash, result.RoleName);

                        var (tier, companyName) = ResolveTenantSubscription(result.TenantId, result.RoleName);

                        CurrentSession.Start(
                            effectiveUserId,
                            result.TenantId,
                            result.FullName,
                            result.Email,
                            result.RoleName,
                            result.Token,
                            isOffline: false,
                            tier: tier,
                            tenantName: companyName,
                            assignedBranchId: result.BranchId,
                            assignedBranchName: result.BranchName);
                        BrandingService.InitializeForTenant(result.TenantId, companyName);
                        Debug.WriteLine($"[Auth.Performance] API sign-in completed in {timer.ElapsedMilliseconds} ms.");
                        return new AuthResult { Success = true, WasOffline = false };
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };
                    }
                }
                catch (Exception)
                {
                    // Network unreachable, monsterASP down, API offline, or timeout
                }

                // If localAttempt had a successful cached login, return it
                if (localAttempt.Success)
                {
                    return localAttempt;
                }

                // If local attempt produced an error message (like "Invalid email or password."), return it
                if (!string.IsNullOrWhiteSpace(localAttempt.ErrorMessage))
                {
                    return localAttempt;
                }

                return TryOfflineLogin(email, password);
            }).ConfigureAwait(false);
        }

        public AuthResult TryLocalDbLogin(string email, string password)
        {
            var lowerEmail = email.Trim().ToLowerInvariant();
            var cachedLogin = _localCache.TryGetCachedLogin(lowerEmail);

            // Check Master database for real SuperAdmin user
            bool mayBeSuperAdmin = cachedLogin?.TenantId == 0 ||
                lowerEmail.Contains("superadmin", StringComparison.OrdinalIgnoreCase) ||
                lowerEmail.Contains("super.admin", StringComparison.OrdinalIgnoreCase);
            if (mayBeSuperAdmin)
            {
                try
                {
                    using var masterDb = LocalDb.CreateMasterContext(initializeDatabase: false);
                    var sa = masterDb.SuperAdmins
                        .AsNoTracking()
                        .FirstOrDefault(s => s.Email == lowerEmail && s.IsActive);

                    if (sa != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sa.PasswordHash) && PasswordHasher.Verify(password, sa.PasswordHash))
                        {
                            string fullName = $"{sa.FirstName} {sa.LastName}".Trim();
                            if (string.IsNullOrWhiteSpace(fullName)) fullName = "Super Admin";

                            _localCache.SaveSuccessfulLogin(0, sa.SuperAdminId, fullName, sa.Email, sa.PasswordHash, "SuperAdmin");

                            CurrentSession.Start(
                                sa.SuperAdminId,
                                0,
                                fullName,
                                sa.Email,
                                "SuperAdmin",
                                jwtToken: null,
                                isOffline: false,
                                tier: domain.entities.TenantTier.Master,
                                tenantName: "Master Platform Administration");
                            BrandingService.Clear();
                            return new AuthResult { Success = true, WasOffline = false };
                        }

                        return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };
                    }
                }
                catch
                {
                    // Fallback to cached credentials or API.
                }
            }

            // Determine prioritized tenant search order based on email domain or prefixes
            int[] tenantIds;
            if (cachedLogin?.TenantId > 0)
            {
                tenantIds = new[] { cachedLogin.TenantId };
            }
            else if (lowerEmail.Contains("tenantc") || lowerEmail.Contains(".c@") || lowerEmail.EndsWith("@tenantc.com") ||
                lowerEmail == "carlos.mendoza@test.com" || lowerEmail == "beatrice.ong@test.com" || lowerEmail == "gabriel.santos@test.com" ||
                lowerEmail == "althea.garcia@test.com" || lowerEmail == "mateo.lim@test.com" || lowerEmail == "patricia.alvarez@test.com" || lowerEmail == "dominic.suarez@test.com")
            {
                tenantIds = new[] { 3 };
            }
            else if (lowerEmail.Contains("tenantb") || lowerEmail.Contains(".b@") || lowerEmail.EndsWith("@tenantb.com") ||
                lowerEmail == "valerie.cross@test.com" || lowerEmail == "elena.rostova@test.com" || lowerEmail == "marcus.vance@test.com" ||
                lowerEmail == "chloe.bennett@test.com" || lowerEmail == "nathan.drake@test.com")
            {
                tenantIds = new[] { 2 };
            }
            else if (lowerEmail.Contains("tenanta") || lowerEmail.Contains(".a@") || lowerEmail.EndsWith("@tenanta.com") ||
                lowerEmail == "admin@test.com" || lowerEmail == "manager@test.com" || lowerEmail == "agent@test.com" ||
                lowerEmail == "sarah.jenkins@test.com" || lowerEmail == "michael.chang@test.com" || lowerEmail == "jessica.torres@test.com" ||
                lowerEmail == "david.reyes@test.com" || lowerEmail == "amanda.lim@test.com" || lowerEmail == "robert.tan@test.com")
            {
                tenantIds = new[] { 1 };
            }
            else
            {
                tenantIds = new[] { 1, 2, 3 };
            }

            try
            {
                foreach (var tid in tenantIds)
                {
                    using var db = LocalDb.CreateContext(tid, initializeDatabase: false);
                    var user = db.Users
                        .Include(u => u.Role)
                        .Include(u => u.Branch)
                        .AsNoTracking()
                        .FirstOrDefault(u => u.Email == lowerEmail);

                    if (user != null)
                    {
                        bool verify = PasswordHasher.Verify(password, user.PasswordHash);
                        if (verify)
                        {
                            var tenantAccess = ResolveTenantAccess(tid);
                            if (tenantAccess.Suspended)
                            {
                                return new AuthResult
                                {
                                    Success = false,
                                    ErrorMessage = $"Access Suspended: '{tenantAccess.CompanyName}' has been suspended by platform administration."
                                };
                            }

                            // Enforce individual user status check
                            if (!string.IsNullOrWhiteSpace(user.Status) &&
                                !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
                            {
                                return new AuthResult
                                {
                                    Success = false,
                                    ErrorMessage = $"Your user account is currently {user.Status.ToLowerInvariant()}. Please contact your administrator."
                                };
                            }

                            var role = user.Role ?? db.Roles.AsNoTracking().FirstOrDefault(r => r.RoleId == user.RoleId);
                            string roleName = role?.RoleName ?? "Agent";
                            int tenantId = tid;
                            string displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;

                            int? userBranchId = (user.BranchId.HasValue && user.BranchId.Value > 0) ? user.BranchId : null;
                            string? userBranchName = user.Branch?.BranchName;
                            if (userBranchId.HasValue && string.IsNullOrWhiteSpace(userBranchName))
                            {
                                userBranchName = db.Branches.AsNoTracking().FirstOrDefault(b => b.BranchId == userBranchId.Value)?.BranchName;
                            }

                            _localCache.SaveSuccessfulLogin(tenantId, user.UserId, displayName, user.Email, user.PasswordHash, roleName, userBranchId, userBranchName);

                            CurrentSession.Start(
                                user.UserId,
                                tenantId,
                                displayName,
                                user.Email,
                                roleName,
                                jwtToken: null,
                                isOffline: false,
                                tier: tenantAccess.Tier,
                                tenantName: tenantAccess.CompanyName,
                                assignedBranchId: userBranchId,
                                assignedBranchName: userBranchName);

                            BrandingService.InitializeForTenant(tenantId, tenantAccess.CompanyName);

                            return new AuthResult { Success = true, WasOffline = false };
                        }
                        else
                        {
                            return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };
                        }
                    }
                }
            }
            catch
            {
                // Fallback to cached credentials
            }

            return TryOfflineLogin(email, password);
        }

        private AuthResult TryOfflineLogin(string email, string password)
        {
            var cached = _localCache.TryGetCachedLogin(email.Trim());

            if (cached is null)
            {
                return new AuthResult
                {
                    Success = false,
                    WasOffline = true,
                    ErrorMessage = "No internet connection, and no previous login found on this device."
                };
            }

            bool passwordMatches =
                !string.IsNullOrWhiteSpace(cached.PasswordHash)
                && PasswordHasher.Verify(password, cached.PasswordHash);

            if (!passwordMatches)
            {
                return new AuthResult
                {
                    Success = false,
                    WasOffline = true,
                    ErrorMessage = "No internet connection, and offline credentials didn't match."
                };
            }

            bool isSuperAdmin = cached.RoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                cached.RoleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);
            int tenantId = isSuperAdmin ? 0 : (cached.TenantId > 0 ? cached.TenantId : 1);
            var (tier, companyName) = GetFallbackTenantIdentity(tenantId, cached.RoleName);

            CurrentSession.Start(
                cached.UserId,
                tenantId,
                cached.FullName,
                cached.Email,
                cached.RoleName,
                jwtToken: null,
                isOffline: true,
                tier: tier,
                tenantName: companyName,
                assignedBranchId: cached.BranchId,
                assignedBranchName: cached.BranchName);

            BrandingService.InitializeForTenant(tenantId, companyName);

            return new AuthResult
            {
                Success = true,
                WasOffline = true
            };
        }

        private (domain.entities.TenantTier Tier, string CompanyName) ResolveTenantSubscription(int tenantId, string roleName)
        {
            var access = ResolveTenantAccess(tenantId, roleName);
            return (access.Tier, access.CompanyName);
        }

        private (domain.entities.TenantTier Tier, string CompanyName, bool Suspended) ResolveTenantAccess(int tenantId, string roleName = "")
        {
            if (roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (domain.entities.TenantTier.Master, "Master Platform Administration", false);
            }

            try
            {
                using var masterDb = LocalDb.CreateMasterContext(initializeDatabase: false);

                var company = masterDb.Companies
                    .Include(c => c.Subscriptions)
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CompanyId == tenantId);

                if (company != null)
                {
                    var activeSub = company.Subscriptions
                        .OrderByDescending(s => s.StartDate)
                        .FirstOrDefault(s => s.Status == "Active");

                    if (activeSub != null)
                    {
                        return (activeSub.Tier, company.CompanyName, !company.IsActive);
                    }
                    return (domain.entities.TenantTier.TenantA, company.CompanyName, !company.IsActive);
                }
            }
            catch
            {
                // Fallback gracefully if Master DB is not populated yet
            }

            var fallback = GetFallbackTenantIdentity(tenantId, roleName);
            return (fallback.Tier, fallback.CompanyName, false);
        }

        private (domain.entities.TenantTier Tier, string CompanyName) GetFallbackTenantIdentity(int tenantId, string roleName)
        {
            if (roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (domain.entities.TenantTier.Master, "Master Platform Administration");
            }

            var defaultTier = tenantId switch
            {
                3 => domain.entities.TenantTier.TenantC,
                2 => domain.entities.TenantTier.TenantB,
                _ => domain.entities.TenantTier.TenantA
            };
            return (defaultTier, $"Tenant #{tenantId}");
        }

        private int EnsureLocalUser(int userId, int tenantId, string fullName, string email, string? passwordHash, string roleName)
        {
            try
            {
                if (tenantId <= 0) tenantId = 1;
                using var db = LocalDb.CreateContext(tenantId, initializeDatabase: false);

                // 1. Ensure Role exists
                var role = db.Roles.FirstOrDefault(r => r.RoleName.ToLower() == roleName.Trim().ToLower());
                if (role == null)
                {
                    role = new domain.entities.Role { TenantId = tenantId, RoleName = roleName.Trim() };
                    db.Roles.Add(role);
                    db.SaveChanges();
                }

                // 2. Check if user exists by UserId
                var userById = db.Users.FirstOrDefault(u => u.UserId == userId);
                if (userById != null)
                {
                    if (!string.IsNullOrWhiteSpace(passwordHash))
                        userById.PasswordHash = passwordHash;
                    userById.RoleId = role.RoleId;
                    if (string.IsNullOrWhiteSpace(userById.Status))
                        userById.Status = "active";
                    db.SaveChanges();
                    return userId;
                }

                // 3. Check if user exists by Email
                var cleanEmail = email.Trim();
                var userByEmail = db.Users.FirstOrDefault(u => u.Email == cleanEmail);
                if (userByEmail != null)
                {
                    if (!string.IsNullOrWhiteSpace(passwordHash))
                        userByEmail.PasswordHash = passwordHash;
                    userByEmail.RoleId = role.RoleId;
                    if (string.IsNullOrWhiteSpace(userByEmail.Status))
                        userByEmail.Status = "active";
                    db.SaveChanges();
                    return userByEmail.UserId;
                }

                // 4. User does not exist locally; insert using IDENTITY_INSERT
                var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string firstName = parts.Length > 0 ? parts[0] : "User";
                string lastName = parts.Length > 1 ? parts[1] : "";

                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = {0})
                    BEGIN
                        SET IDENTITY_INSERT Users ON;
                        INSERT INTO Users (UserId, FirstName, LastName, Email, PasswordHash, RoleId, Status, CreatedAt)
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 'active', GETUTCDATE());
                        SET IDENTITY_INSERT Users OFF;
                    END",
                    userId, firstName, lastName, email.Trim(), passwordHash ?? "", role.RoleId);

                return userId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureLocalUser error: {ex.Message}");
                return userId;
            }
        }

        private record LoginApiResponse(string Token, int UserId, int TenantId, string FullName, string Email, string RoleName, int? BranchId = null, string? BranchName = null);
    }
}
