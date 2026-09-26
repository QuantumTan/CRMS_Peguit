using System;
using System.Threading.Tasks;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Services
{
    public class LoginValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }

        public static LoginValidationResult Success() => new() { IsValid = true };
        public static LoginValidationResult Failure(string error) => new() { IsValid = false, ErrorMessage = error };
    }

    public class AuthApiService
    {
        private readonly AuthService _authService;

        public AuthApiService(string? apiBaseUrl = null)
        {
            _authService = new AuthService(apiBaseUrl ?? DbConfiguration.GetApiBaseUrl());
        }

        public AuthApiService(AuthService authService)
        {
            _authService = authService;
        }

        public LoginValidationResult ValidateCredentials(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return LoginValidationResult.Failure("Email is required.");
            }

            if (!ContactEmailService.IsValidEmail(email.Trim()))
            {
                return LoginValidationResult.Failure("Enter a valid email address.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return LoginValidationResult.Failure("Password is required.");
            }

            return LoginValidationResult.Success();
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var validation = ValidateCredentials(email, password);
            if (!validation.IsValid)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            return await _authService.LoginAsync(
                email.Trim(),
                password
            );
        }

        public void SignOut()
        {
            CurrentSession.SignOut();
        }
    }
}
