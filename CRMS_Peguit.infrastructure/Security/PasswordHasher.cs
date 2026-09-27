using BCrypt.Net;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CRMS_Peguit.infrastructure.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string plainTextPassword)
        {
            if (string.IsNullOrEmpty(plainTextPassword))
                throw new ArgumentException("Password cannot be empty.", nameof(plainTextPassword));

            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
        }

        public static bool Verify(string plainTextPassword, string? storedHash)
        {
            if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            var trimmedHash = storedHash.Trim();

            try
            {
                // Primary path: Verify BCrypt hash ($2a$, $2b$, $2y$, $2x$)
                if (trimmedHash.Length >= 4 &&
                    trimmedHash[0] == '$' &&
                    trimmedHash[1] == '2' &&
                    (trimmedHash[2] == 'a' || trimmedHash[2] == 'b' || trimmedHash[2] == 'y' || trimmedHash[2] == 'x') &&
                    trimmedHash[3] == '$')
                {
                    return BCrypt.Net.BCrypt.Verify(plainTextPassword, trimmedHash);
                }

                // Support unsalted SHA-256 legacy verification for transparent upgrade on login
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainTextPassword));
                    var shaBase64 = Convert.ToBase64String(bytes);
                    if (trimmedHash == shaBase64)
                        return true;
                }

                // Plaintext is not permitted in production paths; only allowed in Development
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isDevelopment = string.IsNullOrWhiteSpace(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase);
                if (isDevelopment && plainTextPassword == trimmedHash)
                {
                    return true;
                }

                return false;
            }
            catch (SaltParseException)
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isDevelopment = string.IsNullOrWhiteSpace(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase);
                return isDevelopment && plainTextPassword == trimmedHash;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}