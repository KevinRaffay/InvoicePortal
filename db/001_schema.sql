/*
    Invoice Portal Admin - database schema (proof of concept)

    Tables used by the admin UI, plus everything they reference through a foreign key.
    Generated from the local SQL Server 2022 Express container, then committed so the repo
    stands on its own: `docker compose up` builds the database from this file and
    002_seed_demo_data.sql, with no bacpac and no connection to any Monster Energy system.

    Contains NO production data. See 002_seed_demo_data.sql for the synthetic demo rows.

    Apply order: 001_schema.sql, then 002_seed_demo_data.sql.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================ tables

CREATE TABLE [dbo].[AspNetUsers] (
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_AspNetUsers_UpdatedAt] DEFAULT (sysutcdatetime()),
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL,
    [TwoFactorEnabled] BIT NOT NULL,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL,
    [AccessFailedCount] INT NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Bottlers] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [SalesOrganization] NVARCHAR(4) NOT NULL,
    [CurrencyId] INT NOT NULL,
    [CountryId] INT NOT NULL,
    [CommercialManagerId] INT NULL,
    [BrandSegmentCategoryId] INT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Bottlers_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Bottlers_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Bottlers] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[BrandSegmentCategories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [CompanyId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_BrandSegmentCategories_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_BrandSegmentCategories_UpdatedAt] DEFAULT (sysutcdatetime()),
    [CreatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_BrandSegmentCategories_CreatedBy] DEFAULT (N''),
    [Status] INT NOT NULL CONSTRAINT [DF_BrandSegmentCategories_Status] DEFAULT ((1)),
    [UpdatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_BrandSegmentCategories_UpdatedBy] DEFAULT (N''),
    CONSTRAINT [PK_BrandSegmentCategories] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[CancellationCategories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(255) NOT NULL,
    [CancellationCategoryStatus] INT NOT NULL CONSTRAINT [DF_CancellationCategories_CancellationCategoryStatus] DEFAULT ((1)),
    [Deleted] BIT NOT NULL CONSTRAINT [DF_CancellationCategories_Deleted] DEFAULT (CONVERT([bit],(0))),
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_CancellationCategories_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_CancellationCategories_UpdatedAt] DEFAULT (sysutcdatetime()),
    [CreatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_CancellationCategories_CreatedBy] DEFAULT (N''),
    [UpdatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_CancellationCategories_UpdatedBy] DEFAULT (N''),
    CONSTRAINT [PK_CancellationCategories] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[CommercialManagers] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Email] NVARCHAR(256) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_CommercialManagers_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_CommercialManagers_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_CommercialManagers] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Companies] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(3) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Companies_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Companies_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Countries] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(3) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Countries_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Countries_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Currencies] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(3) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Currencies_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Currencies_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Currencies] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[FundingElements] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(MAX) NOT NULL,
    [AccountingYear] INT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_FundingElements_IsActive] DEFAULT (CONVERT([bit],(0))),
    [ProgramTypeId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_FundingElements_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_FundingElements_UpdatedAt] DEFAULT (sysutcdatetime()),
    [AppliesToDfp] BIT NOT NULL CONSTRAINT [DF_FundingElements_AppliesToDfp] DEFAULT (CONVERT([bit],(0))),
    CONSTRAINT [PK_FundingElements] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Invoices] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Amount] DECIMAL(19,4) NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Number] NVARCHAR(32) NULL,
    [BottlerId] INT NULL,
    [ProgramId] NVARCHAR(16) NULL,
    [CoOpPercentage] DECIMAL(5,2) NULL,
    [AccountType] INT NULL,
    [PayerId] INT NULL,
    [SalesCenterId] INT NULL,
    [BrandSegmentCategoryId] INT NULL,
    [InvoiceDate] DATE NULL,
    [InvoiceStatus] INT NOT NULL,
    [CurrencyId] INT NULL,
    [AutoGenerateInvoiceAttachment] BIT NOT NULL CONSTRAINT [DF_Invoices_AutoGenerateInvoiceAttachment] DEFAULT (CONVERT([bit],(0))),
    [RevisedInvoiceId] INT NULL,
    [RevisionDate] DATETIME2(7) NULL,
    [RevisionReason] INT NULL,
    [HistoryCorrelationId] UNIQUEIDENTIFIER NULL,
    [RevisionNumber] INT NOT NULL,
    [ExportedToSap] DATETIME2(7) NULL,
    [ClaimId] INT NULL,
    [PromoPeriodFrom] DATE NULL,
    [PromoPeriodTo] DATE NULL,
    [CompanyId] INT NULL,
    [UserName] NVARCHAR(256) NOT NULL CONSTRAINT [DF_Invoices_UserName] DEFAULT (N''),
    [UserEmail] NVARCHAR(256) NOT NULL CONSTRAINT [DF_Invoices_UserEmail] DEFAULT (N''),
    [SapInvoiceNumber] AS (case when len([Number])>(0) then substring([Number],(1),(16))  end),
    [CountryId] INT NULL,
    [InvoiceType] NVARCHAR(32) NOT NULL,
    [UnlistedProgramName] NVARCHAR(4000) NULL,
    [UnlistedProgramTypeId] INT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Invoices_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Invoices_UpdatedAt] DEFAULT (sysutcdatetime()),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Invoices_IsDeleted] DEFAULT (CONVERT([bit],(0))),
    [InvoiceFinalizedDetailsId] INT NULL,
    [HasPayment] BIT NOT NULL CONSTRAINT [DF_Invoices_HasPayment] DEFAULT (CONVERT([bit],(0))),
    [CalculatedBottlerInvoiceStatus] AS (case when [InvoiceStatus]=(15) OR [InvoiceStatus]=(14) OR [InvoiceStatus]=(13) OR [InvoiceStatus]=(12) OR [InvoiceStatus]=(11) OR [InvoiceStatus]=(10) OR [InvoiceStatus]=(8) OR [InvoiceStatus]=(7) OR [InvoiceStatus]=(6) then (0) when [InvoiceStatus]=(19) OR [InvoiceStatus]=(16) OR [InvoiceStatus]=(9) then (1) when [InvoiceStatus]=(17) AND [HasPayment]=(0) then (17) when [InvoiceStatus]=(17) AND [HasPayment]=(1) then (2) else CONVERT([int],[InvoiceStatus]) end),
    [CalculatedAdminInvoiceStatus] AS (case when [ClaimId] IS NULL AND [InvoiceStatus]<>(3) AND [InvoiceStatus]<>(4) then (18) when [InvoiceStatus]=(3) then (3) else CONVERT([int],[InvoiceStatus]) end),
    [CancellationCategoryId] INT NULL,
    [PoNumber] NVARCHAR(11) NULL,
    [SamplesPurposeId] INT NULL,
    [DoNotHavePoNumber] BIT NULL,
    [RefDocNumber] NVARCHAR(16) NULL,
    [PoGrandTotal] DECIMAL(19,4) NULL,
    [PoInvoiceNumber] NVARCHAR(32) NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Payers] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [BottlerId] INT NOT NULL,
    [Address] NVARCHAR(128) NOT NULL CONSTRAINT [DF_Payers_Address] DEFAULT (N''),
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Payers_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Payers_UpdatedAt] DEFAULT (sysutcdatetime()),
    [ZipCode] NVARCHAR(16) NOT NULL CONSTRAINT [DF_Payers_ZipCode] DEFAULT (N''),
    [City] NVARCHAR(128) NOT NULL CONSTRAINT [DF_Payers_City] DEFAULT (N''),
    [State] NVARCHAR(5) NOT NULL CONSTRAINT [DF_Payers_State] DEFAULT (N''),
    [StatementContactId] INT NULL,
    [VendorId] INT NULL,
    [DoNotUse] BIT NULL CONSTRAINT [DF_Payers_DoNotUse] DEFAULT ((0)),
    CONSTRAINT [PK_Payers] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Programs] (
    [Id] NVARCHAR(16) NOT NULL,
    [Name] NVARCHAR(4000) NOT NULL,
    [From] DATETIME2(7) NOT NULL,
    [To] DATETIME2(7) NOT NULL,
    [BottlerId] INT NOT NULL,
    [CoOpPercentage] DECIMAL(5,2) NOT NULL,
    [CoOpAmount] DECIMAL(18,2) NOT NULL,
    [AmountSettled] DECIMAL(18,2) NOT NULL,
    [FundingElementId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Programs_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Programs_UpdatedAt] DEFAULT (sysutcdatetime()),
    [IsDeleted] BIT NULL,
    CONSTRAINT [PK_Programs] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[ProgramTypes] (
    [Id] INT NOT NULL,
    [ProgramTypeKind] NVARCHAR(32) NOT NULL,
    [Name] NVARCHAR(64) NOT NULL,
    [SapCategoryCode] INT NULL,
    [SapClaimReason] NVARCHAR(3) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ProgramTypes_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ProgramTypes_UpdatedAt] DEFAULT (sysutcdatetime()),
    [Condition] INT NULL,
    [CreatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_ProgramTypes_CreatedBy] DEFAULT (N''),
    [ProgramTypeStatus] INT NOT NULL CONSTRAINT [DF_ProgramTypes_ProgramTypeStatus] DEFAULT ((1)),
    [UpdatedBy] NVARCHAR(256) NOT NULL CONSTRAINT [DF_ProgramTypes_UpdatedBy] DEFAULT (N''),
    [Source] AS (case when [ProgramTypeKind]='Listed' AND [Id]=(99999) then (0) when [ProgramTypeKind]='Listed' AND [Id]<>(99999) then (1) else (0) end),
    [ShownOnSetup] BIT NOT NULL CONSTRAINT [DF_ProgramTypes_ShownOnSetup] DEFAULT (CONVERT([bit],(1))),
    CONSTRAINT [PK_ProgramTypes] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[SalesCenters] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Address] NVARCHAR(128) NOT NULL,
    [PayerId] INT NOT NULL,
    [IsNonVipShipTo] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_SalesCenters_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_SalesCenters_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_SalesCenters] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[SamplesPurposes] (
    [Id] INT NOT NULL,
    [Name] NVARCHAR(64) NOT NULL,
    [Description] NVARCHAR(128) NULL,
    [ClaimReason] NVARCHAR(3) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_SamplesPurposes_IsActive] DEFAULT (CONVERT([bit],(1))),
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_SamplesPurposes_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_SamplesPurposes_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_SamplesPurposes] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[Vendors] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Number] NVARCHAR(16) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Address] NVARCHAR(128) NOT NULL CONSTRAINT [DF_Vendors_Address] DEFAULT (N''),
    [City] NVARCHAR(128) NOT NULL CONSTRAINT [DF_Vendors_City] DEFAULT (N''),
    [State] NVARCHAR(5) NOT NULL CONSTRAINT [DF_Vendors_State] DEFAULT (N''),
    [PostalCode] NVARCHAR(16) NOT NULL CONSTRAINT [DF_Vendors_PostalCode] DEFAULT (N''),
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Vendors_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Vendors_UpdatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Vendors] PRIMARY KEY CLUSTERED ([Id])
);

GO

-- ============================================================ indexes

CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName]) WHERE ([NormalizedUserName] IS NOT NULL);
CREATE INDEX [IX_AspNetUsers_UpdatedAt] ON [dbo].[AspNetUsers] ([UpdatedAt]);
CREATE INDEX [IX_AspNetUsers_CreatedAt] ON [dbo].[AspNetUsers] ([CreatedAt]);
CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail]);
CREATE INDEX [IX_Bottlers_CreatedAt] ON [dbo].[Bottlers] ([CreatedAt]);
CREATE INDEX [IX_Bottlers_SalesOrganization] ON [dbo].[Bottlers] ([SalesOrganization]);
CREATE INDEX [IX_Bottlers_CommercialManagerId] ON [dbo].[Bottlers] ([CommercialManagerId]);
CREATE INDEX [IX_Bottlers_CountryId] ON [dbo].[Bottlers] ([CountryId]);
CREATE INDEX [IX_Bottlers_CurrencyId] ON [dbo].[Bottlers] ([CurrencyId]);
CREATE INDEX [IX_Bottlers_Name] ON [dbo].[Bottlers] ([Name]);
CREATE INDEX [IX_Bottlers_BrandSegmentCategoryId] ON [dbo].[Bottlers] ([BrandSegmentCategoryId]);
CREATE INDEX [IX_Bottlers_UpdatedAt] ON [dbo].[Bottlers] ([UpdatedAt]);
CREATE UNIQUE INDEX [IX_BrandSegmentCategories_Name] ON [dbo].[BrandSegmentCategories] ([Name]);
CREATE INDEX [IX_BrandSegmentCategories_UpdatedAt] ON [dbo].[BrandSegmentCategories] ([UpdatedAt]);
CREATE INDEX [IX_BrandSegmentCategories_CreatedAt] ON [dbo].[BrandSegmentCategories] ([CreatedAt]);
CREATE INDEX [IX_BrandSegmentCategories_CreatedBy] ON [dbo].[BrandSegmentCategories] ([CreatedBy]);
CREATE INDEX [IX_BrandSegmentCategories_CompanyId] ON [dbo].[BrandSegmentCategories] ([CompanyId]);
CREATE INDEX [IX_BrandSegmentCategories_UpdatedBy] ON [dbo].[BrandSegmentCategories] ([UpdatedBy]);
CREATE INDEX [IX_CancellationCategories_Deleted] ON [dbo].[CancellationCategories] ([Deleted]);
CREATE INDEX [IX_CancellationCategories_CreatedBy] ON [dbo].[CancellationCategories] ([CreatedBy]);
CREATE INDEX [IX_CancellationCategories_Name] ON [dbo].[CancellationCategories] ([Name]);
CREATE INDEX [IX_CancellationCategories_UpdatedAt] ON [dbo].[CancellationCategories] ([UpdatedAt]);
CREATE INDEX [IX_CancellationCategories_CancellationCategoryStatus] ON [dbo].[CancellationCategories] ([CancellationCategoryStatus]);
CREATE INDEX [IX_CancellationCategories_CreatedAt] ON [dbo].[CancellationCategories] ([CreatedAt]);
CREATE INDEX [IX_CancellationCategories_UpdatedBy] ON [dbo].[CancellationCategories] ([UpdatedBy]);
CREATE INDEX [IX_CommercialManagers_CreatedAt] ON [dbo].[CommercialManagers] ([CreatedAt]);
CREATE INDEX [IX_CommercialManagers_UpdatedAt] ON [dbo].[CommercialManagers] ([UpdatedAt]);
CREATE INDEX [IX_Companies_Name] ON [dbo].[Companies] ([Name]);
CREATE INDEX [IX_Companies_UpdatedAt] ON [dbo].[Companies] ([UpdatedAt]);
CREATE INDEX [IX_Companies_CreatedAt] ON [dbo].[Companies] ([CreatedAt]);
CREATE UNIQUE INDEX [IX_Countries_Name] ON [dbo].[Countries] ([Name]);
CREATE INDEX [IX_Countries_CreatedAt] ON [dbo].[Countries] ([CreatedAt]);
CREATE UNIQUE INDEX [IX_Countries_Code] ON [dbo].[Countries] ([Code]);
CREATE INDEX [IX_Countries_UpdatedAt] ON [dbo].[Countries] ([UpdatedAt]);
CREATE INDEX [IX_Currencies_CreatedAt] ON [dbo].[Currencies] ([CreatedAt]);
CREATE INDEX [IX_Currencies_UpdatedAt] ON [dbo].[Currencies] ([UpdatedAt]);
CREATE UNIQUE INDEX [IX_Currencies_Code] ON [dbo].[Currencies] ([Code]);
CREATE INDEX [IX_FundingElements_CreatedAt] ON [dbo].[FundingElements] ([CreatedAt]);
CREATE INDEX [IX_FundingElements_UpdatedAt] ON [dbo].[FundingElements] ([UpdatedAt]);
CREATE INDEX [IX_FundingElements_ProgramTypeId] ON [dbo].[FundingElements] ([ProgramTypeId]);
CREATE INDEX [IX_Invoices_UserId] ON [dbo].[Invoices] ([UserId]);
CREATE INDEX [IX_Invoices_InvoiceFinalizedDetailsId] ON [dbo].[Invoices] ([InvoiceFinalizedDetailsId]);
CREATE INDEX [IX_Invoices_SamplesPurposeId] ON [dbo].[Invoices] ([SamplesPurposeId]);
CREATE INDEX [IX_Invoices_Number] ON [dbo].[Invoices] ([Number]);
CREATE INDEX [IX_Invoices_ExportedToSap] ON [dbo].[Invoices] ([ExportedToSap]);
CREATE INDEX [IX_Invoices_SalesCenterId] ON [dbo].[Invoices] ([SalesCenterId]);
CREATE INDEX [IX_Invoices_CurrencyId] ON [dbo].[Invoices] ([CurrencyId]);
CREATE INDEX [IX_Invoices_UnlistedProgramTypeId] ON [dbo].[Invoices] ([UnlistedProgramTypeId]);
CREATE INDEX [IX_Invoices_PoNumber] ON [dbo].[Invoices] ([PoNumber]) WHERE ([PoNumber] IS NOT NULL);
CREATE INDEX [IX_Invoices_UserEmail] ON [dbo].[Invoices] ([UserEmail]);
CREATE INDEX [IX_Invoices_IsDeleted] ON [dbo].[Invoices] ([IsDeleted]);
CREATE INDEX [IX_Invoices_CreatedAt] ON [dbo].[Invoices] ([CreatedAt]);
CREATE INDEX [IX_Invoices_IsDeleted_InvoiceStatus] ON [dbo].[Invoices] ([IsDeleted], [InvoiceStatus]);
CREATE INDEX [IX_Invoices_CountryId] ON [dbo].[Invoices] ([CountryId]);
CREATE INDEX [IX_Invoices_CancellationCategoryId] ON [dbo].[Invoices] ([CancellationCategoryId]);
CREATE INDEX [IX_Invoices_InvoiceType] ON [dbo].[Invoices] ([InvoiceType]);
CREATE INDEX [IX_Invoices_RevisedInvoiceId] ON [dbo].[Invoices] ([RevisedInvoiceId]);
CREATE INDEX [IX_Invoices_SapInvoiceNumber] ON [dbo].[Invoices] ([SapInvoiceNumber]);
CREATE INDEX [IX_Invoices_CompanyId] ON [dbo].[Invoices] ([CompanyId]);
CREATE INDEX [IX_Invoices_InvoiceStatus] ON [dbo].[Invoices] ([InvoiceStatus]);
CREATE INDEX [IX_Invoices_Amount] ON [dbo].[Invoices] ([Amount]);
CREATE INDEX [IX_Invoices_BottlerId] ON [dbo].[Invoices] ([BottlerId]);
CREATE INDEX [IX_Invoices_BrandSegmentCategoryId] ON [dbo].[Invoices] ([BrandSegmentCategoryId]);
CREATE INDEX [IX_Invoices_CalculatedAdminInvoiceStatus] ON [dbo].[Invoices] ([CalculatedAdminInvoiceStatus]);
CREATE INDEX [IX_Invoices_CalculatedBottlerInvoiceStatus] ON [dbo].[Invoices] ([CalculatedBottlerInvoiceStatus]);
CREATE INDEX [IX_Invoices_PayerId] ON [dbo].[Invoices] ([PayerId]);
CREATE INDEX [IX_Invoices_ProgramId] ON [dbo].[Invoices] ([ProgramId]);
CREATE INDEX [IX_Invoices_UpdatedAt] ON [dbo].[Invoices] ([UpdatedAt]);
CREATE INDEX [IX_Payers_UpdatedAt] ON [dbo].[Payers] ([UpdatedAt]);
CREATE UNIQUE INDEX [IX_Payers_VendorId] ON [dbo].[Payers] ([VendorId]) WHERE ([VendorId] IS NOT NULL);
CREATE INDEX [IX_Payers_BottlerId] ON [dbo].[Payers] ([BottlerId]);
CREATE INDEX [IX_Payers_CreatedAt] ON [dbo].[Payers] ([CreatedAt]);
CREATE INDEX [IX_Payers_Name] ON [dbo].[Payers] ([Name]);
CREATE INDEX [IX_Programs_UpdatedAt] ON [dbo].[Programs] ([UpdatedAt]);
CREATE INDEX [IX_Programs_FundingElementId] ON [dbo].[Programs] ([FundingElementId]);
CREATE INDEX [IX_Programs_CreatedAt] ON [dbo].[Programs] ([CreatedAt]);
CREATE INDEX [IX_Programs_BottlerId] ON [dbo].[Programs] ([BottlerId]);
CREATE INDEX [IX_ProgramTypes_UpdatedBy] ON [dbo].[ProgramTypes] ([UpdatedBy]);
CREATE INDEX [IX_ProgramTypes_UpdatedAt] ON [dbo].[ProgramTypes] ([UpdatedAt]);
CREATE INDEX [IX_ProgramTypes_ShownOnSetup] ON [dbo].[ProgramTypes] ([ShownOnSetup]);
CREATE INDEX [IX_ProgramTypes_Name] ON [dbo].[ProgramTypes] ([Name]);
CREATE INDEX [IX_ProgramTypes_CreatedBy] ON [dbo].[ProgramTypes] ([CreatedBy]);
CREATE INDEX [IX_ProgramTypes_CreatedAt] ON [dbo].[ProgramTypes] ([CreatedAt]);
CREATE INDEX [IX_SalesCenters_Name] ON [dbo].[SalesCenters] ([Name]);
CREATE INDEX [IX_SalesCenters_PayerId] ON [dbo].[SalesCenters] ([PayerId]);
CREATE INDEX [IX_SalesCenters_UpdatedAt] ON [dbo].[SalesCenters] ([UpdatedAt]);
CREATE INDEX [IX_SalesCenters_CreatedAt] ON [dbo].[SalesCenters] ([CreatedAt]);
CREATE INDEX [IX_SamplesPurposes_UpdatedAt] ON [dbo].[SamplesPurposes] ([UpdatedAt]);
CREATE INDEX [IX_SamplesPurposes_CreatedAt] ON [dbo].[SamplesPurposes] ([CreatedAt]);
CREATE INDEX [IX_SamplesPurposes_Name] ON [dbo].[SamplesPurposes] ([Name]);
CREATE INDEX [IX_SamplesPurposes_IsActive] ON [dbo].[SamplesPurposes] ([IsActive]);
CREATE INDEX [IX_Vendors_UpdatedAt] ON [dbo].[Vendors] ([UpdatedAt]);
CREATE INDEX [IX_Vendors_Number] ON [dbo].[Vendors] ([Number]);
CREATE INDEX [IX_Vendors_Name] ON [dbo].[Vendors] ([Name]);
CREATE INDEX [IX_Vendors_CreatedAt] ON [dbo].[Vendors] ([CreatedAt]);
GO

-- ============================================================ foreign keys

ALTER TABLE [dbo].[Bottlers] ADD CONSTRAINT [FK_Bottlers_CommercialManagers_CommercialManagerId] FOREIGN KEY ([CommercialManagerId]) REFERENCES [dbo].[CommercialManagers] ([Id]);
ALTER TABLE [dbo].[Bottlers] ADD CONSTRAINT [FK_Bottlers_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Countries] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Bottlers] ADD CONSTRAINT [FK_Bottlers_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[Currencies] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Bottlers] ADD CONSTRAINT [FK_Bottlers_BrandSegmentCategories_BrandSegmentCategoryId] FOREIGN KEY ([BrandSegmentCategoryId]) REFERENCES [dbo].[BrandSegmentCategories] ([Id]);
ALTER TABLE [dbo].[BrandSegmentCategories] ADD CONSTRAINT [FK_BrandSegmentCategories_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[FundingElements] ADD CONSTRAINT [FK_FundingElements_ProgramTypes_ProgramTypeId] FOREIGN KEY ([ProgramTypeId]) REFERENCES [dbo].[ProgramTypes] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [dbo].[Currencies] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_CancellationCategories_CancellationCategoryId] FOREIGN KEY ([CancellationCategoryId]) REFERENCES [dbo].[CancellationCategories] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Programs_ProgramId] FOREIGN KEY ([ProgramId]) REFERENCES [dbo].[Programs] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_SalesCenters_SalesCenterId] FOREIGN KEY ([SalesCenterId]) REFERENCES [dbo].[SalesCenters] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_ProgramTypes_UnlistedProgramTypeId] FOREIGN KEY ([UnlistedProgramTypeId]) REFERENCES [dbo].[ProgramTypes] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Bottlers_BottlerId] FOREIGN KEY ([BottlerId]) REFERENCES [dbo].[Bottlers] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_BrandSegmentCategories_BrandSegmentCategoryId] FOREIGN KEY ([BrandSegmentCategoryId]) REFERENCES [dbo].[BrandSegmentCategories] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_SamplesPurposes_SamplesPurposeId] FOREIGN KEY ([SamplesPurposeId]) REFERENCES [dbo].[SamplesPurposes] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Invoices_RevisedInvoiceId] FOREIGN KEY ([RevisedInvoiceId]) REFERENCES [dbo].[Invoices] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Countries] ([Id]);
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [FK_Invoices_Payers_PayerId] FOREIGN KEY ([PayerId]) REFERENCES [dbo].[Payers] ([Id]);
ALTER TABLE [dbo].[Payers] ADD CONSTRAINT [FK_Payers_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendors] ([Id]);
ALTER TABLE [dbo].[Payers] ADD CONSTRAINT [FK_Payers_Bottlers_BottlerId] FOREIGN KEY ([BottlerId]) REFERENCES [dbo].[Bottlers] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Programs] ADD CONSTRAINT [FK_Programs_FundingElements_FundingElementId] FOREIGN KEY ([FundingElementId]) REFERENCES [dbo].[FundingElements] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Programs] ADD CONSTRAINT [FK_Programs_Bottlers_BottlerId] FOREIGN KEY ([BottlerId]) REFERENCES [dbo].[Bottlers] ([Id]) ON DELETE CASCADE;
ALTER TABLE [dbo].[SalesCenters] ADD CONSTRAINT [FK_SalesCenters_Payers_PayerId] FOREIGN KEY ([PayerId]) REFERENCES [dbo].[Payers] ([Id]) ON DELETE CASCADE;
GO
