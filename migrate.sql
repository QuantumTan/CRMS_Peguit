IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Roles] (
    [RoleId] int NOT NULL IDENTITY,
    [RoleName] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
);

CREATE TABLE [Subscriptions] (
    [SubscriptionId] int NOT NULL IDENTITY,
    [PlanName] nvarchar(100) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [BillingAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([SubscriptionId])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [RoleId] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION
);

CREATE TABLE [BackupLogs] (
    [BackupId] int NOT NULL IDENTITY,
    [PerformedByUserId] int NOT NULL,
    [BackupDate] datetime2 NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [FileLocation] nvarchar(500) NULL,
    CONSTRAINT [PK_BackupLogs] PRIMARY KEY ([BackupId]),
    CONSTRAINT [FK_BackupLogs_Users_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Customers] (
    [CustomerId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [Type] nvarchar(50) NOT NULL,
    [AssignedAgentId] int NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_Customers_Users_AssignedAgentId] FOREIGN KEY ([AssignedAgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [LoginSessions] (
    [SessionId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [LoginAt] datetime2 NOT NULL,
    [LogoutAt] datetime2 NULL,
    [IpAddress] nvarchar(50) NULL,
    CONSTRAINT [PK_LoginSessions] PRIMARY KEY ([SessionId]),
    CONSTRAINT [FK_LoginSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [SystemSettings] (
    [SettingId] int NOT NULL IDENTITY,
    [SettingKey] nvarchar(200) NOT NULL,
    [SettingValue] nvarchar(2000) NOT NULL,
    [UpdatedByUserId] int NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([SettingId]),
    CONSTRAINT [FK_SystemSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [BuyerProfiles] (
    [CustomerId] int NOT NULL,
    [Budget] decimal(18,2) NOT NULL,
    [PreferredLocation] nvarchar(200) NULL,
    [PreferredPropertyType] nvarchar(100) NULL,
    CONSTRAINT [PK_BuyerProfiles] PRIMARY KEY ([CustomerId]),
    CONSTRAINT [FK_BuyerProfiles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE
);

CREATE TABLE [Leads] (
    [LeadId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [Source] nvarchar(100) NULL,
    [AssignedAgentId] int NULL,
    [Stage] nvarchar(50) NOT NULL,
    [ConvertedCustomerId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Leads] PRIMARY KEY ([LeadId]),
    CONSTRAINT [FK_Leads_Customers_ConvertedCustomerId] FOREIGN KEY ([ConvertedCustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Leads_Users_AssignedAgentId] FOREIGN KEY ([AssignedAgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Properties] (
    [PropertyId] int NOT NULL IDENTITY,
    [OwnerCustomerId] int NOT NULL,
    [ListedByAgentId] int NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [PropertyType] nvarchar(100) NULL,
    [Price] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Properties] PRIMARY KEY ([PropertyId]),
    CONSTRAINT [FK_Properties_Customers_OwnerCustomerId] FOREIGN KEY ([OwnerCustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Properties_Users_ListedByAgentId] FOREIGN KEY ([ListedByAgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [SupportTickets] (
    [TicketId] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [RaisedByUserId] int NOT NULL,
    [AssignedToUserId] int NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ResolvedAt] datetime2 NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([TicketId]),
    CONSTRAINT [FK_SupportTickets_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SupportTickets_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportTickets_Users_RaisedByUserId] FOREIGN KEY ([RaisedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Activities] (
    [ActivityId] int NOT NULL IDENTITY,
    [Type] nvarchar(100) NOT NULL,
    [RelatedLeadId] int NULL,
    [RelatedCustomerId] int NULL,
    [LoggedByAgentId] int NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [ActivityDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Activities] PRIMARY KEY ([ActivityId]),
    CONSTRAINT [FK_Activities_Customers_RelatedCustomerId] FOREIGN KEY ([RelatedCustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Activities_Leads_RelatedLeadId] FOREIGN KEY ([RelatedLeadId]) REFERENCES [Leads] ([LeadId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Activities_Users_LoggedByAgentId] FOREIGN KEY ([LoggedByAgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Deals] (
    [DealId] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [PropertyId] int NOT NULL,
    [AgentId] int NOT NULL,
    [Value] decimal(18,2) NOT NULL,
    [CommissionRate] decimal(5,2) NOT NULL,
    [Stage] nvarchar(50) NOT NULL,
    [ExpectedCloseDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Deals] PRIMARY KEY ([DealId]),
    CONSTRAINT [FK_Deals_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Deals_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([PropertyId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Deals_Users_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [PropertyShowingDetails] (
    [ShowingDetailId] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [PropertyId] int NOT NULL,
    [ScheduledDate] datetime2 NULL,
    [FeedbackNotes] nvarchar(2000) NULL,
    CONSTRAINT [PK_PropertyShowingDetails] PRIMARY KEY ([ShowingDetailId]),
    CONSTRAINT [FK_PropertyShowingDetails_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([ActivityId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PropertyShowingDetails_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([PropertyId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Activities_LoggedByAgentId] ON [Activities] ([LoggedByAgentId]);

CREATE INDEX [IX_Activities_RelatedCustomerId] ON [Activities] ([RelatedCustomerId]);

CREATE INDEX [IX_Activities_RelatedLeadId] ON [Activities] ([RelatedLeadId]);

CREATE INDEX [IX_BackupLogs_PerformedByUserId] ON [BackupLogs] ([PerformedByUserId]);

CREATE INDEX [IX_Customers_AssignedAgentId] ON [Customers] ([AssignedAgentId]);

CREATE INDEX [IX_Deals_AgentId] ON [Deals] ([AgentId]);

CREATE INDEX [IX_Deals_CustomerId] ON [Deals] ([CustomerId]);

CREATE INDEX [IX_Deals_PropertyId] ON [Deals] ([PropertyId]);

CREATE INDEX [IX_Leads_AssignedAgentId] ON [Leads] ([AssignedAgentId]);

CREATE UNIQUE INDEX [IX_Leads_ConvertedCustomerId] ON [Leads] ([ConvertedCustomerId]) WHERE [ConvertedCustomerId] IS NOT NULL;

CREATE INDEX [IX_LoginSessions_UserId] ON [LoginSessions] ([UserId]);

CREATE INDEX [IX_Properties_ListedByAgentId] ON [Properties] ([ListedByAgentId]);

CREATE INDEX [IX_Properties_OwnerCustomerId] ON [Properties] ([OwnerCustomerId]);

CREATE UNIQUE INDEX [IX_PropertyShowingDetails_ActivityId] ON [PropertyShowingDetails] ([ActivityId]);

CREATE INDEX [IX_PropertyShowingDetails_PropertyId] ON [PropertyShowingDetails] ([PropertyId]);

CREATE INDEX [IX_SupportTickets_AssignedToUserId] ON [SupportTickets] ([AssignedToUserId]);

CREATE INDEX [IX_SupportTickets_CustomerId] ON [SupportTickets] ([CustomerId]);

CREATE INDEX [IX_SupportTickets_RaisedByUserId] ON [SupportTickets] ([RaisedByUserId]);

CREATE UNIQUE INDEX [IX_SystemSettings_SettingKey] ON [SystemSettings] ([SettingKey]);

CREATE INDEX [IX_SystemSettings_UpdatedByUserId] ON [SystemSettings] ([UpdatedByUserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260904144136_InitialTenantCreate', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [SystemSettings] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [SupportTickets] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Subscriptions] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Roles] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [PropertyShowingDetails] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Properties] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [LoginSessions] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Leads] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Deals] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Customers] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [BuyerProfiles] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [BackupLogs] ADD [TenantId] int NOT NULL DEFAULT 0;

ALTER TABLE [Activities] ADD [TenantId] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260905002759_AddTenantId', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Customers] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Customers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260905084516_AddSoftDeleteToLead', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Leads]') AND [c].[name] = N'Name');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Leads] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Leads] DROP COLUMN [Name];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'Name');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Customers] DROP COLUMN [Name];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Leads]') AND [c].[name] = N'Email');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Leads] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Leads] ALTER COLUMN [Email] nvarchar(255) NULL;

ALTER TABLE [Leads] ADD [DeletedAt] datetime2 NULL;

ALTER TABLE [Leads] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Leads] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Leads] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Leads] ADD [MiddleName] nvarchar(100) NULL;

ALTER TABLE [Leads] ADD [Suffix] nvarchar(20) NULL;

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'Email');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Customers] ALTER COLUMN [Email] nvarchar(255) NULL;

ALTER TABLE [Customers] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Customers] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Customers] ADD [MiddleName] nvarchar(100) NULL;

ALTER TABLE [Customers] ADD [Suffix] nvarchar(20) NULL;

CREATE INDEX [IX_Leads_IsDeleted] ON [Leads] ([IsDeleted]);

CREATE INDEX [IX_Leads_TenantId] ON [Leads] ([TenantId]);

CREATE INDEX [IX_Leads_TenantId_LastName_FirstName] ON [Leads] ([TenantId], [LastName], [FirstName]);

CREATE INDEX [IX_Customers_IsDeleted] ON [Customers] ([IsDeleted]);

CREATE INDEX [IX_Customers_TenantId] ON [Customers] ([TenantId]);

CREATE INDEX [IX_Customers_TenantId_LastName_FirstName] ON [Customers] ([TenantId], [LastName], [FirstName]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260905101921_InitialCreate', N'10.0.11');

COMMIT;
GO

