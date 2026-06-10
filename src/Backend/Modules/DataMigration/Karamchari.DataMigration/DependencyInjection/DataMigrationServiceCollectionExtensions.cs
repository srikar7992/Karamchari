// -----------------------------------------------------------------------
// <copyright file="DataMigrationServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Features.HistoricalPayroll;
using Karamchari.DataMigration.Features.LeaveBalance;
using Karamchari.DataMigration.Features.SalaryComponent;
using Karamchari.DataMigration.Features.SalaryRevision;
using Karamchari.DataMigration.Persistence;
using Karamchari.DataMigration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.DataMigration.DependencyInjection;

public static class DataMigrationServiceCollectionExtensions
{
    public static IServiceCollection AddKaramchariDataMigration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException("Connection string 'KaramchariDb' is not configured.");

        services.AddDbContext<DataMigrationDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "migration");
                sql.EnableRetryOnFailure();
            });
            opts.AddKaramchariInterceptors(sp);
        });

        // File parsing
        services.AddScoped<IImportFileParser, CsvImportFileParser>();

        // Reference resolution (cross-context lookups)
        services.AddScoped<IReferenceResolver, ReferenceResolver>();

        // File storage (local for dev; swap to AzureBlobImportFileStore in production)
        services.AddSingleton<IImportFileStore>(_ =>
            new LocalImportFileStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imports")));

        // ── Employee Import ──────────────────────────────────────────────────
        services.AddScoped<IImportMapper<EmployeeImportRecord>, EmployeeImportMapper>();
        services.AddScoped<IImportValidator<EmployeeImportRecord>, EmployeeImportValidator>();
        services.AddScoped<IImportProcessor<EmployeeImportRecord>, EmployeeImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<EmployeeImportRecord>>();

        // ── Leave Balance Import ─────────────────────────────────────────────
        services.AddScoped<IImportMapper<LeaveBalanceImportRecord>, LeaveBalanceImportMapper>();
        services.AddScoped<IImportValidator<LeaveBalanceImportRecord>, LeaveBalanceImportValidator>();
        services.AddScoped<IImportProcessor<LeaveBalanceImportRecord>, LeaveBalanceImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<LeaveBalanceImportRecord>>();

        // ── Salary Component Import ──────────────────────────────────────────
        services.AddScoped<IImportMapper<SalaryComponentImportRecord>, SalaryComponentImportMapper>();
        services.AddScoped<IImportValidator<SalaryComponentImportRecord>, SalaryComponentImportValidator>();
        services.AddScoped<IImportProcessor<SalaryComponentImportRecord>, SalaryComponentImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<SalaryComponentImportRecord>>();

        // ── Salary Revision Import ───────────────────────────────────────────
        services.AddScoped<IImportMapper<SalaryRevisionImportRecord>, SalaryRevisionImportMapper>();
        services.AddScoped<IImportValidator<SalaryRevisionImportRecord>, SalaryRevisionImportValidator>();
        services.AddScoped<IImportProcessor<SalaryRevisionImportRecord>, SalaryRevisionImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<SalaryRevisionImportRecord>>();

        // ── Historical Payroll Import ────────────────────────────────────────
        services.AddScoped<IImportMapper<HistoricalPayrollImportRecord>, HistoricalPayrollImportMapper>();
        services.AddScoped<IImportValidator<HistoricalPayrollImportRecord>, HistoricalPayrollImportValidator>();
        services.AddScoped<IImportProcessor<HistoricalPayrollImportRecord>, HistoricalPayrollImportProcessor>();
        services.AddScoped<IImportPipeline, ImportPipeline<HistoricalPayrollImportRecord>>();

        return services;
    }
}
