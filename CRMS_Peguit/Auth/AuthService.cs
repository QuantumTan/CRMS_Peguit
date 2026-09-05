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

        public AuthService(string apiBaseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(6)
            };

            _localCache = new LocalAuthCache();
        }

        public async Task<AuthResult> LoginAsync(
            string companyId,
            string email,
            string password)
        {
            try
            {
                // ==========================================
                // LOGIN REQUEST
                // ==========================================

                // Login does not have a JWT yet.
                // Therefore Company ID is sent through the
                // X-Company-Id header.
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/auth/login"
                )
                {
                    Content = JsonContent.Create(
                        new
                        {
                            email,
                            password
                        }
                    )
                };

                request.Headers.Add(
                    "X-Company-Id",
                    companyId
                );

                // ==========================================
                // SEND REQUEST
                // ==========================================

                var response =
                    await _httpClient.SendAsync(request);

                // ==========================================
                // LOGIN SUCCESS
                // ==========================================

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content
                            .ReadFromJsonAsync<LoginApiResponse>();

                    if (result is null)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            ErrorMessage =
                                "Unexpected response from server."
                        };
                    }

                    // ======================================
                    // CACHE LOGIN FOR OFFLINE USE
                    // ======================================

                    var localHash =
                        PasswordHasher.Hash(password);

                    _localCache.SaveSuccessfulLogin(
                        companyId,
                        result.UserId,
                        result.FullName,
                        result.Email,
                        localHash,
                        result.RoleName
                    );

                    // ======================================
                    // CREATE CURRENT SESSION
                    // ======================================

                    CurrentSession.Start(
                        result.UserId,
                        result.FullName,
                        result.Email,
                        result.RoleName,
                        result.Token,
                        isOffline: false
                    );

                    return new AuthResult
                    {
                        Success = true,
                        WasOffline = false
                    };
                }

                // ==========================================
                // INVALID CREDENTIALS
                // ==========================================

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage =
                            "Invalid email or password."
                    };
                }

                // ==========================================
                // OTHER SERVER ERROR
                // ==========================================

                return new AuthResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Server error ({(int)response.StatusCode})."
                };
            }
            catch (HttpRequestException)
            {
                // ==========================================
                // NETWORK FAILURE
                // ==========================================
                //
                // Only network-related failures should
                // trigger offline login.
                //
                return TryOfflineLogin(
                    companyId,
                    email,
                    password
                );
            }
            catch (TaskCanceledException)
            {
                // Timeout
                return TryOfflineLogin(
                    companyId,
                    email,
                    password
                );
            }
            catch (Exception ex)
            {
                // ==========================================
                // REAL PROGRAMMING / DATA ERROR
                // ==========================================
                //
                // Don't silently treat this as offline.
                //
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage =
                        $"Login error: {ex.Message}"
                };
            }
        }

        private AuthResult TryOfflineLogin(
            string companyId,
            string email,
            string password)
        {
            // ==========================================
            // FIND CACHED ACCOUNT
            // ==========================================

            var cached =
                _localCache.TryGetCachedLogin(
                    companyId,
                    email
                );

            if (cached is null)
            {
                return new AuthResult
                {
                    Success = false,
                    WasOffline = true,
                    ErrorMessage =
                        "No internet connection, and no previous login found on this device."
                };
            }

            // ==========================================
            // VERIFY PASSWORD
            // ==========================================

            bool passwordMatches =
                !string.IsNullOrEmpty(cached.PasswordHash)
                &&
                PasswordHasher.Verify(
                    password,
                    cached.PasswordHash
                );

            if (!passwordMatches)
            {
                return new AuthResult
                {
                    Success = false,
                    WasOffline = true,
                    ErrorMessage =
                        "No internet connection, and offline credentials didn't match."
                };
            }

            // ==========================================
            // CREATE OFFLINE SESSION
            // ==========================================

            CurrentSession.Start(
                cached.UserId,
                cached.FullName,
                cached.Email,
                cached.RoleName,
                jwtToken: null,
                isOffline: true
            );

            return new AuthResult
            {
                Success = true,
                WasOffline = true
            };
        }

        // ==============================================
        // API LOGIN RESPONSE
        // ==============================================

        private record LoginApiResponse(
            string Token,
            int UserId,
            string FullName,
            string Email,
            string RoleName
        );
    }
}