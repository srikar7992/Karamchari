-- =============================================================================
-- Karamchari — Local Dev Seed Script
-- =============================================================================
-- PREREQUISITES:
--
--  Provisioning is fully automated. Do NOT create schemas or run migrations by
--  hand. From a clean checkout, one command brings up infra, migrates every
--  DbContext, applies Row-Level Security, and provisions the dev/acme/contoso
--  tenants:
--
--       ./setup-local.sh        (Unix/macOS)
--       ./setup-local.ps1       (Windows)
--
--  Equivalently, the underlying steps are:
--       docker compose -f infrastructure/local/docker-compose.yml up -d
--       dotnet run --project src/Backend/Karamchari.Api -- --provision-dev-tenants
--
--  This seed script is OPTIONAL sample data. setup-local.sh applies it for you;
--  to run it manually against the Docker SQL Server:
--
--       docker exec -i <sqlserver-container> /opt/mssql-tools18/bin/sqlcmd \
--         -S localhost -U sa -P 'Karamchari@123' -d Karamchari -C \
--         -i docs/seed/local-dev-seed.sql
--
-- This script is IDEMPOTENT — it uses MERGE so re-running is safe.
-- All GUIDs are stable so foreign keys resolve correctly on re-runs.
--
-- Tenant for local dev: 'dev'   Schema: [tenant_dev]
-- Gateway fingerprint : 'local-dev-gateway'   (matches appsettings.Development.json)
-- Dev auth header     : X-Tenant-Id: dev  +  X-Karamchari-Gateway: local-dev-gateway
-- =============================================================================

USE [Karamchari];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;  -- roll back the whole script if any statement fails
GO

-- =============================================================================
-- 0. Set tenant session context so RLS predicates pass during seeding
-- =============================================================================
EXEC sys.sp_set_session_context @key = N'TenantId', @value = N'dev', @read_only = 0;
GO

-- =============================================================================
-- 1. Salary Components  (master definitions — canonical GUIDs)
-- =============================================================================
-- These must exist before SalaryTemplates reference them.

DECLARE @basic         UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @hra           UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @special       UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @lta           UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @medical       UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @pfEmployee    UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';
DECLARE @pfEmployer    UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000007';

-- ComponentType: 0=Earning, 1=Deduction, 2=EmployerContribution
-- RoundingStrategy: 0=NearestRupee, 1=Floor, 2=Ceil, 3=Exact

MERGE [tenant_dev].[SalaryComponents] AS target
USING (VALUES
    (@basic,       'Basic',                 0, 0, 'dev'),
    (@hra,         'HRA',                   0, 0, 'dev'),
    (@special,     'Special Allowance',     0, 0, 'dev'),
    (@lta,         'Leave Travel Allowance',0, 0, 'dev'),
    (@medical,     'Medical Allowance',     0, 0, 'dev'),
    (@pfEmployee,  'EPF Employee',          1, 1, 'dev'),
    (@pfEmployer,  'EPF Employer',          2, 1, 'dev')
) AS source ([Id], [Name], [Type], [Rounding], [TenantId])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED THEN
    INSERT ([Id], [Name], [Type], [Rounding], [TenantId])
    VALUES (source.[Id], source.[Name], source.[Type], source.[Rounding], source.[TenantId]);
GO

-- =============================================================================
-- 2. Salary Templates
-- =============================================================================
-- SalaryTemplate.Components is stored as JSON (EF Core OwnsMany ToJson).
-- Layout:  Basic 40% CTC  |  HRA 40% of Basic  |  LTA fixed ₹24000/yr
--          Medical fixed ₹15000/yr  |  Special = Remainder
--
-- ComponentCalculationType: 0=FixedAmount, 1=PercentageOfCTC, 2=PercentageOfComponents, 3=Remainder
-- DependencyAggregationType: 0=None, 1=Sum, 2=Max

DECLARE @stdTemplateId UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';

MERGE [tenant_dev].[SalaryTemplates] AS target
USING (VALUES
    (
        @stdTemplateId,
        'Software Engineer — Standard',
        'dev',
        -- JSON column for Components (EF OwnsMany ToJson)
        N'[
            {"ComponentId":"11111111-0000-0000-0000-000000000001","CalculationType":1,"Value":40.0,"ExecutionPriority":1,"DependsOnComponentIds":[],"AggregationType":0},
            {"ComponentId":"11111111-0000-0000-0000-000000000002","CalculationType":2,"Value":40.0,"ExecutionPriority":2,"DependsOnComponentIds":["11111111-0000-0000-0000-000000000001"],"AggregationType":1},
            {"ComponentId":"11111111-0000-0000-0000-000000000004","CalculationType":0,"Value":24000.0,"ExecutionPriority":3,"DependsOnComponentIds":[],"AggregationType":0},
            {"ComponentId":"11111111-0000-0000-0000-000000000005","CalculationType":0,"Value":15000.0,"ExecutionPriority":4,"DependsOnComponentIds":[],"AggregationType":0},
            {"ComponentId":"11111111-0000-0000-0000-000000000003","CalculationType":3,"Value":null,"ExecutionPriority":99,"DependsOnComponentIds":[],"AggregationType":0}
        ]'
    )
) AS source ([Id], [Name], [TenantId], [Components])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED THEN
    INSERT ([Id], [Name], [TenantId], [Components])
    VALUES (source.[Id], source.[Name], source.[TenantId], source.[Components])
WHEN MATCHED THEN
    UPDATE SET [Name] = source.[Name], [Components] = source.[Components];
GO

-- =============================================================================
-- 3. Payroll Profiles (3 sample employees)
-- =============================================================================
-- TaxRegime: 0=New, 1=Old
-- PayType:   0=Salaried, 1=Hourly

DECLARE @stdTemplateId2 UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';

-- Employee IDs (must match the HR domain's dev employees if you've seeded HR)
DECLARE @emp1 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
DECLARE @emp2 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000002';
DECLARE @emp3 UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';

MERGE [tenant_dev].[PayrollProfiles] AS target
USING (VALUES
    -- ( Id,  EmployeeId,  EmployeeName,  TenantId, PayType, BaseSalary, HourlyRate, Currency,
    --   IsActive, OptedForVoluntaryPF, IsEsicLocked, StateCode, TaxRegime, IsMetro, AnnualCTC, SalaryTemplateId )
    (NEWID(), @emp1, 'Priya Sharma',   'dev', 0, 0, 0, 'INR', 1, 0, 0, 'TS', 0, 0, 1200000, @stdTemplateId2),
    (NEWID(), @emp2, 'Arun Reddy',    'dev', 0, 0, 0, 'INR', 1, 0, 0, 'TS', 0, 1, 2400000, @stdTemplateId2),
    (NEWID(), @emp3, 'Kavya Nair',    'dev', 0, 0, 0, 'INR', 1, 1, 0, 'TS', 1, 0,  900000, @stdTemplateId2)
) AS source (
    [Id], [EmployeeId], [EmployeeName], [TenantId], [PayType], [BaseSalary], [HourlyRate], [Currency],
    [IsActive], [OptedForVoluntaryPF], [IsEsicLocked], [StateCode], [TaxRegime], [IsMetro], [AnnualCTC], [SalaryTemplateId]
)
ON target.[EmployeeId] = source.[EmployeeId]
WHEN NOT MATCHED THEN
    INSERT (
        [Id], [EmployeeId], [EmployeeName], [TenantId], [PayType], [BaseSalary], [HourlyRate], [Currency],
        [IsActive], [OptedForVoluntaryPF], [IsEsicLocked], [StateCode], [TaxRegime], [IsMetro], [AnnualCTC], [SalaryTemplateId]
    )
    VALUES (
        source.[Id], source.[EmployeeId], source.[EmployeeName], source.[TenantId],
        source.[PayType], source.[BaseSalary], source.[HourlyRate], source.[Currency],
        source.[IsActive], source.[OptedForVoluntaryPF], source.[IsEsicLocked],
        source.[StateCode], source.[TaxRegime], source.[IsMetro],
        source.[AnnualCTC], source.[SalaryTemplateId]
    )
WHEN MATCHED THEN
    UPDATE SET
        [EmployeeName]     = source.[EmployeeName],
        [TaxRegime]        = source.[TaxRegime],
        [AnnualCTC]        = source.[AnnualCTC],
        [SalaryTemplateId] = source.[SalaryTemplateId];
GO

-- =============================================================================
-- 4. Professional Tax Slabs — Telangana (StateCode = 'TS')
-- =============================================================================
-- PT in TS: ₹0–₹14,999 → ₹0 | ₹15,000–₹19,999 → ₹150 | ₹20,000+ → ₹200
-- MonthExclusions: PT is not applicable in February (month 2).

MERGE [tenant_dev].[ProfessionalTaxSlabs] AS target
USING (VALUES
    ('44444444-0000-0000-0000-000000000001', 'TS', 0,      14999.99, 0,   0, 2027, N'[2]',   'dev'),
    ('44444444-0000-0000-0000-000000000002', 'TS', 15000,  19999.99, 150, 0, 2027, N'[2]',   'dev'),
    ('44444444-0000-0000-0000-000000000003', 'TS', 20000,  NULL,     200, 0, 2027, N'[2]',   'dev')
) AS source ([Id], [StateCode], [MinGross], [MaxGross], [MonthlyTaxAmount], [FinancialYearStart], [FinancialYearEnd], [MonthExclusions], [TenantId])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED THEN
    INSERT ([Id], [StateCode], [MinGross], [MaxGross], [MonthlyTaxAmount], [FinancialYearStart], [FinancialYearEnd], [MonthExclusions], [TenantId])
    VALUES (source.[Id], source.[StateCode], source.[MinGross], source.[MaxGross], source.[MonthlyTaxAmount], source.[FinancialYearStart], source.[FinancialYearEnd], source.[MonthExclusions], source.[TenantId])
WHEN MATCHED THEN
    UPDATE SET [MonthlyTaxAmount] = source.[MonthlyTaxAmount];
GO

-- =============================================================================
-- 5. IT Declarations (80C, 80D for Priya Sharma — FY 2026-27)
-- =============================================================================
-- Status: 0=Draft, 1=Submitted, 2=PendingReview, 3=Approved, 4=Rejected
-- VerificationStatus: these map to string values (HasConversion<string>())

DECLARE @emp1Id UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
DECLARE @submittedAt DATETIME2 = '2026-04-15 10:00:00';

MERGE [tenant_dev].[ITDeclarations] AS target
USING (VALUES
    ('55555555-0000-0000-0000-000000000001', @emp1Id, 2026, '80C',
     'ELSS Mutual Fund', 150000, 150000, 'Approved', 1, @submittedAt, 'dev'),
    ('55555555-0000-0000-0000-000000000002', @emp1Id, 2026, '80D',
     'Health Insurance Premium', 25000,  25000, 'Approved', 1, @submittedAt, 'dev'),
    ('55555555-0000-0000-0000-000000000003', @emp1Id, 2026, 'HRA',
     'Monthly Rent', 15000,  15000, 'Approved', 1, @submittedAt, 'dev')
) AS source (
    [Id], [EmployeeId], [FinancialYear], [Category],
    [Description], [ClaimedAmount], [ApprovedAmount], [Status], [Version], [SubmittedAt], [TenantId]
)
ON target.[Id] = source.[Id]
WHEN NOT MATCHED THEN
    INSERT (
        [Id], [EmployeeId], [FinancialYear], [Category],
        [Description], [ClaimedAmount], [ApprovedAmount], [Status], [Version], [SubmittedAt], [TenantId]
    )
    VALUES (
        source.[Id], source.[EmployeeId], source.[FinancialYear], source.[Category],
        source.[Description], source.[ClaimedAmount], source.[ApprovedAmount], source.[Status],
        source.[Version], source.[SubmittedAt], source.[TenantId]
    );
GO

-- =============================================================================
-- 6. Verify seed (quick sanity check — rows should be > 0 in each table)
-- =============================================================================
SELECT 'SalaryComponents'  AS [Table], COUNT(*) AS [Rows] FROM [tenant_dev].[SalaryComponents]  WHERE TenantId = 'dev'
UNION ALL
SELECT 'SalaryTemplates',                COUNT(*)           FROM [tenant_dev].[SalaryTemplates]   WHERE TenantId = 'dev'
UNION ALL
SELECT 'PayrollProfiles',                COUNT(*)           FROM [tenant_dev].[PayrollProfiles]   WHERE TenantId = 'dev'
UNION ALL
SELECT 'ProfessionalTaxSlabs',           COUNT(*)           FROM [tenant_dev].[ProfessionalTaxSlabs] WHERE TenantId = 'dev'
UNION ALL
SELECT 'ITDeclarations',                 COUNT(*)           FROM [tenant_dev].[ITDeclarations]    WHERE TenantId = 'dev';
GO

PRINT 'Seed complete. Start the API and use the headers below to test:';
PRINT '  X-Tenant-Id: dev';
PRINT '  X-Karamchari-Gateway: local-dev-gateway';
GO
