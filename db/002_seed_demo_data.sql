/*
    Invoice Portal Admin - synthetic demo data (proof of concept)

    Fictional data, invented for this repo. No row here is derived from, sampled from, or
    anonymised out of any production system. Every company, distributor, person, email address
    and invoice below is made up. Volumes are chosen to exercise the UI: server-side paging,
    sorting and filtering on the invoice grid, the lookup screens, and the AI query pages.

      Companies                2      Bottlers                 40
      Countries                2      Payers                   90
      Currencies               2      Sales centers           200
      Brand segment cats       3      Programs                400
      Users                   24      Invoices              4,000

    Dates are relative to the moment the script runs, so a freshly built demo database always
    looks current. Everything else is deterministic: the generated ids, names and amounts come
    from an arithmetic hash of the row number, so two runs produce the same database.

    Apply after 001_schema.sql. Safe to re-run: it exits if Bottlers already has rows.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM [dbo].[Bottlers])
BEGIN
    PRINT 'Demo data already present - nothing to do.';
    SET NOEXEC ON;
END
GO

DECLARE @today date = CAST(SYSUTCDATETIME() AS date);

BEGIN TRANSACTION;

-- ============================================================ reference data

SET IDENTITY_INSERT [dbo].[Companies] ON;
INSERT INTO [dbo].[Companies] ([Id], [Code], [Name]) VALUES
    (1, N'NBC', N'Northwind Beverage Co'),
    (2, N'SPB', N'Southpoint Bottling');
SET IDENTITY_INSERT [dbo].[Companies] OFF;

SET IDENTITY_INSERT [dbo].[Countries] ON;
INSERT INTO [dbo].[Countries] ([Id], [Code], [Name]) VALUES
    (1, N'US', N'United States'),
    (2, N'CA', N'Canada');
SET IDENTITY_INSERT [dbo].[Countries] OFF;

SET IDENTITY_INSERT [dbo].[Currencies] ON;
INSERT INTO [dbo].[Currencies] ([Id], [Code]) VALUES
    (1, N'USD'),
    (2, N'CAD');
SET IDENTITY_INSERT [dbo].[Currencies] OFF;

SET IDENTITY_INSERT [dbo].[BrandSegmentCategories] ON;
INSERT INTO [dbo].[BrandSegmentCategories] ([Id], [Name], [CompanyId], [CreatedBy], [UpdatedBy]) VALUES
    (1, N'Energy',    1, N'seed@example.com', N'seed@example.com'),
    (2, N'Sparkling', 1, N'seed@example.com', N'seed@example.com'),
    (3, N'Hydration', 2, N'seed@example.com', N'seed@example.com');
SET IDENTITY_INSERT [dbo].[BrandSegmentCategories] OFF;

SET IDENTITY_INSERT [dbo].[CancellationCategories] ON;
INSERT INTO [dbo].[CancellationCategories] ([Id], [Name], [CreatedBy], [UpdatedBy]) VALUES
    (1, N'Duplicate submission',      N'seed@example.com', N'seed@example.com'),
    (2, N'Incorrect amount',          N'seed@example.com', N'seed@example.com'),
    (3, N'Incorrect distributor',     N'seed@example.com', N'seed@example.com'),
    (4, N'Program ended',             N'seed@example.com', N'seed@example.com'),
    (5, N'Submitted in error',        N'seed@example.com', N'seed@example.com'),
    (6, N'Superseded by revision',    N'seed@example.com', N'seed@example.com');
SET IDENTITY_INSERT [dbo].[CancellationCategories] OFF;

INSERT INTO [dbo].[SamplesPurposes] ([Id], [Name], [Description], [ClaimReason], [IsActive]) VALUES
    (1, N'Retail sampling',      N'In-store product sampling events',        N'S01', 1),
    (2, N'Event activation',     N'Festivals, concerts and sports events',   N'S02', 1),
    (3, N'New account trial',    N'First-order samples for new accounts',    N'S03', 1),
    (4, N'Trade show',           N'Industry trade show giveaways',           N'S04', 1),
    (5, N'Employee program',     N'Internal employee sampling',              N'S05', 1),
    (6, N'Charitable donation',  N'Donations to registered charities',       N'S06', 1),
    (7, N'Quality assurance',    N'Lab and QA retention samples',            N'S07', 0),
    (8, N'Media and influencer', N'Press kits and influencer seeding',       N'S08', 1);

INSERT INTO [dbo].[ProgramTypes] ([Id], [ProgramTypeKind], [Name], [SapCategoryCode], [SapClaimReason], [CreatedBy], [UpdatedBy], [ShownOnSetup]) VALUES
    (101, N'Listed',   N'Display allowance',      210, N'P01', N'seed@example.com', N'seed@example.com', 1),
    (102, N'Listed',   N'Feature advertising',    211, N'P02', N'seed@example.com', N'seed@example.com', 1),
    (103, N'Listed',   N'Price promotion',        212, N'P03', N'seed@example.com', N'seed@example.com', 1),
    (104, N'Listed',   N'Cold vault placement',   213, N'P04', N'seed@example.com', N'seed@example.com', 1),
    (105, N'Listed',   N'End cap',                214, N'P05', N'seed@example.com', N'seed@example.com', 1),
    (106, N'Listed',   N'Local sponsorship',      215, N'P06', N'seed@example.com', N'seed@example.com', 1),
    (107, N'Listed',   N'Equipment placement',    216, N'P07', N'seed@example.com', N'seed@example.com', 1),
    (108, N'Listed',   N'Route incentive',        217, N'P08', N'seed@example.com', N'seed@example.com', 0),
    (109, N'Unlisted', N'Other promotional',      218, N'P09', N'seed@example.com', N'seed@example.com', 1),
    (110, N'Unlisted', N'Other samples',          219, N'P10', N'seed@example.com', N'seed@example.com', 1),
    (99999, N'Listed', N'Unmapped',               NULL, NULL,  N'seed@example.com', N'seed@example.com', 0);

INSERT INTO [dbo].[CommercialManagers] ([Id], [Name], [Email]) VALUES
    (1, N'Avery Lindqvist',  N'avery.lindqvist@example.com'),
    (2, N'Bo Nakamura',      N'bo.nakamura@example.com'),
    (3, N'Casey Ferreira',   N'casey.ferreira@example.com'),
    (4, N'Devin Okonkwo',    N'devin.okonkwo@example.com'),
    (5, N'Elin Castellanos', N'elin.castellanos@example.com'),
    (6, N'Frankie Duong',    N'frankie.duong@example.com'),
    (7, N'Gale Petrosyan',   N'gale.petrosyan@example.com'),
    (8, N'Harper Alvarado',  N'harper.alvarado@example.com'),
    (9, N'Indra Mbeki',      N'indra.mbeki@example.com'),
    (10, N'Jules Farkas',    N'jules.farkas@example.com'),
    (11, N'Kai Thorsen',     N'kai.thorsen@example.com'),
    (12, N'Lior Ben-Ari',    N'lior.ben-ari@example.com');

-- ============================================================ users

-- Deterministic, obviously-fake GUIDs: 00000000-0000-4000-8000-0000000000NN.
INSERT INTO [dbo].[AspNetUsers]
    ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
     [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount])
SELECT
    CAST('00000000-0000-4000-8000-' + RIGHT('000000000000' + CAST(v.i AS varchar(12)), 12) AS uniqueidentifier),
    v.email, UPPER(v.email), v.email, UPPER(v.email), 1, 0, 0, 1, 0
FROM (VALUES
    (1,  N'ada.whitfield@example.com'),   (2,  N'ben.calloway@example.com'),
    (3,  N'cora.delgado@example.com'),    (4,  N'dmitri.volkov@example.com'),
    (5,  N'eve.harrington@example.com'),  (6,  N'finn.oleary@example.com'),
    (7,  N'gita.raman@example.com'),      (8,  N'hugo.bernard@example.com'),
    (9,  N'iris.kowalski@example.com'),   (10, N'jonah.mbaye@example.com'),
    (11, N'kira.lindgren@example.com'),   (12, N'luca.moretti@example.com'),
    (13, N'mira.hassan@example.com'),     (14, N'nils.andersen@example.com'),
    (15, N'omar.sadiq@example.com'),      (16, N'pia.novak@example.com'),
    (17, N'quinn.abbott@example.com'),    (18, N'rosa.iglesias@example.com'),
    (19, N'sven.holmberg@example.com'),   (20, N'tara.venkatesh@example.com'),
    (21, N'uma.chatterjee@example.com'),  (22, N'viktor.havel@example.com'),
    (23, N'wren.mcallister@example.com'), (24, N'yusuf.demir@example.com')
) AS v(i, email);

-- ============================================================ vendors

INSERT INTO [dbo].[Vendors] ([Number], [Name], [Address], [City], [State], [PostalCode])
SELECT
    N'V' + RIGHT('000000' + CAST(n.i AS varchar(6)), 6),
    c.city + N' Beverage Holdings',
    CAST(100 + (n.i * 37) % 8900 AS nvarchar(8)) + N' ' + s.street,
    c.city, c.state,
    RIGHT('00000' + CAST(10000 + (n.i * 613) % 79999 AS varchar(5)), 5)
FROM (SELECT TOP (40) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects) AS n
CROSS APPLY (SELECT city, state FROM (VALUES
    (1,N'Akron',N'OH'),(2,N'Boise',N'ID'),(3,N'Charlotte',N'NC'),(4,N'Durham',N'NC'),
    (5,N'Eugene',N'OR'),(6,N'Fresno',N'CA'),(7,N'Gary',N'IN'),(8,N'Helena',N'MT'),
    (9,N'Irving',N'TX'),(10,N'Joliet',N'IL'),(11,N'Kenosha',N'WI'),(12,N'Laredo',N'TX'),
    (13,N'Mobile',N'AL'),(14,N'Naperville',N'IL'),(15,N'Ogden',N'UT'),(16,N'Peoria',N'AZ'),
    (17,N'Quincy',N'MA'),(18,N'Reno',N'NV'),(19,N'Salem',N'OR'),(20,N'Tacoma',N'WA')
) AS t(k, city, state) WHERE t.k = ((n.i - 1) % 20) + 1) AS c
CROSS APPLY (SELECT street FROM (VALUES
    (1,N'Industrial Pkwy'),(2,N'Commerce Dr'),(3,N'Warehouse Rd'),(4,N'Distribution Way'),(5,N'Depot St')
) AS t(k, street) WHERE t.k = ((n.i - 1) % 5) + 1) AS s;

-- ============================================================ funding elements

INSERT INTO [dbo].[FundingElements] ([Id], [Name], [AccountingYear], [IsActive], [ProgramTypeId], [AppliesToDfp])
SELECT
    500 + n.i,
    N'FE-' + RIGHT('000' + CAST(n.i AS varchar(3)), 3) + N' ' + p.Name,
    YEAR(DATEADD(year, -((n.i - 1) % 3), @today)),
    CASE WHEN (n.i * 7) % 10 = 0 THEN 0 ELSE 1 END,
    p.Id,
    CASE WHEN (n.i * 3) % 4 = 0 THEN 1 ELSE 0 END
FROM (SELECT TOP (30) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects) AS n
CROSS APPLY (
    SELECT TOP (1) pt.Id, pt.Name
    FROM [dbo].[ProgramTypes] pt
    WHERE pt.Id BETWEEN 101 AND 110
    ORDER BY (pt.Id + n.i) % 10, pt.Id
) AS p;

-- ============================================================ bottlers

INSERT INTO [dbo].[Bottlers]
    ([Id], [Name], [SalesOrganization], [CurrencyId], [CountryId], [CommercialManagerId], [BrandSegmentCategoryId])
SELECT
    10000 + b.i,
    b.name,
    CASE WHEN b.i % 4 = 0 THEN N'4000' WHEN b.i % 3 = 0 THEN N'3000' WHEN b.i % 2 = 0 THEN N'2000' ELSE N'1000' END,
    CASE WHEN b.i % 7 = 0 THEN 2 ELSE 1 END,               -- CAD for roughly one in seven
    CASE WHEN b.i % 7 = 0 THEN 2 ELSE 1 END,               -- ...and the matching country
    ((b.i - 1) % 12) + 1,
    ((b.i - 1) % 3) + 1
FROM (VALUES
    (1,N'Blue Ridge Distributing'),      (2,N'Cascade Beverage Partners'),
    (3,N'Granite State Wholesale'),      (4,N'Harborline Distributors'),
    (5,N'Iron Creek Beverage'),          (6,N'Lakeshore Supply Group'),
    (7,N'Mesa Verde Distributing'),      (8,N'Northgate Beverage'),
    (9,N'Overland Trail Wholesale'),     (10,N'Pinehurst Distributors'),
    (11,N'Quarry Road Beverage'),        (12,N'Redwood Valley Supply'),
    (13,N'Silver Fork Distributing'),    (14,N'Tidewater Beverage Co'),
    (15,N'Union Mills Wholesale'),       (16,N'Vista Point Distributors'),
    (17,N'Westbrook Beverage'),          (18,N'Yellow Birch Supply'),
    (19,N'Amberfield Distributing'),     (20,N'Bayou Bend Beverage'),
    (21,N'Copper Hollow Wholesale'),     (22,N'Dunmore Distributors'),
    (23,N'Eastwind Beverage Group'),     (24,N'Fairhaven Supply'),
    (25,N'Glenbrook Distributing'),      (26,N'Hollowell Beverage'),
    (27,N'Ivy Lane Wholesale'),          (28,N'Juniper Flats Distributors'),
    (29,N'Kettle River Beverage'),       (30,N'Longmeadow Supply Co'),
    (31,N'Marbleton Distributing'),      (32,N'Nightingale Beverage'),
    (33,N'Oakcrest Wholesale'),          (34,N'Preston Gap Distributors'),
    (35,N'Ravenswood Beverage'),         (36,N'Stonebridge Supply'),
    (37,N'Thistlewood Distributing'),    (38,N'Umber Hills Beverage'),
    (39,N'Valewood Wholesale'),          (40,N'Wickersham Distributors')
) AS b(i, name);

-- ============================================================ payers

-- Between two and three payers per bottler, each in its own city.
INSERT INTO [dbo].[Payers] ([Id], [Name], [BottlerId], [Address], [ZipCode], [City], [State], [VendorId], [DoNotUse])
SELECT
    20000 + n.i,
    b.Name + N' - ' + c.city,
    b.Id,
    CAST(200 + (n.i * 53) % 8700 AS nvarchar(8)) + N' ' + c.street,
    RIGHT('00000' + CAST(10000 + (n.i * 811) % 79999 AS varchar(5)), 5),
    c.city, c.state,
    CASE WHEN n.i <= 40 THEN (SELECT Id FROM [dbo].[Vendors] WHERE Number = N'V' + RIGHT('000000' + CAST(n.i AS varchar(6)), 6)) END,
    CASE WHEN n.i % 30 = 0 THEN 1 ELSE 0 END
FROM (SELECT TOP (90) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects) AS n
CROSS APPLY (SELECT Id, Name FROM [dbo].[Bottlers] ORDER BY Id OFFSET ((n.i - 1) % 40) ROWS FETCH NEXT 1 ROWS ONLY) AS b
CROSS APPLY (SELECT city, state, street FROM (VALUES
    (1,N'Altoona',N'PA',N'Market St'),      (2,N'Bellingham',N'WA',N'Harbor Ave'),
    (3,N'Cedar Falls',N'IA',N'Main St'),    (4,N'Dover',N'DE',N'State St'),
    (5,N'Elkhart',N'IN',N'Jackson Blvd'),   (6,N'Flagstaff',N'AZ',N'Milton Rd'),
    (7,N'Galesburg',N'IL',N'Seminary St'),  (8,N'Hattiesburg',N'MS',N'Hardy St'),
    (9,N'Ithaca',N'NY',N'Cayuga St'),       (10,N'Jonesboro',N'AR',N'Caraway Rd'),
    (11,N'Kalispell',N'MT',N'Idaho St'),    (12,N'Lafayette',N'LA',N'Johnston St'),
    (13,N'Marquette',N'MI',N'Washington St'),(14,N'Nampa',N'ID',N'Garrity Blvd'),
    (15,N'Owensboro',N'KY',N'Frederica St'),(16,N'Pocatello',N'ID',N'Yellowstone Ave'),
    (17,N'Roanoke',N'VA',N'Campbell Ave'),  (18,N'Sheboygan',N'WI',N'Erie Ave')
) AS t(k, city, state, street) WHERE t.k = ((n.i - 1) % 18) + 1) AS c;

-- ============================================================ sales centers

INSERT INTO [dbo].[SalesCenters] ([Id], [Name], [Address], [PayerId], [IsNonVipShipTo])
SELECT
    30000 + n.i,
    p.City + N' ' + z.zone + N' Sales Center',
    CAST(1000 + (n.i * 71) % 8000 AS nvarchar(8)) + N' Distribution Way',
    p.Id,
    CASE WHEN n.i % 12 = 0 THEN 1 ELSE 0 END
FROM (SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects) AS n
CROSS APPLY (SELECT Id, City FROM [dbo].[Payers] ORDER BY Id OFFSET ((n.i - 1) % 90) ROWS FETCH NEXT 1 ROWS ONLY) AS p
CROSS APPLY (SELECT zone FROM (VALUES
    (1,N'North'),(2,N'South'),(3,N'East'),(4,N'West'),(5,N'Central')
) AS t(k, zone) WHERE t.k = ((n.i - 1) % 5) + 1) AS z;

-- ============================================================ programs

-- Ten programs per bottler, ids PRG-00001..PRG-00400.
INSERT INTO [dbo].[Programs]
    ([Id], [Name], [From], [To], [BottlerId], [CoOpPercentage], [CoOpAmount], [AmountSettled], [FundingElementId], [IsDeleted])
SELECT
    N'PRG-' + RIGHT('00000' + CAST(n.i AS varchar(5)), 5),
    q.quarter + N' ' + pt.Name + N' - ' + b.Name,
    DATEADD(day, -(((n.i * 17) % 24) * 30) - 30, @today),
    DATEADD(day, -(((n.i * 17) % 24) * 30) + 60, @today),
    b.Id,
    CAST(25 * (((n.i * 13) % 4) + 1) AS decimal(5,2)),
    CAST(5000 + (n.i * 977) % 95000 AS decimal(18,2)),
    CAST((5000 + (n.i * 977) % 95000) * (((n.i * 7) % 5) * 0.2) AS decimal(18,2)),
    fe.Id,
    CASE WHEN n.i % 50 = 0 THEN 1 ELSE 0 END
FROM (SELECT TOP (400) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects) AS n
CROSS APPLY (SELECT Id, Name FROM [dbo].[Bottlers] ORDER BY Id OFFSET ((n.i - 1) % 40) ROWS FETCH NEXT 1 ROWS ONLY) AS b
CROSS APPLY (SELECT Id, ProgramTypeId FROM [dbo].[FundingElements] ORDER BY Id OFFSET ((n.i - 1) % 30) ROWS FETCH NEXT 1 ROWS ONLY) AS fe
CROSS APPLY (SELECT TOP (1) Name FROM [dbo].[ProgramTypes] WHERE Id = fe.ProgramTypeId) AS pt
CROSS APPLY (SELECT quarter FROM (VALUES
    (1,N'Q1'),(2,N'Q2'),(3,N'Q3'),(4,N'Q4')
) AS t(k, quarter) WHERE t.k = ((n.i - 1) % 4) + 1) AS q;

-- ============================================================ invoices

/*
    4,000 invoices. Each one is internally consistent: the sales center belongs to the payer,
    the payer to the bottler, and the program to that same bottler. Statuses are spread across
    the InvoiceStatus enum so the grid's status filter and the calculated-status columns have
    something to show; roughly one in forty is soft-deleted, and one in twenty is a Samples
    invoice rather than Promotional.
*/
INSERT INTO [dbo].[Invoices]
    ([Amount], [UserId], [Number], [BottlerId], [ProgramId], [CoOpPercentage], [AccountType],
     [PayerId], [SalesCenterId], [BrandSegmentCategoryId], [InvoiceDate], [InvoiceStatus], [CurrencyId],
     [AutoGenerateInvoiceAttachment], [RevisionNumber], [ExportedToSap], [ClaimId],
     [PromoPeriodFrom], [PromoPeriodTo], [CompanyId], [UserName], [UserEmail], [CountryId],
     [InvoiceType], [UnlistedProgramName], [UnlistedProgramTypeId], [CreatedAt], [UpdatedAt],
     [IsDeleted], [HasPayment], [CancellationCategoryId], [PoNumber], [SamplesPurposeId],
     [DoNotHavePoNumber], [RefDocNumber], [PoGrandTotal], [PoInvoiceNumber])
SELECT
    CAST(250 + (h.v % 84750) AS decimal(19,4)),
    u.Id,
    N'INV-' + RIGHT('000000' + CAST(n.i AS varchar(6)), 6),
    b.Id,
    CASE WHEN h.v % 20 = 0 THEN NULL ELSE pg.Id END,       -- a few unlisted invoices carry no program
    CAST(25 * ((h.v % 4) + 1) AS decimal(5,2)),
    h.v % 2,                                                -- AccountType: 0 On Premise, 1 Off Premise
    p.Id,
    sc.Id,
    b.BrandSegmentCategoryId,
    DATEADD(day, -(h.v % 730), @today),
    st.status,
    b.CurrencyId,
    CASE WHEN h.v % 6 = 0 THEN 1 ELSE 0 END,
    CASE WHEN h.v % 25 = 0 THEN 1 ELSE 0 END,
    CASE WHEN st.status IN (2, 17) THEN DATEADD(day, -(h.v % 700), @today) END,
    CASE WHEN st.status IN (0, 5, 18) THEN NULL ELSE 900000 + n.i END,
    DATEADD(day, -(h.v % 730) - 30, @today),
    DATEADD(day, -(h.v % 730) + 30, @today),
    CASE WHEN b.BrandSegmentCategoryId = 3 THEN 2 ELSE 1 END,  -- Hydration sits under Southpoint
    u.UserName,
    u.Email,
    b.CountryId,
    CASE WHEN h.v % 20 = 0 THEN N'Samples' ELSE N'Promotional' END,
    CASE WHEN h.v % 20 = 0 THEN N'Ad-hoc sampling activation' END,
    CASE WHEN h.v % 20 = 0 THEN 110 END,
    DATEADD(day, -(h.v % 730), CAST(@today AS datetime2(7))),
    DATEADD(day, -(h.v % 730) + (h.v % 5), CAST(@today AS datetime2(7))),
    CASE WHEN n.i % 40 = 0 THEN 1 ELSE 0 END,
    CASE WHEN st.status = 17 AND h.v % 3 = 0 THEN 1 ELSE 0 END,
    CASE WHEN st.status IN (3, 4) THEN (h.v % 6) + 1 END,
    CASE WHEN h.v % 4 = 0 THEN NULL ELSE N'PO' + RIGHT('000000000' + CAST(h.v % 999999999 AS varchar(9)), 9) END,
    CASE WHEN h.v % 20 = 0 THEN (h.v % 8) + 1 END,
    CASE WHEN h.v % 4 = 0 THEN 1 ELSE 0 END,
    N'RD' + RIGHT('00000000' + CAST(n.i AS varchar(8)), 8),
    CAST(250 + (h.v % 84750) AS decimal(19,4)),
    N'PINV-' + RIGHT('000000' + CAST(n.i AS varchar(6)), 6)
FROM (SELECT TOP (4000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
      FROM sys.all_objects o1 CROSS JOIN sys.all_objects o2) AS n
CROSS APPLY (SELECT CAST((CAST(n.i AS bigint) * 2654435761) % 1000003 AS int) AS v) AS h
CROSS APPLY (SELECT Id, City, BottlerId FROM [dbo].[Payers] ORDER BY Id OFFSET (h.v % 90) ROWS FETCH NEXT 1 ROWS ONLY) AS p
CROSS APPLY (SELECT TOP (1) Id, BrandSegmentCategoryId, CurrencyId, CountryId FROM [dbo].[Bottlers] WHERE Id = p.BottlerId) AS b
CROSS APPLY (SELECT Id FROM [dbo].[SalesCenters] WHERE PayerId = p.Id ORDER BY Id OFFSET (h.v % 2) ROWS FETCH NEXT 1 ROWS ONLY) AS sc
CROSS APPLY (SELECT Id FROM [dbo].[Programs] WHERE BottlerId = b.Id ORDER BY Id OFFSET (h.v % 10) ROWS FETCH NEXT 1 ROWS ONLY) AS pg
CROSS APPLY (SELECT Id, UserName, Email FROM [dbo].[AspNetUsers] ORDER BY Id OFFSET (h.v % 24) ROWS FETCH NEXT 1 ROWS ONLY) AS u
CROSS APPLY (SELECT status FROM (VALUES
    (0,0),(1,17),(2,17),(3,2),(4,17),(5,6),(6,17),(7,10),(8,2),(9,17),
    (10,5),(11,17),(12,7),(13,2),(14,11),(15,17),(16,1),(17,2),(18,17),(19,3),
    (20,17),(21,8),(22,2),(23,17),(24,4),(25,17),(26,12),(27,2),(28,17),(29,9),
    (30,2),(31,17),(32,13),(33,17),(34,2),(35,16),(36,17),(37,2),(38,19),(39,17)
) AS t(k, status) WHERE t.k = h.v % 40) AS st;

COMMIT TRANSACTION;
GO

SET NOEXEC OFF;
GO
