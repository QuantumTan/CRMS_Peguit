using BCrypt.Net;

namespace CRMS_Peguit.infrastructure.Security
{
    // Real authentication uses BCrypt (salted, slow-by-design) instead of the
    // plain SHA-256 in NEXA.Model.User, which stays only as an OOP/encapsulation
    // demonstration and is not used for real login.
    //
    // NuGet: Install-Package BCrypt.Net-Next
    public static class PasswordHasher
    {
        public static string Hash(string plainTextPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
        }

        public static bool Verify(string plainTextPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHash);
        }
    }
}