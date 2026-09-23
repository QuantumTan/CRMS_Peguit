using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Secure database configuration provider.
    /// Reads credentials from environment variables (CRMS_CONNECTION, CRMS_CLOUD_CONNECTION)
    /// or from a local git-ignored db.local.json file, preventing hardcoded credentials in source control.
    /// </summary>
    public static class DbConfiguration
    {
        private const string DefaultLocalDbConnection =
            "Server=(localdb)\\mssqllocaldb;Database=CRMS_Local;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;MultipleActiveResultSets=True;";

        public static string GetLocalConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("CRMS_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim();
            }

            var fileSettings = LoadSettingsFromFile();
            if (!string.IsNullOrWhiteSpace(fileSettings?.LocalConnection))
            {
                return fileSettings.LocalConnection.Trim();
            }

            return DefaultLocalDbConnection;
        }

        public static string GetMasterConnectionString()
        {
            var baseConn = GetLocalConnectionString();
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConn)
                {
                    InitialCatalog = "CRMS_Master"
                };
                return builder.ConnectionString;
            }
            catch
            {
                return ReplaceDatabaseInConnectionString(baseConn, "CRMS_Master");
            }
        }

        public static string GetTenantConnectionString(int tenantId)
        {
            if (tenantId <= 0) tenantId = 1;
            string dbName = $"CRMS_Tenant_{tenantId}";
            var baseConn = GetLocalConnectionString();
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConn)
                {
                    InitialCatalog = dbName
                };
                return builder.ConnectionString;
            }
            catch
            {
                return ReplaceDatabaseInConnectionString(baseConn, dbName);
            }
        }

        private static string ReplaceDatabaseInConnectionString(string connectionString, string newDatabaseName)
        {
            var parts = connectionString.Split(';');
            var newParts = new List<string>();
            bool replaced = false;
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    newParts.Add($"Database={newDatabaseName}");
                    replaced = true;
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    newParts.Add(trimmed);
                }
            }
            if (!replaced)
            {
                newParts.Add($"Database={newDatabaseName}");
            }
            return string.Join(";", newParts) + ";";
        }

        public static string? GetCloudConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("CRMS_CLOUD_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim();
            }

            var fileSettings = LoadSettingsFromFile();
            if (!string.IsNullOrWhiteSpace(fileSettings?.CloudConnection))
            {
                return fileSettings.CloudConnection.Trim();
            }

            return null;
        }

        private static DbSettingsFile? LoadSettingsFromFile()
        {
            foreach (var path in GetCandidatePaths())
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<DbSettingsFile>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Ignore corrupted local file and continue fallback
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, "db.local.json");
            yield return Path.Combine(Environment.CurrentDirectory, "db.local.json");

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                yield return Path.Combine(dir.FullName, "db.local.json");
                dir = dir.Parent;
            }
        }

        private sealed class DbSettingsFile
        {
            public string? LocalConnection { get; set; }
            public string? CloudConnection { get; set; }
        }
    }
}
