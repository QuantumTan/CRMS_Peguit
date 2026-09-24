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
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
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
        private readonly object _syncLock = new();
        private int _failureCount;
        private int _baseIntervalSeconds;

        public bool IsOnline { get; private set; } = true;
        public bool IsSyncing => _isSyncing;
        public bool IsDraining => _isSyncing;

        public event EventHandler<bool>? ConnectivityChanged;
        public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

        private static readonly HashSet<string> TablesWithIdentity = new(StringComparer.OrdinalIgnoreCase)
        {
            "Roles", "Persons", "Users", "LoginSessions", "Customers", "Properties", "Leads", "Deals",
            "Activities", "PropertyShowingDetails", "SupportTickets", "Subscriptions",
            "SystemSettings", "BackupLogs", "DealContingencies", "DealClauses", "BuyerProfiles"
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
            _failureCount = 0;
        }

        public SyncService(string? localConnection, string? cloudConnection)
            : this(DbConfiguration.GetApiBaseUrl())
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
            if (!string.IsNullOrWhiteSpace(_cloudConnection))
            {
                try
                {
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(_cloudConnection);
                    await conn.OpenAsync();
                    bool wasOnline = IsOnline;
                    IsOnline = true;
                    if (!wasOnline)
                    {
                        ConnectivityChanged?.Invoke(this, true);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    bool wasOnline = IsOnline;
                    IsOnline = false;
                    if (wasOnline)
                    {
                        ConnectivityChanged?.Invoke(this, false);
                        Log($"Network status: OFFLINE ({ex.Message})");
                    }
                    return false;
                }
            }

            // Fallback to HTTP ping
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var response = await _httpClient.GetAsync("api/auth/ping", cts.Token);
                bool currentOnline = response.IsSuccessStatusCode;
                bool wasOnline = IsOnline;
                IsOnline = currentOnline;
                if (wasOnline != currentOnline)
                {
                    ConnectivityChanged?.Invoke(this, currentOnline);
                }
                return currentOnline;
            }
            catch
            {
                bool wasOnline = IsOnline;
                IsOnline = false;
                if (wasOnline)
                {
                    ConnectivityChanged?.Invoke(this, false);
                }
                return false;
            }
        }

        /// <summary>
        /// Main synchronization trigger — runs automatically in background and on demand via 'Sync Now'.
        /// Pushes local CRM data directly to cloud SQL Server, drains offline SQLite queue, and refreshes mirror cache.
        /// </summary>
        public async Task SyncAsync()
        {
            if (_isSyncing) return;
            lock (_syncLock)
            {
                if (_isSyncing) return;
                _isSyncing = true;
            }

            try
            {
                NotifyProgress("Checking cloud database connection...", isRunning: true);

                bool online = await CheckConnectivityAsync();
                if (!online)
                {
                    NotifyProgress("Cloud database offline / unreachable.", isRunning: false);
                    return;
                }

                Log("Sync started (local → cloud)...");
                NotifyProgress("Synchronizing data to cloud...", isRunning: true);

                int totalChanges = 0;
                var details = new List<string>();

                if (!string.IsNullOrWhiteSpace(_cloudConnection))
                {
                    int activeTenant = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                    var tenantsToSync = new List<int> { activeTenant };

                    // Also sync other tenants if running in elevated mode or database exists
                    foreach (var tid in new[] { 1, 2, 3 })
                    {
                        if (!tenantsToSync.Contains(tid))
                            tenantsToSync.Add(tid);
                    }

                    foreach (var tid in tenantsToSync)
                    {
                        try
                        {
                            await using var local = LocalDb.CreateContext(tid);
                            await using var cloud = CreateCloudContext(tid);

                            // Ensure cloud schema columns
                            SchemaRepairService.EnsureCrmPolishColumns(cloud);

                            // Pull remotely registered users and roles
                            await PullMissingUsersAndRoles(local, cloud, tid);

                            int tChanges = 0;
                            tChanges += await SyncTable<Role>(local, cloud, "Roles", details);
                            tChanges += await SyncTable<Person>(local, cloud, "Persons", details);
                            tChanges += await SyncTable<User>(local, cloud, "Users", details);
                            tChanges += await SyncTable<Customer>(local, cloud, "Customers", details);
                            tChanges += await SyncTable<BuyerProfile>(local, cloud, "BuyerProfiles", details);
                            tChanges += await SyncTable<Property>(local, cloud, "Properties", details);
                            tChanges += await SyncTable<Lead>(local, cloud, "Leads", details);
                            tChanges += await SyncTable<Deal>(local, cloud, "Deals", details);
                            tChanges += await SyncTable<DealContingency>(local, cloud, "DealContingencies", details);
                            tChanges += await SyncTable<DealClause>(local, cloud, "DealClauses", details);
                            tChanges += await SyncTable<Activity>(local, cloud, "Activities", details);
                            tChanges += await SyncTable<PropertyShowingDetail>(local, cloud, "PropertyShowingDetails", details);
                            tChanges += await SyncTable<SupportTicket>(local, cloud, "SupportTickets", details);
                            tChanges += await SyncTable<Subscription>(local, cloud, "Subscriptions", details);
                            tChanges += await SyncTable<SystemSetting>(local, cloud, "SystemSettings", details);
                            tChanges += await SyncTable<BackupLog>(local, cloud, "BackupLogs", details);
                            tChanges += await SyncTable<LoginSession>(local, cloud, "LoginSessions", details);

                            totalChanges += tChanges;
                        }
                        catch (Exception ex)
                        {
                            Log($"[Tenant {tid}] Sync notice: {ex.Message}");
                            if (ex.InnerException != null)
                                Log($"[Tenant {tid}] INNER: {ex.InnerException.Message}");
                        }
                    }
                }

                // Drain any pending SQLite offline queue items
                await DrainQueueAsync();

                // Refresh user SQLite mirror cache
                int curTenant = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                await RefreshUserCacheAsync(curTenant, CurrentSession.UserId, CurrentSession.CurrentUser?.Role.ToString() ?? "Admin");

                if (totalChanges > 0)
                {
                    Log($"Sync completed successfully at {DateTime.Now:HH:mm:ss}. Pushed {totalChanges} record change(s) ({string.Join(", ", details)}).");
                    NotifyProgress($"Sync completed! Pushed {totalChanges} record change(s).", isRunning: false);
                }
                else
                {
                    Log($"Sync completed successfully at {DateTime.Now:HH:mm:ss}. Cloud database is up to date (0 changes).");
                    NotifyProgress("Cloud database is up to date.", isRunning: false);
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
            }
        }

        private RealEstateDbContext CreateCloudContext(int tenantId)
        {
            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(_cloudConnection, sql => sql.CommandTimeout(60).EnableRetryOnFailure(3))
                .Options;
            return new RealEstateDbContext(options, tenantId);
        }

        private async Task<int> SyncTable<T>(
            RealEstateDbContext local,
            RealEstateDbContext cloud,
            string tableName,
            List<string> details)
            where T : class
        {
            cloud.ChangeTracker.Clear();

            var localRows = await local.Set<T>().IgnoreQueryFilters().AsNoTracking().ToListAsync();
            if (localRows.Count == 0) return 0;

            var cloudRows = await cloud.Set<T>().IgnoreQueryFilters().AsNoTracking().ToListAsync();

            var entityType = cloud.Model.FindEntityType(typeof(T));
            var primaryKey = entityType?.FindPrimaryKey();
            var keyProp = primaryKey?.Properties[0].PropertyInfo;
            if (keyProp == null) return 0;

            var cloudMap = cloudRows.ToDictionary(r => keyProp.GetValue(r)!, r => r);

            var toInsert = new List<T>();
            var toUpdate = new List<T>();

            foreach (var localRow in localRows)
            {
                var keyVal = keyProp.GetValue(localRow)!;
                if (!cloudMap.TryGetValue(keyVal, out var existing))
                {
                    toInsert.Add(localRow);
                }
                else
                {
                    bool isDifferent = false;
                    foreach (var prop in entityType!.GetProperties())
                    {
                        var pInfo = prop.PropertyInfo;
                        if (pInfo == null) continue;
                        var localVal = pInfo.GetValue(localRow);
                        var cloudVal = pInfo.GetValue(existing);

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
                        toUpdate.Add(localRow);
                    }
                }
            }

            if (toInsert.Count == 0 && toUpdate.Count == 0)
            {
                return 0;
            }

            var cloudSet = cloud.Set<T>();

            foreach (var item in toUpdate)
            {
                cloudSet.Attach(item);
                cloud.Entry(item).State = EntityState.Modified;
            }

            foreach (var item in toInsert)
            {
                cloudSet.Add(item);
            }

            bool hasIdentity = TablesWithIdentity.Contains(tableName);
            var strategy = cloud.Database.CreateExecutionStrategy();

            if (toInsert.Count > 0 && hasIdentity)
            {
                await strategy.ExecuteAsync(async () =>
                {
                    var safeTableName = tableName.Replace("]", "]]");
                    await using var tx = await cloud.Database.BeginTransactionAsync();

                    await cloud.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{safeTableName}] ON;");
                    await cloud.SaveChangesAsync();
                    await cloud.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{safeTableName}] OFF;");

                    await tx.CommitAsync();
                });
            }
            else
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await cloud.SaveChangesAsync();
                });
            }

            cloud.ChangeTracker.Clear();

            int tableChanges = toInsert.Count + toUpdate.Count;
            details.Add($"{tableName}: +{toInsert.Count} ins, ~{toUpdate.Count} upd");
            return tableChanges;
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

                var localUsers = await local.Users.IgnoreQueryFilters().Include(u => u.Person).AsNoTracking().ToListAsync();
                var cloudUsers = await cloud.Users.IgnoreQueryFilters().Include(u => u.Person).Include(u => u.Role).AsNoTracking().ToListAsync();
                var missingUsers = cloudUsers.Where(cu => cu.Role?.TenantId == tenantId && !localUsers.Any(lu => lu.UserId == cu.UserId || (lu.Person != null && cu.Person != null && lu.Person.Email.ToLower() == cu.Person.Email.ToLower()))).ToList();

                foreach (var user in missingUsers)
                {
                    string email = user.Person?.Email ?? "";
                    if (string.IsNullOrWhiteSpace(email)) continue;

                    await local.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = {0})
                        BEGIN
                            DECLARE @PersonId INT;
                            SELECT TOP 1 @PersonId = PersonId FROM Persons WHERE Email = {3};
                            IF @PersonId IS NULL
                            BEGIN
                                INSERT INTO Persons (FirstName, MiddleName, LastName, Suffix, Email, Phone, CreatedAt)
                                VALUES ({1}, {2}, {4}, {5}, {3}, {6}, GETUTCDATE());
                                SET @PersonId = SCOPE_IDENTITY();
                            END

                            SET IDENTITY_INSERT Users ON;
                            INSERT INTO Users (UserId, PersonId, PasswordHash, RoleId, Status, CreatedAt, BranchId)
                            VALUES ({0}, @PersonId, {7}, {8}, {9}, {10}, {11});
                            SET IDENTITY_INSERT Users OFF;
                        END",
                        user.UserId,
                        user.Person?.FirstName ?? "User",
                        (object?)user.Person?.MiddleName ?? DBNull.Value,
                        email,
                        user.Person?.LastName ?? "",
                        (object?)user.Person?.Suffix ?? DBNull.Value,
                        (object?)user.Person?.Phone ?? DBNull.Value,
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

        public PendingSyncQueue EnqueueOfflineCreate<T>(string entityType, T entity, int tenantId, int userId)
        {
            string json = JsonSerializer.Serialize(entity);
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
            item.EntityLocalId = (-queueId).ToString();
            _localCache.UpdateEntityLocalId(queueId, item.EntityLocalId);

            Log($"Queued offline INSERT for {entityType} [TempId: -{queueId}] by User #{userId}");
            NotifyProgress($"Queued {entityType} creation for sync.");
            return item;
        }

        public PendingSyncQueue EnqueueOfflineUpdate<T>(string entityType, int serverId, T entity, int tenantId, int userId, DateTime? cachedVersionTimestamp)
        {
            string json = JsonSerializer.Serialize(entity);
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

            Log($"Queued offline UPDATE for {entityType} #{serverId} by User #{userId}");
            NotifyProgress($"Queued {entityType} update for sync.");
            return item;
        }

        public async Task DrainQueueAsync()
        {
            try
            {
                int tenantId = CurrentSession.TenantId > 0 ? CurrentSession.TenantId : 1;
                var pendingItems = _localCache.GetPendingQueue(tenantId);
                if (pendingItems.Count == 0) return;

                Log($"Draining {pendingItems.Count} offline queued item(s)...");
                ApplyAuthHeader();

                var tempIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in pendingItems)
                {
                    try
                    {
                        _localCache.UpdateQueueStatus(item.QueueId, "Syncing");
                        bool success = await ProcessQueueItemAsync(item, tempIdMap);
                        if (!success && !IsOnline) break;
                    }
                    catch (Exception ex)
                    {
                        Log($"Queue item #{item.QueueId} failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"DrainQueueAsync error: {ex.Message}");
            }
        }

        private async Task<bool> ProcessQueueItemAsync(PendingSyncQueue item, Dictionary<string, int> tempIdMap)
        {
            try
            {
                string endpoint = GetEndpointForEntity(item.EntityType);
                if (item.Operation.Equals("Insert", StringComparison.OrdinalIgnoreCase))
                {
                    string payload = RewriteTempForeignKeys(item.PayloadJson, tempIdMap);
                    using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(endpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var createdJson = await response.Content.ReadAsStringAsync();
                        int serverId = ExtractIdFromJson(createdJson, item.EntityType);

                        if (!string.IsNullOrEmpty(item.EntityLocalId) && serverId > 0)
                        {
                            tempIdMap[$"{item.EntityType}:{item.EntityLocalId}"] = serverId;
                        }

                        _localCache.UpdateQueueSuccess(item.QueueId, serverId);
                        Log($"[Replay Success] Queued INSERT #{item.QueueId} created {item.EntityType} #{serverId} on server.");
                        return true;
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        _localCache.UpdateQueueFailure(item.QueueId, $"Server returned {(int)response.StatusCode}: {err}");
                        return false;
                    }
                }
                else if (item.Operation.Equals("Update", StringComparison.OrdinalIgnoreCase))
                {
                    if (!item.ServerEntityId.HasValue)
                    {
                        _localCache.UpdateQueueFailure(item.QueueId, "Cannot update without ServerEntityId.");
                        return false;
                    }

                    int serverId = item.ServerEntityId.Value;
                    var checkRes = await _httpClient.GetAsync($"{endpoint}/{serverId}");

                    string serverPayload = string.Empty;
                    if (checkRes.StatusCode == System.Net.HttpStatusCode.Conflict ||
                        (checkRes.IsSuccessStatusCode && CheckTimestampConflict(await checkRes.Content.ReadAsStringAsync(), item.ServerVersionTimestamp, out serverPayload)))
                    {
                        _localCache.UpdateQueueConflict(item.QueueId, !string.IsNullOrEmpty(serverPayload) ? serverPayload : await checkRes.Content.ReadAsStringAsync());
                        Log($"[Conflict Detected] {item.EntityType} #{serverId} was updated on cloud while client was offline.");
                        return false;
                    }

                    string payload = RewriteTempForeignKeys(item.PayloadJson, tempIdMap);
                    using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{endpoint}/{serverId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _localCache.UpdateQueueSuccess(item.QueueId, serverId);
                        Log($"[Replay Success] Queued UPDATE #{item.QueueId} applied to {item.EntityType} #{serverId}.");
                        return true;
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        _localCache.UpdateQueueFailure(item.QueueId, $"Server returned {(int)response.StatusCode}: {err}");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _localCache.UpdateQueueFailure(item.QueueId, ex.Message);
                return false;
            }
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

                var custQuery = db.Customers.Include(c => c.Person).Where(c => !c.IsDeleted);
                if (isAgent && userId > 0)
                {
                    custQuery = custQuery.Where(c =>
                        (c.AssignedAgentId.HasValue && c.AssignedAgentId.Value > 0)
                            ? c.AssignedAgentId.Value == userId
                            : c.CreatedByUserId == userId);
                }
                var customers = await custQuery.AsNoTracking().ToListAsync();
                _localCache.SaveCustomersMirror(tenantId, customers);

                var leadQuery = db.Leads.Include(l => l.Person).Where(l => !l.IsDeleted);
                if (isAgent && userId > 0)
                {
                    leadQuery = leadQuery.Where(l =>
                        (l.AssignedAgentId.HasValue && l.AssignedAgentId.Value > 0)
                            ? l.AssignedAgentId.Value == userId
                            : l.CreatedByUserId == userId);
                }
                var leads = await leadQuery.AsNoTracking().ToListAsync();
                _localCache.SaveLeadsMirror(tenantId, leads);

                var dealQuery = db.Deals.Include(d => d.Customer).ThenInclude(c => c!.Person)
                    .Include(d => d.Property)
                    .Include(d => d.Agent).ThenInclude(a => a!.Person)
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

                var ticketQuery = db.SupportTickets.Include(t => t.Customer).ThenInclude(c => c!.Person)
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

        public void Stop() => _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        public void Dispose()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
