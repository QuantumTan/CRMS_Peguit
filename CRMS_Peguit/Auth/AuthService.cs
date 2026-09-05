using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CRMS_Peguit.infrastructure.Security;

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
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(6) // fail fast so offline fallback doesn't hang the UI
            };
            _localCache = new LocalAuthCache();
        }

        public async Task<AuthResult> LoginAsync(string companyId, string email, string password)
        {
            try
            {
                // Login has no JWT yet, so the tenant must be sent explicitly -
                // this is the one request HttpTenantResolver trusts the header for.
                var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
                {
                    Content = JsonContent.Create(new { email, password })
                };
                request.Headers.Add("X-Company-Id", companyId);

                var response = await _httpClient.SendAsync(request);

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
                    _localCache.SaveSuccessfulLogin(companyId, result.UserId, result.FullName, result.Email, localHash, result.RoleName);

                    CurrentSession.Start(result.UserId, result.FullName, result.Email, result.RoleName, result.Token, isOffline: false);
                    return new AuthResult { Success = true };
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };

                return new AuthResult { Success = false, ErrorMessage = $"Server error ({(int)response.StatusCode})." };
            }
            catch (Exception) // network unreachable, monsterASP down, timeout, etc.
            {
                return TryOfflineLogin(companyId, email, password);
            }
        }

        private AuthResult TryOfflineLogin(string companyId, string email, string password)
        {
            var cached = _localCache.TryGetCachedLogin(companyId, email);
            if (cached is null)
            {
                return new AuthResult
                {
                    Success = false,
                    WasOffline = true,
                    ErrorMessage = "No internet connection, and no previous login found on this device."
                };
            }

            bool passwordMatches = !string.IsNullOrEmpty(cached.PasswordHash)
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

            CurrentSession.Start(cached.UserId, cached.FullName, cached.Email, cached.RoleName, jwtToken: null, isOffline: true);
            return new AuthResult { Success = true, WasOffline = true };
        }

        private record LoginApiResponse(string Token, int UserId, string FullName, string Email, string RoleName);
    }
}