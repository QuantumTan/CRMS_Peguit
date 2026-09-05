using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace CRMS_Peguit.winforms.Auth
{
    // Caches the last successful online login per (company, email), so a
    // user who has logged in before can still get into the app when
    // monsterASP/the API is unreachable.
    // Keyed by CompanyId + Email so one device can hold cached logins for
    // more than one tenant without them colliding.
    // NuGet: Install-Package Microsoft.Data.Sqlite
    public class LocalAuthCache
    {
        private readonly string _connectionString;

        public LocalAuthCache()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CRMS_Peguit", "local_cache.db"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath}";
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CachedLogin (
                    CompanyId TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    UserId INTEGER NOT NULL,
                    FullName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    RoleName TEXT NOT NULL,
                    LastSyncedAt TEXT NOT NULL,
                    PRIMARY KEY (CompanyId, Email)
                );";
            cmd.ExecuteNonQuery();
        }

        // Called after every successful ONLINE login, so the cache stays fresh
        public void SaveSuccessfulLogin(string companyId, int userId, string fullName, string email, string passwordHash, string roleName)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CachedLogin (CompanyId, Email, UserId, FullName, PasswordHash, RoleName, LastSyncedAt)
                VALUES ($companyId, $email, $userId, $fullName, $hash, $role, $syncedAt)
                ON CONFLICT(CompanyId, Email) DO UPDATE SET
                    UserId = excluded.UserId,
                    FullName = excluded.FullName,
                    PasswordHash = excluded.PasswordHash,
                    RoleName = excluded.RoleName,
                    LastSyncedAt = excluded.LastSyncedAt;";
            cmd.Parameters.AddWithValue("$companyId", companyId);
            cmd.Parameters.AddWithValue("$email", email);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$fullName", fullName);
            cmd.Parameters.AddWithValue("$hash", passwordHash);
            cmd.Parameters.AddWithValue("$role", roleName);
            cmd.Parameters.AddWithValue("$syncedAt", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        public CachedLoginRecord? TryGetCachedLogin(string companyId, string email)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT UserId, FullName, PasswordHash, RoleName, LastSyncedAt
                                 FROM CachedLogin WHERE CompanyId = $companyId AND Email = $email;";
            cmd.Parameters.AddWithValue("$companyId", companyId);
            cmd.Parameters.AddWithValue("$email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new CachedLoginRecord(
                UserId: reader.GetInt32(0),
                FullName: reader.GetString(1),
                Email: email,
                PasswordHash: reader.GetString(2),
                RoleName: reader.GetString(3),
                LastSyncedAt: DateTime.Parse(reader.GetString(4))
            );
        }
    }

    public record CachedLoginRecord(
        int UserId,
        string FullName,
        string Email,
        string PasswordHash,
        string RoleName,
        DateTime LastSyncedAt
    );
}