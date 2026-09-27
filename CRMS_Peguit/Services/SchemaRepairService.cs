using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Models.Services
{
    public static class SchemaRepairService
    {
        public static void EnsureCrmPolishColumns(DbContext db)
        {
            var connection = db.Database.GetDbConnection();
            bool shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                EnsureLeadColumns((SqlConnection)connection);
                EnsureUserColumns((SqlConnection)connection);
                EnsurePropertyColumns((SqlConnection)connection);
                EnsureDealColumns((SqlConnection)connection);
                EnsureAssignmentColumns((SqlConnection)connection, "Leads");
                EnsureAssignmentColumns((SqlConnection)connection, "Customers");
                EnsureAssignmentColumns((SqlConnection)connection, "Properties");
                EnsureNotificationTables((SqlConnection)connection);
                DeduplicateTenantData((SqlConnection)connection);

                if (db is CRMS_Peguit.infrastructure.data.RealEstateDbContext reDb)
                {
                    LocalDb.EnsureAllSchemas(reDb);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private static void EnsureLeadColumns(SqlConnection connection)
        {
            ExecuteIfMissing(
                connection,
                "Leads",
                "ExpectedValue",
                "ALTER TABLE [Leads] ADD [ExpectedValue] decimal(18,2) NULL;");

            ExecuteIfMissing(
                connection,
                "Leads",
                "Notes",
                "ALTER TABLE [Leads] ADD [Notes] nvarchar(2000) NULL;");

            ExecuteIfMissing(
                connection,
                "Leads",
                "Priority",
                "ALTER TABLE [Leads] ADD [Priority] nvarchar(20) NULL;");
        }

        private static void EnsureDealColumns(SqlConnection connection)
        {
            ExecuteIfMissing(connection, "Deals", "PaymentScheme", "ALTER TABLE [Deals] ADD [PaymentScheme] nvarchar(50) NULL;");
            ExecuteIfMissing(connection, "Deals", "ReservationFee", "ALTER TABLE [Deals] ADD [ReservationFee] decimal(18,2) NULL;");
            ExecuteIfMissing(connection, "Deals", "DownPaymentPercent", "ALTER TABLE [Deals] ADD [DownPaymentPercent] decimal(5,2) NULL;");
            ExecuteIfMissing(connection, "Deals", "CgtPayer", "ALTER TABLE [Deals] ADD [CgtPayer] nvarchar(50) NULL;");
            ExecuteIfMissing(connection, "Deals", "DstPayer", "ALTER TABLE [Deals] ADD [DstPayer] nvarchar(50) NULL;");
            ExecuteIfMissing(connection, "Deals", "TransferTaxPayer", "ALTER TABLE [Deals] ADD [TransferTaxPayer] nvarchar(50) NULL;");
            ExecuteIfMissing(connection, "Deals", "RegistrationFeePayer", "ALTER TABLE [Deals] ADD [RegistrationFeePayer] nvarchar(50) NULL;");
            ExecuteIfMissing(connection, "Deals", "SpecialStipulations", "ALTER TABLE [Deals] ADD [SpecialStipulations] nvarchar(max) NULL;");
            ExecuteIfMissing(connection, "Deals", "ContractSignedDate", "ALTER TABLE [Deals] ADD [ContractSignedDate] datetime2 NULL;");
            ExecuteIfMissing(connection, "Deals", "CreatedByUserId", "ALTER TABLE [Deals] ADD [CreatedByUserId] int NOT NULL DEFAULT 1;");
        }

        private static void EnsurePropertyColumns(SqlConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Properties') 
    AND name = 'ListedByAgentId' 
    AND is_nullable = 0
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Properties_ListedByAgentId' AND object_id = OBJECT_ID('dbo.Properties'))
        DROP INDEX [IX_Properties_ListedByAgentId] ON [dbo].[Properties];

    ALTER TABLE [dbo].[Properties] ALTER COLUMN [ListedByAgentId] int NULL;

    CREATE NONCLUSTERED INDEX [IX_Properties_ListedByAgentId] ON [dbo].[Properties] ([ListedByAgentId]);
END";
            command.ExecuteNonQuery();
        }

        private static void EnsureUserColumns(SqlConnection connection)
        {
            // 3NF: Personal info is normalized into Persons table.
            ExecuteIfMissing(
                connection,
                "Users",
                "PersonId",
                "ALTER TABLE [Users] ADD [PersonId] int NOT NULL DEFAULT 1;");
        }

        private static void EnsureAssignmentColumns(SqlConnection connection, string tableName)
        {
            ExecuteIfMissing(
                connection,
                tableName,
                "AssignmentStatus",
                $"ALTER TABLE [{tableName}] ADD [AssignmentStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_{tableName}_AssignmentStatus] DEFAULT ('approved');");

            ExecuteIfMissing(
                connection,
                tableName,
                "AssignmentReviewedByUserId",
                $"ALTER TABLE [{tableName}] ADD [AssignmentReviewedByUserId] int NULL;");

            ExecuteIfMissing(
                connection,
                tableName,
                "AssignmentReviewedAt",
                $"ALTER TABLE [{tableName}] ADD [AssignmentReviewedAt] datetime2 NULL;");

            ExecuteIfMissing(
                connection,
                tableName,
                "AssignmentReviewNotes",
                $"ALTER TABLE [{tableName}] ADD [AssignmentReviewNotes] nvarchar(1000) NULL;");

            ExecuteIfMissing(
                connection,
                tableName,
                "CreatedByUserId",
                $"ALTER TABLE [{tableName}] ADD [CreatedByUserId] int NULL;");

            using var cmd = connection.CreateCommand();
            if (tableName == "Customers")
            {
                cmd.CommandText = "UPDATE c SET c.[CreatedByUserId] = ISNULL(a.[LoggedByAgentId], ISNULL(c.[AssignedAgentId], 1)) FROM [Customers] c OUTER APPLY (SELECT TOP 1 [LoggedByAgentId] FROM [Activities] WHERE [RelatedCustomerId] = c.[CustomerId] AND [Type] = 'Customer Created') a WHERE c.[CreatedByUserId] IS NULL;";
                cmd.ExecuteNonQuery();
            }
            else if (tableName == "Leads")
            {
                cmd.CommandText = "UPDATE l SET l.[CreatedByUserId] = ISNULL(a.[LoggedByAgentId], ISNULL(l.[AssignedAgentId], 1)) FROM [Leads] l OUTER APPLY (SELECT TOP 1 [LoggedByAgentId] FROM [Activities] WHERE [RelatedLeadId] = l.[LeadId] AND [Type] = 'Lead Created') a WHERE l.[CreatedByUserId] IS NULL;";
                cmd.ExecuteNonQuery();
            }
            else if (tableName == "Properties")
            {
                cmd.CommandText = "UPDATE p SET p.[CreatedByUserId] = ISNULL(p.[ListedByAgentId], 1) FROM [Properties] p WHERE p.[CreatedByUserId] IS NULL;";
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteIfMissing(
            SqlConnection connection,
            string tableName,
            string columnName,
            string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                $"IF COL_LENGTH('dbo.{tableName}', '{columnName}') IS NULL BEGIN {sql} END";
            command.ExecuteNonQuery();
        }

        private static void EnsureNotificationTables(SqlConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] int NOT NULL IDENTITY(1,1),
        [TenantId] int NOT NULL,
        [RecipientUserId] int NOT NULL,
        [Type] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [RelatedEntityType] nvarchar(50) NULL,
        [RelatedEntityId] int NULL,
        [IsRead] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [ReadAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Notifications_TenantId] ON [dbo].[Notifications] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_Notifications_RecipientUserId] ON [dbo].[Notifications] ([RecipientUserId]);
    CREATE NONCLUSTERED INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications] ([IsRead]);
    CREATE NONCLUSTERED INDEX [IX_Notifications_CreatedAt] ON [dbo].[Notifications] ([CreatedAt]);
END;

IF OBJECT_ID('dbo.NotificationPreferences', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationPreferences] (
        [NotificationPreferenceId] int NOT NULL IDENTITY(1,1),
        [TenantId] int NOT NULL,
        [UserId] int NOT NULL,
        [Type] int NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([NotificationPreferenceId]),
        CONSTRAINT [FK_NotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_NotificationPreferences_TenantId] ON [dbo].[NotificationPreferences] ([TenantId]);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NotificationPreferences_UserId_Type] ON [dbo].[NotificationPreferences] ([UserId], [Type]);
END;";
            cmd.ExecuteNonQuery();
        }

        public static void DeduplicateTenantData(SqlConnection connection)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 120;
                cmd.CommandText = @"
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL AND OBJECT_ID('dbo.Users', 'U') IS NOT NULL AND OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    -- 1. Deduplicate Customers by TenantId, LOWER(LTRIM(RTRIM(Email)))
    IF OBJECT_ID('tempdb..#DupCust') IS NOT NULL DROP TABLE #DupCust;
    ;WITH CustDuplicates AS (
        SELECT c.CustomerId,
               ROW_NUMBER() OVER (
                   PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(c.Email))) 
                   ORDER BY c.CustomerId ASC
               ) as rn,
               FIRST_VALUE(c.CustomerId) OVER (
                   PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(c.Email))) 
                   ORDER BY c.CustomerId ASC
               ) as CanonicalId
        FROM Customers c
        JOIN Users u ON c.CreatedByUserId = u.UserId
        JOIN Roles r ON u.RoleId = r.RoleId
        WHERE c.Email IS NOT NULL AND LTRIM(RTRIM(c.Email)) <> ''
    )
    SELECT CustomerId, CanonicalId INTO #DupCust FROM CustDuplicates WHERE rn > 1;

    IF EXISTS (SELECT 1 FROM #DupCust)
    BEGIN
        IF OBJECT_ID('dbo.BuyerProfiles', 'U') IS NOT NULL
        BEGIN
            UPDATE bp SET bp.CustomerId = d.CanonicalId 
            FROM BuyerProfiles bp 
            JOIN #DupCust d ON bp.CustomerId = d.CustomerId
            WHERE NOT EXISTS (SELECT 1 FROM BuyerProfiles existing WHERE existing.CustomerId = d.CanonicalId);

            DELETE bp FROM BuyerProfiles bp JOIN #DupCust d ON bp.CustomerId = d.CustomerId;
        END

        IF OBJECT_ID('dbo.Properties', 'U') IS NOT NULL
            UPDATE p SET p.OwnerCustomerId = d.CanonicalId FROM Properties p JOIN #DupCust d ON p.OwnerCustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.Deals', 'U') IS NOT NULL
            UPDATE dl SET dl.CustomerId = d.CanonicalId FROM Deals dl JOIN #DupCust d ON dl.CustomerId = d.CustomerId;
        
        IF OBJECT_ID('dbo.Leads', 'U') IS NOT NULL
        BEGIN
            UPDATE l
            SET l.ConvertedCustomerId = NULL
            FROM Leads l
            JOIN #DupCust d ON l.ConvertedCustomerId = d.CustomerId
            WHERE EXISTS (SELECT 1 FROM Leads l_orig WHERE l_orig.ConvertedCustomerId = d.CanonicalId);

            ;WITH LeadRank AS (
                SELECT l.LeadId, d.CanonicalId,
                       ROW_NUMBER() OVER (PARTITION BY d.CanonicalId ORDER BY l.LeadId ASC) as rn
                FROM Leads l
                JOIN #DupCust d ON l.ConvertedCustomerId = d.CustomerId
            )
            UPDATE l
            SET l.ConvertedCustomerId = CASE WHEN lr.rn = 1 THEN lr.CanonicalId ELSE NULL END
            FROM Leads l
            JOIN LeadRank lr ON l.LeadId = lr.LeadId;
        END

        IF OBJECT_ID('dbo.Activities', 'U') IS NOT NULL
            UPDATE a SET a.RelatedCustomerId = d.CanonicalId FROM Activities a JOIN #DupCust d ON a.RelatedCustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.SupportTickets', 'U') IS NOT NULL
            UPDATE t SET t.CustomerId = d.CanonicalId FROM SupportTickets t JOIN #DupCust d ON t.CustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.TaskReminders', 'U') IS NOT NULL
            UPDATE tr SET tr.RelatedCustomerId = d.CanonicalId FROM TaskReminders tr JOIN #DupCust d ON tr.RelatedCustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.MarketUpdateLogs', 'U') IS NOT NULL
            UPDATE m SET m.CustomerId = d.CanonicalId FROM MarketUpdateLogs m JOIN #DupCust d ON m.CustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.RetentionRequests', 'U') IS NOT NULL
            UPDATE r SET r.CustomerId = d.CanonicalId FROM RetentionRequests r JOIN #DupCust d ON r.CustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.RetentionEmailLogs', 'U') IS NOT NULL
            UPDATE r SET r.CustomerId = d.CanonicalId FROM RetentionEmailLogs r JOIN #DupCust d ON r.CustomerId = d.CustomerId;

        IF OBJECT_ID('dbo.RetentionAuditLogs', 'U') IS NOT NULL
            UPDATE r SET r.TargetCustomerId = d.CanonicalId FROM RetentionAuditLogs r JOIN #DupCust d ON r.TargetCustomerId = d.CustomerId;

        DELETE c FROM Customers c JOIN #DupCust d ON c.CustomerId = d.CustomerId;
    END
    IF OBJECT_ID('tempdb..#DupCust') IS NOT NULL DROP TABLE #DupCust;

    -- 2. Deduplicate Properties by TenantId, LOWER(LTRIM(RTRIM(Address)))
    IF OBJECT_ID('dbo.Properties', 'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#DupProp') IS NOT NULL DROP TABLE #DupProp;
        ;WITH PropDuplicates AS (
            SELECT p.PropertyId,
                   ROW_NUMBER() OVER (
                       PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(p.Address))) 
                       ORDER BY p.PropertyId ASC
                   ) as rn,
                   FIRST_VALUE(p.PropertyId) OVER (
                       PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(p.Address))) 
                       ORDER BY p.PropertyId ASC
                   ) as CanonicalId
            FROM Properties p
            JOIN Users u ON p.CreatedByUserId = u.UserId
            JOIN Roles r ON u.RoleId = r.RoleId
            WHERE p.Address IS NOT NULL AND LTRIM(RTRIM(p.Address)) <> ''
        )
        SELECT PropertyId, CanonicalId INTO #DupProp FROM PropDuplicates WHERE rn > 1;

        IF EXISTS (SELECT 1 FROM #DupProp)
        BEGIN
            IF OBJECT_ID('dbo.Deals', 'U') IS NOT NULL
                UPDATE d SET d.PropertyId = dp.CanonicalId FROM Deals d JOIN #DupProp dp ON d.PropertyId = dp.PropertyId;

            IF OBJECT_ID('dbo.PropertyShowingDetails', 'U') IS NOT NULL
                UPDATE psd SET psd.PropertyId = dp.CanonicalId FROM PropertyShowingDetails psd JOIN #DupProp dp ON psd.PropertyId = dp.PropertyId;

            DELETE p FROM Properties p JOIN #DupProp dp ON p.PropertyId = dp.PropertyId;
        END
        IF OBJECT_ID('tempdb..#DupProp') IS NOT NULL DROP TABLE #DupProp;
    END

    -- 3. Deduplicate Leads by TenantId, LOWER(LTRIM(RTRIM(Email)))
    IF OBJECT_ID('dbo.Leads', 'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#DupLead') IS NOT NULL DROP TABLE #DupLead;
        ;WITH LeadDuplicates AS (
            SELECT l.LeadId,
                   ROW_NUMBER() OVER (
                       PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(l.Email))) 
                       ORDER BY l.LeadId ASC
                   ) as rn,
                   FIRST_VALUE(l.LeadId) OVER (
                       PARTITION BY r.TenantId, LOWER(LTRIM(RTRIM(l.Email))) 
                       ORDER BY l.LeadId ASC
                   ) as CanonicalId
            FROM Leads l
            JOIN Users u ON l.CreatedByUserId = u.UserId
            JOIN Roles r ON u.RoleId = r.RoleId
            WHERE l.Email IS NOT NULL AND LTRIM(RTRIM(l.Email)) <> ''
        )
        SELECT LeadId, CanonicalId INTO #DupLead FROM LeadDuplicates WHERE rn > 1;

        IF EXISTS (SELECT 1 FROM #DupLead)
        BEGIN
            IF OBJECT_ID('dbo.Activities', 'U') IS NOT NULL
                UPDATE a SET a.RelatedLeadId = dl.CanonicalId FROM Activities a JOIN #DupLead dl ON a.RelatedLeadId = dl.LeadId;

            IF OBJECT_ID('dbo.TaskReminders', 'U') IS NOT NULL
                UPDATE tr SET tr.RelatedLeadId = dl.CanonicalId FROM TaskReminders tr JOIN #DupLead dl ON tr.RelatedLeadId = dl.LeadId;

            UPDATE l SET l.ConvertedCustomerId = NULL FROM Leads l JOIN #DupLead dl ON l.LeadId = dl.LeadId;
            DELETE l FROM Leads l JOIN #DupLead dl ON l.LeadId = dl.LeadId;
        END
        IF OBJECT_ID('tempdb..#DupLead') IS NOT NULL DROP TABLE #DupLead;
    END

    -- 4. Deduplicate TaskReminders by AssignedToUserId, LOWER(LTRIM(RTRIM(Title)))
    IF OBJECT_ID('dbo.TaskReminders', 'U') IS NOT NULL
    BEGIN
        ;WITH TaskDuplicates AS (
            SELECT TaskReminderId,
                   ROW_NUMBER() OVER (
                       PARTITION BY AssignedToUserId, LOWER(LTRIM(RTRIM(Title))) 
                       ORDER BY TaskReminderId ASC
                   ) as rn
            FROM TaskReminders
            WHERE Title IS NOT NULL AND LTRIM(RTRIM(Title)) <> ''
        )
        DELETE tr
        FROM TaskReminders tr
        JOIN TaskDuplicates td ON tr.TaskReminderId = td.TaskReminderId
        WHERE td.rn > 1;
    END

    -- 5. Deduplicate Campaigns by TenantId, LOWER(LTRIM(RTRIM(Name)))
    IF OBJECT_ID('dbo.Campaigns', 'U') IS NOT NULL
    BEGIN
        ;WITH CampDuplicates AS (
            SELECT CampaignId,
                   ROW_NUMBER() OVER (
                       PARTITION BY TenantId, LOWER(LTRIM(RTRIM(Name))) 
                       ORDER BY CampaignId ASC
                   ) as rn
            FROM Campaigns
            WHERE Name IS NOT NULL AND LTRIM(RTRIM(Name)) <> ''
        )
        DELETE c
        FROM Campaigns c
        JOIN CampDuplicates cd ON c.CampaignId = cd.CampaignId
        WHERE cd.rn > 1;
    END

    -- 6. Deduplicate SupportTickets by TenantId, UPPER(LTRIM(RTRIM(TicketNumber)))
    IF OBJECT_ID('dbo.SupportTickets', 'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#DupTicket') IS NOT NULL DROP TABLE #DupTicket;
        ;WITH TicketDuplicates AS (
            SELECT st.TicketId,
                   ROW_NUMBER() OVER (
                       PARTITION BY r.TenantId, UPPER(LTRIM(RTRIM(st.TicketNumber))) 
                       ORDER BY st.TicketId ASC
                   ) as rn,
                   FIRST_VALUE(st.TicketId) OVER (
                       PARTITION BY r.TenantId, UPPER(LTRIM(RTRIM(st.TicketNumber))) 
                       ORDER BY st.TicketId ASC
                   ) as CanonicalId
            FROM SupportTickets st
            JOIN Users u ON st.RaisedByUserId = u.UserId
            JOIN Roles r ON u.RoleId = r.RoleId
            WHERE st.TicketNumber IS NOT NULL AND LTRIM(RTRIM(st.TicketNumber)) <> ''
        )
        SELECT TicketId, CanonicalId INTO #DupTicket FROM TicketDuplicates WHERE rn > 1;

        IF EXISTS (SELECT 1 FROM #DupTicket)
        BEGIN
            IF OBJECT_ID('dbo.TicketComments', 'U') IS NOT NULL
                UPDATE tc SET tc.TicketId = dt.CanonicalId FROM TicketComments tc JOIN #DupTicket dt ON tc.TicketId = dt.TicketId;
            DELETE st FROM SupportTickets st JOIN #DupTicket dt ON st.TicketId = dt.TicketId;
        END
        IF OBJECT_ID('tempdb..#DupTicket') IS NOT NULL DROP TABLE #DupTicket;
    END

    -- 7. Deduplicate Deals by TenantId, CustomerId, PropertyId
    IF OBJECT_ID('dbo.Deals', 'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#DupDeal') IS NOT NULL DROP TABLE #DupDeal;
        ;WITH DealDuplicates AS (
            SELECT d.DealId,
                   ROW_NUMBER() OVER (
                       PARTITION BY r.TenantId, d.CustomerId, d.PropertyId 
                       ORDER BY d.DealId ASC
                   ) as rn,
                   FIRST_VALUE(d.DealId) OVER (
                       PARTITION BY r.TenantId, d.CustomerId, d.PropertyId 
                       ORDER BY d.DealId ASC
                   ) as CanonicalId
            FROM Deals d
            JOIN Users u ON d.CreatedByUserId = u.UserId
            JOIN Roles r ON u.RoleId = r.RoleId
        )
        SELECT DealId, CanonicalId INTO #DupDeal FROM DealDuplicates WHERE rn > 1;

        IF EXISTS (SELECT 1 FROM #DupDeal)
        BEGIN
            IF OBJECT_ID('dbo.DealContingencies', 'U') IS NOT NULL
            BEGIN
                UPDATE dc SET dc.DealId = dd.CanonicalId 
                FROM DealContingencies dc 
                JOIN #DupDeal dd ON dc.DealId = dd.DealId
                WHERE NOT EXISTS (SELECT 1 FROM DealContingencies existing WHERE existing.DealId = dd.CanonicalId AND existing.ContingencyName = dc.ContingencyName);

                DELETE dc FROM DealContingencies dc JOIN #DupDeal dd ON dc.DealId = dd.DealId;
            END

            IF OBJECT_ID('dbo.DealClauses', 'U') IS NOT NULL
            BEGIN
                UPDATE dcl SET dcl.DealId = dd.CanonicalId 
                FROM DealClauses dcl 
                JOIN #DupDeal dd ON dcl.DealId = dd.DealId
                WHERE NOT EXISTS (SELECT 1 FROM DealClauses existing WHERE existing.DealId = dd.CanonicalId AND existing.ClauseId = dcl.ClauseId);

                DELETE dcl FROM DealClauses dcl JOIN #DupDeal dd ON dcl.DealId = dd.DealId;
            END

            DELETE d FROM Deals d JOIN #DupDeal dd ON d.DealId = dd.DealId;
        END
        IF OBJECT_ID('tempdb..#DupDeal') IS NOT NULL DROP TABLE #DupDeal;
    END
END";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Silently ignore if schema or locks are transient
            }
        }
    }
}
