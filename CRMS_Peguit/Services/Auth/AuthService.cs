using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.Security;
using CRMS_Peguit.winforms.Models.Services;

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
                Timeout = TimeSpan.FromSeconds(6) // fail fast so offline fallback doesn't hang the UI
            };
            _localCache = new LocalAuthCache();
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                // User emails are globally unique; tenant is discovered automatically
                // on the server and returned inside the JWT claims.
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
                {
                    email = email.Trim(),
                    password
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
                    if (result is null)
                        return new AuthResult { Success = false, ErrorMessage = "Unexpected response from server." };

                    // Cache this success for offline use later. We hash the
                    // password ourselves right here (never send the server's
                    // hash back to the client) so offline login can verify
                    // against it next time.
                    var localHash = PasswordHasher.Hash(password);
                    _localCache.SaveSuccessfulLogin(result.TenantId, result.UserId, result.FullName, result.Email, localHash, result.RoleName);

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
                        tenantName: companyName);
                    return new AuthResult { Success = true };
                }

                // Architecture: Local -> Cloud then Sync.
                // If cloud returns 401 or server error, check local DB/aliases before failing.
                var localAttempt = TryLocalDbLogin(email, password);
                if (localAttempt.Success)
                    return localAttempt;

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };

                return new AuthResult { Success = false, ErrorMessage = $"Server error ({(int)response.StatusCode})." };
            }
            catch (Exception) // network unreachable, monsterASP down, timeout, etc.
            {
                return TryLocalDbLogin(email, password);
            }
        }

        public AuthResult TryLocalDbLogin(string email, string password)
        {
            var lowerEmail = email.Trim().ToLowerInvariant();

            // Check Master database for real SuperAdmin user
            try
            {
                using var masterDb = LocalDb.CreateMasterContext();
                var sa = masterDb.SuperAdmins
                    .AsNoTracking()
                    .FirstOrDefault(s => s.Email != null && s.Email.ToLower() == lowerEmail && s.IsActive);

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
                        return new AuthResult { Success = true, WasOffline = false };
                    }
                    else
                    {
                        return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };
                    }
                }
            }
            catch
            {
                // Fallback to tenant DB search
            }

            // Determine prioritized tenant search order based on email domain or prefixes
            int[] tenantIds;
            if (lowerEmail.Contains("tenantc") || lowerEmail.Contains(".c@") || lowerEmail.EndsWith("@tenantc.com") ||
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
                    using var db = LocalDb.CreateContext(tid);
                    var user = db.Users
                        .Include(u => u.Role)
                        .AsNoTracking()
                        .FirstOrDefault(u => u.Email != null && u.Email.ToLower() == lowerEmail);

                    if (user != null)
                    {
                        bool verify = PasswordHasher.Verify(password, user.PasswordHash);
                        if (verify)
                        {
                            // Enforce tenant organization suspension check
                            try
                            {
                                using var masterDb = LocalDb.CreateMasterContext();
                                var company = masterDb.Companies.AsNoTracking().FirstOrDefault(c => c.CompanyId == tid);
                                if (company != null && !company.IsActive)
                                {
                                    return new AuthResult
                                    {
                                        Success = false,
                                        ErrorMessage = $"Access Suspended: '{company.CompanyName}' has been suspended by platform administration."
                                    };
                                }
                            }
                            catch
                            {
                                // Master DB access fallback
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

                            _localCache.SaveSuccessfulLogin(tenantId, user.UserId, displayName, user.Email, user.PasswordHash, roleName);

                            var (tier, companyName) = ResolveTenantSubscription(tenantId, roleName);

                            CurrentSession.Start(
                                user.UserId,
                                tenantId,
                                displayName,
                                user.Email,
                                roleName,
                                jwtToken: null,
                                isOffline: false,
                                tier: tier,
                                tenantName: companyName);

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

            int tenantId = cached.TenantId > 0 ? cached.TenantId : 1;

            int effectiveUserId = EnsureLocalUser(cached.UserId, tenantId, cached.FullName, cached.Email, cached.PasswordHash, cached.RoleName);

            var (tier, companyName) = ResolveTenantSubscription(tenantId, cached.RoleName);

            CurrentSession.Start(
                effectiveUserId,
                tenantId,
                cached.FullName,
                cached.Email,
                cached.RoleName,
                jwtToken: null,
                isOffline: true,
                tier: tier,
                tenantName: companyName);

            return new AuthResult
            {
                Success = true,
                WasOffline = true
            };
        }

        private (domain.entities.TenantTier Tier, string CompanyName) ResolveTenantSubscription(int tenantId, string roleName)
        {
            if (roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (domain.entities.TenantTier.Master, "Master Platform Administration");
            }

            try
            {
                using var masterDb = LocalDb.CreateMasterContext();

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
                        return (activeSub.Tier, company.CompanyName);
                    }
                    return (domain.entities.TenantTier.TenantA, company.CompanyName);
                }
            }
            catch
            {
                // Fallback gracefully if Master DB is not populated yet
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
                using var db = LocalDb.CreateContext(tenantId);

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
                    userById.Status = "active";
                    db.SaveChanges();
                    return userId;
                }

                // 3. Check if user exists by Email
                var userByEmail = db.Users.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == email.Trim().ToLower());
                if (userByEmail != null)
                {
                    if (!string.IsNullOrWhiteSpace(passwordHash))
                        userByEmail.PasswordHash = passwordHash;
                    userByEmail.RoleId = role.RoleId;
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

        private record LoginApiResponse(string Token, int UserId, int TenantId, string FullName, string Email, string RoleName);
    }
}