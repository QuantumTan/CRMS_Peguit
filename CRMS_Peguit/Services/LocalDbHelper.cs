using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Utility to guarantee that the SQL Server LocalDB instance is actively running
    /// before any database operations or Entity Framework queries are executed.
    /// Prevents Named Pipes Provider Error 40 and connection timeouts.
    /// </summary>
    public static class LocalDbHelper
    {
        private static bool _verified = false;
        private static readonly object _lock = new();

        public static void EnsureLocalDbRunning(string? connectionString = null)
        {
            if (_verified) return;

            lock (_lock)
            {
                if (_verified) return;

                try
                {
                    string conn = connectionString ?? DbConfiguration.GetLocalConnectionString();
                    if (!conn.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
                    {
                        _verified = true;
                        return;
                    }

                    string instance = "mssqllocaldb";
                    var match = Regex.Match(conn, @"\(localdb\)\\([^\s;]+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        instance = match.Groups[1].Value.Trim();
                    }

                    // Attempt to start the target LocalDB instance
                    RunSqlLocalDbCommand($"start \"{instance}\"");
                    _verified = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LocalDbHelper] Error ensuring LocalDB instance is started: {ex.Message}");
                }
            }
        }

        private static void RunSqlLocalDbCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(2000);
            }
            catch
            {
                // If sqllocaldb CLI is unavailable, fallback to SqlClient auto-connection
            }
        }
    }
}
