using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using CRMS_Peguit.infrastructure.Security;
using CRMS_Peguit.infrastructure.Seeding;

namespace CRMS_Peguit.winforms.Models.Services
{
    public static class LocalDb
    {
        private static readonly ConcurrentDictionary<int, bool> _initializedTenants = new();
        private static bool _masterDbInitialized = false;
        private static readonly object _masterLock = new();
        private static readonly object _lockObj = new();

        public static string MasterConnectionString =>
            DbConfiguration.GetMasterConnectionString();

        public static string GetTenantConnectionString(int tenantId) =>
            DbConfiguration.GetTenantConnectionString(tenantId);

        public static string ConnectionString =>
            DbConfiguration.GetLocalConnectionString();

        public static MasterCrmsDbContext CreateMasterContext()
        {
            var masterConn = MasterConnectionString;
            LocalDbHelper.EnsureLocalDbRunning(masterConn);

            var options = new DbContextOptionsBuilder<MasterCrmsDbContext>()
                .UseSqlServer(masterConn, sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null))
                .Options;

            var context = new MasterCrmsDbContext(options);

            if (!_masterDbInitialized)
            {
                lock (_masterLock)
                {
                    if (!_masterDbInitialized)
                    {
                        EnsureMasterDatabaseInitialized(context);
                        _masterDbInitialized = true;
                    }
                }
            }

            return context;
        }

        public static async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var masterConn = MasterConnectionString;
                LocalDbHelper.EnsureLocalDbRunning(masterConn);
                var options = new DbContextOptionsBuilder<MasterCrmsDbContext>()
                    .UseSqlServer(masterConn, sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
                    .Options;
                using var context = new MasterCrmsDbContext(options);
                return await context.Database.CanConnectAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        public static RealEstateDbContext CreateContext(int tenantId = 1)
        {
            if (tenantId <= 0) tenantId = 1;
            var tenantConn = GetTenantConnectionString(tenantId);
            LocalDbHelper.EnsureLocalDbRunning(tenantConn);

            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(tenantConn, sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null))
                .Options;

            var context = new RealEstateDbContext(options, tenantId: tenantId);

            if (!_initializedTenants.ContainsKey(tenantId))
            {
                lock (_lockObj)
                {
                    if (!_initializedTenants.ContainsKey(tenantId))
                    {
                        EnsureTenantDatabaseInitialized(context, tenantId);
                        _initializedTenants[tenantId] = true;
                    }
                }
            }

            return context;
        }

        public static async Task SeedAllTenantsAsync(IProgress<string>? progress = null)
        {
            progress?.Report("Ensuring Master Platform database and physical database mapping...");
            using (var masterDb = CreateMasterContext())
            {
                // Master DB initialization and subscription records ensured by CreateMasterContext()
            }

            int[] tenantIds = { 1, 2, 3 };
            foreach (var tid in tenantIds)
            {
                string tenantName = tid switch
                {
                    1 => "Apex Realty (Tenant 1)",
                    2 => "BlueHorizon Properties (Tenant 2)",
                    3 => "Crestview Holdings (Tenant 3 - Multi-Branch)",
                    _ => $"Tenant {tid}"
                };

                progress?.Report($"[Tenant {tid}] Initializing schema & seeding datasets for {tenantName}...");
                using var tenantDb = CreateContext(tid);
                await DbSeeder.SeedTestUsersAsync(tenantDb, tid);
                progress?.Report($"[Tenant {tid}] Completed seeding for {tenantName}.");
            }

            progress?.Report("All tenants successfully seeded!");
        }

        public static void SeedAllTenants() => SeedAllTenantsAsync().GetAwaiter().GetResult();

        private static void EnsureMasterDatabaseInitialized(MasterCrmsDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();

                // 1. Ensure Companies
                if (!context.Companies.Any())
                {
                    var compA = new Company
                    {
                        CompanyCode = "TENANT-A",
                        CompanyName = "Apex Realty (Tenant A)",
                        CreatedAt = DateTime.UtcNow.AddMonths(-6)
                    };
                    var compB = new Company
                    {
                        CompanyCode = "TENANT-B",
                        CompanyName = "BlueHorizon Properties (Tenant B)",
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    };
                    var compC = new Company
                    {
                        CompanyCode = "TENANT-C",
                        CompanyName = "Crestview Holdings (Tenant C)",
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    };

                    context.Companies.AddRange(compA, compB, compC);
                    context.SaveChanges();

                    // 2. Ensure CompanyDatabases (Physical Database mapping)
                    var dbA = new CompanyDatabase
                    {
                        CompanyId = compA.CompanyId,
                        ServerName = "(localdb)\\mssqllocaldb",
                        DatabaseName = "CRMS_Tenant_1",
                        CredentialKey = "TenantA",
                        IsActive = true
                    };
                    var dbB = new CompanyDatabase
                    {
                        CompanyId = compB.CompanyId,
                        ServerName = "(localdb)\\mssqllocaldb",
                        DatabaseName = "CRMS_Tenant_2",
                        CredentialKey = "TenantB",
                        IsActive = true
                    };
                    var dbC = new CompanyDatabase
                    {
                        CompanyId = compC.CompanyId,
                        ServerName = "(localdb)\\mssqllocaldb",
                        DatabaseName = "CRMS_Tenant_3",
                        CredentialKey = "TenantC",
                        IsActive = true
                    };

                    context.CompanyDatabases.AddRange(dbA, dbB, dbC);

                    // 3. Ensure Subscriptions
                    var subA = new Subscription
                    {
                        CompanyId = compA.CompanyId,
                        PlanName = "Tenant A",
                        StartDate = DateTime.UtcNow.AddMonths(-6),
                        EndDate = DateTime.UtcNow.AddMonths(6),
                        BillingAmount = 2500m,
                        Status = "Active"
                    };
                    var subB = new Subscription
                    {
                        CompanyId = compB.CompanyId,
                        PlanName = "Tenant B",
                        StartDate = DateTime.UtcNow.AddMonths(-4),
                        EndDate = DateTime.UtcNow.AddMonths(8),
                        BillingAmount = 7500m,
                        Status = "Active"
                    };
                    var subC = new Subscription
                    {
                        CompanyId = compC.CompanyId,
                        PlanName = "Tenant C",
                        StartDate = DateTime.UtcNow.AddMonths(-2),
                        EndDate = DateTime.UtcNow.AddMonths(10),
                        BillingAmount = 15000m,
                        Status = "Active"
                    };

                    context.Subscriptions.AddRange(subA, subB, subC);
                    context.SaveChanges();
                }

                // 4. Ensure SuperAdmin in Master
                if (!context.SuperAdmins.Any())
                {
                    var sa = new SuperAdmin
                    {
                        FirstName = "Platform",
                        LastName = "Super Admin",
                        Email = "superadmin@crms.com",
                        PasswordHash = PasswordHasher.Hash("SuperAdmin123!"),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddYears(-1)
                    };
                    context.SuperAdmins.Add(sa);
                    context.SaveChanges();
                }
            }
            catch
            {
                // Fallback gracefully if database initialization encounters transient error
            }
        }

        public static void EnsureAllSchemas(RealEstateDbContext context)
        {
            EnsureDealSchema(context);
            EnsureSupportTicketSchema(context);
            EnsureFollowUpSchema(context);
            EnsureCampaignSchema(context);
            EnsureActivitySchema(context);
            EnsureAutomatedEmailSchema(context);
            EnsureBranchSchema(context);
        }

        private static void EnsureTenantDatabaseInitialized(RealEstateDbContext context, int tenantId)
        {
            try
            {
                context.Database.EnsureCreated();

                EnsureAllSchemas(context);

                if (!context.Users.Any())
                {
                    DbSeeder.SeedTestUsersAsync(context, tenantId).GetAwaiter().GetResult();
                }
            }
            catch
            {
                // Graceful fallback for schema updates
            }
        }

        private static void EnsureDealSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Deals', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('Deals', 'PaymentScheme') IS NULL
                        BEGIN
                            ALTER TABLE Deals ADD PaymentScheme NVARCHAR(50) NULL;
                            ALTER TABLE Deals ADD ReservationFee DECIMAL(18,2) NULL;
                            ALTER TABLE Deals ADD DownPaymentPercent DECIMAL(5,2) NULL;
                            ALTER TABLE Deals ADD CgtPayer NVARCHAR(50) NULL;
                            ALTER TABLE Deals ADD DstPayer NVARCHAR(50) NULL;
                            ALTER TABLE Deals ADD TransferTaxPayer NVARCHAR(50) NULL;
                            ALTER TABLE Deals ADD RegistrationFeePayer NVARCHAR(50) NULL;
                            ALTER TABLE Deals ADD SpecialStipulations NVARCHAR(MAX) NULL;
                            ALTER TABLE Deals ADD ContractSignedDate DATETIME2 NULL;
                        END

                        IF COL_LENGTH('Deals', 'CreatedByUserId') IS NULL
                        BEGIN
                            ALTER TABLE Deals ADD CreatedByUserId INT NULL;
                            EXEC('UPDATE d SET d.CreatedByUserId = COALESCE(d.AgentId, c.CreatedByUserId, 1) FROM Deals d LEFT JOIN Customers c ON d.CustomerId = c.CustomerId WHERE d.CreatedByUserId IS NULL');
                            ALTER TABLE Deals ALTER COLUMN CreatedByUserId INT NOT NULL;
                        END

                        UPDATE Deals SET
                            CgtPayer = ISNULL(CgtPayer, 'Seller'),
                            DstPayer = ISNULL(DstPayer, 'Buyer'),
                            TransferTaxPayer = ISNULL(TransferTaxPayer, 'Buyer'),
                            RegistrationFeePayer = ISNULL(RegistrationFeePayer, 'Buyer'),
                            PaymentScheme = ISNULL(PaymentScheme, 'Bank Financing')
                        WHERE CgtPayer IS NULL OR PaymentScheme IS NULL;
                    END

                    IF OBJECT_ID('DealContingencies', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[DealContingencies] (
                            [DealContingencyId] INT IDENTITY(1,1) NOT NULL,
                            [DealId] INT NOT NULL,
                            [ContingencyName] NVARCHAR(100) NOT NULL,
                            [Description] NVARCHAR(500) NULL,
                            [DueDate] DATETIME2 NULL,
                            [IsSatisfied] BIT NOT NULL DEFAULT 0,
                            [SatisfiedAt] DATETIME2 NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_DealContingencies] PRIMARY KEY CLUSTERED ([DealContingencyId] ASC),
                            CONSTRAINT [FK_DealContingencies_Deals_DealId] FOREIGN KEY ([DealId]) REFERENCES [dbo].[Deals] ([DealId]) ON DELETE CASCADE
                        );
                    END

                    IF OBJECT_ID('DealClauses', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[DealClauses] (
                            [DealClauseId] INT IDENTITY(1,1) NOT NULL,
                            [DealId] INT NOT NULL,
                            [ClauseId] NVARCHAR(50) NOT NULL,
                            [Title] NVARCHAR(200) NULL,
                            [ClauseText] NVARCHAR(MAX) NULL,
                            [IsApproved] BIT NOT NULL DEFAULT 1,
                            [ApprovedAt] DATETIME2 NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_DealClauses] PRIMARY KEY CLUSTERED ([DealClauseId] ASC),
                            CONSTRAINT [FK_DealClauses_Deals_DealId] FOREIGN KEY ([DealId]) REFERENCES [dbo].[Deals] ([DealId]) ON DELETE CASCADE
                        );
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already created
            }
        }

        private static void EnsureSupportTicketSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('SupportTickets', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('SupportTickets', 'TicketNumber') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD TicketNumber NVARCHAR(30) NULL;
                            EXEC('UPDATE SupportTickets SET TicketNumber = ''TCK-'' + RIGHT(''00000'' + CAST(TicketId AS VARCHAR(10)), 5) WHERE TicketNumber IS NULL');
                            ALTER TABLE SupportTickets ALTER COLUMN TicketNumber NVARCHAR(30) NOT NULL;
                        END

                        IF COL_LENGTH('SupportTickets', 'Category') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD Category NVARCHAR(50) NOT NULL CONSTRAINT DF_SupportTickets_Category DEFAULT 'Other';
                        END

                        IF COL_LENGTH('SupportTickets', 'DueDate') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD DueDate DATETIME2 NULL;
                        END

                        IF COL_LENGTH('SupportTickets', 'FirstRespondedAt') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD FirstRespondedAt DATETIME2 NULL;
                        END

                        IF COL_LENGTH('SupportTickets', 'IsDeleted') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD IsDeleted BIT NOT NULL CONSTRAINT DF_SupportTickets_IsDeleted DEFAULT 0;
                        END

                        IF COL_LENGTH('SupportTickets', 'DeletedAt') IS NULL
                        BEGIN
                            ALTER TABLE SupportTickets ADD DeletedAt DATETIME2 NULL;
                        END
                    END

                    IF OBJECT_ID('TicketComments', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[TicketComments] (
                            [TicketCommentId] INT IDENTITY(1,1) NOT NULL,
                            [TicketId] INT NOT NULL,
                            [AuthorUserId] INT NOT NULL,
                            [CommentText] NVARCHAR(2000) NOT NULL,
                            [CommentType] NVARCHAR(50) NOT NULL DEFAULT 'Comment',
                            [IsInternal] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_TicketComments] PRIMARY KEY CLUSTERED ([TicketCommentId] ASC),
                            CONSTRAINT [FK_TicketComments_SupportTickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [dbo].[SupportTickets] ([TicketId]) ON DELETE CASCADE,
                            CONSTRAINT [FK_TicketComments_Users_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [dbo].[Users] ([UserId])
                        );
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already updated
            }
        }

        private static void EnsureFollowUpSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('TaskReminders', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[TaskReminders] (
                            [TaskReminderId] INT IDENTITY(1,1) NOT NULL,
                            [Title] NVARCHAR(200) NOT NULL,
                            [DueDate] DATETIME2 NOT NULL,
                            [AssignedToUserId] INT NOT NULL,
                            [RelatedCustomerId] INT NULL,
                            [RelatedLeadId] INT NULL,
                            [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                            [Type] NVARCHAR(50) NOT NULL DEFAULT 'Call',
                            [Notes] NVARCHAR(2000) NULL,
                            [Priority] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] DATETIME2 NULL,
                            [CompletedAt] DATETIME2 NULL,
                            [IsDeleted] BIT NOT NULL DEFAULT 0,
                            [DeletedAt] DATETIME2 NULL,
                            CONSTRAINT [PK_TaskReminders] PRIMARY KEY CLUSTERED ([TaskReminderId] ASC),
                            CONSTRAINT [FK_TaskReminders_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [dbo].[Users] ([UserId]),
                            CONSTRAINT [FK_TaskReminders_Customers_RelatedCustomerId] FOREIGN KEY ([RelatedCustomerId]) REFERENCES [dbo].[Customers] ([CustomerId]) ON DELETE SET NULL,
                            CONSTRAINT [FK_TaskReminders_Leads_RelatedLeadId] FOREIGN KEY ([RelatedLeadId]) REFERENCES [dbo].[Leads] ([LeadId]) ON DELETE SET NULL
                        );

                        CREATE INDEX [IX_TaskReminders_AssignedToUserId] ON [dbo].[TaskReminders] ([AssignedToUserId]);
                        CREATE INDEX [IX_TaskReminders_DueDate] ON [dbo].[TaskReminders] ([DueDate]);
                        CREATE INDEX [IX_TaskReminders_IsDeleted] ON [dbo].[TaskReminders] ([IsDeleted]);
                        CREATE INDEX [IX_TaskReminders_RelatedCustomerId] ON [dbo].[TaskReminders] ([RelatedCustomerId]);
                        CREATE INDEX [IX_TaskReminders_RelatedLeadId] ON [dbo].[TaskReminders] ([RelatedLeadId]);
                        CREATE INDEX [IX_TaskReminders_Status] ON [dbo].[TaskReminders] ([Status]);
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already created
            }
        }

        private static void EnsureCampaignSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Campaigns', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[Campaigns] (
                            [CampaignId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [Name] NVARCHAR(150) NOT NULL,
                            [Channel] NVARCHAR(100) NULL,
                            [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',
                            [Budget] DECIMAL(18,2) NULL,
                            [StartDate] DATETIME2 NULL,
                            [EndDate] DATETIME2 NULL,
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_Campaigns] PRIMARY KEY CLUSTERED ([CampaignId] ASC)
                        );

                        CREATE INDEX [IX_Campaigns_TenantId] ON [dbo].[Campaigns] ([TenantId]);
                        CREATE INDEX [IX_Campaigns_IsActive] ON [dbo].[Campaigns] ([IsActive]);
                        CREATE INDEX [IX_Campaigns_Name] ON [dbo].[Campaigns] ([Name]);

                        INSERT INTO [dbo].[Campaigns] ([TenantId], [Name], [Channel], [Status], [IsActive], [CreatedAt])
                        VALUES 
                            (1, 'Facebook Ad', 'Social Media', 'Active', 1, GETUTCDATE()),
                            (1, 'Referral', 'Referral', 'Active', 1, GETUTCDATE()),
                            (1, 'Walk-in', 'Direct', 'Active', 1, GETUTCDATE()),
                            (1, 'Website', 'Website', 'Active', 1, GETUTCDATE()),
                            (1, 'Property Portal', 'Property Portal', 'Active', 1, GETUTCDATE()),
                            (1, 'Google Ads', 'Search Engine', 'Active', 1, GETUTCDATE()),
                            (1, 'Billboard / Outdoor', 'Outdoor', 'Active', 1, GETUTCDATE()),
                            (1, 'Open House / Event', 'Event', 'Active', 1, GETUTCDATE());
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already created
            }
        }

        private static void EnsureActivitySchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Activities', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('Activities', 'Outcome') IS NULL
                        BEGIN
                            ALTER TABLE Activities ADD Outcome NVARCHAR(50) NULL;
                        END
                        IF COL_LENGTH('Activities', 'DurationMinutes') IS NULL
                        BEGIN
                            ALTER TABLE Activities ADD DurationMinutes INT NULL;
                        END
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already updated
            }
        }

        private static void EnsureAutomatedEmailSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Customers', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('Customers', 'LastMarketUpdateSentAt') IS NULL
                        BEGIN
                            ALTER TABLE Customers ADD LastMarketUpdateSentAt DATETIME2 NULL;
                        END
                    END

                    IF OBJECT_ID('AutomatedEmailSettings', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[AutomatedEmailSettings] (
                            [SettingsId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [IsEnabled] BIT NOT NULL DEFAULT 0,
                            [FrequencyDays] INT NOT NULL DEFAULT 180,
                            [AnnualAppreciationRatePercent] DECIMAL(5,2) NOT NULL DEFAULT 5.0,
                            [EmailFormat] NVARCHAR(20) NOT NULL DEFAULT 'Html',
                            [TargetAudience] NVARCHAR(50) NOT NULL DEFAULT 'All',
                            [BrokerageName] NVARCHAR(150) NOT NULL DEFAULT 'NEXA Real Estate Advisory',
                            [CallToActionText] NVARCHAR(200) NOT NULL DEFAULT 'Schedule a Complimentary Equity Consultation',
                            [CallToActionUrl] NVARCHAR(500) NOT NULL DEFAULT 'https://nexacrm.local/cma-request',
                            [SubjectTemplate] NVARCHAR(300) NOT NULL,
                            [BodyTemplate] NVARCHAR(MAX) NOT NULL,
                            [LastBatchRunAt] DATETIME2 NULL,
                            [LastBatchStatus] NVARCHAR(500) NULL,
                            [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_AutomatedEmailSettings] PRIMARY KEY CLUSTERED ([SettingsId] ASC)
                        );

                        CREATE INDEX [IX_AutomatedEmailSettings_TenantId] ON [dbo].[AutomatedEmailSettings] ([TenantId]);
                    END
                    ELSE
                    BEGIN
                        IF COL_LENGTH('AutomatedEmailSettings', 'EmailFormat') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD EmailFormat NVARCHAR(20) NOT NULL CONSTRAINT DF_AES_EmailFormat DEFAULT 'Html';
                        IF COL_LENGTH('AutomatedEmailSettings', 'TargetAudience') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD TargetAudience NVARCHAR(50) NOT NULL CONSTRAINT DF_AES_TargetAudience DEFAULT 'All';
                        IF COL_LENGTH('AutomatedEmailSettings', 'BrokerageName') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD BrokerageName NVARCHAR(150) NOT NULL CONSTRAINT DF_AES_BrokerageName DEFAULT 'NEXA Real Estate Advisory';
                        IF COL_LENGTH('AutomatedEmailSettings', 'CallToActionText') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD CallToActionText NVARCHAR(200) NOT NULL CONSTRAINT DF_AES_CtaText DEFAULT 'Schedule a Complimentary Equity Consultation';
                        IF COL_LENGTH('AutomatedEmailSettings', 'CallToActionUrl') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD CallToActionUrl NVARCHAR(500) NOT NULL CONSTRAINT DF_AES_CtaUrl DEFAULT 'https://nexacrm.local/cma-request';
                    END

                    IF OBJECT_ID('MarketUpdateLogs', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[MarketUpdateLogs] (
                            [LogId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [CustomerId] INT NULL,
                            [CustomerName] NVARCHAR(150) NOT NULL,
                            [RecipientEmail] NVARCHAR(255) NOT NULL,
                            [PropertyAddress] NVARCHAR(300) NULL,
                            [PropertyType] NVARCHAR(50) NULL,
                            [OriginalPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [EstimatedValue] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [EquityGainAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [EquityGainPercent] DECIMAL(6,2) NOT NULL DEFAULT 0,
                            [EmailFormat] NVARCHAR(20) NOT NULL DEFAULT 'Html',
                            [Status] NVARCHAR(30) NOT NULL DEFAULT 'Sent',
                            [ErrorMessage] NVARCHAR(MAX) NULL,
                            [TriggerType] NVARCHAR(30) NOT NULL DEFAULT 'Scheduler',
                            [SentAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_MarketUpdateLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
                            CONSTRAINT [FK_MarketUpdateLogs_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([CustomerId]) ON DELETE SET NULL
                        );

                        CREATE INDEX [IX_MarketUpdateLogs_TenantId] ON [dbo].[MarketUpdateLogs] ([TenantId]);
                        CREATE INDEX [IX_MarketUpdateLogs_SentAt] ON [dbo].[MarketUpdateLogs] ([SentAt]);
                        CREATE INDEX [IX_MarketUpdateLogs_CustomerId] ON [dbo].[MarketUpdateLogs] ([CustomerId]);
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already created
            }
        }

        private static void EnsureBranchSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Branches', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[Branches] (
                            [BranchId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [BranchCode] NVARCHAR(50) NOT NULL,
                            [BranchName] NVARCHAR(150) NOT NULL,
                            [Address] NVARCHAR(300) NULL,
                            [Phone] NVARCHAR(50) NULL,
                            [ManagerUserId] INT NULL,
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_Branches] PRIMARY KEY CLUSTERED ([BranchId] ASC)
                        );
                        CREATE INDEX [IX_Branches_TenantId] ON [dbo].[Branches] ([TenantId]);
                    END

                    IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'BranchId') IS NULL
                    BEGIN
                        ALTER TABLE Users ADD BranchId INT NULL;
                    END

                    IF OBJECT_ID('Properties', 'U') IS NOT NULL AND COL_LENGTH('Properties', 'BranchId') IS NULL
                    BEGIN
                        ALTER TABLE Properties ADD BranchId INT NULL;
                    END

                    IF OBJECT_ID('Leads', 'U') IS NOT NULL AND COL_LENGTH('Leads', 'BranchId') IS NULL
                    BEGIN
                        ALTER TABLE Leads ADD BranchId INT NULL;
                    END

                    IF OBJECT_ID('Deals', 'U') IS NOT NULL AND COL_LENGTH('Deals', 'BranchId') IS NULL
                    BEGIN
                        ALTER TABLE Deals ADD BranchId INT NULL;
                    END
                ");
            }
            catch
            {
                // Silent fallback if server offline or already created
            }
        }
    }
}