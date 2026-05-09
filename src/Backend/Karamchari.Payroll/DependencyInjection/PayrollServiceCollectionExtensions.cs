using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Persistence.Provisioning;
using Karamchari.Payroll.Consumers;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Persistence;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Payslip;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.Payroll.Services.FnF;
using Karamchari.Payroll.Services.Arrears;
using Karamchari.Payroll.Services.Loans;
using Karamchari.Payroll.Services.Reimbursements;
using Karamchari.Payroll.Services.Simulation;
using Karamchari.Payroll.Services.Disbursement;
using Karamchari.Payroll.Services.Disbursement.BankAdapters;
using Karamchari.Payroll.Services.Reconciliation;
using Karamchari.Payroll.Domain.Disbursement;
using Karamchari.Payroll.StateMachines;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Payroll.DependencyInjection;

/// <summary>
/// Extension methods for registering Payroll module services.
/// </summary>
public static class PayrollServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the Payroll module services to the service collection and configures MassTransit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="busConfigurator">The MassTransit bus registration configurator.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariPayroll(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(busConfigurator);

        services.AddSingleton<CTCTemplateCompiler>();
        services.AddSingleton<CTCBreakdownService>();
        services.AddSingleton<StatutoryPipelineEngine>();

        // Phase 1A services
        services.AddScoped<FnFCalculationService>();
        services.AddScoped<ArrearCalculationEngine>();
        services.AddScoped<PayrollSimulationEngine>();
        services.AddScoped<BankDisbursementOrchestrator>();
        services.AddScoped<PayrollReconciliationService>();

        // Bank adapters — registered as IEnumerable<IBankDisbursementAdapter>
        services.AddSingleton<IBankDisbursementAdapter, HdfcBankAdapter>();
        
        services.AddScoped<IProfessionalTaxRepository, ProfessionalTaxRepository>();
        services.AddScoped<IProfessionalTaxProvider, CachedProfessionalTaxProvider>();
        
        services.AddScoped<IIncomeProjectionService, IncomeProjectionService>();
        services.AddScoped<IExemptionCalculator, ExemptionCalculator>();
        services.AddScoped<ITaxSlabProvider, TaxSlabProvider>();
        services.AddScoped<IITDeclarationRepository, ITDeclarationRepository>();
        
        services.AddSingleton<IPayslipGenerator, QuestPdfPayslipGenerator>();
        services.AddScoped<IPayslipStorage, LocalFilePayslipStorage>();

        // Post-provisioning tasks: run after SELECT * INTO table clone to apply indexes.
        // SELECT * INTO copies column structure but not indexes — this task recreates
        // the critical indexes defined in the EF model for every new tenant schema.
        services.AddSingleton<ITenantPostProvisioningTask, PayrollLedgerIndexProvisioningTask>();

        // Declaration & Proof Services
        // Bind options from the "DocumentIntelligence" section — secrets resolved from
        // Azure Key Vault via Managed Identity in production, absent in dev (graceful degradation).
        services.Configure<DocumentIntelligenceOptions>(
            configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        services.AddScoped<IDocumentAnalyzer, AzureDocumentAnalyzer>();
        services.AddScoped<IProofStorage, LocalProofStorage>();
        services.AddScoped<ITDeclarationService>();

        services.AddMemoryCache();

        // RLS Configuration
        services.RegisterTenantTable("PayrollProfiles");
        services.RegisterTenantTable("PayrollRunStates");
        services.RegisterTenantTable("PayrollDeductions");
        services.RegisterTenantTable("PayrollSchedules");
        services.RegisterTenantTable("PayrollTimesheetLedger");
        services.RegisterTenantTable("SalaryComponents");
        services.RegisterTenantTable("SalaryTemplates");
        services.RegisterTenantTable("ProfessionalTaxSlabs");
        services.RegisterTenantTable("ITDeclarations");
        services.RegisterTenantTable("PayrollLedger");

        // Phase 1A RLS tables
        services.RegisterTenantTable("FnFSettlements");
        services.RegisterTenantTable("FnFLineItems");
        services.RegisterTenantTable("ArrearCalculations");
        services.RegisterTenantTable("PayrollCorrections");
        services.RegisterTenantTable("ReimbursementClaims");
        services.RegisterTenantTable("EmployeeLoans");
        services.RegisterTenantTable("LoanInstallments");
        services.RegisterTenantTable("VariablePayAllocations");
        services.RegisterTenantTable("SalaryRevisions");
        services.RegisterTenantTable("DisbursementBatches");
        services.RegisterTenantTable("DisbursementEntries");
        services.RegisterTenantTable("PayrollSimulations");
        services.RegisterTenantTable("ReconciliationJobs");
        services.RegisterTenantTable("PayrollAnomalies");
        services.RegisterTenantTable("FnFSettlementStates");
        services.RegisterTenantTable("DisbursementBatchStates");
        services.RegisterTenantTable("PayrollCorrectionStates");

        services.AddDbContext<PayrollDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before PayrollDbContext can be resolved.");

            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(serviceProvider);
        });

        busConfigurator.AddConsumer<EmployeeOnboardedConsumer>();
        busConfigurator.AddConsumer<CalculateAllEmployeePayCommandConsumer>();
        busConfigurator.AddConsumer<PayrollBatchConsumer>();
        busConfigurator.AddConsumer<GeneratePayslipConsumer>();
        busConfigurator.AddConsumer<PayrollRunLockedConsumer>();
        busConfigurator.AddConsumer<LeaveRequestApprovedConsumer>();
        busConfigurator.AddConsumer<TenantProvisionedConsumer>();
        busConfigurator.AddConsumer<TimesheetApprovedConsumer>();

        // Phase 1A consumers
        busConfigurator.AddConsumer<InitiateFnFCalculationConsumer>();
        busConfigurator.AddConsumer<DisburseFnFConsumer>();
        busConfigurator.AddConsumer<SalaryRevisionApprovedArrearConsumer>();
        busConfigurator.AddConsumer<ArrearApprovedConsumer>();
        busConfigurator.AddConsumer<InitiateDisbursementConsumer>();
        busConfigurator.AddConsumer<RetryDisbursementConsumer>();
        busConfigurator.AddConsumer<PayrollNotificationRouter>();

        busConfigurator.AddSagaStateMachine<PayrollRunStateMachine, PayrollRunState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, PayrollDbContext>();
            });

        busConfigurator.AddSagaStateMachine<FnFSettlementStateMachine, FnFSettlementState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, PayrollDbContext>();
            });

        busConfigurator.AddSagaStateMachine<DisbursementBatchStateMachine, DisbursementBatchState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, PayrollDbContext>();
            });

        busConfigurator.AddSagaStateMachine<PayrollCorrectionStateMachine, PayrollCorrectionState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, PayrollDbContext>();
            });

        return services;
    }
}
