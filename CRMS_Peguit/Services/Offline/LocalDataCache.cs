using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using CRMS_Peguit.domain.entities;

namespace CRMS_Peguit.winforms.Services.Offline
{
    /// <summary>
    /// SQLite local database manager extending LocalAuthCache.
    /// Manages the PendingSyncQueue and read-only local mirror tables for operational CRM data.
    /// </summary>
    public class LocalDataCache
    {
        private static readonly Lazy<LocalDataCache> _instance = new(() => new LocalDataCache());
        public static LocalDataCache Instance => _instance.Value;

        private readonly string _connectionString;
        private readonly object _dbLock = new();

        public LocalDataCache()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CRMS_Peguit", "local_cache.db"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath}";
            EnsureTablesExist();
        }

        private SqliteConnection CreateOpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void EnsureTablesExist()
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    -- PENDING SYNC QUEUE
                    CREATE TABLE IF NOT EXISTS PendingSyncQueue (
                        QueueId INTEGER PRIMARY KEY AUTOINCREMENT,
                        TenantId INTEGER NOT NULL,
                        UserId INTEGER NOT NULL,
                        EntityType TEXT NOT NULL,
                        EntityLocalId TEXT NOT NULL,
                        ServerEntityId INTEGER,
                        Operation TEXT NOT NULL,
                        PayloadJson TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        FailureReason TEXT,
                        ServerVersionTimestamp TEXT,
                        ServerConflictPayload TEXT,
                        AttemptCount INTEGER NOT NULL DEFAULT 0,
                        LastAttemptAt TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_PendingSyncQueue_Status ON PendingSyncQueue(Status);
                    CREATE INDEX IF NOT EXISTS IX_PendingSyncQueue_TenantUser ON PendingSyncQueue(TenantId, UserId);

                    -- CUSTOMER MIRROR
                    CREATE TABLE IF NOT EXISTS CustomerMirror (
                        CustomerId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        PersonId INTEGER NOT NULL,
                        FirstName TEXT NOT NULL,
                        MiddleName TEXT,
                        LastName TEXT NOT NULL,
                        Suffix TEXT,
                        Phone TEXT,
                        Email TEXT,
                        Type TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        CreatedByUserId INTEGER NOT NULL,
                        AssignedAgentId INTEGER,
                        AssignmentStatus TEXT NOT NULL,
                        AssignmentReviewedByUserId INTEGER,
                        AssignmentReviewedAt TEXT,
                        AssignmentReviewNotes TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT,
                        LastMarketUpdateSentAt TEXT,
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        DeletedAt TEXT,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_CustomerMirror_Tenant ON CustomerMirror(TenantId);

                    -- LEAD MIRROR
                    CREATE TABLE IF NOT EXISTS LeadMirror (
                        LeadId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        PersonId INTEGER NOT NULL,
                        FirstName TEXT NOT NULL,
                        MiddleName TEXT,
                        LastName TEXT NOT NULL,
                        Suffix TEXT,
                        Phone TEXT,
                        Email TEXT,
                        Source TEXT,
                        Notes TEXT,
                        Stage TEXT NOT NULL,
                        Priority TEXT,
                        ExpectedValue REAL,
                        CreatedByUserId INTEGER NOT NULL,
                        AssignedAgentId INTEGER,
                        AssignmentStatus TEXT NOT NULL,
                        AssignmentReviewedByUserId INTEGER,
                        AssignmentReviewedAt TEXT,
                        AssignmentReviewNotes TEXT,
                        ConvertedCustomerId INTEGER,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT,
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        DeletedAt TEXT,
                        BranchId INTEGER,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_LeadMirror_Tenant ON LeadMirror(TenantId);

                    -- DEAL MIRROR
                    CREATE TABLE IF NOT EXISTS DealMirror (
                        DealId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        CustomerId INTEGER NOT NULL,
                        CustomerName TEXT,
                        PropertyId INTEGER NOT NULL,
                        PropertyAddress TEXT,
                        AgentId INTEGER,
                        AgentName TEXT,
                        CreatedByUserId INTEGER NOT NULL,
                        Value REAL NOT NULL,
                        CommissionRate REAL NOT NULL,
                        Stage TEXT NOT NULL,
                        ExpectedCloseDate TEXT,
                        PaymentScheme TEXT,
                        ReservationFee REAL,
                        DownPaymentPercent REAL,
                        CgtPayer TEXT,
                        DstPayer TEXT,
                        TransferTaxPayer TEXT,
                        RegistrationFeePayer TEXT,
                        SpecialStipulations TEXT,
                        ContractSignedDate TEXT,
                        BranchId INTEGER,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_DealMirror_Tenant ON DealMirror(TenantId);

                    -- ACTIVITY MIRROR
                    CREATE TABLE IF NOT EXISTS ActivityMirror (
                        ActivityId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        Type TEXT NOT NULL,
                        RelatedLeadId INTEGER,
                        RelatedCustomerId INTEGER,
                        LoggedByAgentId INTEGER NOT NULL,
                        Notes TEXT,
                        ActivityDate TEXT NOT NULL,
                        Outcome TEXT,
                        DurationMinutes INTEGER,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_ActivityMirror_Tenant ON ActivityMirror(TenantId);

                    -- TASK REMINDER MIRROR
                    CREATE TABLE IF NOT EXISTS TaskReminderMirror (
                        TaskReminderId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        DueDate TEXT NOT NULL,
                        AssignedToUserId INTEGER NOT NULL,
                        RelatedCustomerId INTEGER,
                        RelatedLeadId INTEGER,
                        Status TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        Notes TEXT,
                        Priority TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT,
                        CompletedAt TEXT,
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        DeletedAt TEXT,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_TaskReminderMirror_Tenant ON TaskReminderMirror(TenantId);

                    -- SUPPORT TICKET MIRROR
                    CREATE TABLE IF NOT EXISTS SupportTicketMirror (
                        TicketId INTEGER PRIMARY KEY,
                        TenantId INTEGER NOT NULL,
                        TicketNumber TEXT NOT NULL,
                        CustomerId INTEGER NOT NULL,
                        CustomerName TEXT,
                        RaisedByUserId INTEGER NOT NULL,
                        AssignedToUserId INTEGER,
                        Category TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Priority TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        DueDate TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT,
                        FirstRespondedAt TEXT,
                        ResolvedAt TEXT,
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        DeletedAt TEXT,
                        LastSyncedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_SupportTicketMirror_Tenant ON SupportTicketMirror(TenantId);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        // =========================================================================
        // PENDING SYNC QUEUE OPERATIONS
        // =========================================================================

        public int Enqueue(PendingSyncQueue item)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    INSERT INTO PendingSyncQueue (
                        TenantId, UserId, EntityType, EntityLocalId, ServerEntityId,
                        Operation, PayloadJson, CreatedAt, Status, FailureReason,
                        ServerVersionTimestamp, ServerConflictPayload, AttemptCount, LastAttemptAt
                    ) VALUES (
                        $tenantId, $userId, $entityType, $entityLocalId, $serverEntityId,
                        $operation, $payloadJson, $createdAt, $status, $failureReason,
                        $serverVersionTimestamp, $serverConflictPayload, $attemptCount, $lastAttemptAt
                    );
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("$tenantId", item.TenantId);
                cmd.Parameters.AddWithValue("$userId", item.UserId);
                cmd.Parameters.AddWithValue("$entityType", item.EntityType);
                cmd.Parameters.AddWithValue("$entityLocalId", item.EntityLocalId);
                cmd.Parameters.AddWithValue("$serverEntityId", (object?)item.ServerEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$operation", item.Operation);
                cmd.Parameters.AddWithValue("$payloadJson", item.PayloadJson);
                cmd.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("O"));
                cmd.Parameters.AddWithValue("$status", item.Status);
                cmd.Parameters.AddWithValue("$failureReason", (object?)item.FailureReason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$serverVersionTimestamp", item.ServerVersionTimestamp.HasValue ? item.ServerVersionTimestamp.Value.ToString("O") : DBNull.Value);
                cmd.Parameters.AddWithValue("$serverConflictPayload", (object?)item.ServerConflictPayload ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$attemptCount", item.AttemptCount);
                cmd.Parameters.AddWithValue("$lastAttemptAt", item.LastAttemptAt.HasValue ? item.LastAttemptAt.Value.ToString("O") : DBNull.Value);

                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                item.QueueId = newId;
                return newId;
            }
        }

        public List<PendingSyncQueue> GetPendingQueue(int tenantId)
        {
            lock (_dbLock)
            {
                var list = new List<PendingSyncQueue>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT QueueId, TenantId, UserId, EntityType, EntityLocalId, ServerEntityId,
                           Operation, PayloadJson, CreatedAt, Status, FailureReason,
                           ServerVersionTimestamp, ServerConflictPayload, AttemptCount, LastAttemptAt
                    FROM PendingSyncQueue
                    WHERE TenantId = $tenantId AND Status IN ('Pending', 'Syncing', 'Failed')
                    ORDER BY QueueId ASC;";
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(ReadQueueItem(reader));
                }
                return list;
            }
        }

        public List<PendingSyncQueue> GetAllQueueItems(int tenantId)
        {
            lock (_dbLock)
            {
                var list = new List<PendingSyncQueue>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT QueueId, TenantId, UserId, EntityType, EntityLocalId, ServerEntityId,
                           Operation, PayloadJson, CreatedAt, Status, FailureReason,
                           ServerVersionTimestamp, ServerConflictPayload, AttemptCount, LastAttemptAt
                    FROM PendingSyncQueue
                    WHERE TenantId = $tenantId
                    ORDER BY QueueId DESC;";
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(ReadQueueItem(reader));
                }
                return list;
            }
        }

        public PendingSyncQueue? GetQueueItem(int queueId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT QueueId, TenantId, UserId, EntityType, EntityLocalId, ServerEntityId,
                           Operation, PayloadJson, CreatedAt, Status, FailureReason,
                           ServerVersionTimestamp, ServerConflictPayload, AttemptCount, LastAttemptAt
                    FROM PendingSyncQueue
                    WHERE QueueId = $queueId;";
                cmd.Parameters.AddWithValue("$queueId", queueId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return ReadQueueItem(reader);
                }
                return null;
            }
        }

        public void UpdateQueueStatus(int queueId, string status, string? failureReason = null, int? serverEntityId = null, string? conflictPayload = null)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    UPDATE PendingSyncQueue
                    SET Status = $status,
                        FailureReason = $failureReason,
                        ServerEntityId = COALESCE($serverEntityId, ServerEntityId),
                        ServerConflictPayload = COALESCE($conflictPayload, ServerConflictPayload),
                        AttemptCount = AttemptCount + 1,
                        LastAttemptAt = $now
                    WHERE QueueId = $queueId;";

                cmd.Parameters.AddWithValue("$status", status);
                cmd.Parameters.AddWithValue("$failureReason", (object?)failureReason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$serverEntityId", (object?)serverEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$conflictPayload", (object?)conflictPayload ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("$queueId", queueId);

                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateQueueSuccess(int queueId, int serverEntityId)
        {
            UpdateQueueStatus(queueId, "Synced", null, serverEntityId, null);
        }

        public void UpdateQueueFailure(int queueId, string failureReason)
        {
            UpdateQueueStatus(queueId, "Failed", failureReason);
        }

        public void UpdateQueueConflict(int queueId, string conflictPayload)
        {
            UpdateQueueStatus(queueId, "Conflict", "Conflict detected with server state.", null, conflictPayload);
        }

        public void UpdateEntityLocalId(int queueId, string entityLocalId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE PendingSyncQueue SET EntityLocalId = $localId WHERE QueueId = $queueId;";
                cmd.Parameters.AddWithValue("$localId", entityLocalId);
                cmd.Parameters.AddWithValue("$queueId", queueId);
                cmd.ExecuteNonQuery();
            }
        }

        public int EnqueueItem(PendingSyncQueue item) => Enqueue(item);

        public void UpdateQueuePayload(int queueId, string newPayloadJson)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    UPDATE PendingSyncQueue
                    SET PayloadJson = $payload,
                        Status = 'Pending',
                        FailureReason = NULL,
                        ServerConflictPayload = NULL
                    WHERE QueueId = $queueId;";

                cmd.Parameters.AddWithValue("$payload", newPayloadJson);
                cmd.Parameters.AddWithValue("$queueId", queueId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteQueueItem(int queueId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM PendingSyncQueue WHERE QueueId = $queueId;";
                cmd.Parameters.AddWithValue("$queueId", queueId);
                cmd.ExecuteNonQuery();
            }
        }

        public void ClearSyncedQueue(int tenantId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM PendingSyncQueue WHERE TenantId = $tenantId AND Status = 'Synced';";
                cmd.Parameters.AddWithValue("$tenantId", tenantId);
                cmd.ExecuteNonQuery();
            }
        }

        public void ClearAllQueue(int tenantId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM PendingSyncQueue WHERE TenantId = $tenantId;";
                cmd.Parameters.AddWithValue("$tenantId", tenantId);
                cmd.ExecuteNonQuery();
            }
        }

        public (int Pending, int Syncing, int Failed, int Conflict, int Synced) GetQueueCounts(int tenantId)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Status, COUNT(*)
                    FROM PendingSyncQueue
                    WHERE TenantId = $tenantId
                    GROUP BY Status;";
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                int pending = 0, syncing = 0, failed = 0, conflict = 0, synced = 0;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var status = reader.GetString(0);
                    var count = reader.GetInt32(1);
                    switch (status)
                    {
                        case "Pending": pending = count; break;
                        case "Syncing": syncing = count; break;
                        case "Failed": failed = count; break;
                        case "Conflict": conflict = count; break;
                        case "Synced": synced = count; break;
                    }
                }
                return (pending, syncing, failed, conflict, synced);
            }
        }

        private static PendingSyncQueue ReadQueueItem(SqliteDataReader reader)
        {
            return new PendingSyncQueue
            {
                QueueId = reader.GetInt32(0),
                TenantId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                EntityType = reader.GetString(3),
                EntityLocalId = reader.GetString(4),
                ServerEntityId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Operation = reader.GetString(6),
                PayloadJson = reader.GetString(7),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
                Status = reader.GetString(9),
                FailureReason = reader.IsDBNull(10) ? null : reader.GetString(10),
                ServerVersionTimestamp = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
                ServerConflictPayload = reader.IsDBNull(12) ? null : reader.GetString(12),
                AttemptCount = reader.GetInt32(13),
                LastAttemptAt = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14))
            };
        }

        // =========================================================================
        // LOCAL READ-ONLY MIRROR CACHE OPERATIONS
        // =========================================================================

        public void SaveCustomersMirror(int tenantId, IEnumerable<Customer> customers)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var c in customers)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO CustomerMirror (
                            CustomerId, TenantId, PersonId, FirstName, MiddleName, LastName, Suffix,
                            Phone, Email, Type, Status, CreatedByUserId, AssignedAgentId,
                            AssignmentStatus, AssignmentReviewedByUserId, AssignmentReviewedAt,
                            AssignmentReviewNotes, CreatedAt, UpdatedAt, LastMarketUpdateSentAt,
                            IsDeleted, DeletedAt, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $personId, $first, $middle, $last, $suffix,
                            $phone, $email, $type, $status, $createdBy, $assignedAgent,
                            $assignStatus, $reviewedBy, $reviewedAt, $reviewNotes,
                            $createdAt, $updatedAt, $marketUpdate, $isDeleted, $deletedAt, $syncedAt
                        )
                        ON CONFLICT(CustomerId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            PersonId = excluded.PersonId,
                            FirstName = excluded.FirstName,
                            MiddleName = excluded.MiddleName,
                            LastName = excluded.LastName,
                            Suffix = excluded.Suffix,
                            Phone = excluded.Phone,
                            Email = excluded.Email,
                            Type = excluded.Type,
                            Status = excluded.Status,
                            CreatedByUserId = excluded.CreatedByUserId,
                            AssignedAgentId = excluded.AssignedAgentId,
                            AssignmentStatus = excluded.AssignmentStatus,
                            AssignmentReviewedByUserId = excluded.AssignmentReviewedByUserId,
                            AssignmentReviewedAt = excluded.AssignmentReviewedAt,
                            AssignmentReviewNotes = excluded.AssignmentReviewNotes,
                            CreatedAt = excluded.CreatedAt,
                            UpdatedAt = excluded.UpdatedAt,
                            LastMarketUpdateSentAt = excluded.LastMarketUpdateSentAt,
                            IsDeleted = excluded.IsDeleted,
                            DeletedAt = excluded.DeletedAt,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", c.CustomerId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$personId", c.PersonId);
                    cmd.Parameters.AddWithValue("$first", c.FirstName ?? "");
                    cmd.Parameters.AddWithValue("$middle", (object?)c.MiddleName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$last", c.LastName ?? "");
                    cmd.Parameters.AddWithValue("$suffix", (object?)c.Suffix ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$phone", (object?)c.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$email", (object?)c.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$type", c.Type ?? "buyer");
                    cmd.Parameters.AddWithValue("$status", c.Status ?? "active");
                    cmd.Parameters.AddWithValue("$createdBy", c.CreatedByUserId);
                    cmd.Parameters.AddWithValue("$assignedAgent", (object?)c.AssignedAgentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$assignStatus", c.AssignmentStatus ?? "pending_review");
                    cmd.Parameters.AddWithValue("$reviewedBy", (object?)c.AssignmentReviewedByUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$reviewedAt", c.AssignmentReviewedAt.HasValue ? c.AssignmentReviewedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$reviewNotes", (object?)c.AssignmentReviewNotes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdAt", c.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$updatedAt", c.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$marketUpdate", c.LastMarketUpdateSentAt.HasValue ? c.LastMarketUpdateSentAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$isDeleted", c.IsDeleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("$deletedAt", c.DeletedAt.HasValue ? c.DeletedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<Customer> GetCachedCustomers(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<Customer>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT CustomerId, PersonId, FirstName, MiddleName, LastName, Suffix, Phone, Email, Type, Status, CreatedByUserId, AssignedAgentId, AssignmentStatus, CreatedAt, IsDeleted FROM CustomerMirror WHERE TenantId = $tenantId AND IsDeleted = 0";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND ((AssignedAgentId IS NOT NULL AND AssignedAgentId = $userId) OR (AssignedAgentId IS NULL AND CreatedByUserId = $userId))";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY LastName, FirstName;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var p = new Person
                    {
                        PersonId = reader.GetInt32(1),
                        FirstName = reader.GetString(2),
                        MiddleName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        LastName = reader.GetString(4),
                        Suffix = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Email = reader.IsDBNull(7) ? null : reader.GetString(7)
                    };
                    var c = new Customer
                    {
                        CustomerId = reader.GetInt32(0),
                        PersonId = p.PersonId,
                        Person = p,
                        FirstName = p.FirstName,
                        MiddleName = p.MiddleName,
                        LastName = p.LastName,
                        Suffix = p.Suffix,
                        Phone = p.Phone,
                        Email = p.Email,
                        Type = reader.GetString(8),
                        Status = reader.GetString(9),
                        CreatedByUserId = reader.GetInt32(10),
                        AssignedAgentId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                        AssignmentStatus = reader.GetString(12),
                        CreatedAt = DateTime.Parse(reader.GetString(13)),
                        IsDeleted = reader.GetInt32(14) == 1
                    };
                    list.Add(c);
                }
                return list;
            }
        }

        public void SaveLeadsMirror(int tenantId, IEnumerable<Lead> leads)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var l in leads)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO LeadMirror (
                            LeadId, TenantId, PersonId, FirstName, MiddleName, LastName, Suffix,
                            Phone, Email, Source, Notes, Stage, Priority, ExpectedValue,
                            CreatedByUserId, AssignedAgentId, AssignmentStatus, AssignmentReviewedByUserId,
                            AssignmentReviewedAt, AssignmentReviewNotes, ConvertedCustomerId,
                            CreatedAt, UpdatedAt, IsDeleted, DeletedAt, BranchId, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $personId, $first, $middle, $last, $suffix,
                            $phone, $email, $source, $notes, $stage, $priority, $val,
                            $createdBy, $assignedAgent, $assignStatus, $reviewedBy,
                            $reviewedAt, $reviewNotes, $converted,
                            $createdAt, $updatedAt, $isDeleted, $deletedAt, $branchId, $syncedAt
                        )
                        ON CONFLICT(LeadId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            PersonId = excluded.PersonId,
                            FirstName = excluded.FirstName,
                            MiddleName = excluded.MiddleName,
                            LastName = excluded.LastName,
                            Suffix = excluded.Suffix,
                            Phone = excluded.Phone,
                            Email = excluded.Email,
                            Source = excluded.Source,
                            Notes = excluded.Notes,
                            Stage = excluded.Stage,
                            Priority = excluded.Priority,
                            ExpectedValue = excluded.ExpectedValue,
                            CreatedByUserId = excluded.CreatedByUserId,
                            AssignedAgentId = excluded.AssignedAgentId,
                            AssignmentStatus = excluded.AssignmentStatus,
                            AssignmentReviewedByUserId = excluded.AssignmentReviewedByUserId,
                            AssignmentReviewedAt = excluded.AssignmentReviewedAt,
                            AssignmentReviewNotes = excluded.AssignmentReviewNotes,
                            ConvertedCustomerId = excluded.ConvertedCustomerId,
                            CreatedAt = excluded.CreatedAt,
                            UpdatedAt = excluded.UpdatedAt,
                            IsDeleted = excluded.IsDeleted,
                            DeletedAt = excluded.DeletedAt,
                            BranchId = excluded.BranchId,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", l.LeadId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$personId", l.PersonId);
                    cmd.Parameters.AddWithValue("$first", l.FirstName ?? "");
                    cmd.Parameters.AddWithValue("$middle", (object?)l.MiddleName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$last", l.LastName ?? "");
                    cmd.Parameters.AddWithValue("$suffix", (object?)l.Suffix ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$phone", (object?)l.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$email", (object?)l.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$source", (object?)l.Source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$notes", (object?)l.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$stage", l.Stage ?? "new");
                    cmd.Parameters.AddWithValue("$priority", (object?)l.Priority ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$val", (object?)l.ExpectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdBy", l.CreatedByUserId);
                    cmd.Parameters.AddWithValue("$assignedAgent", (object?)l.AssignedAgentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$assignStatus", l.AssignmentStatus ?? "pending_review");
                    cmd.Parameters.AddWithValue("$reviewedBy", (object?)l.AssignmentReviewedByUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$reviewedAt", l.AssignmentReviewedAt.HasValue ? l.AssignmentReviewedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$reviewNotes", (object?)l.AssignmentReviewNotes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$converted", (object?)l.ConvertedCustomerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdAt", l.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$updatedAt", l.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$isDeleted", l.IsDeleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("$deletedAt", l.DeletedAt.HasValue ? l.DeletedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$branchId", (object?)l.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<Lead> GetCachedLeads(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<Lead>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT LeadId, PersonId, FirstName, MiddleName, LastName, Suffix, Phone, Email, Source, Notes, Stage, Priority, ExpectedValue, CreatedByUserId, AssignedAgentId, AssignmentStatus, CreatedAt, IsDeleted FROM LeadMirror WHERE TenantId = $tenantId AND IsDeleted = 0";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND ((AssignedAgentId IS NOT NULL AND AssignedAgentId = $userId) OR (AssignedAgentId IS NULL AND CreatedByUserId = $userId))";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY LastName, FirstName;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var p = new Person
                    {
                        PersonId = reader.GetInt32(1),
                        FirstName = reader.GetString(2),
                        MiddleName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        LastName = reader.GetString(4),
                        Suffix = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Email = reader.IsDBNull(7) ? null : reader.GetString(7)
                    };
                    var l = new Lead
                    {
                        LeadId = reader.GetInt32(0),
                        PersonId = p.PersonId,
                        Person = p,
                        FirstName = p.FirstName,
                        MiddleName = p.MiddleName,
                        LastName = p.LastName,
                        Suffix = p.Suffix,
                        Phone = p.Phone,
                        Email = p.Email,
                        Source = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Notes = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Stage = reader.GetString(10),
                        Priority = reader.IsDBNull(11) ? null : reader.GetString(11),
                        ExpectedValue = reader.IsDBNull(12) ? null : Convert.ToDecimal(reader.GetDouble(12)),
                        CreatedByUserId = reader.GetInt32(13),
                        AssignedAgentId = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                        AssignmentStatus = reader.GetString(15),
                        CreatedAt = DateTime.Parse(reader.GetString(16)),
                        IsDeleted = reader.GetInt32(17) == 1
                    };
                    list.Add(l);
                }
                return list;
            }
        }

        public void SaveDealsMirror(int tenantId, IEnumerable<Deal> deals)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var d in deals)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO DealMirror (
                            DealId, TenantId, CustomerId, CustomerName, PropertyId, PropertyAddress,
                            AgentId, AgentName, CreatedByUserId, Value, CommissionRate, Stage,
                            ExpectedCloseDate, PaymentScheme, ReservationFee, DownPaymentPercent,
                            CgtPayer, DstPayer, TransferTaxPayer, RegistrationFeePayer,
                            SpecialStipulations, ContractSignedDate, BranchId, CreatedAt, UpdatedAt, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $customerId, $customerName, $propId, $propAddress,
                            $agentId, $agentName, $createdBy, $val, $comm, $stage,
                            $closeDate, $scheme, $resFee, $dpPct,
                            $cgt, $dst, $tt, $reg,
                            $stip, $signedDate, $branchId, $createdAt, $updatedAt, $syncedAt
                        )
                        ON CONFLICT(DealId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            CustomerId = excluded.CustomerId,
                            CustomerName = excluded.CustomerName,
                            PropertyId = excluded.PropertyId,
                            PropertyAddress = excluded.PropertyAddress,
                            AgentId = excluded.AgentId,
                            AgentName = excluded.AgentName,
                            CreatedByUserId = excluded.CreatedByUserId,
                            Value = excluded.Value,
                            CommissionRate = excluded.CommissionRate,
                            Stage = excluded.Stage,
                            ExpectedCloseDate = excluded.ExpectedCloseDate,
                            PaymentScheme = excluded.PaymentScheme,
                            ReservationFee = excluded.ReservationFee,
                            DownPaymentPercent = excluded.DownPaymentPercent,
                            CgtPayer = excluded.CgtPayer,
                            DstPayer = excluded.DstPayer,
                            TransferTaxPayer = excluded.TransferTaxPayer,
                            RegistrationFeePayer = excluded.RegistrationFeePayer,
                            SpecialStipulations = excluded.SpecialStipulations,
                            ContractSignedDate = excluded.ContractSignedDate,
                            BranchId = excluded.BranchId,
                            CreatedAt = excluded.CreatedAt,
                            UpdatedAt = excluded.UpdatedAt,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", d.DealId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$customerId", d.CustomerId);
                    cmd.Parameters.AddWithValue("$customerName", (object?)d.Customer?.FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$propId", d.PropertyId);
                    cmd.Parameters.AddWithValue("$propAddress", (object?)d.Property?.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$agentId", (object?)d.AgentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$agentName", (object?)d.Agent?.FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdBy", d.CreatedByUserId);
                    cmd.Parameters.AddWithValue("$val", Convert.ToDouble(d.Value));
                    cmd.Parameters.AddWithValue("$comm", Convert.ToDouble(d.CommissionRate));
                    cmd.Parameters.AddWithValue("$stage", d.Stage ?? "Active");
                    cmd.Parameters.AddWithValue("$closeDate", d.ExpectedCloseDate.HasValue ? d.ExpectedCloseDate.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$scheme", (object?)d.PaymentScheme ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$resFee", d.ReservationFee.HasValue ? Convert.ToDouble(d.ReservationFee.Value) : DBNull.Value);
                    cmd.Parameters.AddWithValue("$dpPct", d.DownPaymentPercent.HasValue ? Convert.ToDouble(d.DownPaymentPercent.Value) : DBNull.Value);
                    cmd.Parameters.AddWithValue("$cgt", (object?)d.CgtPayer ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$dst", (object?)d.DstPayer ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$tt", (object?)d.TransferTaxPayer ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$reg", (object?)d.RegistrationFeePayer ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$stip", (object?)d.SpecialStipulations ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$signedDate", d.ContractSignedDate.HasValue ? d.ContractSignedDate.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$branchId", (object?)d.BranchId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdAt", d.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$updatedAt", d.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<Deal> GetCachedDeals(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<Deal>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT DealId, CustomerId, CustomerName, PropertyId, PropertyAddress, AgentId, CreatedByUserId, Value, CommissionRate, Stage, ExpectedCloseDate, PaymentScheme, CreatedAt FROM DealMirror WHERE TenantId = $tenantId";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND (AgentId = $userId OR CreatedByUserId = $userId)";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY DealId DESC;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var d = new Deal
                    {
                        DealId = reader.GetInt32(0),
                        CustomerId = reader.GetInt32(1),
                        PropertyId = reader.GetInt32(3),
                        AgentId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        CreatedByUserId = reader.GetInt32(6),
                        Value = Convert.ToDecimal(reader.GetDouble(7)),
                        CommissionRate = Convert.ToDecimal(reader.GetDouble(8)),
                        Stage = reader.GetString(9),
                        ExpectedCloseDate = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
                        PaymentScheme = reader.IsDBNull(11) ? null : reader.GetString(11),
                        CreatedAt = DateTime.Parse(reader.GetString(12))
                    };
                    list.Add(d);
                }
                return list;
            }
        }

        public void SaveActivitiesMirror(int tenantId, IEnumerable<Activity> activities)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var a in activities)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO ActivityMirror (
                            ActivityId, TenantId, Type, RelatedLeadId, RelatedCustomerId,
                            LoggedByAgentId, Notes, ActivityDate, Outcome, DurationMinutes, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $type, $leadId, $custId,
                            $agentId, $notes, $date, $outcome, $duration, $syncedAt
                        )
                        ON CONFLICT(ActivityId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            Type = excluded.Type,
                            RelatedLeadId = excluded.RelatedLeadId,
                            RelatedCustomerId = excluded.RelatedCustomerId,
                            LoggedByAgentId = excluded.LoggedByAgentId,
                            Notes = excluded.Notes,
                            ActivityDate = excluded.ActivityDate,
                            Outcome = excluded.Outcome,
                            DurationMinutes = excluded.DurationMinutes,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", a.ActivityId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$type", a.Type ?? "");
                    cmd.Parameters.AddWithValue("$leadId", (object?)a.RelatedLeadId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$custId", (object?)a.RelatedCustomerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$agentId", a.LoggedByAgentId);
                    cmd.Parameters.AddWithValue("$notes", (object?)a.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$date", a.ActivityDate.ToString("O"));
                    cmd.Parameters.AddWithValue("$outcome", a.Outcome.HasValue ? a.Outcome.Value.ToString() : DBNull.Value);
                    cmd.Parameters.AddWithValue("$duration", (object?)a.DurationMinutes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<Activity> GetCachedActivities(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<Activity>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT ActivityId, Type, RelatedLeadId, RelatedCustomerId, LoggedByAgentId, Notes, ActivityDate, DurationMinutes FROM ActivityMirror WHERE TenantId = $tenantId";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND LoggedByAgentId = $userId";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY ActivityDate DESC;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var a = new Activity
                    {
                        ActivityId = reader.GetInt32(0),
                        Type = reader.GetString(1),
                        RelatedLeadId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        RelatedCustomerId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        LoggedByAgentId = reader.GetInt32(4),
                        Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                        ActivityDate = DateTime.Parse(reader.GetString(6)),
                        DurationMinutes = reader.IsDBNull(7) ? null : reader.GetInt32(7)
                    };
                    list.Add(a);
                }
                return list;
            }
        }

        public void SaveTaskRemindersMirror(int tenantId, IEnumerable<TaskReminder> reminders)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var r in reminders)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO TaskReminderMirror (
                            TaskReminderId, TenantId, Title, DueDate, AssignedToUserId,
                            RelatedCustomerId, RelatedLeadId, Status, Type, Notes, Priority,
                            CreatedAt, UpdatedAt, CompletedAt, IsDeleted, DeletedAt, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $title, $due, $assignedTo,
                            $custId, $leadId, $status, $type, $notes, $priority,
                            $createdAt, $updatedAt, $completedAt, $isDeleted, $deletedAt, $syncedAt
                        )
                        ON CONFLICT(TaskReminderId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            Title = excluded.Title,
                            DueDate = excluded.DueDate,
                            AssignedToUserId = excluded.AssignedToUserId,
                            RelatedCustomerId = excluded.RelatedCustomerId,
                            RelatedLeadId = excluded.RelatedLeadId,
                            Status = excluded.Status,
                            Type = excluded.Type,
                            Notes = excluded.Notes,
                            Priority = excluded.Priority,
                            CreatedAt = excluded.CreatedAt,
                            UpdatedAt = excluded.UpdatedAt,
                            CompletedAt = excluded.CompletedAt,
                            IsDeleted = excluded.IsDeleted,
                            DeletedAt = excluded.DeletedAt,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", r.TaskReminderId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$title", r.Title ?? "");
                    cmd.Parameters.AddWithValue("$due", r.DueDate.ToString("O"));
                    cmd.Parameters.AddWithValue("$assignedTo", r.AssignedToUserId);
                    cmd.Parameters.AddWithValue("$custId", (object?)r.RelatedCustomerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$leadId", (object?)r.RelatedLeadId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$status", r.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("$type", r.Type ?? "Call");
                    cmd.Parameters.AddWithValue("$notes", (object?)r.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$priority", r.Priority ?? "Medium");
                    cmd.Parameters.AddWithValue("$createdAt", r.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$updatedAt", r.UpdatedAt.HasValue ? r.UpdatedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$completedAt", r.CompletedAt.HasValue ? r.CompletedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$isDeleted", r.IsDeleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("$deletedAt", r.DeletedAt.HasValue ? r.DeletedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<TaskReminder> GetCachedTaskReminders(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<TaskReminder>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT TaskReminderId, Title, DueDate, AssignedToUserId, RelatedCustomerId, RelatedLeadId, Status, Type, Notes, Priority, CreatedAt, UpdatedAt, CompletedAt FROM TaskReminderMirror WHERE TenantId = $tenantId AND IsDeleted = 0";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND AssignedToUserId = $userId";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY DueDate ASC;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var r = new TaskReminder
                    {
                        TaskReminderId = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        DueDate = DateTime.Parse(reader.GetString(2)),
                        AssignedToUserId = reader.GetInt32(3),
                        RelatedCustomerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                        RelatedLeadId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        Status = reader.GetString(6),
                        Type = reader.GetString(7),
                        Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
                        Priority = reader.GetString(9),
                        CreatedAt = DateTime.Parse(reader.GetString(10)),
                        UpdatedAt = reader.IsDBNull(11) ? null : DateTime.Parse(reader.GetString(11)),
                        CompletedAt = reader.IsDBNull(12) ? null : DateTime.Parse(reader.GetString(12))
                    };
                    list.Add(r);
                }
                return list;
            }
        }

        public void SaveSupportTicketsMirror(int tenantId, IEnumerable<SupportTicket> tickets)
        {
            lock (_dbLock)
            {
                using var conn = CreateOpenConnection();
                using var tx = conn.BeginTransaction();
                var now = DateTime.UtcNow.ToString("O");

                foreach (var t in tickets)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO SupportTicketMirror (
                            TicketId, TenantId, TicketNumber, CustomerId, CustomerName,
                            RaisedByUserId, AssignedToUserId, Category, Description,
                            Priority, Status, DueDate, CreatedAt, UpdatedAt,
                            FirstRespondedAt, ResolvedAt, IsDeleted, DeletedAt, LastSyncedAt
                        ) VALUES (
                            $id, $tenantId, $number, $custId, $custName,
                            $raisedBy, $assignedTo, $category, $desc,
                            $priority, $status, $due, $createdAt, $updatedAt,
                            $respondedAt, $resolvedAt, $isDeleted, $deletedAt, $syncedAt
                        )
                        ON CONFLICT(TicketId) DO UPDATE SET
                            TenantId = excluded.TenantId,
                            TicketNumber = excluded.TicketNumber,
                            CustomerId = excluded.CustomerId,
                            CustomerName = excluded.CustomerName,
                            RaisedByUserId = excluded.RaisedByUserId,
                            AssignedToUserId = excluded.AssignedToUserId,
                            Category = excluded.Category,
                            Description = excluded.Description,
                            Priority = excluded.Priority,
                            Status = excluded.Status,
                            DueDate = excluded.DueDate,
                            CreatedAt = excluded.CreatedAt,
                            UpdatedAt = excluded.UpdatedAt,
                            FirstRespondedAt = excluded.FirstRespondedAt,
                            ResolvedAt = excluded.ResolvedAt,
                            IsDeleted = excluded.IsDeleted,
                            DeletedAt = excluded.DeletedAt,
                            LastSyncedAt = excluded.LastSyncedAt;";

                    cmd.Parameters.AddWithValue("$id", t.TicketId);
                    cmd.Parameters.AddWithValue("$tenantId", tenantId);
                    cmd.Parameters.AddWithValue("$number", t.TicketNumber ?? "");
                    cmd.Parameters.AddWithValue("$custId", t.CustomerId);
                    cmd.Parameters.AddWithValue("$custName", (object?)t.Customer?.FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$raisedBy", t.RaisedByUserId);
                    cmd.Parameters.AddWithValue("$assignedTo", (object?)t.AssignedToUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$category", t.Category ?? "Other");
                    cmd.Parameters.AddWithValue("$desc", t.Description ?? "");
                    cmd.Parameters.AddWithValue("$priority", t.Priority ?? "Medium");
                    cmd.Parameters.AddWithValue("$status", t.Status ?? "Open");
                    cmd.Parameters.AddWithValue("$due", t.DueDate.HasValue ? t.DueDate.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$createdAt", t.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$updatedAt", t.CreatedAt.ToString("O"));
                    cmd.Parameters.AddWithValue("$respondedAt", t.FirstRespondedAt.HasValue ? t.FirstRespondedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$resolvedAt", t.ResolvedAt.HasValue ? t.ResolvedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$isDeleted", t.IsDeleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("$deletedAt", t.DeletedAt.HasValue ? t.DeletedAt.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("$syncedAt", now);

                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        public List<SupportTicket> GetCachedSupportTickets(int tenantId, int? userId = null, bool isAgent = false)
        {
            lock (_dbLock)
            {
                var list = new List<SupportTicket>();
                using var conn = CreateOpenConnection();
                using var cmd = conn.CreateCommand();

                string query = "SELECT TicketId, TicketNumber, CustomerId, CustomerName, RaisedByUserId, AssignedToUserId, Category, Description, Priority, Status, DueDate, CreatedAt FROM SupportTicketMirror WHERE TenantId = $tenantId AND IsDeleted = 0";
                if (isAgent && userId.HasValue && userId.Value > 0)
                {
                    query += " AND (AssignedToUserId = $userId OR RaisedByUserId = $userId)";
                    cmd.Parameters.AddWithValue("$userId", userId.Value);
                }
                query += " ORDER BY TicketId DESC;";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("$tenantId", tenantId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var t = new SupportTicket
                    {
                        TicketId = reader.GetInt32(0),
                        TicketNumber = reader.GetString(1),
                        CustomerId = reader.GetInt32(2),
                        RaisedByUserId = reader.GetInt32(4),
                        AssignedToUserId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        Category = reader.GetString(6),
                        Description = reader.GetString(7),
                        Priority = reader.GetString(8),
                        Status = reader.GetString(9),
                        DueDate = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
                        CreatedAt = DateTime.Parse(reader.GetString(11))
                    };
                    list.Add(t);
                }
                return list;
            }
        }
    }
}
