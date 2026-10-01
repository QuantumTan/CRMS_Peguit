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

        public static MasterCrmsDbContext CreateMasterContext(bool initializeDatabase = true)
        {
            var masterConn = MasterConnectionString;
            LocalDbHelper.EnsureLocalDbRunning(masterConn);

            var optionsBuilder = new DbContextOptionsBuilder<MasterCrmsDbContext>();
            optionsBuilder.UseSqlServer(masterConn, sql =>
            {
                if (initializeDatabase)
                {
                    sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }
                else
                {
                    sql.CommandTimeout(3);
                }
            });
            var options = optionsBuilder.Options;

            var context = new MasterCrmsDbContext(options);

            if (initializeDatabase && !_masterDbInitialized)
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
                    .UseSqlServer(masterConn, sql => sql.EnableRetryOnFailure(2, TimeSpan.FromSeconds(2), null))
                    .Options;
                using var context = new MasterCrmsDbContext(options);
                return await context.Database.CanConnectAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        public static RealEstateDbContext CreateContext(int tenantId = 1, bool initializeDatabase = true)
        {
            if (tenantId <= 0) tenantId = 1;
            var tenantConn = GetTenantConnectionString(tenantId);
            LocalDbHelper.EnsureLocalDbRunning(tenantConn);

            var optionsBuilder = new DbContextOptionsBuilder<RealEstateDbContext>();
            optionsBuilder.UseSqlServer(tenantConn, sql =>
            {
                if (initializeDatabase)
                {
                    sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }
                else
                {
                    sql.CommandTimeout(3);
                }
            });
            var options = optionsBuilder.Options;

            var context = new RealEstateDbContext(options, tenantId: tenantId);

            if (initializeDatabase && !_initializedTenants.ContainsKey(tenantId))
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

        /// <summary>
        /// Performs schema maintenance outside latency-sensitive UI operations.
        /// Authentication should use contexts with initializeDatabase: false.
        /// </summary>
        public static void WarmAuthenticationDatabases()
        {
            try
            {
                using var masterDb = CreateMasterContext();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalDb.WarmAuthenticationDatabases] Master DB: {ex.Message}");
            }

            foreach (int tenantId in new[] { 1, 2, 3 })
            {
                try
                {
                    using var tenantDb = CreateContext(tenantId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocalDb.WarmAuthenticationDatabases] Tenant {tenantId}: {ex.Message}");
                }
            }
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

                // 5. Ensure PlatformAuditLogs Table & Seed
                try
                {
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlatformAuditLogs')
                        BEGIN
                            CREATE TABLE PlatformAuditLogs (
                                AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
                                PerformedBySuperAdminId INT NOT NULL,
                                PerformedByName NVARCHAR(200) NOT NULL,
                                ActionType NVARCHAR(100) NOT NULL,
                                Detail NVARCHAR(2000) NOT NULL,
                                TargetCompanyId INT NULL,
                                TargetCompanyName NVARCHAR(200) NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                            CREATE INDEX IX_PlatformAuditLogs_ActionType ON PlatformAuditLogs(ActionType);
                            CREATE INDEX IX_PlatformAuditLogs_CreatedAt ON PlatformAuditLogs(CreatedAt);
                        END");

                    if (!context.PlatformAuditLogs.Any())
                    {
                        var now = DateTime.UtcNow;
                        context.PlatformAuditLogs.AddRange(new[]
                        {
                            new PlatformAuditLog
                            {
                                PerformedBySuperAdminId = 1,
                                PerformedByName = "Platform Super Admin",
                                ActionType = "BackupCreated",
                                Detail = "System baseline automated snapshot created",
                                TargetCompanyId = null,
                                TargetCompanyName = "Platform-Wide",
                                CreatedAt = now.AddDays(-2).AddHours(-4)
                            },
                            new PlatformAuditLog
                            {
                                PerformedBySuperAdminId = 1,
                                PerformedByName = "Platform Super Admin",
                                ActionType = "SubscriptionChanged",
                                Detail = "Company: Crestview Holdings (Tenant C) tier upgraded to Tenant C (Enterprise)",
                                TargetCompanyId = 3,
                                TargetCompanyName = "Crestview Holdings",
                                CreatedAt = now.AddDays(-1).AddHours(-2)
                            },
                            new PlatformAuditLog
                            {
                                PerformedBySuperAdminId = 1,
                                PerformedByName = "Platform Super Admin",
                                ActionType = "SystemSettingChanged",
                                Detail = "Security Policy: SessionTimeoutMinutes set to 60",
                                TargetCompanyId = null,
                                TargetCompanyName = "Platform-Wide",
                                CreatedAt = now.AddHours(-1)
                            }
                        });
                        context.SaveChanges();
                    }
                }
                catch
                {
                    // Non-critical fallback
                }

                // 6. Ensure PaymentRecords Table & Seed
                try
                {
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentRecords')
                        BEGIN
                            CREATE TABLE PaymentRecords (
                                PaymentRecordId INT IDENTITY(1,1) PRIMARY KEY,
                                SubscriptionId INT NOT NULL,
                                AmountPaid DECIMAL(18,2) NOT NULL,
                                PaymentMethod NVARCHAR(50) NOT NULL,
                                PaymentReference NVARCHAR(200) NOT NULL,
                                PaymentDate DATETIME2 NOT NULL,
                                RecordedByUserId INT NOT NULL,
                                Notes NVARCHAR(1000) NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                CONSTRAINT FK_PaymentRecords_Subscriptions_SubscriptionId FOREIGN KEY (SubscriptionId) REFERENCES Subscriptions(SubscriptionId) ON DELETE CASCADE,
                                CONSTRAINT FK_PaymentRecords_SuperAdmins_RecordedByUserId FOREIGN KEY (RecordedByUserId) REFERENCES SuperAdmins(SuperAdminId) ON DELETE NO ACTION
                            );
                            CREATE INDEX IX_PaymentRecords_SubscriptionId ON PaymentRecords(SubscriptionId);
                            CREATE INDEX IX_PaymentRecords_PaymentDate ON PaymentRecords(PaymentDate);
                            CREATE INDEX IX_PaymentRecords_PaymentReference ON PaymentRecords(PaymentReference);
                        END");

                    if (!context.PaymentRecords.Any())
                    {
                        var subs = context.Subscriptions.ToList();
                        var superAdmin = context.SuperAdmins.FirstOrDefault();
                        int saId = superAdmin?.SuperAdminId ?? 1;

                        foreach (var sub in subs)
                        {
                            var paymentDate = DateTime.UtcNow.AddDays(-15);
                            var payRec = new PaymentRecord
                            {
                                SubscriptionId = sub.SubscriptionId,
                                AmountPaid = sub.BillingAmount,
                                PaymentMethod = sub.CompanyId switch
                                {
                                    1 => PaymentMethod.GCash,
                                    2 => PaymentMethod.BankTransfer,
                                    _ => PaymentMethod.Check
                                },
                                PaymentReference = sub.CompanyId switch
                                {
                                    1 => "GCASH-98234182903",
                                    2 => "BDO-FT-20260901-884",
                                    _ => "CHK-MBTC-004928"
                                },
                                PaymentDate = paymentDate,
                                RecordedByUserId = saId,
                                Notes = "Subscription manual offline payment confirmed and reconciled against bank statement.",
                                CreatedAt = paymentDate
                            };
                            context.PaymentRecords.Add(payRec);

                            // Recalculate status automatically based on EndDate
                            sub.Status = Subscription.CalculateStatus(sub.EndDate);
                        }
                        context.SaveChanges();
                    }
                    else
                    {
                        var subs = context.Subscriptions.ToList();
                        foreach (var sub in subs)
                        {
                            sub.Status = Subscription.CalculateStatus(sub.EndDate);
                        }
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureMasterDatabaseInitialized] PaymentRecords error: {ex.Message}");
                }

                // 7. Ensure TenantBrandings Table & Backfill Existing Tenants
                try
                {
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TenantBrandings')
                        BEGIN
                            CREATE TABLE TenantBrandings (
                                CompanyId INT PRIMARY KEY,
                                DisplayName NVARCHAR(100) NOT NULL,
                                LogoImage VARBINARY(MAX) NULL,
                                LogoVersion INT NOT NULL DEFAULT 1,
                                AccentColor NVARCHAR(20) NULL,
                                ContactEmail NVARCHAR(255) NULL,
                                ContactPhone NVARCHAR(50) NULL,
                                Address NVARCHAR(500) NULL,
                                HidePoweredBy BIT NOT NULL DEFAULT 0,
                                UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                                UpdatedByUserId INT NULL,
                                CONSTRAINT FK_TenantBrandings_Companies_CompanyId FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId) ON DELETE CASCADE
                            );
                        END");

                    var existingCompanies = context.Companies.ToList();
                    var existingBrandings = context.TenantBrandings.ToDictionary(b => b.CompanyId);

                    bool changed = false;
                    foreach (var comp in existingCompanies)
                    {
                        if (!existingBrandings.ContainsKey(comp.CompanyId))
                        {
                            var branding = new TenantBranding
                            {
                                CompanyId = comp.CompanyId,
                                DisplayName = comp.CompanyName,
                                LogoVersion = 1,
                                HidePoweredBy = comp.CompanyId == 3,
                                AccentColor = comp.CompanyId == 3 ? "#0284C7" : null,
                                ContactEmail = comp.CompanyId switch
                                {
                                    1 => "contact@apexrealty.com",
                                    2 => "info@bluehorizon.com",
                                    3 => "concierge@crestview.ph",
                                    _ => null
                                },
                                ContactPhone = comp.CompanyId switch
                                {
                                    1 => "+63 2 8123 4567",
                                    2 => "+63 2 8987 6543",
                                    3 => "+63 2 8555 1234",
                                    _ => null
                                },
                                Address = comp.CompanyId switch
                                {
                                    1 => "Ayala Triangle, Makati City, Metro Manila",
                                    2 => "High Street, BGC, Taguig City",
                                    3 => "Emerald Avenue, Ortigas Center, Pasig City",
                                    _ => null
                                },
                                UpdatedAt = DateTime.UtcNow
                            };
                            context.TenantBrandings.Add(branding);
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureMasterDatabaseInitialized] TenantBrandings error: {ex.Message}");
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
            EnsureEmailTemplateSchema(context);
            EnsureBranchSchema(context);
            EnsureRetentionSchema(context);
        }

        private static void EnsureTenantDatabaseInitialized(RealEstateDbContext context, int tenantId)
        {
            try
            {
                context.Database.EnsureCreated();

                EnsureAllSchemas(context);
                EnsureAuthenticationIndexes(context);

                // Only seed base users if no users exist in this tenant's database
                try
                {
                    if (!context.Users.Any())
                    {
                        Task.Run(async () =>
                        {
                            await DbSeeder.SeedTestUsersAsync(context, tenantId).ConfigureAwait(false);
                        }).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureTenantDatabaseInitialized] Seeding users error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalDb] EnsureTenantDatabaseInitialized notice: {ex.Message}");
            }
        }

        private static void EnsureAuthenticationIndexes(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('[dbo].[Users]', 'U') IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1 FROM sys.indexes
                           WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('[dbo].[Users]')
                       )
                    BEGIN
                        CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);
                    END");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureAuthenticationIndexes] {ex.Message}");
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
                        IF COL_LENGTH('AutomatedEmailSettings', 'ActiveTemplateId') IS NULL
                            ALTER TABLE AutomatedEmailSettings ADD ActiveTemplateId INT NULL;
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

        private static void EnsureEmailTemplateSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('EmailTemplates', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[EmailTemplates] (
                            [TemplateId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [Name] NVARCHAR(150) NOT NULL,
                            [Category] NVARCHAR(50) NOT NULL DEFAULT 'Equity Retention',
                            [TargetAudience] NVARCHAR(50) NOT NULL DEFAULT 'All',
                            [EmailFormat] NVARCHAR(20) NOT NULL DEFAULT 'Html',
                            [Subject] NVARCHAR(300) NOT NULL,
                            [Body] NVARCHAR(MAX) NOT NULL,
                            [CallToActionText] NVARCHAR(200) NULL,
                            [CallToActionUrl] NVARCHAR(500) NULL,
                            [IsSystem] BIT NOT NULL DEFAULT 0,
                            [IsActive] BIT NOT NULL DEFAULT 1,
                            [CreatedByRole] NVARCHAR(50) NOT NULL DEFAULT 'System',
                            [CreatedByUserId] INT NULL,
                            [IsDeleted] BIT NOT NULL DEFAULT 0,
                            [DeletedAt] DATETIME2 NULL,
                            [DeletedByUserId] INT NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_EmailTemplates] PRIMARY KEY CLUSTERED ([TemplateId] ASC)
                        );

                        CREATE INDEX [IX_EmailTemplates_TenantId] ON [dbo].[EmailTemplates] ([TenantId]);
                        CREATE INDEX [IX_EmailTemplates_IsDeleted] ON [dbo].[EmailTemplates] ([IsDeleted]);
                        CREATE INDEX [IX_EmailTemplates_IsActive] ON [dbo].[EmailTemplates] ([IsActive]);
                    END
                ");

                // Seed system templates if none exist
                if (!context.EmailTemplates.Any(t => t.IsSystem && !t.IsDeleted))
                {
                    var systemTemplates = new[]
                    {
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Annual Homeowner Equity & Valuation Report",
                            Category = "Equity Retention",
                            TargetAudience = "Buyers",
                            EmailFormat = "Html",
                            Subject = "Homeowner Equity Update: Your property at {PropertyAddress} has appreciated!",
                            CallToActionText = "Explore Home Equity & Wealth Strategy",
                            CallToActionUrl = "https://nexacrm.local/cma-request",
                            Body = "Dear {FirstName},\n\nCongratulations on your continuing journey as a homeowner with NEXA Real Estate Advisory!\n\nIt has been {YearsOwned} year(s) since you acquired your property, and we wanted to share an exciting update regarding your investment performance:\n\n• Property Address: {PropertyAddress} ({PropertyType})\n• Acquisition Price: {OriginalPrice}\n• Estimated Current Value: {EstimatedValue}\n• Net Equity Accumulated: +{EquityGain} (+{EquityPercent}% gain since purchase!)\n\nYour home continues to be one of your most dependable wealth-building assets. Many homeowners use this accumulated equity to fund home improvements, eliminate mortgage insurance, or leverage into a secondary income-generating rental property.\n\nIf you would like a detailed breakdown of your equity options or have questions on the current neighborhood market, feel free to reply directly to this email or reach out anytime.\n\nBest regards,\n{AgentName}\nYour Dedicated Real Estate Advisor",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "High Buyer Demand & Market Timing Valuation",
                            Category = "Listing CMA",
                            TargetAudience = "Sellers",
                            EmailFormat = "Html",
                            Subject = "High Buyer Demand in Your Area: What your property at {PropertyAddress} is worth today",
                            CallToActionText = "Request Comprehensive CMA & Net Sheet",
                            CallToActionUrl = "https://nexacrm.local/cma-request",
                            Body = "Dear {CustomerName},\n\nMarket conditions in your neighborhood are creating prime opportunities for property owners!\n\nInventory levels remain tight, and qualified buyers are actively looking for properties in your area. Based on recent comparable sales, here is what your asset could command in today's active market:\n\n• Property: {PropertyAddress} ({PropertyType})\n• Historical Benchmark Price: {OriginalPrice}\n• Estimated Current Market Value: {EstimatedValue} (+{AppreciationRate}% annual benchmark)\n• Potential Capital Gain: +{EquityGain} (+{EquityPercent}% upside)\n\nIf you have been considering selling, upgrading to a larger home, or reallocating your capital into higher-yielding investments, now may be an optimal time to capitalize on peak market valuation.\n\nWe would be delighted to prepare a comprehensive Comparative Market Analysis (CMA) and estimate your net proceeds at no cost or obligation.\n\nWarm regards,\n{AgentName}\nSenior Real Estate Listing Specialist\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "General Client Care & Asset Valuation",
                            Category = "Equity Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Market Valuation & Equity Report for {PropertyAddress}",
                            CallToActionText = "Schedule a Complimentary Equity Consultation",
                            CallToActionUrl = "https://nexacrm.local/cma-request",
                            Body = "Dear {CustomerName},\n\nWe hope you are doing well!\n\nAs part of our continuous client care service at NEXA Real Estate Advisory, we actively track local transactions and neighborhood appreciation trends.\n\nBased on recent market activity in your area, here is an updated valuation and equity summary for your property:\n\n• Property: {PropertyAddress} ({PropertyType})\n• Acquisition Price: {OriginalPrice}\n• Holding Period: {YearsOwned} year(s)\n• Estimated Current Market Value: {EstimatedValue} (+{AppreciationRate}% est. annual growth)\n• Estimated Equity Growth: +{EquityGain} (+{EquityPercent}% total)\n\nWhether you are thinking about making home improvements, exploring refinancing options, or simply keeping tabs on your net worth, we are here to support your real estate goals.\n\nWarm regards,\n{AgentName}\nNEXA Real Estate Advisory Team",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Home Purchase Anniversary & Milestone Celebration",
                            Category = "Client Milestone",
                            TargetAudience = "Buyers",
                            EmailFormat = "Html",
                            Subject = "Happy Home Anniversary! Celebrating {YearsOwned} Year(s) at {PropertyAddress}",
                            CallToActionText = "Schedule Annual Home Check-up",
                            CallToActionUrl = "https://nexacrm.local/anniversary",
                            Body = "Dear {FirstName},\n\nHappy Home Anniversary! It has been {YearsOwned} year(s) since you closed on {PropertyAddress}.\n\nTime flies, and we hope your home has brought you wonderful memories and steady investment growth. As a valued client of NEXA Real Estate Advisory, we are always here to help you review local market trends, property tax assessments, or trusted contractor recommendations.\n\nThank you for trusting us with your real estate journey!\n\nWarmest regards,\n{AgentName}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Exclusive New Listing & Investment Alert",
                            Category = "New Listing",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Exclusive Real Estate Alert: New Prime Property Opportunity",
                            CallToActionText = "View Property Dossier & Photos",
                            CallToActionUrl = "https://nexacrm.local/featured-properties",
                            Body = "Dear {CustomerName},\n\nWe are excited to share an exclusive new property listing that has just become available in our portfolio!\n\nWhether you are seeking a new family residence or exploring prime high-yield investment properties to expand your portfolio, this asset offers outstanding location advantages and strong capital appreciation potential.\n\nReply directly to this email or click the link below to review full architectural floor plans, pricing sheets, and private viewing schedules.\n\nBest regards,\n{AgentName}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "VIP Property Showing & Phase Launch Invitation",
                            Category = "Showing VIP",
                            TargetAudience = "Buyers",
                            EmailFormat = "Html",
                            Subject = "VIP Invitation: Private Property Showing & Advance Preview",
                            CallToActionText = "Reserve Your Private Showing",
                            CallToActionUrl = "https://nexacrm.local/vip-showing",
                            Body = "Dear {FirstName},\n\nYou are cordially invited to an exclusive VIP advance showing for premier properties in our collection.\n\nAs a priority client, you receive first-look access before the property is marketed to the general public. Our team will be on site to provide comprehensive property tours, investment yield analysis, and personalized advisory.\n\nPlease reserve your viewing slot today or contact me directly to confirm your preferred schedule.\n\nSincerely,\n{AgentName}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        }
                    };

                    context.EmailTemplates.AddRange(systemTemplates);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalDb] EnsureEmailTemplateSchema notice: {ex.Message}");
            }
        }

        private static void EnsureRetentionSchema(RealEstateDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Customers', 'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH('Customers', 'CurrentRetentionSegment') IS NULL
                        BEGIN
                            ALTER TABLE Customers ADD CurrentRetentionSegment NVARCHAR(100) NOT NULL CONSTRAINT DF_Customers_RetentionSegment DEFAULT 'Prospective Client';
                        END
                        IF COL_LENGTH('Customers', 'RetentionSegmentCalculatedAt') IS NULL
                        BEGIN
                            ALTER TABLE Customers ADD RetentionSegmentCalculatedAt DATETIME2 NULL;
                        END
                        IF COL_LENGTH('Customers', 'LastRetentionEmailSentAt') IS NULL
                        BEGIN
                            ALTER TABLE Customers ADD LastRetentionEmailSentAt DATETIME2 NULL;
                        END
                    END

                    IF OBJECT_ID('RetentionRequests', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[RetentionRequests] (
                            [RequestId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [CustomerId] INT NOT NULL,
                            [SubmittedByUserId] INT NOT NULL,
                            [AssignedAgentId] INT NULL,
                            [TargetSegment] NVARCHAR(100) NOT NULL,
                            [ActionType] NVARCHAR(100) NOT NULL DEFAULT 'Incentive Offer',
                            [ProposedIncentive] NVARCHAR(255) NOT NULL,
                            [RetentionDetails] NVARCHAR(2000) NOT NULL,
                            [ReasonCategory] NVARCHAR(100) NOT NULL DEFAULT 'Improve Customer Retention',
                            [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                            [ReviewedByUserId] INT NULL,
                            [ReviewedAt] DATETIME2 NULL,
                            [ReviewerRemarks] NVARCHAR(1000) NULL,
                            [RejectionReason] NVARCHAR(1000) NULL,
                            [AddedToCampaign] BIT NOT NULL DEFAULT 0,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_RetentionRequests] PRIMARY KEY CLUSTERED ([RequestId] ASC)
                        );
                        CREATE INDEX [IX_RetentionRequests_TenantId] ON [dbo].[RetentionRequests] ([TenantId]);
                        CREATE INDEX [IX_RetentionRequests_CustomerId] ON [dbo].[RetentionRequests] ([CustomerId]);
                        CREATE INDEX [IX_RetentionRequests_Status] ON [dbo].[RetentionRequests] ([Status]);
                        CREATE INDEX [IX_RetentionRequests_SubmittedByUserId] ON [dbo].[RetentionRequests] ([SubmittedByUserId]);
                    END

                    IF OBJECT_ID('RetentionEmailLogs', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[RetentionEmailLogs] (
                            [EmailLogId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [CustomerId] INT NOT NULL,
                            [RetentionRequestId] INT NULL,
                            [RecipientEmail] NVARCHAR(255) NOT NULL,
                            [RecipientName] NVARCHAR(150) NOT NULL,
                            [Segment] NVARCHAR(100) NOT NULL,
                            [Subject] NVARCHAR(300) NOT NULL,
                            [Body] NVARCHAR(MAX) NOT NULL,
                            [IncentiveOffered] NVARCHAR(255) NULL,
                            [Status] NVARCHAR(50) NOT NULL DEFAULT 'Queued',
                            [GenerationSource] NVARCHAR(50) NOT NULL DEFAULT 'AutomatedRequest',
                            [DispatchedByUserId] INT NULL,
                            [DispatchedAt] DATETIME2 NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [ErrorMessage] NVARCHAR(1000) NULL,
                            [IsCooldownOverride] BIT NOT NULL DEFAULT 0,
                            [CooldownOverrideReason] NVARCHAR(500) NULL,
                            CONSTRAINT [PK_RetentionEmailLogs] PRIMARY KEY CLUSTERED ([EmailLogId] ASC)
                        );
                        CREATE INDEX [IX_RetentionEmailLogs_TenantId] ON [dbo].[RetentionEmailLogs] ([TenantId]);
                        CREATE INDEX [IX_RetentionEmailLogs_CustomerId] ON [dbo].[RetentionEmailLogs] ([CustomerId]);
                        CREATE INDEX [IX_RetentionEmailLogs_Status] ON [dbo].[RetentionEmailLogs] ([Status]);
                        CREATE INDEX [IX_RetentionEmailLogs_RetentionRequestId] ON [dbo].[RetentionEmailLogs] ([RetentionRequestId]);
                    END

                    IF OBJECT_ID('RetentionAuditLogs', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[RetentionAuditLogs] (
                            [AuditId] INT IDENTITY(1,1) NOT NULL,
                            [TenantId] INT NOT NULL DEFAULT 1,
                            [PerformedByUserId] INT NOT NULL,
                            [PerformedByName] NVARCHAR(150) NOT NULL,
                            [UserRole] NVARCHAR(50) NOT NULL,
                            [ActionType] NVARCHAR(100) NOT NULL,
                            [TargetCustomerId] INT NULL,
                            [TargetCustomerName] NVARCHAR(150) NULL,
                            [Detail] NVARCHAR(2000) NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_RetentionAuditLogs] PRIMARY KEY CLUSTERED ([AuditId] ASC)
                        );
                        CREATE INDEX [IX_RetentionAuditLogs_TenantId] ON [dbo].[RetentionAuditLogs] ([TenantId]);
                        CREATE INDEX [IX_RetentionAuditLogs_ActionType] ON [dbo].[RetentionAuditLogs] ([ActionType]);
                        CREATE INDEX [IX_RetentionAuditLogs_TargetCustomerId] ON [dbo].[RetentionAuditLogs] ([TargetCustomerId]);
                    END
                ");

                // Seed Retention Templates if not present
                bool hasRetentionTemplates = context.EmailTemplates.Any(t => t.Category == "Retention");
                if (!hasRetentionTemplates)
                {
                    var retentionTemplates = new List<EmailTemplate>
                    {
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "New Client - First-Transaction Gratitude & Referral",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Thank You for Trusting Us with Your Real Estate Journey, {{customer_name}}!",
                            CallToActionText = "Share a Referral or Feedback",
                            CallToActionUrl = "https://nexacrm.local/referral",
                            Body = "Dear {{customer_name}},\n\nOn behalf of our entire team, thank you for choosing us to help you close your recent transaction!\n\nIt was a privilege to assist you, and we remain dedicated to supporting you with any ongoing property documents or questions. If you know friends or colleagues looking to buy or sell, we would be honored to provide them with the same high level of care.\n\nWarm regards,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Recent Client - Post-Move Settlement Check-In",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Checking In: How is Everything Going at {{property_address}}?",
                            CallToActionText = "Schedule Post-Move Consultation",
                            CallToActionUrl = "https://nexacrm.local/checkin",
                            Body = "Hi {{customer_name}},\n\nWe wanted to take a moment to check in and see how you are settling in since closing on your property.\n\nOur client care doesn't stop at closing — whether you need recommendations for trusted contractors or tax guidance, we are always here for you.\n\nBest regards,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Repeat Client - VIP Recognition & Priority Concession",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Exclusive VIP Client Appreciation: Priority Access & Concession",
                            CallToActionText = "Claim Your VIP Privilege",
                            CallToActionUrl = "https://nexacrm.local/vip-client",
                            Body = "Dear {{customer_name}},\n\nThank you for your ongoing partnership with NEXA Real Estate Advisory. Having worked together on multiple transactions, you are one of our most valued clients.\n\nAs a token of our appreciation, we are pleased to offer you: {{proposed_incentive}}.\n\nWhenever you're ready to evaluate your portfolio or explore your next move, let's connect.\n\nWarmest regards,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "At Risk - Neighborhood Market Re-Engagement",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Market Update for {{property_address}}: Active Buyer Demand Nearby",
                            CallToActionText = "Request Local Market Snapshot",
                            CallToActionUrl = "https://nexacrm.local/market-update",
                            Body = "Hello {{customer_name}},\n\nIt's been a little while since we last spoke! We have noticed significant movement in local market inventory and pricing trends in your neighborhood.\n\nWe would love to share a complimentary market snapshot with you. No pressure at all — just keeping you informed on what your property is worth in today's market.\n\nSincerely,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Inactive Client - Comprehensive Portfolio Property Valuation",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "A Lot Has Changed: Complimentary Property Valuation & Consultation",
                            CallToActionText = "Book Free Valuation Consultation",
                            CallToActionUrl = "https://nexacrm.local/free-valuation",
                            Body = "Dear {{customer_name}},\n\nThe real estate market has experienced substantial shifts since our last conversation. Values, interest benchmarks, and neighborhood demand have evolved considerably.\n\nWe would love to reconnect and offer you a complimentary, comprehensive property valuation and equity review at your convenience.\n\nWarm regards,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        },
                        new EmailTemplate
                        {
                            TenantId = 1,
                            Name = "Prospective Client - First-Time Buyer & Investor Nurture",
                            Category = "Retention",
                            TargetAudience = "All",
                            EmailFormat = "Html",
                            Subject = "Exclusive Market Insights & Curated Opportunities",
                            CallToActionText = "Explore Prime Listings",
                            CallToActionUrl = "https://nexacrm.local/curated-listings",
                            Body = "Dear {{customer_name}},\n\nWe hope this email finds you well. As you navigate your real estate options, having clear market data and early access to opportunities makes all the difference.\n\nHere is our latest curated property selection and neighborhood analysis. We are always here to answer questions or arrange private viewings tailored to your timeline.\n\nBest regards,\n{{agent_name}}\nNEXA Real Estate Advisory",
                            IsSystem = true,
                            IsActive = true,
                            CreatedByRole = "System"
                        }
                    };

                    context.EmailTemplates.AddRange(retentionTemplates);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalDb] EnsureRetentionSchema notice: {ex.Message}");
            }
        }
    }
}
