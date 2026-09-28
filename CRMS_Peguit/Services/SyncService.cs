using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using CRMS_Peguit.infrastructure.Seeding;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Services.Offline;

namespace CRMS_Peguit.winforms.Models.Services
{
    public class SyncProgressEventArgs : EventArgs
    {
        public string Message { get; init; } = string.Empty;
        public int PendingCount { get; init; }
        public int FailedCount { get; init; }
        public int ConflictCount { get; init; }
        public int SyncedCount { get; init; }
        public bool IsRunning { get; init; }
    }

    /// <summary>
    /// NEXA Hybrid Unified Data Sync Engine.
    /// Performs direct local-to-cloud SQL synchronization (local → MonsterASP cloud) automatically
    /// in the background and on demand, while managing offline write queuing in local SQLite.
    /// </summary>
    public class SyncService : IDisposable
    {
        private static SyncService? _instance;
        private static readonly object _instanceLock = new();

        public static SyncService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SyncService();
                            _instance.Start(30);
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly string _localConnection;
        private readonly string _cloudConnection;
        private readonly HttpClient _httpClient;
        private readonly LocalDataCache _localCache;
        private readonly string _apiBaseUrl;
        private readonly string _logPath;
        private System.Threading.Timer? _timer;
        private bool _isSyncing;
        private static readonly SemaphoreSlim _syncSemaphore = new(1, 1);
        private static volatile bool _needsAnotherPass = false;
        private static DateTime _lastPeriodicMaintenanceUtc = DateTime.MinValue;
        private static DateTime _lastUserPushUtc = DateTime.MinValue;
        private int _baseIntervalSeconds;

        private static bool _cloudSchemaEnsured = false;
        private static int _consecutiveFailures = 0;
        public bool IsOnline { get; private set; } = true;
        public bool IsSyncing => _isSyncing;
        public bool IsDraining => _isSyncing;

        public event EventHandler<bool>? ConnectivityChanged;
        public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

        private static readonly HashSet<string> TablesWithIdentity = new(StringComparer.OrdinalIgnoreCase)
        {
            "Roles", "Users", "LoginSessions", "Customers", "Properties", "Leads", "Deals",
            "Activities", "PropertyShowingDetails", "SupportTickets", "Subscriptions",
            "SystemSettings", "BackupLogs", "DealContingencies", "DealClauses"
        };

        public SyncService(string? apiBaseUrl = null)
        {
            _localConnection = DbConfiguration.GetLocalConnectionString();
            _cloudConnection = DbConfiguration.GetCloudConnectionString() ?? string.Empty;

            var baseUrl = apiBaseUrl ?? DbConfiguration.GetApiBaseUrl();
            _apiBaseUrl = baseUrl.TrimEnd('/') + "/";
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
                BaseAddress = new Uri(_apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(6)
            };
            _localCache = LocalDataCache.Instance;
            _logPath = Path.Combine(AppContext.BaseDirectory, "sync-log.txt");
        }

        public SyncService(string? localConnection, string? cloudConnection, string? apiBaseUrl = null)
            : this(apiBaseUrl ?? DbConfiguration.GetApiBaseUrl())
        {
            if (!string.IsNullOrWhiteSpace(localConnection)) _localConnection = localConnection;
            if (!string.IsNullOrWhiteSpace(cloudConnection)) _cloudConnection = cloudConnection;
        }

        public void Start(int intervalSeconds = 30)
        {
            _baseIntervalSeconds = intervalSeconds;
            Log($"SyncService started. Background interval: {intervalSeconds}s");

            _timer?.Dispose();
            _timer = new System.Threading.Timer(
                async _ =>
                {
                    try
                    {
                        await SyncAsync();
                    }
                    catch (Exception ex)
                    {
                        Log($"[Auto-Sync Timer Error] {ex.Message}");
                    }
                },
                null,
                TimeSpan.FromSeconds(3), // Initial check 3s after startup
                TimeSpan.FromSeconds(intervalSeconds));
        }

        public async Task<bool> CheckConnectivityAsync()
        {
            bool hasNetwork = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!hasNetwork)
            {
                SetOnlineStatus(false, "No active network interface");
                return false;
            }

            // 1. Fast HTTP API ping first (lightweight 3-second timeout)
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.GetAsync("api/auth/ping", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    _consecutiveFailures = 0;
                    SetOnlineStatus(true);
                    return true;
                }
            }
            catch
            {
                // Fall through to cloud database check
            }

            // 2. Direct Cloud SQL connection test with 10-second timeout
            if (!string.IsNullOrWhiteSpace(_cloudConnection))
            {
                try
                {
                    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_cloudConnection)
                    {
                        ConnectTimeout = 10
                    };
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
                    await conn.OpenAsync(cts.Token);
                    _consecutiveFailures = 0;
                    SetOnlineStatus(true);
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"[Cloud Connectivity Ping Warning] {ex.Message}");
                }
            }

            _consecutiveFailures++;
            // If device has network connection, do not falsely label offline on a single transient hiccup
            if (_consecutiveFailures >= 2)
            {
                SetOnlineStatus(false, "Cloud server unreachable");
                return false;
            }

            return IsOnline;
        }

        private void SetOnlineStatus(bool online, string? reason = null)
        {
            bool wasOnline = IsOnline;
            IsOnline = online;
            if (wasOnline != online)
            {
                ConnectivityChanged?.Invoke(this, online);
                if (!online)
                {
                    Log($"Network status: OFFLINE ({reason ?? "Unreachable"})");
                }
                else
                {
                    Log("Network status: ONLINE (Connected)");
                }
            }
        }

        /// <summary>
        /// Main synchronization trigger — runs automatically in background and on demand via 'Sync Now'.
        /// Pushes local CRM data directly to cloud SQL Server, drains offline SQLite queue, and refreshes mirror cache.
        /// Strictly isolated to the current tenant session.
        /// </summary>
        public async Task SyncAsync(bool waitIfBusy = false, bool isFullSync = false)
        {
            if (waitIfBusy)
            {
                await _syncSemaphore.WaitAsync();
            }
            else
            {
                if (!await _syncSemaphore.WaitAsync(0))
                {
                    _needsAnotherPass = true;
                    return;
                }
            }
            _isSyncing = true;

            try
            {
                int activeTenant = CurrentSession.TenantId;
                if (activeTenant <= 0) activeTenant = 1;

                NotifyProgress("Checking cloud database connection...", isRunning: true);

                bool online = await CheckConnectivityAsync();
                if (!online)
                {
                    NotifyProgress("Cloud database offline / unreachable.", isRunning: false);
                    return;
                }

                Log($"Sync started for Tenant {activeTenant} (local → cloud)...");
                NotifyProgress("Synchronizing data to cloud...", isRunning: true);

                // 1. Drain all pending SQLite offline queue items directly to MonsterASP Cloud DB IMMEDIATELY
                do
                {
                    _needsAnotherPass = false;
                    await DrainQueueAsync();

                    var currentCounts = _localCache.GetQueueCounts(activeTenant);
                    if (currentCounts.Pending > 0)
                    {
                        _needsAnotherPass = true;
                    }
                } while (_needsAnotherPass);

                // 2. Automatically push local users & roles to cloud across all active local tenants
                // Runs on full sync (e.g. user added/updated, sync now, or periodic maintenance every 5 min)
                bool shouldPushUsers = isFullSync || (DateTime.UtcNow - _lastUserPushUtc > TimeSpan.FromMinutes(5));
                if (shouldPushUsers && !string.IsNullOrWhiteSpace(_cloudConnection))
                {
                    _lastUserPushUtc = DateTime.UtcNow;
                    try
                    {
                        using var cloudConn = new SqlConnection(_cloudConnection);
                        await cloudConn.OpenAsync();
                        await PushAllTenantsUsersAndRolesToCloudAsync(cloudConn, activeTenant);
                    }
                    catch (Exception ex)
                    {
                        Log($"User push error: {ex.Message}");
                    }
                }

                // 2. Full synchronization maintenance (schema verify, user/roles pull)
                // ONLY runs when explicitly requested (e.g. 'Sync Now' button or periodic maintenance)
                if (isFullSync && !string.IsNullOrWhiteSpace(_cloudConnection))
                {
                    _lastPeriodicMaintenanceUtc = DateTime.UtcNow;

                    if (!_cloudSchemaEnsured)
                    {
                        try
                        {
                            await using var testCloud = CreateCloudContext(activeTenant);
                            SchemaRepairService.EnsureCrmPolishColumns(testCloud);
                            _cloudSchemaEnsured = true;
                        }
                        catch (Exception ex)
                        {
                            Log($"Cloud schema check skipped: {ex.Message}");
                        }
                    }

                    try
                    {
                        await using var local = LocalDb.CreateContext(activeTenant);
                        await using var cloud = CreateCloudContext(activeTenant);

                        // Pull remotely registered users and roles for this tenant only
                        await PullMissingUsersAndRoles(local, cloud, activeTenant);
                    }
                    catch (Exception ex)
                    {
                        Log($"[Tenant {activeTenant}] Users/Roles sync notice: {ex.Message}");
                    }
                }

                // If during full maintenance new mutations were enqueued, drain them immediately!
                while (_localCache.GetQueueCounts(activeTenant).Pending > 0)
                {
                    _needsAnotherPass = false;
                    await DrainQueueAsync();
                }

                // 3. Fast local mirror refresh
                await RefreshUserCacheAsync(activeTenant, CurrentSession.UserId, CurrentSession.CurrentUser?.Role.ToString() ?? "Admin");

                // Final check: if anything was enqueued while refreshing cache, drain it immediately
                while (_localCache.GetQueueCounts(activeTenant).Pending > 0)
                {
                    _needsAnotherPass = false;
                    await DrainQueueAsync();
                }

                var counts = _localCache.GetQueueCounts(activeTenant);
                if (counts.Pending == 0 && counts.Failed == 0)
                {
                    Log($"Sync completed successfully at {DateTime.Now:HH:mm:ss}. Cloud database is up to date.");
                    NotifyProgress("Cloud database is up to date.", isRunning: false);
                }
                else
                {
                    Log($"Sync completed with {counts.Pending} pending, {counts.Failed} failed item(s).");
                    NotifyProgress($"Sync finished ({counts.Failed} failed).", isRunning: false);
                }
            }
            catch (Exception ex)
            {
                Log($"SYNC FAILED: {ex.Message}");
                if (ex.InnerException != null)
                    Log($"INNER: {ex.InnerException.Message}");
                NotifyProgress($"Sync failed: {ex.Message}", isRunning: false);
            }
            finally
            {
                _isSyncing = false;
                _syncSemaphore.Release();

                // If another pass was requested while releasing, schedule an immediate pass
                if (_needsAnotherPass)
                {
                    _ = Task.Run(() => SyncAsync(waitIfBusy: false, isFullSync: false));
                }
            }
        }

        private RealEstateDbContext CreateCloudContext(int tenantId)
        {
            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(_cloudConnection, sql => sql.CommandTimeout(60).EnableRetryOnFailure(3))
                .Options;
            return new RealEstateDbContext(options, tenantId);
        }

        private static async Task<List<T>> GetCloudTenantRowsAsync<T>(RealEstateDbContext cloud, int tenantId) where T : class
        {
            if (typeof(T) == typeof(Role))
            {
                var rows = await cloud.Roles.IgnoreQueryFilters().AsNoTracking().Where(r => r.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(User))
            {
                var rows = await cloud.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.Role != null && u.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Customer))
            {
                var rows = await cloud.Customers.IgnoreQueryFilters().AsNoTracking().Where(c => c.CreatedByUser != null && c.CreatedByUser.Role != null && c.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(BuyerProfile))
            {
                var rows = await cloud.BuyerProfiles.IgnoreQueryFilters().AsNoTracking().Where(bp => bp.Customer != null && bp.Customer.CreatedByUser != null && bp.Customer.CreatedByUser.Role != null && bp.Customer.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Branch))
            {
                var rows = await cloud.Branches.IgnoreQueryFilters().AsNoTracking().Where(b => b.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Property))
            {
                var rows = await cloud.Properties.IgnoreQueryFilters().AsNoTracking().Where(p => p.CreatedByUser != null && p.CreatedByUser.Role != null && p.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Lead))
            {
                var rows = await cloud.Leads.IgnoreQueryFilters().AsNoTracking().Where(l => l.CreatedByUser != null && l.CreatedByUser.Role != null && l.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Deal))
            {
                var rows = await cloud.Deals.IgnoreQueryFilters().AsNoTracking().Where(d => d.CreatedByUser != null && d.CreatedByUser.Role != null && d.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(DealContingency))
            {
                var rows = await cloud.DealContingencies.IgnoreQueryFilters().AsNoTracking().Where(dc => dc.Deal != null && dc.Deal.CreatedByUser != null && dc.Deal.CreatedByUser.Role != null && dc.Deal.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(DealClause))
            {
                var rows = await cloud.DealClauses.IgnoreQueryFilters().AsNoTracking().Where(dc => dc.Deal != null && dc.Deal.CreatedByUser != null && dc.Deal.CreatedByUser.Role != null && dc.Deal.CreatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Activity))
            {
                var rows = await cloud.Activities.IgnoreQueryFilters().AsNoTracking().Where(a => a.LoggedByAgent != null && a.LoggedByAgent.Role != null && a.LoggedByAgent.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(PropertyShowingDetail))
            {
                var rows = await cloud.PropertyShowingDetails.IgnoreQueryFilters().AsNoTracking().Where(psd => psd.Activity != null && psd.Activity.LoggedByAgent != null && psd.Activity.LoggedByAgent.Role != null && psd.Activity.LoggedByAgent.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(Campaign))
            {
                var rows = await cloud.Campaigns.IgnoreQueryFilters().AsNoTracking().Where(c => c.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(TaskReminder))
            {
                var rows = await cloud.TaskReminders.IgnoreQueryFilters().AsNoTracking().Where(tr => tr.AssignedToUser != null && tr.AssignedToUser.Role != null && tr.AssignedToUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(SupportTicket))
            {
                var rows = await cloud.SupportTickets.IgnoreQueryFilters().AsNoTracking().Where(st => st.RaisedByUser != null && st.RaisedByUser.Role != null && st.RaisedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(TicketComment))
            {
                var rows = await cloud.TicketComments.IgnoreQueryFilters().AsNoTracking().Where(tc => tc.Ticket != null && tc.Ticket.RaisedByUser != null && tc.Ticket.RaisedByUser.Role != null && tc.Ticket.RaisedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(SystemSetting))
            {
                var rows = await cloud.SystemSettings.IgnoreQueryFilters().AsNoTracking().Where(ss => ss.UpdatedByUser != null && ss.UpdatedByUser.Role != null && ss.UpdatedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(BackupLog))
            {
                var rows = await cloud.BackupLogs.IgnoreQueryFilters().AsNoTracking().Where(bl => bl.PerformedByUser != null && bl.PerformedByUser.Role != null && bl.PerformedByUser.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }
            if (typeof(T) == typeof(LoginSession))
            {
                var rows = await cloud.LoginSessions.IgnoreQueryFilters().AsNoTracking().Where(ls => ls.User != null && ls.User.Role != null && ls.User.Role.TenantId == tenantId).ToListAsync();
                return (rows as List<T>)!;
            }

            return await cloud.Set<T>().AsNoTracking().ToListAsync();
        }

        private async Task<int> SyncTable<T>(
            RealEstateDbContext local,
            RealEstateDbContext cloud,
            int tenantId,
            string tableName,
            List<string> details)
            where T : class
        {
            local.ChangeTracker.Clear();
            cloud.ChangeTracker.Clear();

            var localRows = await local.Set<T>().IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var cloudRows = await GetCloudTenantRowsAsync<T>(cloud, tenantId);

            if (localRows.Count == 0 && cloudRows.Count == 0) return 0;

            var entityType = local.Model.FindEntityType(typeof(T)) ?? cloud.Model.FindEntityType(typeof(T));
            var primaryKey = entityType?.FindPrimaryKey();
            var keyProp = primaryKey?.Properties[0].PropertyInfo;
            if (keyProp == null) return 0;

            var localMap = localRows.ToDictionary(r => keyProp.GetValue(r)!, r => r);
            var cloudMap = cloudRows.ToDictionary(r => keyProp.GetValue(r)!, r => r);

            var cloudToInsert = new List<T>();
            var cloudToUpdate = new List<T>();
            var localToInsert = new List<T>();
            var localToUpdate = new List<T>();

            // 1. PULL (Cloud -> Local): if record exists on cloud but missing on local, pull it down!
            foreach (var cloudRow in cloudRows)
            {
                var keyVal = keyProp.GetValue(cloudRow)!;
                if (!localMap.ContainsKey(keyVal))
                {
                    localToInsert.Add(cloudRow);
                }
            }

            // 2. PUSH (Local -> Cloud): if record exists on local but missing on cloud, push it up!
            foreach (var localRow in localRows)
            {
                var keyVal = keyProp.GetValue(localRow)!;
                if (!cloudMap.TryGetValue(keyVal, out var existingCloud))
                {
                    cloudToInsert.Add(localRow);
                }
                else
                {
                    // Both have it: check if values differ
                    bool isDifferent = false;
                    foreach (var prop in entityType!.GetProperties())
                    {
                        var pInfo = prop.PropertyInfo;
                        if (pInfo == null) continue;
                        var localVal = pInfo.GetValue(localRow);
                        var cloudVal = pInfo.GetValue(existingCloud);

                        if (localVal is DateTime dt1 && cloudVal is DateTime dt2)
                        {
                            if (Math.Abs((dt1 - dt2).TotalSeconds) > 1)
                            {
                                isDifferent = true;
                                break;
                            }
                            continue;
                        }

                        if (!Equals(localVal, cloudVal))
                        {
                            isDifferent = true;
                            break;
                        }
                    }

                    if (isDifferent)
                    {
                        // Operational local changes take priority for synchronization
                        cloudToUpdate.Add(localRow);
                    }
                }
            }

            bool hasIdentity = TablesWithIdentity.Contains(tableName);

            // Execute local changes (PULL)
            if (localToInsert.Count > 0 || localToUpdate.Count > 0)
            {
                try
                {
                    await SaveWithIdentityInsertAsync(local, localToInsert, localToUpdate, tableName, hasIdentity);
                }
                catch (Exception ex)
                {
                    Log($"[Pull to Local Error] {tableName}: {ex.Message}");
                }
            }

            // Execute cloud changes (PUSH)
            if (cloudToInsert.Count > 0 || cloudToUpdate.Count > 0)
            {
                try
                {
                    await SaveWithIdentityInsertAsync(cloud, cloudToInsert, cloudToUpdate, tableName, hasIdentity);
                }
                catch (Exception ex)
                {
                    Log($"[Push to Cloud Error] {tableName}: {ex.Message}");
                }
            }

            int totalChanges = localToInsert.Count + localToUpdate.Count + cloudToInsert.Count + cloudToUpdate.Count;
            if (totalChanges > 0)
            {
                var parts = new List<string>();
                if (localToInsert.Count > 0) parts.Add($"↓{localToInsert.Count} pull");
                if (cloudToInsert.Count > 0) parts.Add($"↑{cloudToInsert.Count} push");
                if (cloudToUpdate.Count > 0) parts.Add($"~{cloudToUpdate.Count} upd");
                details.Add($"{tableName}: {string.Join(", ", parts)}");
            }
            return totalChanges;
        }

        private async Task SaveWithIdentityInsertAsync<T>(
            RealEstateDbContext db,
            List<T> toInsert,
            List<T> toUpdate,
            string tableName,
            bool hasIdentity)
            where T : class
        {
            if (toInsert.Count == 0 && toUpdate.Count == 0) return;

            db.ChangeTracker.Clear();
            var dbSet = db.Set<T>();

            foreach (var item in toUpdate)
            {
                dbSet.Attach(item);
                db.Entry(item).State = EntityState.Modified;
            }

            foreach (var item in toInsert)
            {
                dbSet.Add(item);
            }

            var strategy = db.Database.CreateExecutionStrategy();
            if (toInsert.Count > 0 && hasIdentity)
            {
                await strategy.ExecuteAsync(async () =>
                {
                    var safeTableName = tableName.Replace("]", "]]");
                    await using var tx = await db.Database.BeginTransactionAsync();

#pragma warning disable EF1002
                    await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{safeTableName}] ON;");
                    await db.SaveChangesAsync();
                    await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{safeTableName}] OFF;");
#pragma warning restore EF1002

                    await tx.CommitAsync();
                });
            }
            else
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await db.SaveChangesAsync();
                });
            }

            db.ChangeTracker.Clear();
        }

        private class CloudUserSummary
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public int RoleId { get; set; }
            public int? TenantId { get; set; }
            public string? RoleName { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Phone { get; set; }
            public int? BranchId { get; set; }
        }

        public async Task PushAllTenantsUsersAndRolesToCloudAsync(SqlConnection cloudConn, int currentTenantId = 0)
        {
            try
            {
                var tenantsToSync = new HashSet<int> { 1, 2, 3 };
                if (currentTenantId > 0) tenantsToSync.Add(currentTenantId);

                try
                {
                    using var master = LocalDb.CreateMasterContext();
                    var dbTenants = await master.Companies.Where(c => c.IsActive).Select(c => c.CompanyId).ToListAsync();
                    foreach (var tid in dbTenants) tenantsToSync.Add(tid);
                }
                catch { }

                // 1. Fetch all cloud roles in a single round-trip
                var cloudRoles = new Dictionary<(int TenantId, string RoleName), int>();
                using (var getRolesCmd = cloudConn.CreateCommand())
                {
                    getRolesCmd.CommandText = "SELECT RoleId, TenantId, RoleName FROM Roles";
                    using var rdr = await getRolesCmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        int rid = rdr.GetInt32(0);
                        int tid = rdr.GetInt32(1);
                        string rn = rdr.GetString(2).Trim();
                        cloudRoles[(tid, rn.ToLowerInvariant())] = rid;
                    }
                }

                // 2. Fetch all cloud users in a single round-trip
                var cloudUsersByEmail = new Dictionary<string, List<CloudUserSummary>>(StringComparer.OrdinalIgnoreCase);
                using (var getUsersCmd = cloudConn.CreateCommand())
                {
                    getUsersCmd.CommandText = @"
                        SELECT u.UserId, u.Email, u.RoleId, r.TenantId, r.RoleName, u.FirstName, u.LastName, u.Status, u.Phone, u.BranchId
                        FROM Users u
                        LEFT JOIN Roles r ON u.RoleId = r.RoleId";
                    using var rdr = await getUsersCmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        string email = rdr.IsDBNull(1) ? "" : rdr.GetString(1).Trim();
                        if (string.IsNullOrWhiteSpace(email)) continue;

                        var u = new CloudUserSummary
                        {
                            UserId = rdr.GetInt32(0),
                            Email = email,
                            RoleId = rdr.GetInt32(2),
                            TenantId = rdr.IsDBNull(3) ? null : rdr.GetInt32(3),
                            RoleName = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                            FirstName = rdr.IsDBNull(5) ? "" : rdr.GetString(5),
                            LastName = rdr.IsDBNull(6) ? "" : rdr.GetString(6),
                            Status = rdr.IsDBNull(7) ? "" : rdr.GetString(7),
                            Phone = rdr.IsDBNull(8) ? null : rdr.GetString(8),
                            BranchId = rdr.IsDBNull(9) ? null : rdr.GetInt32(9)
                        };

                        if (!cloudUsersByEmail.TryGetValue(email, out var list))
                        {
                            list = new List<CloudUserSummary>();
                            cloudUsersByEmail[email] = list;
                        }
                        list.Add(u);
                    }
                }

                foreach (var tid in tenantsToSync)
                {
                    try
                    {
                        using var local = LocalDb.CreateContext(tid);
                        await PushLocalUsersAndRolesToCloudOptimizedAsync(cloudConn, local, tid, cloudRoles, cloudUsersByEmail);
                    }
                    catch (Exception ex)
                    {
                        Log($"[Tenant {tid}] PushLocalUsersAndRolesToCloud error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"PushAllTenantsUsersAndRolesToCloudAsync error: {ex.Message}");
            }
        }

        public async Task PushLocalUsersAndRolesToCloudAsync(SqlConnection cloudConn, RealEstateDbContext local, int tenantId)
        {
            await PushAllTenantsUsersAndRolesToCloudAsync(cloudConn, tenantId);
        }

        private async Task PushLocalUsersAndRolesToCloudOptimizedAsync(
            SqlConnection cloudConn,
            RealEstateDbContext local,
            int tenantId,
            Dictionary<(int TenantId, string RoleName), int> cloudRoles,
            Dictionary<string, List<CloudUserSummary>> cloudUsersByEmail)
        {
            var localRoles = await local.Roles.IgnoreQueryFilters().AsNoTracking().ToListAsync();
            var roleMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var lr in localRoles)
            {
                string rName = lr.RoleName.Trim();
                if (!cloudRoles.TryGetValue((tenantId, rName.ToLowerInvariant()), out int roleId))
                {
                    using var insRole = cloudConn.CreateCommand();
                    insRole.CommandText = "INSERT INTO Roles (TenantId, RoleName) OUTPUT INSERTED.RoleId VALUES (@Tid, @Name)";
                    insRole.Parameters.AddWithValue("@Tid", tenantId);
                    insRole.Parameters.AddWithValue("@Name", rName);
                    roleId = Convert.ToInt32(await insRole.ExecuteScalarAsync());
                    cloudRoles[(tenantId, rName.ToLowerInvariant())] = roleId;
                    Log($"[Tenant {tenantId}] Created cloud role '{rName}' (RoleId: {roleId}).");
                }
                roleMap[rName] = roleId;
            }

            var localUsers = await local.Users.IgnoreQueryFilters()
                .Include(u => u.Role)
                .AsNoTracking()
                .ToListAsync();

            foreach (var lu in localUsers)
            {
                string email = lu.Email?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(email)) continue;

                string roleName = lu.Role?.RoleName?.Trim() ?? "Agent";
                if (!roleMap.TryGetValue(roleName, out int targetRoleId))
                {
                    targetRoleId = roleMap.Values.FirstOrDefault();
                }
                if (targetRoleId <= 0) continue;

                if (cloudUsersByEmail.TryGetValue(email, out var existingMatches) && existingMatches.Count > 0)
                {
                    var canonical = existingMatches.OrderBy(m => m.TenantId == tenantId ? 0 : 1).ThenBy(m => m.UserId).First();

                    bool isIdentical =
                        string.Equals(canonical.FirstName, lu.FirstName ?? "User", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(canonical.LastName, lu.LastName ?? "Member", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(canonical.Status, lu.Status ?? "active", StringComparison.OrdinalIgnoreCase) &&
                        canonical.RoleId == targetRoleId &&
                        canonical.BranchId == lu.BranchId &&
                        string.Equals(canonical.Phone ?? "", lu.Phone ?? "", StringComparison.OrdinalIgnoreCase);

                    if (!isIdentical)
                    {
                        using var upd = cloudConn.CreateCommand();
                        upd.CommandText = @"
                            UPDATE Users SET 
                                FirstName = @First, 
                                MiddleName = @Middle, 
                                LastName = @Last, 
                                Suffix = @Suffix, 
                                Email = @Email, 
                                Phone = @Phone, 
                                RoleId = @RoleId, 
                                Status = @Status, 
                                BranchId = @BranchId 
                            WHERE UserId = @Uid";
                        upd.Parameters.AddWithValue("@First", lu.FirstName ?? "User");
                        upd.Parameters.AddWithValue("@Middle", (object?)lu.MiddleName ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@Last", lu.LastName ?? "Member");
                        upd.Parameters.AddWithValue("@Suffix", (object?)lu.Suffix ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@Email", email);
                        upd.Parameters.AddWithValue("@Phone", (object?)lu.Phone ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@RoleId", targetRoleId);
                        upd.Parameters.AddWithValue("@Status", lu.Status ?? "active");
                        upd.Parameters.AddWithValue("@BranchId", (object?)lu.BranchId ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@Uid", canonical.UserId);
                        await upd.ExecuteNonQueryAsync();

                        canonical.FirstName = lu.FirstName ?? "User";
                        canonical.LastName = lu.LastName ?? "Member";
                        canonical.RoleId = targetRoleId;
                        canonical.Status = lu.Status ?? "active";
                        canonical.Phone = lu.Phone;
                        canonical.BranchId = lu.BranchId;
                    }

                    // Deduplicate any secondary duplicate rows
                    for (int i = 1; i < existingMatches.Count; i++)
                    {
                        int dupUid = existingMatches[i].UserId;
                        try
                        {
                            using var rebrandCmd = cloudConn.CreateCommand();
                            rebrandCmd.CommandText = $@"
                                DELETE FROM NotificationPreferences WHERE UserId = {dupUid};
                                DELETE FROM LoginSessions WHERE UserId = {dupUid};
                                UPDATE Activities SET LoggedByAgentId = {canonical.UserId} WHERE LoggedByAgentId = {dupUid};
                                UPDATE Deals SET AgentId = {canonical.UserId} WHERE AgentId = {dupUid};
                                UPDATE Deals SET CreatedByUserId = {canonical.UserId} WHERE CreatedByUserId = {dupUid};
                                UPDATE Leads SET AssignedAgentId = {canonical.UserId} WHERE AssignedAgentId = {dupUid};
                                UPDATE Leads SET CreatedByUserId = {canonical.UserId} WHERE CreatedByUserId = {dupUid};
                                UPDATE Customers SET AssignedAgentId = {canonical.UserId} WHERE AssignedAgentId = {dupUid};
                                UPDATE Customers SET CreatedByUserId = {canonical.UserId} WHERE CreatedByUserId = {dupUid};
                                UPDATE Properties SET ListedByAgentId = {canonical.UserId} WHERE ListedByAgentId = {dupUid};
                                UPDATE Properties SET CreatedByUserId = {canonical.UserId} WHERE CreatedByUserId = {dupUid};
                                UPDATE TaskReminders SET AssignedToUserId = {canonical.UserId} WHERE AssignedToUserId = {dupUid};
                                UPDATE SupportTickets SET AssignedToUserId = {canonical.UserId} WHERE AssignedToUserId = {dupUid};
                                UPDATE SupportTickets SET RaisedByUserId = {canonical.UserId} WHERE RaisedByUserId = {dupUid};
                                UPDATE TicketComments SET AuthorUserId = {canonical.UserId} WHERE AuthorUserId = {dupUid};
                                UPDATE Notifications SET RecipientUserId = {canonical.UserId} WHERE RecipientUserId = {dupUid};
                                UPDATE SystemSettings SET UpdatedByUserId = {canonical.UserId} WHERE UpdatedByUserId = {dupUid};
                                UPDATE BackupLogs SET PerformedByUserId = {canonical.UserId} WHERE PerformedByUserId = {dupUid};
                                DELETE FROM Users WHERE UserId = {dupUid};";
                            await rebrandCmd.ExecuteNonQueryAsync();
                        }
                        catch { }
                    }
                    existingMatches.RemoveAll(m => m.UserId != canonical.UserId);
                }
                else
                {
                    using var ins = cloudConn.CreateCommand();
                    ins.CommandText = @"
                        INSERT INTO Users (FirstName, MiddleName, LastName, Suffix, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, BranchId)
                        OUTPUT INSERTED.UserId
                        VALUES (@First, @Middle, @Last, @Suffix, @Email, @Phone, @Pass, @RoleId, @Status, GETUTCDATE(), @BranchId)";
                    ins.Parameters.AddWithValue("@First", lu.FirstName ?? "User");
                    ins.Parameters.AddWithValue("@Middle", (object?)lu.MiddleName ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Last", lu.LastName ?? "Member");
                    ins.Parameters.AddWithValue("@Suffix", (object?)lu.Suffix ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Email", email);
                    ins.Parameters.AddWithValue("@Phone", (object?)lu.Phone ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Pass", !string.IsNullOrWhiteSpace(lu.PasswordHash) ? lu.PasswordHash : PasswordHasher.Hash("P@ssword123!"));
                    ins.Parameters.AddWithValue("@RoleId", targetRoleId);
                    ins.Parameters.AddWithValue("@Status", lu.Status ?? "active");
                    ins.Parameters.AddWithValue("@BranchId", (object?)lu.BranchId ?? DBNull.Value);
                    int newUid = Convert.ToInt32(await ins.ExecuteScalarAsync());

                    var newSummary = new CloudUserSummary
                    {
                        UserId = newUid,
                        Email = email,
                        RoleId = targetRoleId,
                        TenantId = tenantId,
                        RoleName = roleName,
                        FirstName = lu.FirstName ?? "User",
                        LastName = lu.LastName ?? "Member",
                        Status = lu.Status ?? "active",
                        Phone = lu.Phone,
                        BranchId = lu.BranchId
                    };
                    cloudUsersByEmail[email] = new List<CloudUserSummary> { newSummary };
                    Log($"[Tenant {tenantId}] Inserted cloud user '{email}' as {roleName} (UserId: {newUid}).");
                }
            }
        }

        private async Task PullMissingUsersAndRoles(RealEstateDbContext local, RealEstateDbContext cloud, int tenantId)
        {
            try
            {
                var localRoles = await local.Roles.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                var cloudRoles = await cloud.Roles.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                var missingRoles = cloudRoles.Where(cr => cr.TenantId == tenantId && !localRoles.Any(lr => lr.RoleId == cr.RoleId || lr.RoleName.ToLower() == cr.RoleName.ToLower())).ToList();

                foreach (var role in missingRoles)
                {
                    await local.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = {0} OR RoleName = {2})
                        BEGIN
                            SET IDENTITY_INSERT Roles ON;
                            INSERT INTO Roles (RoleId, TenantId, RoleName) VALUES ({0}, {1}, {2});
                            SET IDENTITY_INSERT Roles OFF;
                        END",
                        role.RoleId, role.TenantId, role.RoleName);
                }

                var localUsers = await local.Users.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                var cloudUsers = await cloud.Users.IgnoreQueryFilters().Include(u => u.Role).AsNoTracking().ToListAsync();
                var missingUsers = cloudUsers.Where(cu => cu.Role?.TenantId == tenantId && !localUsers.Any(lu => lu.UserId == cu.UserId || (!string.IsNullOrEmpty(lu.Email) && !string.IsNullOrEmpty(cu.Email) && string.Equals(lu.Email, cu.Email, StringComparison.OrdinalIgnoreCase)))).ToList();

                foreach (var user in missingUsers)
                {
                    string email = user.Email ?? "";
                    if (string.IsNullOrWhiteSpace(email)) continue;

                    await local.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = {0})
                        BEGIN
                            SET IDENTITY_INSERT Users ON;
                            INSERT INTO Users (UserId, FirstName, MiddleName, LastName, Suffix, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, BranchId)
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11});
                            SET IDENTITY_INSERT Users OFF;
                        END",
                        user.UserId,
                        user.FirstName ?? "User",
                        (object?)user.MiddleName ?? DBNull.Value,
                        user.LastName ?? "",
                        (object?)user.Suffix ?? DBNull.Value,
                        email,
                        (object?)user.Phone ?? DBNull.Value,
                        user.PasswordHash ?? "",
                        user.RoleId,
                        user.Status ?? "active",
                        user.CreatedAt,
                        (object?)user.BranchId ?? DBNull.Value);
                }
            }
            catch (Exception ex)
            {
                Log($"PullMissingUsersAndRoles warning: {ex.Message}");
            }
        }

        // ==========================================
        // OFFLINE QUEUE MANAGEMENT (SQLite)
        // ==========================================

        public PendingSyncQueue EnqueueOfflineCreate<T>(string entityType, T entity, int tenantId, int userId, string? localId = null)
        {
            string json = JsonSerializer.Serialize(entity, new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles });
            var item = new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = userId,
                EntityType = entityType,
                Operation = "Insert",
                PayloadJson = json,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                AttemptCount = 0
            };

            int queueId = _localCache.EnqueueItem(item);
            item.QueueId = queueId;
            item.EntityLocalId = !string.IsNullOrEmpty(localId) ? localId : (-queueId).ToString();
            _localCache.UpdateEntityLocalId(queueId, item.EntityLocalId);

            Log($"Queued INSERT for {entityType} [LocalId: {item.EntityLocalId}] by User #{userId}");
            NotifyProgress($"Queued {entityType} creation for sync.");
            return item;
        }

        public PendingSyncQueue EnqueueOfflineDelete(string entityType, int localId, int tenantId, int userId, int? serverId = null)
        {
            var item = new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = userId,
                EntityType = entityType,
                EntityLocalId = localId.ToString(),
                ServerEntityId = serverId,
                Operation = "Delete",
                PayloadJson = JsonSerializer.Serialize(new { Id = localId }),
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                AttemptCount = 0
            };

            int queueId = _localCache.EnqueueItem(item);
            item.QueueId = queueId;

            Log($"Queued DELETE for {entityType} #{localId} by User #{userId}");
            NotifyProgress($"Queued {entityType} deletion for sync.");
            return item;
        }

        public PendingSyncQueue EnqueueOfflineUpdate<T>(string entityType, int serverId, T entity, int tenantId, int userId, DateTime? cachedVersionTimestamp)
        {
            string json = JsonSerializer.Serialize(entity, new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles });
            var item = new PendingSyncQueue
            {
                TenantId = tenantId,
                UserId = userId,
                EntityType = entityType,
                ServerEntityId = serverId,
                Operation = "Update",
                PayloadJson = json,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                ServerVersionTimestamp = cachedVersionTimestamp,
                AttemptCount = 0
            };

            int queueId = _localCache.EnqueueItem(item);
            item.QueueId = queueId;
            item.EntityLocalId = serverId.ToString();
            _localCache.UpdateEntityLocalId(queueId, item.EntityLocalId);

            Log($"Queued UPDATE for {entityType} #{serverId} by User #{userId}");
            NotifyProgress($"Queued {entityType} update for sync.");
            return item;
        }

        public async Task EnqueueUnsyncedLocalRecordsAsync(int activeTenant, int currentUserId)
        {
            try
            {
                using var local = LocalDb.CreateContext(activeTenant);
                var allQueue = _localCache.GetAllQueueItems(activeTenant);
                var knownLocalIds = new HashSet<string>(
                    allQueue.Select(q => $"{q.EntityType}:{q.EntityLocalId}"),
                    StringComparer.OrdinalIgnoreCase
                );

                int userId = currentUserId > 0 ? currentUserId : 1;

                // Leads
                var localLeads = await local.Leads.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var lead in localLeads)
                {
                    if (!knownLocalIds.Contains($"Lead:{lead.LeadId}"))
                    {
                        EnqueueOfflineCreate("Lead", lead, activeTenant, userId, lead.LeadId.ToString());
                        knownLocalIds.Add($"Lead:{lead.LeadId}");
                    }
                }

                // Customers
                var localCustomers = await local.Customers.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var cust in localCustomers)
                {
                    if (!knownLocalIds.Contains($"Customer:{cust.CustomerId}"))
                    {
                        EnqueueOfflineCreate("Customer", cust, activeTenant, userId, cust.CustomerId.ToString());
                        knownLocalIds.Add($"Customer:{cust.CustomerId}");
                    }
                }

                // Properties
                var localProperties = await local.Properties.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var prop in localProperties)
                {
                    if (!knownLocalIds.Contains($"Property:{prop.PropertyId}"))
                    {
                        EnqueueOfflineCreate("Property", prop, activeTenant, userId, prop.PropertyId.ToString());
                        knownLocalIds.Add($"Property:{prop.PropertyId}");
                    }
                }

                // Deals
                var localDeals = await local.Deals.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var deal in localDeals)
                {
                    if (!knownLocalIds.Contains($"Deal:{deal.DealId}"))
                    {
                        EnqueueOfflineCreate("Deal", deal, activeTenant, userId, deal.DealId.ToString());
                        knownLocalIds.Add($"Deal:{deal.DealId}");
                    }
                }

                // Activities
                var localActivities = await local.Activities.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var act in localActivities)
                {
                    if (!knownLocalIds.Contains($"Activity:{act.ActivityId}"))
                    {
                        EnqueueOfflineCreate("Activity", act, activeTenant, userId, act.ActivityId.ToString());
                        knownLocalIds.Add($"Activity:{act.ActivityId}");
                    }
                }

                // TaskReminders
                var localReminders = await local.TaskReminders.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var rem in localReminders)
                {
                    if (!knownLocalIds.Contains($"TaskReminder:{rem.TaskReminderId}"))
                    {
                        EnqueueOfflineCreate("TaskReminder", rem, activeTenant, userId, rem.TaskReminderId.ToString());
                        knownLocalIds.Add($"TaskReminder:{rem.TaskReminderId}");
                    }
                }

                // SupportTickets
                var localTickets = await local.SupportTickets.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                foreach (var tck in localTickets)
                {
                    if (!knownLocalIds.Contains($"SupportTicket:{tck.TicketId}"))
                    {
                        EnqueueOfflineCreate("SupportTicket", tck, activeTenant, userId, tck.TicketId.ToString());
                        knownLocalIds.Add($"SupportTicket:{tck.TicketId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"EnqueueUnsyncedLocalRecordsAsync warning: {ex.Message}");
            }
        }

        public async Task DrainQueueAsync()
        {
            try
            {
                int tenantId = CurrentSession.TenantId;
                if (tenantId <= 0) return;
                var pendingItems = _localCache.GetPendingQueue(tenantId);
                if (pendingItems.Count == 0) return;

                Log($"Draining {pendingItems.Count} offline queued item(s) for Tenant {tenantId}...");

                if (string.IsNullOrWhiteSpace(_cloudConnection))
                {
                    Log("DrainQueueAsync skipped: No cloud connection configured.");
                    return;
                }

                using var cloudConn = new SqlConnection(_cloudConnection);
                await cloudConn.OpenAsync();
                using var local = LocalDb.CreateContext(tenantId);

                var tempIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                int processed = 0;
                foreach (var item in pendingItems)
                {
                    try
                    {
                        _localCache.UpdateQueueStatus(item.QueueId, "Syncing");
                        NotifyProgress($"Syncing {item.EntityType} #{item.EntityLocalId} to cloud...", isRunning: true);

                        int serverId = 0;
                        if (string.Equals(item.Operation, "Delete", StringComparison.OrdinalIgnoreCase))
                        {
                            await SyncDeleteToCloudAsync(item, cloudConn, local, tempIdMap);
                            _localCache.UpdateQueueSuccess(item.QueueId, item.ServerEntityId ?? 0);
                        }
                        else
                        {
                            serverId = item.EntityType.ToLowerInvariant() switch
                            {
                                "lead" => await SyncLeadToCloudAsync(item, cloudConn, local, tempIdMap),
                                "customer" => await SyncCustomerToCloudAsync(item, cloudConn, local, tempIdMap),
                                "property" => await SyncPropertyToCloudAsync(item, cloudConn, local, tempIdMap),
                                "deal" => await SyncDealToCloudAsync(item, cloudConn, local, tempIdMap),
                                "activity" => await SyncActivityToCloudAsync(item, cloudConn, local, tempIdMap),
                                "taskreminder" => await SyncTaskReminderToCloudAsync(item, cloudConn, local, tempIdMap),
                                "supportticket" => await SyncSupportTicketToCloudAsync(item, cloudConn, local, tempIdMap),
                                _ => throw new NotSupportedException($"Entity type '{item.EntityType}' is not supported for cloud sync.")
                            };

                            if (!string.IsNullOrEmpty(item.EntityLocalId) && serverId > 0)
                            {
                                tempIdMap[$"{item.EntityType}:{item.EntityLocalId}"] = serverId;
                            }

                            _localCache.UpdateQueueSuccess(item.QueueId, serverId);
                        }

                        processed++;
                        Log($"[Replay Success] Queued {item.Operation} #{item.QueueId} ({item.EntityType}) synced to cloud (Server ID: {serverId}).");
                    }
                    catch (Exception ex)
                    {
                        Log($"Queue item #{item.QueueId} ({item.EntityType}) failed: {ex.Message}");
                        _localCache.UpdateQueueFailure(item.QueueId, ex.Message);
                    }
                }

                NotifyProgress($"Processed {processed}/{pendingItems.Count} item(s).", isRunning: false);
            }
            catch (Exception ex)
            {
                Log($"DrainQueueAsync error: {ex.Message}");
                NotifyProgress($"Drain failed: {ex.Message}", isRunning: false);
            }
        }

        private async Task<int> ResolveCloudUserIdAsync(SqlConnection conn, RealEstateDbContext local, int localUserId)
        {
            if (localUserId > 0)
            {
                var localUser = await local.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.UserId == localUserId);
                if (!string.IsNullOrWhiteSpace(localUser?.Email))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT TOP 1 UserId FROM Users WHERE LOWER(Email) = LOWER(@Email)";
                    cmd.Parameters.AddWithValue("@Email", localUser.Email.Trim());
                    var obj = await cmd.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TOP 1 UserId FROM Users WHERE UserId = @Uid";
                    cmd.Parameters.AddWithValue("@Uid", localUserId);
                    var obj = await cmd.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 UserId FROM Users ORDER BY UserId ASC";
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
            }

            return 1;
        }

        private async Task<int?> ResolveCloudBranchIdAsync(SqlConnection conn, int? localBranchId)
        {
            if (!localBranchId.HasValue || localBranchId.Value <= 0) return null;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 BranchId FROM Branches WHERE BranchId = @Bid";
            cmd.Parameters.AddWithValue("@Bid", localBranchId.Value);
            var obj = await cmd.ExecuteScalarAsync();
            if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
            return null;
        }

        private async Task<int?> ResolveCloudCustomerIdAsync(SqlConnection conn, RealEstateDbContext local, int? localCustomerId, Dictionary<string, int> tempIdMap)
        {
            if (!localCustomerId.HasValue || localCustomerId.Value <= 0) return null;
            if (tempIdMap.TryGetValue($"Customer:{localCustomerId.Value}", out int mappedId)) return mappedId;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 CustomerId FROM Customers WHERE CustomerId = @Cid";
                cmd.Parameters.AddWithValue("@Cid", localCustomerId.Value);
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
            }

            var localCust = await local.Customers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == localCustomerId.Value);
            if (localCust != null)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP 1 CustomerId 
                    FROM Customers 
                    WHERE (@Email IS NOT NULL AND LOWER(Email) = LOWER(@Email))
                       OR (LOWER(FirstName) = LOWER(@First) AND LOWER(LastName) = LOWER(@Last))";
                cmd.Parameters.AddWithValue("@Email", (object?)localCust.Email?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@First", localCust.FirstName.Trim());
                cmd.Parameters.AddWithValue("@Last", localCust.LastName.Trim());
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                {
                    int foundId = Convert.ToInt32(obj);
                    tempIdMap[$"Customer:{localCustomerId.Value}"] = foundId;
                    return foundId;
                }
            }

            return null;
        }

        private async Task<int?> ResolveCloudLeadIdAsync(SqlConnection conn, RealEstateDbContext local, int? localLeadId, Dictionary<string, int> tempIdMap)
        {
            if (!localLeadId.HasValue || localLeadId.Value <= 0) return null;
            if (tempIdMap.TryGetValue($"Lead:{localLeadId.Value}", out int mappedId)) return mappedId;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 LeadId FROM Leads WHERE LeadId = @Lid";
                cmd.Parameters.AddWithValue("@Lid", localLeadId.Value);
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
            }

            var localLead = await local.Leads.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == localLeadId.Value);
            if (localLead != null)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP 1 LeadId 
                    FROM Leads 
                    WHERE (@Email IS NOT NULL AND LOWER(Email) = LOWER(@Email))
                       OR (LOWER(FirstName) = LOWER(@First) AND LOWER(LastName) = LOWER(@Last))";
                cmd.Parameters.AddWithValue("@Email", (object?)localLead.Email?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@First", localLead.FirstName.Trim());
                cmd.Parameters.AddWithValue("@Last", localLead.LastName.Trim());
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                {
                    int foundId = Convert.ToInt32(obj);
                    tempIdMap[$"Lead:{localLeadId.Value}"] = foundId;
                    return foundId;
                }
            }

            return null;
        }

        private async Task<int?> ResolveCloudPropertyIdAsync(SqlConnection conn, RealEstateDbContext local, int? localPropId, Dictionary<string, int> tempIdMap)
        {
            if (!localPropId.HasValue || localPropId.Value <= 0) return null;
            if (tempIdMap.TryGetValue($"Property:{localPropId.Value}", out int mappedId)) return mappedId;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 PropertyId FROM Properties WHERE PropertyId = @Pid";
                cmd.Parameters.AddWithValue("@Pid", localPropId.Value);
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) return Convert.ToInt32(obj);
            }

            var localProp = await local.Properties.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(p => p.PropertyId == localPropId.Value);
            if (localProp != null && !string.IsNullOrWhiteSpace(localProp.Address))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TOP 1 PropertyId FROM Properties WHERE LOWER(Address) = LOWER(@Address)";
                cmd.Parameters.AddWithValue("@Address", localProp.Address.Trim());
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                {
                    int foundId = Convert.ToInt32(obj);
                    tempIdMap[$"Property:{localPropId.Value}"] = foundId;
                    return foundId;
                }
            }

            return null;
        }

        private async Task<int> SyncLeadToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            Lead? lead = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                lead = await local.Leads.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == localId);
            }
            if (lead == null)
            {
                lead = JsonSerializer.Deserialize<Lead>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (lead == null) throw new InvalidOperationException("Could not deserialize Lead payload.");

            int cloudCreatedByUserId = await ResolveCloudUserIdAsync(conn, local, lead.CreatedByUserId);
            int? cloudAgentId = lead.AssignedAgentId.HasValue && lead.AssignedAgentId.Value > 0
                ? await ResolveCloudUserIdAsync(conn, local, lead.AssignedAgentId.Value)
                : null;
            int? cloudBranchId = await ResolveCloudBranchIdAsync(conn, lead.BranchId);

            int? targetCloudLeadId = item.ServerEntityId;
            if (!targetCloudLeadId.HasValue || targetCloudLeadId.Value <= 0)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT TOP 1 LeadId FROM Leads 
                    WHERE (@Email IS NOT NULL AND LOWER(Email) = LOWER(@Email))
                       OR (LOWER(FirstName) = LOWER(@First) AND LOWER(LastName) = LOWER(@Last))";
                checkCmd.Parameters.AddWithValue("@Email", (object?)lead.Email?.Trim() ?? DBNull.Value);
                checkCmd.Parameters.AddWithValue("@First", (lead.FirstName ?? "").Trim());
                checkCmd.Parameters.AddWithValue("@Last", (lead.LastName ?? "").Trim());
                var obj = await checkCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) targetCloudLeadId = Convert.ToInt32(obj);
            }

            if (targetCloudLeadId.HasValue && targetCloudLeadId.Value > 0)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE Leads SET
                        FirstName = @First,
                        MiddleName = @Middle,
                        LastName = @Last,
                        Suffix = @Suffix,
                        Email = @Email,
                        Phone = @Phone,
                        Source = @Source,
                        Notes = @Notes,
                        Stage = @Stage,
                        Priority = @Priority,
                        ExpectedValue = @ExpectedValue,
                        AssignedAgentId = @AgentId,
                        AssignmentStatus = @AssignmentStatus,
                        AssignmentReviewedByUserId = @ReviewedBy,
                        AssignmentReviewedAt = @ReviewedAt,
                        AssignmentReviewNotes = @ReviewNotes,
                        BranchId = @BranchId,
                        IsDeleted = @IsDeleted,
                        DeletedAt = @DeletedAt
                    WHERE LeadId = @LeadId";
                updateCmd.Parameters.AddWithValue("@First", lead.FirstName ?? "Lead");
                updateCmd.Parameters.AddWithValue("@Middle", (object?)lead.MiddleName ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Last", lead.LastName ?? "Contact");
                updateCmd.Parameters.AddWithValue("@Suffix", (object?)lead.Suffix ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Email", (object?)lead.Email ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Phone", (object?)lead.Phone ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Source", (object?)lead.Source ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Notes", (object?)lead.Notes ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Stage", lead.Stage ?? "New");
                updateCmd.Parameters.AddWithValue("@Priority", (object?)lead.Priority ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ExpectedValue", (object?)lead.ExpectedValue ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@AssignmentStatus", lead.AssignmentStatus ?? "Unassigned");
                updateCmd.Parameters.AddWithValue("@ReviewedBy", (object?)lead.AssignmentReviewedByUserId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewedAt", (object?)lead.AssignmentReviewedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewNotes", (object?)lead.AssignmentReviewNotes ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@IsDeleted", lead.IsDeleted);
                updateCmd.Parameters.AddWithValue("@DeletedAt", (object?)lead.DeletedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@LeadId", targetCloudLeadId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                return targetCloudLeadId.Value;
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Leads (
                        FirstName, MiddleName, LastName, Suffix, Email, Phone,
                        Source, Notes, Stage, Priority, ExpectedValue,
                        CreatedByUserId, AssignedAgentId, AssignmentStatus, AssignmentReviewedByUserId,
                        AssignmentReviewedAt, AssignmentReviewNotes, BranchId, CreatedAt, IsDeleted, DeletedAt
                    )
                    OUTPUT INSERTED.LeadId
                    VALUES (
                        @First, @Middle, @Last, @Suffix, @Email, @Phone,
                        @Source, @Notes, @Stage, @Priority, @ExpectedValue,
                        @CreatedBy, @AgentId, @AssignmentStatus, @ReviewedBy,
                        @ReviewedAt, @ReviewNotes, @BranchId, @CreatedAt, @IsDeleted, @DeletedAt
                    );";
                insertCmd.Parameters.AddWithValue("@First", lead.FirstName ?? "Lead");
                insertCmd.Parameters.AddWithValue("@Middle", (object?)lead.MiddleName ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Last", lead.LastName ?? "Contact");
                insertCmd.Parameters.AddWithValue("@Suffix", (object?)lead.Suffix ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Email", (object?)lead.Email ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Phone", (object?)lead.Phone ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Source", (object?)lead.Source ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Notes", (object?)lead.Notes ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Stage", lead.Stage ?? "New");
                insertCmd.Parameters.AddWithValue("@Priority", (object?)lead.Priority ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ExpectedValue", (object?)lead.ExpectedValue ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedBy", cloudCreatedByUserId);
                insertCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@AssignmentStatus", lead.AssignmentStatus ?? "Unassigned");
                insertCmd.Parameters.AddWithValue("@ReviewedBy", (object?)lead.AssignmentReviewedByUserId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewedAt", (object?)lead.AssignmentReviewedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewNotes", (object?)lead.AssignmentReviewNotes ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedAt", lead.CreatedAt == default ? DateTime.UtcNow : lead.CreatedAt);
                insertCmd.Parameters.AddWithValue("@IsDeleted", lead.IsDeleted);
                insertCmd.Parameters.AddWithValue("@DeletedAt", (object?)lead.DeletedAt ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<int> SyncCustomerToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            Customer? customer = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                customer = await local.Customers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == localId);
            }
            if (customer == null)
            {
                customer = JsonSerializer.Deserialize<Customer>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (customer == null) throw new InvalidOperationException("Could not deserialize Customer payload.");

            int cloudCreatedByUserId = await ResolveCloudUserIdAsync(conn, local, customer.CreatedByUserId);
            int? cloudAgentId = customer.AssignedAgentId.HasValue && customer.AssignedAgentId.Value > 0
                ? await ResolveCloudUserIdAsync(conn, local, customer.AssignedAgentId.Value)
                : null;

            int? targetCloudCustId = item.ServerEntityId;
            if (!targetCloudCustId.HasValue || targetCloudCustId.Value <= 0)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT TOP 1 CustomerId FROM Customers 
                    WHERE (@Email IS NOT NULL AND LOWER(Email) = LOWER(@Email))
                       OR (LOWER(FirstName) = LOWER(@First) AND LOWER(LastName) = LOWER(@Last))";
                checkCmd.Parameters.AddWithValue("@Email", (object?)customer.Email?.Trim() ?? DBNull.Value);
                checkCmd.Parameters.AddWithValue("@First", (customer.FirstName ?? "").Trim());
                checkCmd.Parameters.AddWithValue("@Last", (customer.LastName ?? "").Trim());
                var obj = await checkCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) targetCloudCustId = Convert.ToInt32(obj);
            }

            int finalCustomerId;
            if (targetCloudCustId.HasValue && targetCloudCustId.Value > 0)
            {
                finalCustomerId = targetCloudCustId.Value;
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE Customers SET
                        FirstName = @First,
                        MiddleName = @Middle,
                        LastName = @Last,
                        Suffix = @Suffix,
                        Email = @Email,
                        Phone = @Phone,
                        Type = @Type,
                        Status = @Status,
                        AssignedAgentId = @AgentId,
                        AssignmentStatus = @AssignmentStatus,
                        AssignmentReviewedByUserId = @ReviewedBy,
                        AssignmentReviewedAt = @ReviewedAt,
                        AssignmentReviewNotes = @ReviewNotes,
                        IsDeleted = @IsDeleted,
                        DeletedAt = @DeletedAt
                    WHERE CustomerId = @CustomerId";
                updateCmd.Parameters.AddWithValue("@First", customer.FirstName ?? "Customer");
                updateCmd.Parameters.AddWithValue("@Middle", (object?)customer.MiddleName ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Last", customer.LastName ?? "Contact");
                updateCmd.Parameters.AddWithValue("@Suffix", (object?)customer.Suffix ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Type", customer.Type ?? "buyer");
                updateCmd.Parameters.AddWithValue("@Status", customer.Status ?? "active");
                updateCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@AssignmentStatus", customer.AssignmentStatus ?? "Unassigned");
                updateCmd.Parameters.AddWithValue("@ReviewedBy", (object?)customer.AssignmentReviewedByUserId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewedAt", (object?)customer.AssignmentReviewedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewNotes", (object?)customer.AssignmentReviewNotes ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@IsDeleted", customer.IsDeleted);
                updateCmd.Parameters.AddWithValue("@DeletedAt", (object?)customer.DeletedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@CustomerId", finalCustomerId);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Customers (
                        FirstName, MiddleName, LastName, Suffix, Email, Phone,
                        Type, Status, CreatedByUserId, AssignedAgentId,
                        AssignmentStatus, AssignmentReviewedByUserId, AssignmentReviewedAt, AssignmentReviewNotes,
                        CreatedAt, IsDeleted, DeletedAt, CurrentRetentionSegment
                    )
                    OUTPUT INSERTED.CustomerId
                    VALUES (
                        @First, @Middle, @Last, @Suffix, @Email, @Phone,
                        @Type, @Status, @CreatedBy, @AgentId,
                        @AssignmentStatus, @ReviewedBy, @ReviewedAt, @ReviewNotes,
                        @CreatedAt, @IsDeleted, @DeletedAt, 'Prospective Client'
                    );";
                insertCmd.Parameters.AddWithValue("@First", customer.FirstName ?? "Customer");
                insertCmd.Parameters.AddWithValue("@Middle", (object?)customer.MiddleName ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Last", customer.LastName ?? "Contact");
                insertCmd.Parameters.AddWithValue("@Suffix", (object?)customer.Suffix ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Type", customer.Type ?? "buyer");
                insertCmd.Parameters.AddWithValue("@Status", customer.Status ?? "active");
                insertCmd.Parameters.AddWithValue("@CreatedBy", cloudCreatedByUserId);
                insertCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@AssignmentStatus", customer.AssignmentStatus ?? "Unassigned");
                insertCmd.Parameters.AddWithValue("@ReviewedBy", (object?)customer.AssignmentReviewedByUserId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewedAt", (object?)customer.AssignmentReviewedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewNotes", (object?)customer.AssignmentReviewNotes ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedAt", customer.CreatedAt == default ? DateTime.UtcNow : customer.CreatedAt);
                insertCmd.Parameters.AddWithValue("@IsDeleted", customer.IsDeleted);
                insertCmd.Parameters.AddWithValue("@DeletedAt", (object?)customer.DeletedAt ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                finalCustomerId = Convert.ToInt32(newId);
            }

            if (localId > 0)
            {
                var bp = await local.BuyerProfiles.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(b => b.CustomerId == localId);
                if (bp != null)
                {
                    using var bpCmd = conn.CreateCommand();
                    bpCmd.CommandText = @"
                        IF EXISTS (SELECT 1 FROM BuyerProfiles WHERE CustomerId = @Cid)
                            UPDATE BuyerProfiles SET Budget = @Budget, PreferredLocation = @Location, PreferredPropertyType = @PropType WHERE CustomerId = @Cid
                        ELSE
                            INSERT INTO BuyerProfiles (CustomerId, Budget, PreferredLocation, PreferredPropertyType) VALUES (@Cid, @Budget, @Location, @PropType)";
                    bpCmd.Parameters.AddWithValue("@Cid", finalCustomerId);
                    bpCmd.Parameters.AddWithValue("@Budget", bp.Budget);
                    bpCmd.Parameters.AddWithValue("@Location", (object?)bp.PreferredLocation ?? DBNull.Value);
                    bpCmd.Parameters.AddWithValue("@PropType", (object?)bp.PreferredPropertyType ?? DBNull.Value);
                    await bpCmd.ExecuteNonQueryAsync();
                }
            }

            return finalCustomerId;
        }

        private async Task<int> SyncPropertyToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            Property? prop = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                prop = await local.Properties.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(p => p.PropertyId == localId);
            }
            if (prop == null)
            {
                prop = JsonSerializer.Deserialize<Property>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (prop == null) throw new InvalidOperationException("Could not deserialize Property payload.");

            int cloudCreatedByUserId = await ResolveCloudUserIdAsync(conn, local, prop.CreatedByUserId);
            int? cloudAgentId = prop.ListedByAgentId.HasValue && prop.ListedByAgentId.Value > 0
                ? await ResolveCloudUserIdAsync(conn, local, prop.ListedByAgentId.Value)
                : null;
            int? cloudBranchId = await ResolveCloudBranchIdAsync(conn, prop.BranchId);

            int cloudOwnerId = 0;
            if (prop.OwnerCustomerId > 0)
            {
                var resolvedOwner = await ResolveCloudCustomerIdAsync(conn, local, prop.OwnerCustomerId, tempIdMap);
                if (resolvedOwner.HasValue) cloudOwnerId = resolvedOwner.Value;
            }
            if (cloudOwnerId <= 0)
            {
                using var pickCmd = conn.CreateCommand();
                pickCmd.CommandText = "SELECT TOP 1 CustomerId FROM Customers ORDER BY CustomerId ASC";
                var obj = await pickCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) cloudOwnerId = Convert.ToInt32(obj);
            }

            int? targetCloudPropId = item.ServerEntityId;
            if (!targetCloudPropId.HasValue || targetCloudPropId.Value <= 0)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT TOP 1 PropertyId FROM Properties WHERE LOWER(Address) = LOWER(@Address)";
                checkCmd.Parameters.AddWithValue("@Address", prop.Address.Trim());
                var obj = await checkCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) targetCloudPropId = Convert.ToInt32(obj);
            }

            if (targetCloudPropId.HasValue && targetCloudPropId.Value > 0)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE Properties SET
                        PropertyType = @PropertyType,
                        Price = @Price,
                        Status = @Status,
                        OwnerCustomerId = @OwnerId,
                        ListedByAgentId = @AgentId,
                        AssignmentStatus = @AssignmentStatus,
                        AssignmentReviewedByUserId = @ReviewedBy,
                        AssignmentReviewedAt = @ReviewedAt,
                        AssignmentReviewNotes = @ReviewNotes,
                        BranchId = @BranchId
                    WHERE PropertyId = @PropertyId";
                updateCmd.Parameters.AddWithValue("@PropertyType", prop.PropertyType ?? "Residential");
                updateCmd.Parameters.AddWithValue("@Price", prop.Price);
                updateCmd.Parameters.AddWithValue("@Status", prop.Status ?? "Available");
                updateCmd.Parameters.AddWithValue("@OwnerId", cloudOwnerId);
                updateCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@AssignmentStatus", prop.AssignmentStatus ?? "Unassigned");
                updateCmd.Parameters.AddWithValue("@ReviewedBy", (object?)prop.AssignmentReviewedByUserId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewedAt", (object?)prop.AssignmentReviewedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReviewNotes", (object?)prop.AssignmentReviewNotes ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@PropertyId", targetCloudPropId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                return targetCloudPropId.Value;
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Properties (
                        Address, PropertyType, Price, Status, OwnerCustomerId,
                        ListedByAgentId, CreatedByUserId, BranchId, CreatedAt,
                        AssignmentStatus, AssignmentReviewedByUserId, AssignmentReviewedAt, AssignmentReviewNotes
                    )
                    OUTPUT INSERTED.PropertyId
                    VALUES (
                        @Address, @PropertyType, @Price, @Status, @OwnerId,
                        @AgentId, @CreatedBy, @BranchId, @CreatedAt,
                        @AssignmentStatus, @ReviewedBy, @ReviewedAt, @ReviewNotes
                    );";
                insertCmd.Parameters.AddWithValue("@Address", prop.Address.Trim());
                insertCmd.Parameters.AddWithValue("@PropertyType", prop.PropertyType ?? "Residential");
                insertCmd.Parameters.AddWithValue("@Price", prop.Price);
                insertCmd.Parameters.AddWithValue("@Status", prop.Status ?? "Available");
                insertCmd.Parameters.AddWithValue("@OwnerId", cloudOwnerId);
                insertCmd.Parameters.AddWithValue("@AgentId", (object?)cloudAgentId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedBy", cloudCreatedByUserId);
                insertCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedAt", prop.CreatedAt == default ? DateTime.UtcNow : prop.CreatedAt);
                insertCmd.Parameters.AddWithValue("@AssignmentStatus", prop.AssignmentStatus ?? "Unassigned");
                insertCmd.Parameters.AddWithValue("@ReviewedBy", (object?)prop.AssignmentReviewedByUserId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewedAt", (object?)prop.AssignmentReviewedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReviewNotes", (object?)prop.AssignmentReviewNotes ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<int> SyncDealToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            Deal? deal = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                deal = await local.Deals.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.DealId == localId);
            }
            if (deal == null)
            {
                deal = JsonSerializer.Deserialize<Deal>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (deal == null) throw new InvalidOperationException("Could not deserialize Deal payload.");

            var cloudCustId = await ResolveCloudCustomerIdAsync(conn, local, deal.CustomerId, tempIdMap);
            var cloudPropId = await ResolveCloudPropertyIdAsync(conn, local, deal.PropertyId, tempIdMap);
            if (!cloudCustId.HasValue || !cloudPropId.HasValue)
            {
                throw new InvalidOperationException($"Cannot sync Deal: Customer or Property could not be mapped to cloud.");
            }

            int cloudAgentId = deal.AgentId.HasValue && deal.AgentId.Value > 0
                ? await ResolveCloudUserIdAsync(conn, local, deal.AgentId.Value)
                : await ResolveCloudUserIdAsync(conn, local, deal.CreatedByUserId);
            int cloudCreatedByUserId = await ResolveCloudUserIdAsync(conn, local, deal.CreatedByUserId);
            int? cloudBranchId = await ResolveCloudBranchIdAsync(conn, deal.BranchId);

            int? targetCloudDealId = item.ServerEntityId;
            if (!targetCloudDealId.HasValue || targetCloudDealId.Value <= 0)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT TOP 1 DealId FROM Deals WHERE CustomerId = @Cid AND PropertyId = @Pid";
                checkCmd.Parameters.AddWithValue("@Cid", cloudCustId.Value);
                checkCmd.Parameters.AddWithValue("@Pid", cloudPropId.Value);
                var obj = await checkCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) targetCloudDealId = Convert.ToInt32(obj);
            }

            if (targetCloudDealId.HasValue && targetCloudDealId.Value > 0)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE Deals SET
                        AgentId = @AgentId,
                        Value = @Value,
                        CommissionRate = @CommissionRate,
                        Stage = @Stage,
                        ExpectedCloseDate = @ExpectedCloseDate,
                        PaymentScheme = @PaymentScheme,
                        ReservationFee = @ReservationFee,
                        DownPaymentPercent = @DownPaymentPercent,
                        CgtPayer = @CgtPayer,
                        DstPayer = @DstPayer,
                        TransferTaxPayer = @TransferTaxPayer,
                        RegistrationFeePayer = @RegistrationFeePayer,
                        SpecialStipulations = @SpecialStipulations,
                        ContractSignedDate = @ContractSignedDate,
                        BranchId = @BranchId
                    WHERE DealId = @DealId";
                updateCmd.Parameters.AddWithValue("@AgentId", cloudAgentId);
                updateCmd.Parameters.AddWithValue("@Value", deal.Value);
                updateCmd.Parameters.AddWithValue("@CommissionRate", deal.CommissionRate);
                updateCmd.Parameters.AddWithValue("@Stage", deal.Stage ?? "Offer");
                updateCmd.Parameters.AddWithValue("@ExpectedCloseDate", (object?)deal.ExpectedCloseDate ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@PaymentScheme", (object?)deal.PaymentScheme ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ReservationFee", (object?)deal.ReservationFee ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@DownPaymentPercent", (object?)deal.DownPaymentPercent ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@CgtPayer", (object?)deal.CgtPayer ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@DstPayer", (object?)deal.DstPayer ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@TransferTaxPayer", (object?)deal.TransferTaxPayer ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@RegistrationFeePayer", (object?)deal.RegistrationFeePayer ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@SpecialStipulations", (object?)deal.SpecialStipulations ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@ContractSignedDate", (object?)deal.ContractSignedDate ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@DealId", targetCloudDealId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                return targetCloudDealId.Value;
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Deals (
                        CustomerId, PropertyId, AgentId, Value, CommissionRate, Stage,
                        ExpectedCloseDate, CreatedAt, PaymentScheme, ReservationFee, DownPaymentPercent,
                        CgtPayer, DstPayer, TransferTaxPayer, RegistrationFeePayer, SpecialStipulations,
                        ContractSignedDate, CreatedByUserId, BranchId
                    )
                    OUTPUT INSERTED.DealId
                    VALUES (
                        @Cid, @Pid, @AgentId, @Value, @CommissionRate, @Stage,
                        @ExpectedCloseDate, @CreatedAt, @PaymentScheme, @ReservationFee, @DownPaymentPercent,
                        @CgtPayer, @DstPayer, @TransferTaxPayer, @RegistrationFeePayer, @SpecialStipulations,
                        @ContractSignedDate, @CreatedBy, @BranchId
                    );";
                insertCmd.Parameters.AddWithValue("@Cid", cloudCustId.Value);
                insertCmd.Parameters.AddWithValue("@Pid", cloudPropId.Value);
                insertCmd.Parameters.AddWithValue("@AgentId", cloudAgentId);
                insertCmd.Parameters.AddWithValue("@Value", deal.Value);
                insertCmd.Parameters.AddWithValue("@CommissionRate", deal.CommissionRate);
                insertCmd.Parameters.AddWithValue("@Stage", deal.Stage ?? "Offer");
                insertCmd.Parameters.AddWithValue("@ExpectedCloseDate", (object?)deal.ExpectedCloseDate ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedAt", deal.CreatedAt == default ? DateTime.UtcNow : deal.CreatedAt);
                insertCmd.Parameters.AddWithValue("@PaymentScheme", (object?)deal.PaymentScheme ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReservationFee", (object?)deal.ReservationFee ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@DownPaymentPercent", (object?)deal.DownPaymentPercent ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CgtPayer", (object?)deal.CgtPayer ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@DstPayer", (object?)deal.DstPayer ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TransferTaxPayer", (object?)deal.TransferTaxPayer ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@RegistrationFeePayer", (object?)deal.RegistrationFeePayer ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@SpecialStipulations", (object?)deal.SpecialStipulations ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ContractSignedDate", (object?)deal.ContractSignedDate ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedBy", cloudCreatedByUserId);
                insertCmd.Parameters.AddWithValue("@BranchId", (object?)cloudBranchId ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<int> SyncActivityToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            Activity? act = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                act = await local.Activities.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(a => a.ActivityId == localId);
            }
            if (act == null)
            {
                act = JsonSerializer.Deserialize<Activity>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (act == null) throw new InvalidOperationException("Could not deserialize Activity payload.");

            int? cloudCustId = await ResolveCloudCustomerIdAsync(conn, local, act.RelatedCustomerId, tempIdMap);
            int? cloudLeadId = await ResolveCloudLeadIdAsync(conn, local, act.RelatedLeadId, tempIdMap);
            int cloudAgentId = await ResolveCloudUserIdAsync(conn, local, act.LoggedByAgentId);

            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Activities (
                    Type, RelatedLeadId, RelatedCustomerId, LoggedByAgentId, Notes, ActivityDate, Outcome, DurationMinutes
                )
                OUTPUT INSERTED.ActivityId
                VALUES (
                    @Type, @LeadId, @CustId, @AgentId, @Notes, @ActivityDate, @Outcome, @DurationMinutes
                );";
            insertCmd.Parameters.AddWithValue("@Type", act.Type ?? "Note");
            insertCmd.Parameters.AddWithValue("@LeadId", (object?)cloudLeadId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@CustId", (object?)cloudCustId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@AgentId", cloudAgentId);
            insertCmd.Parameters.AddWithValue("@Notes", (object?)act.Notes ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@ActivityDate", act.ActivityDate == default ? DateTime.UtcNow : act.ActivityDate);
            insertCmd.Parameters.AddWithValue("@Outcome", (object?)act.Outcome ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@DurationMinutes", (object?)act.DurationMinutes ?? DBNull.Value);

            var newId = await insertCmd.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }

        private async Task<int> SyncTaskReminderToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            TaskReminder? reminder = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                reminder = await local.TaskReminders.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(r => r.TaskReminderId == localId);
            }
            if (reminder == null)
            {
                reminder = JsonSerializer.Deserialize<TaskReminder>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (reminder == null) throw new InvalidOperationException("Could not deserialize TaskReminder payload.");

            int? cloudCustId = await ResolveCloudCustomerIdAsync(conn, local, reminder.RelatedCustomerId, tempIdMap);
            int? cloudLeadId = await ResolveCloudLeadIdAsync(conn, local, reminder.RelatedLeadId, tempIdMap);
            int cloudUserId = await ResolveCloudUserIdAsync(conn, local, reminder.AssignedToUserId);

            int? targetReminderId = item.ServerEntityId;
            if (targetReminderId.HasValue && targetReminderId.Value > 0)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE TaskReminders SET
                        Title = @Title,
                        DueDate = @DueDate,
                        AssignedToUserId = @UserId,
                        RelatedCustomerId = @CustId,
                        RelatedLeadId = @LeadId,
                        Status = @Status,
                        Type = @Type,
                        Notes = @Notes,
                        Priority = @Priority,
                        UpdatedAt = @UpdatedAt,
                        CompletedAt = @CompletedAt,
                        IsDeleted = @IsDeleted,
                        DeletedAt = @DeletedAt
                    WHERE TaskReminderId = @Id";
                updateCmd.Parameters.AddWithValue("@Title", reminder.Title ?? "Follow-Up");
                updateCmd.Parameters.AddWithValue("@DueDate", reminder.DueDate);
                updateCmd.Parameters.AddWithValue("@UserId", cloudUserId);
                updateCmd.Parameters.AddWithValue("@CustId", (object?)cloudCustId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@LeadId", (object?)cloudLeadId ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Status", reminder.Status ?? "Pending");
                updateCmd.Parameters.AddWithValue("@Type", reminder.Type ?? "Call");
                updateCmd.Parameters.AddWithValue("@Notes", (object?)reminder.Notes ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Priority", reminder.Priority ?? "Medium");
                updateCmd.Parameters.AddWithValue("@UpdatedAt", (object?)reminder.UpdatedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@CompletedAt", (object?)reminder.CompletedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@IsDeleted", reminder.IsDeleted);
                updateCmd.Parameters.AddWithValue("@DeletedAt", (object?)reminder.DeletedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Id", targetReminderId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                return targetReminderId.Value;
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO TaskReminders (
                        Title, DueDate, AssignedToUserId, RelatedCustomerId, RelatedLeadId,
                        Status, Type, Notes, Priority, CreatedAt, UpdatedAt, CompletedAt, IsDeleted, DeletedAt
                    )
                    OUTPUT INSERTED.TaskReminderId
                    VALUES (
                        @Title, @DueDate, @UserId, @CustId, @LeadId,
                        @Status, @Type, @Notes, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt, @IsDeleted, @DeletedAt
                    );";
                insertCmd.Parameters.AddWithValue("@Title", reminder.Title ?? "Follow-Up");
                insertCmd.Parameters.AddWithValue("@DueDate", reminder.DueDate);
                insertCmd.Parameters.AddWithValue("@UserId", cloudUserId);
                insertCmd.Parameters.AddWithValue("@CustId", (object?)cloudCustId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@LeadId", (object?)cloudLeadId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Status", reminder.Status ?? "Pending");
                insertCmd.Parameters.AddWithValue("@Type", reminder.Type ?? "Call");
                insertCmd.Parameters.AddWithValue("@Notes", (object?)reminder.Notes ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Priority", reminder.Priority ?? "Medium");
                insertCmd.Parameters.AddWithValue("@CreatedAt", reminder.CreatedAt == default ? DateTime.UtcNow : reminder.CreatedAt);
                insertCmd.Parameters.AddWithValue("@UpdatedAt", (object?)reminder.UpdatedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CompletedAt", (object?)reminder.CompletedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@IsDeleted", reminder.IsDeleted);
                insertCmd.Parameters.AddWithValue("@DeletedAt", (object?)reminder.DeletedAt ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<int> SyncSupportTicketToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            SupportTicket? ticket = null;
            if (int.TryParse(item.EntityLocalId, out int localId) && localId > 0)
            {
                ticket = await local.SupportTickets.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == localId);
            }
            if (ticket == null)
            {
                ticket = JsonSerializer.Deserialize<SupportTicket>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (ticket == null) throw new InvalidOperationException("Could not deserialize SupportTicket payload.");

            int? cloudCustId = await ResolveCloudCustomerIdAsync(conn, local, ticket.CustomerId, tempIdMap);
            if (!cloudCustId.HasValue)
            {
                using var pickCmd = conn.CreateCommand();
                pickCmd.CommandText = "SELECT TOP 1 CustomerId FROM Customers ORDER BY CustomerId ASC";
                var obj = await pickCmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value) cloudCustId = Convert.ToInt32(obj);
                else throw new InvalidOperationException("Cannot sync SupportTicket: No customers exist on cloud.");
            }

            int cloudRaisedBy = await ResolveCloudUserIdAsync(conn, local, ticket.RaisedByUserId);
            int? cloudAssignedTo = ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value > 0
                ? await ResolveCloudUserIdAsync(conn, local, ticket.AssignedToUserId.Value)
                : null;

            int? targetTicketId = item.ServerEntityId;
            if (targetTicketId.HasValue && targetTicketId.Value > 0)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = @"
                    UPDATE SupportTickets SET
                        CustomerId = @CustId,
                        AssignedToUserId = @AssignedTo,
                        Description = @Description,
                        Priority = @Priority,
                        Status = @Status,
                        ResolvedAt = @ResolvedAt,
                        Category = @Category,
                        DueDate = @DueDate,
                        FirstRespondedAt = @FirstRespondedAt,
                        IsDeleted = @IsDeleted,
                        DeletedAt = @DeletedAt
                    WHERE TicketId = @Id";
                updateCmd.Parameters.AddWithValue("@CustId", cloudCustId.Value);
                updateCmd.Parameters.AddWithValue("@AssignedTo", (object?)cloudAssignedTo ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Description", (object?)ticket.Description ?? string.Empty);
                updateCmd.Parameters.AddWithValue("@Priority", ticket.Priority ?? "Medium");
                updateCmd.Parameters.AddWithValue("@Status", ticket.Status ?? "Open");
                updateCmd.Parameters.AddWithValue("@ResolvedAt", (object?)ticket.ResolvedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Category", ticket.Category ?? "Other");
                updateCmd.Parameters.AddWithValue("@DueDate", (object?)ticket.DueDate ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@FirstRespondedAt", (object?)ticket.FirstRespondedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@IsDeleted", ticket.IsDeleted);
                updateCmd.Parameters.AddWithValue("@DeletedAt", (object?)ticket.DeletedAt ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Id", targetTicketId.Value);
                await updateCmd.ExecuteNonQueryAsync();

                return targetTicketId.Value;
            }
            else
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO SupportTickets (
                        CustomerId, RaisedByUserId, AssignedToUserId, Description, Priority,
                        Status, CreatedAt, ResolvedAt, TicketNumber, Category, DueDate,
                        FirstRespondedAt, IsDeleted, DeletedAt
                    )
                    OUTPUT INSERTED.TicketId
                    VALUES (
                        @CustId, @RaisedBy, @AssignedTo, @Description, @Priority,
                        @Status, @CreatedAt, @ResolvedAt, @TicketNumber, @Category, @DueDate,
                        @FirstRespondedAt, @IsDeleted, @DeletedAt
                    );";
                insertCmd.Parameters.AddWithValue("@CustId", cloudCustId.Value);
                insertCmd.Parameters.AddWithValue("@RaisedBy", cloudRaisedBy);
                insertCmd.Parameters.AddWithValue("@AssignedTo", (object?)cloudAssignedTo ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Description", (object?)ticket.Description ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@Priority", ticket.Priority ?? "Medium");
                insertCmd.Parameters.AddWithValue("@Status", ticket.Status ?? "Open");
                insertCmd.Parameters.AddWithValue("@CreatedAt", ticket.CreatedAt == default ? DateTime.UtcNow : ticket.CreatedAt);
                insertCmd.Parameters.AddWithValue("@ResolvedAt", (object?)ticket.ResolvedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TicketNumber", string.IsNullOrWhiteSpace(ticket.TicketNumber) ? "TCK-TEMP" : ticket.TicketNumber);
                insertCmd.Parameters.AddWithValue("@Category", ticket.Category ?? "Other");
                insertCmd.Parameters.AddWithValue("@DueDate", (object?)ticket.DueDate ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@FirstRespondedAt", (object?)ticket.FirstRespondedAt ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@IsDeleted", ticket.IsDeleted);
                insertCmd.Parameters.AddWithValue("@DeletedAt", (object?)ticket.DeletedAt ?? DBNull.Value);

                var newId = await insertCmd.ExecuteScalarAsync();
                return Convert.ToInt32(newId);
            }
        }

        private async Task<bool> SyncDeleteToCloudAsync(PendingSyncQueue item, SqlConnection conn, RealEstateDbContext local, Dictionary<string, int> tempIdMap)
        {
            int? cloudId = item.ServerEntityId;
            if (string.Equals(item.EntityType, "Lead", StringComparison.OrdinalIgnoreCase))
            {
                if (!cloudId.HasValue && int.TryParse(item.EntityLocalId, out int localId))
                {
                    cloudId = await ResolveCloudLeadIdAsync(conn, local, localId, tempIdMap);
                }
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE Leads SET IsDeleted = 1, DeletedAt = GETUTCDATE() WHERE LeadId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            else if (string.Equals(item.EntityType, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                if (!cloudId.HasValue && int.TryParse(item.EntityLocalId, out int localId))
                {
                    cloudId = await ResolveCloudCustomerIdAsync(conn, local, localId, tempIdMap);
                }
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE Customers SET IsDeleted = 1, DeletedAt = GETUTCDATE() WHERE CustomerId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            else if (string.Equals(item.EntityType, "Property", StringComparison.OrdinalIgnoreCase))
            {
                if (!cloudId.HasValue && int.TryParse(item.EntityLocalId, out int localId))
                {
                    cloudId = await ResolveCloudPropertyIdAsync(conn, local, localId, tempIdMap);
                }
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM Properties WHERE PropertyId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            else if (string.Equals(item.EntityType, "Deal", StringComparison.OrdinalIgnoreCase))
            {
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM Deals WHERE DealId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            else if (string.Equals(item.EntityType, "TaskReminder", StringComparison.OrdinalIgnoreCase))
            {
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE TaskReminders SET IsDeleted = 1, DeletedAt = GETUTCDATE() WHERE TaskReminderId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
            else if (string.Equals(item.EntityType, "SupportTicket", StringComparison.OrdinalIgnoreCase))
            {
                if (cloudId.HasValue && cloudId.Value > 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE SupportTickets SET IsDeleted = 1, DeletedAt = GETUTCDATE() WHERE TicketId = @Id";
                    cmd.Parameters.AddWithValue("@Id", cloudId.Value);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }

            return true;
        }

        private bool CheckTimestampConflict(string serverJson, DateTime? cachedTimestamp, out string serverPayload)
        {
            serverPayload = serverJson;
            if (!cachedTimestamp.HasValue) return false;

            try
            {
                using var doc = JsonDocument.Parse(serverJson);
                if (doc.RootElement.TryGetProperty("updatedAt", out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(prop.GetString(), out var serverTime))
                    {
                        return serverTime > cachedTimestamp.Value.AddSeconds(1);
                    }
                }
            }
            catch
            {
                // ignore parsing error
            }

            return false;
        }

        private string RewriteTempForeignKeys(string payloadJson, Dictionary<string, int> tempIdMap)
        {
            if (tempIdMap.Count == 0) return payloadJson;
            string rewritten = payloadJson;
            foreach (var kvp in tempIdMap)
            {
                string[] parts = kvp.Key.Split(':');
                if (parts.Length != 2) continue;
                string entityType = parts[0];
                string tempId = parts[1];
                int serverId = kvp.Value;

                if (entityType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    rewritten = rewritten.Replace($"\"RelatedCustomerId\":{tempId}", $"\"RelatedCustomerId\":{serverId}")
                                         .Replace($"\"CustomerId\":{tempId}", $"\"CustomerId\":{serverId}");
                }
                else if (entityType.Equals("Lead", StringComparison.OrdinalIgnoreCase))
                {
                    rewritten = rewritten.Replace($"\"RelatedLeadId\":{tempId}", $"\"RelatedLeadId\":{serverId}")
                                         .Replace($"\"LeadId\":{tempId}", $"\"LeadId\":{serverId}");
                }
            }
            return rewritten;
        }

        private int ExtractIdFromJson(string json, string entityType)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                string idPropName = $"{entityType}Id";
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.Equals(idPropName, StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value.GetInt32();
                    }
                }
            }
            catch
            {
                // fallback
            }
            return 0;
        }

        private string GetEndpointForEntity(string entityType) => entityType.ToLowerInvariant() switch
        {
            "customer" => "api/customers",
            "lead" => "api/leads",
            "deal" => "api/deals",
            "activity" => "api/activities",
            "taskreminder" => "api/taskreminders",
            "supportticket" => "api/supporttickets",
            _ => $"api/{entityType.ToLowerInvariant()}s"
        };

        private void ApplyAuthHeader()
        {
            if (!string.IsNullOrWhiteSpace(CurrentSession.JwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", CurrentSession.JwtToken);
            }
        }

        public async Task<bool> ResolveConflictUseLocalAsync(int queueId)
        {
            var item = _localCache.GetQueueItem(queueId);
            if (item == null) return false;

            item.ServerVersionTimestamp = DateTime.UtcNow;
            _localCache.UpdateQueueStatus(queueId, "Pending");
            Log($"[Conflict Resolution] User chose LOCAL version for item #{queueId}. Re-queued as Pending.");
            await SyncAsync();
            return true;
        }

        public async Task<bool> ResolveConflictUseServerAsync(int queueId)
        {
            var item = _localCache.GetQueueItem(queueId);
            if (item == null) return false;

            _localCache.UpdateQueueSuccess(queueId, item.ServerEntityId ?? 0);
            Log($"[Conflict Resolution] User chose SERVER version for item #{queueId}. Local change discarded.");

            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            await RefreshUserCacheAsync(tenantId, CurrentSession.UserId, CurrentSession.CurrentUser?.Role.ToString() ?? "Agent");
            return true;
        }

        public async Task RetryItemAsync(int queueId)
        {
            _localCache.UpdateQueueStatus(queueId, "Pending");
            await SyncAsync();
        }

        public async Task RetryAllFailedAsync()
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var all = _localCache.GetAllQueueItems(tenantId).Where(q => q.Status == "Failed").ToList();
            foreach (var item in all)
            {
                _localCache.UpdateQueueStatus(item.QueueId, "Pending");
            }
            await SyncAsync();
        }

        public async Task RefreshUserCacheAsync(int tenantId, int userId, string roleName)
        {
            if (tenantId <= 0) tenantId = 1;
            try
            {
                using var db = LocalDb.CreateContext(tenantId);
                bool isAgent = roleName.Equals("Agent", StringComparison.OrdinalIgnoreCase) ||
                               roleName.Equals("SalesStaff", StringComparison.OrdinalIgnoreCase);

                var custQuery = db.Customers.Where(c => !c.IsDeleted);
                if (isAgent && userId > 0)
                {
                    custQuery = custQuery.Where(c =>
                        (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                            ? c.AssignedAgentId.Value == userId
                            : c.CreatedByUserId == userId);
                }
                var customers = await custQuery.AsNoTracking().ToListAsync();
                _localCache.SaveCustomersMirror(tenantId, customers);

                var leadQuery = db.Leads.Where(l => !l.IsDeleted);
                if (isAgent && userId > 0)
                {
                    leadQuery = leadQuery.Where(l =>
                        (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                            ? l.AssignedAgentId.Value == userId
                            : l.CreatedByUserId == userId);
                }
                var leads = await leadQuery.AsNoTracking().ToListAsync();
                _localCache.SaveLeadsMirror(tenantId, leads);

                var dealQuery = db.Deals.Include(d => d.Customer)
                    .Include(d => d.Property)
                    .Include(d => d.Agent)
                    .AsQueryable();
                if (isAgent && userId > 0)
                {
                    dealQuery = dealQuery.Where(d =>
                        (d.AgentId.HasValue && d.AgentId.Value > 0)
                            ? d.AgentId.Value == userId
                            : d.CreatedByUserId == userId);
                }
                var deals = await dealQuery.AsNoTracking().ToListAsync();
                _localCache.SaveDealsMirror(tenantId, deals);

                var actQuery = db.Activities.AsQueryable();
                if (isAgent && userId > 0)
                {
                    actQuery = actQuery.Where(a => a.LoggedByAgentId == userId);
                }
                var activities = await actQuery.AsNoTracking().ToListAsync();
                _localCache.SaveActivitiesMirror(tenantId, activities);

                var reminderQuery = db.TaskReminders.Where(r => !r.IsDeleted);
                if (isAgent && userId > 0)
                {
                    reminderQuery = reminderQuery.Where(r => r.AssignedToUserId == userId);
                }
                var reminders = await reminderQuery.AsNoTracking().ToListAsync();
                _localCache.SaveTaskRemindersMirror(tenantId, reminders);

                var ticketQuery = db.SupportTickets.Include(t => t.Customer)
                    .Where(t => !t.IsDeleted);
                if (isAgent && userId > 0)
                {
                    ticketQuery = ticketQuery.Where(t => t.AssignedToUserId == userId || t.RaisedByUserId == userId);
                }
                var tickets = await ticketQuery.AsNoTracking().ToListAsync();
                _localCache.SaveSupportTicketsMirror(tenantId, tickets);

                Log($"[Cache Refreshed] Local mirror updated for User #{userId} ({roleName}): {customers.Count} Customers, {leads.Count} Leads, {deals.Count} Deals, {activities.Count} Activities, {reminders.Count} Reminders, {tickets.Count} Tickets.");
            }
            catch (Exception ex)
            {
                Log($"[Cache Refresh Warning] Error refreshing local mirror: {ex.Message}");
            }
        }

        private void NotifyProgress(string message, bool isRunning = false)
        {
            int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
            var counts = _localCache.GetQueueCounts(tenantId);
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
            {
                Message = message,
                PendingCount = counts.Pending,
                FailedCount = counts.Failed,
                ConflictCount = counts.Conflict,
                SyncedCount = counts.Synced,
                IsRunning = isRunning
            });
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                // ignore logging failures
            }
        }

        public async Task EnsureTenantDataPopulatedAsync(int tenantId)
        {
            if (tenantId <= 0) tenantId = 1;

            try
            {
                using var local = LocalDb.CreateContext(tenantId);
                bool localEmpty = !await local.Customers.AnyAsync() || !await local.Properties.AnyAsync();

                if (localEmpty)
                {
                    Log($"[Tenant {tenantId}] Local operational data is empty. Starting population...");
                    NotifyProgress("Checking cloud and local data...", isRunning: true);

                    // If online and cloud is configured, pull from cloud
                    bool online = await CheckConnectivityAsync();
                    if (online && !string.IsNullOrWhiteSpace(_cloudConnection))
                    {
                        await SyncAsync();
                    }

                    // Recheck after sync
                    using var localRecheck = LocalDb.CreateContext(tenantId);
                    if (!await localRecheck.Customers.AnyAsync() || !await localRecheck.Properties.AnyAsync())
                    {
                        Log($"[Tenant {tenantId}] Seeding full sample data into local database...");
                        await DbSeeder.SeedTestUsersAsync(localRecheck, tenantId);

                        // If online, push seeded data to cloud
                        if (online && !string.IsNullOrWhiteSpace(_cloudConnection))
                        {
                            await SyncAsync();
                        }
                    }
                }

                // Refresh SQLite mirror cache for fast offline access
                await RefreshUserCacheAsync(
                    tenantId,
                    CurrentSession.UserId,
                    CurrentSession.CurrentUser?.Role.ToString() ?? "Admin");
            }
            catch (Exception ex)
            {
                Log($"[EnsureTenantDataPopulatedAsync Error] {ex.Message}");
            }
        }

        public void Stop() => _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        public void Dispose()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
