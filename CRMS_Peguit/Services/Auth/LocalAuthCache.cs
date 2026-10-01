using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
        private static readonly object InitializationLock = new();
        private static readonly HashSet<string> InitializedPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _connectionString;

        public LocalAuthCache(string? customDbPath = null)
        {
            var dbPath = string.IsNullOrWhiteSpace(customDbPath)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CRMS_Peguit", "local_cache.db")
                : customDbPath;
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                // Custom paths are short-lived test/diagnostic caches and must not
                // remain locked in SQLite's connection pool after each operation.
                Pooling = string.IsNullOrWhiteSpace(customDbPath)
            }.ToString();
            lock (InitializationLock)
            {
                if (!InitializedPaths.Contains(dbPath))
                {
                    EnsureTableExists();
                    InitializedPaths.Add(dbPath);
                }
            }
        }

        private void EnsureTableExists()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Check if existing table has CompanyId column (old schema)
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('CachedLogin') WHERE name = 'CompanyId';";
            var oldColExists = Convert.ToInt32(checkCmd.ExecuteScalar() ?? 0) > 0;
            if (oldColExists)
            {
                // Drop legacy table so it can be recreated with Email alone as PK
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = "DROP TABLE IF EXISTS CachedLogin;";
                dropCmd.ExecuteNonQuery();
            }

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CachedLogin (
                    Email TEXT PRIMARY KEY,
                    TenantId INTEGER NOT NULL,
                    UserId INTEGER NOT NULL,
                    FullName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    RoleName TEXT NOT NULL,
                    LastSyncedAt TEXT NOT NULL,
                    BranchId INTEGER,
                    BranchName TEXT
                );
                CREATE TABLE IF NOT EXISTS CachedTenantBranding (
                    TenantId INTEGER PRIMARY KEY,
                    DisplayName TEXT NOT NULL,
                    LogoBytes BLOB,
                    LogoVersion INTEGER NOT NULL,
                    AccentColor TEXT,
                    ContactEmail TEXT,
                    ContactPhone TEXT,
                    Address TEXT,
                    HidePoweredBy INTEGER NOT NULL DEFAULT 0,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS DeviceState (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // Ensure BranchId and BranchName columns exist in legacy CachedLogin tables
            try
            {
                var addColCmd = conn.CreateCommand();
                addColCmd.CommandText = "ALTER TABLE CachedLogin ADD COLUMN BranchId INTEGER;";
                addColCmd.ExecuteNonQuery();
            }
            catch { }

            try
            {
                var addColCmd = conn.CreateCommand();
                addColCmd.CommandText = "ALTER TABLE CachedLogin ADD COLUMN BranchName TEXT;";
                addColCmd.ExecuteNonQuery();
            }
            catch { }
        }

        // Called after every successful ONLINE login, so the cache stays fresh
        public void SaveSuccessfulLogin(int tenantId, int userId, string fullName, string email, string passwordHash, string roleName, int? branchId = null, string? branchName = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CachedLogin (Email, TenantId, UserId, FullName, PasswordHash, RoleName, LastSyncedAt, BranchId, BranchName)
                VALUES ($email, $tenantId, $userId, $fullName, $hash, $role, $syncedAt, $branchId, $branchName)
                ON CONFLICT(Email) DO UPDATE SET
                    TenantId = excluded.TenantId,
                    UserId = excluded.UserId,
                    FullName = excluded.FullName,
                    PasswordHash = excluded.PasswordHash,
                    RoleName = excluded.RoleName,
                    LastSyncedAt = excluded.LastSyncedAt,
                    BranchId = excluded.BranchId,
                    BranchName = excluded.BranchName;";
            cmd.Parameters.AddWithValue("$email", email);
            cmd.Parameters.AddWithValue("$tenantId", tenantId);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$fullName", fullName);
            cmd.Parameters.AddWithValue("$hash", passwordHash);
            cmd.Parameters.AddWithValue("$role", roleName);
            cmd.Parameters.AddWithValue("$syncedAt", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$branchId", (object?)branchId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$branchName", (object?)branchName ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            if (tenantId > 0)
            {
                SetLastAuthenticatedTenantId(tenantId);
            }
        }

        public CachedLoginRecord? TryGetCachedLogin(string email)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TenantId, UserId, FullName, PasswordHash, RoleName, LastSyncedAt, BranchId, BranchName
                                 FROM CachedLogin WHERE Email = $email COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("$email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            int? branchId = !reader.IsDBNull(6) ? reader.GetInt32(6) : null;
            string? branchName = !reader.IsDBNull(7) ? reader.GetString(7) : null;

            return new CachedLoginRecord(
                TenantId: reader.GetInt32(0),
                UserId: reader.GetInt32(1),
                FullName: reader.GetString(2),
                Email: email,
                PasswordHash: reader.GetString(3),
                RoleName: reader.GetString(4),
                LastSyncedAt: DateTime.TryParse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.UtcNow,
                BranchId: branchId,
                BranchName: branchName
            );
        }

        public void SaveTenantBranding(int tenantId, string displayName, byte[]? logoBytes, int logoVersion, string? accentColor, string? contactEmail, string? contactPhone, string? address, bool hidePoweredBy, DateTime updatedAt)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CachedTenantBranding (TenantId, DisplayName, LogoBytes, LogoVersion, AccentColor, ContactEmail, ContactPhone, Address, HidePoweredBy, UpdatedAt)
                VALUES ($tenantId, $displayName, $logoBytes, $logoVersion, $accentColor, $contactEmail, $contactPhone, $address, $hidePoweredBy, $updatedAt)
                ON CONFLICT(TenantId) DO UPDATE SET
                    DisplayName = excluded.DisplayName,
                    LogoBytes = excluded.LogoBytes,
                    LogoVersion = excluded.LogoVersion,
                    AccentColor = excluded.AccentColor,
                    ContactEmail = excluded.ContactEmail,
                    ContactPhone = excluded.ContactPhone,
                    Address = excluded.Address,
                    HidePoweredBy = excluded.HidePoweredBy,
                    UpdatedAt = excluded.UpdatedAt;";
            cmd.Parameters.AddWithValue("$tenantId", tenantId);
            cmd.Parameters.AddWithValue("$displayName", displayName);
            cmd.Parameters.AddWithValue("$logoBytes", (object?)logoBytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$logoVersion", logoVersion);
            cmd.Parameters.AddWithValue("$accentColor", (object?)accentColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$contactEmail", (object?)contactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$contactPhone", (object?)contactPhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$address", (object?)address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hidePoweredBy", hidePoweredBy ? 1 : 0);
            cmd.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        public CachedTenantBrandingRecord? GetTenantBranding(int tenantId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TenantId, DisplayName, LogoBytes, LogoVersion, AccentColor, ContactEmail, ContactPhone, Address, HidePoweredBy, UpdatedAt
                                 FROM CachedTenantBranding WHERE TenantId = $tenantId;";
            cmd.Parameters.AddWithValue("$tenantId", tenantId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            byte[]? logoBytes = reader.IsDBNull(2) ? null : (byte[])reader["LogoBytes"];

            return new CachedTenantBrandingRecord(
                TenantId: reader.GetInt32(0),
                DisplayName: reader.GetString(1),
                LogoBytes: logoBytes,
                LogoVersion: reader.GetInt32(3),
                AccentColor: reader.IsDBNull(4) ? null : reader.GetString(4),
                ContactEmail: reader.IsDBNull(5) ? null : reader.GetString(5),
                ContactPhone: reader.IsDBNull(6) ? null : reader.GetString(6),
                Address: reader.IsDBNull(7) ? null : reader.GetString(7),
                HidePoweredBy: reader.GetInt32(8) == 1,
                UpdatedAt: DateTime.Parse(reader.GetString(9))
            );
        }

        public void SetLastAuthenticatedTenantId(int tenantId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DeviceState (Key, Value)
                VALUES ('LastAuthenticatedTenantId', $val)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            cmd.Parameters.AddWithValue("$val", tenantId.ToString());
            cmd.ExecuteNonQuery();
        }

        public int? GetLastAuthenticatedTenantId()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Value FROM DeviceState WHERE Key = 'LastAuthenticatedTenantId';";
            var val = cmd.ExecuteScalar()?.ToString();
            return int.TryParse(val, out int tid) ? tid : null;
        }

        public CachedTenantBrandingRecord? GetLastAuthenticatedTenantBranding()
        {
            int? tid = GetLastAuthenticatedTenantId();
            if (tid.HasValue && tid.Value > 0)
            {
                return GetTenantBranding(tid.Value);
            }
            return null;
        }
    }

    public record CachedLoginRecord(
        int TenantId,
        int UserId,
        string FullName,
        string Email,
        string PasswordHash,
        string RoleName,
        DateTime LastSyncedAt,
        int? BranchId = null,
        string? BranchName = null
    );

    public record CachedTenantBrandingRecord(
        int TenantId,
        string DisplayName,
        byte[]? LogoBytes,
        int LogoVersion,
        string? AccentColor,
        string? ContactEmail,
        string? ContactPhone,
        string? Address,
        bool HidePoweredBy,
        DateTime UpdatedAt
    );
}
