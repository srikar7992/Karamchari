using Karamchari.Api;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging.Outbox;
using Karamchari.HR.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Payroll.Contracts;
using MassTransit;
using Karamchari.HR.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.TimeAttendance.Domain.Holidays;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Core.Contracts;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Multitenancy;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Domain;
using Karamchari.PSA.Services;
using Karamchari.PSA.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Karamchari.Payroll.Services.Payslip;
using Karamchari.Billing.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.Performance.Persistence;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Notifications.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Compensation.Persistence;
using Karamchari.Api.BFF.Manager;
using Karamchari.Api.BFF.Employee;
using Karamchari.Api.BFF.HR;
using Karamchari.Api.BFF.Executive;
using Karamchari.Api.BFF.Notifications;
using Karamchari.Api.BFF.Search;

// Entry point for the Karamchari API.
var builder = WebApplication.CreateBuilder(args);

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("ess", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(1);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// OpenTelemetry — traces and metrics exported via OTLP (configure exporter via env in ACA).
// Sources: ASP.NET Core request pipeline, EF Core commands, MassTransit consumers.
// In development the data goes nowhere unless OTEL_EXPORTER_OTLP_ENDPOINT is set.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o =>
        {
            // Exclude health-check endpoints from traces to avoid noise.
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddEntityFrameworkCoreInstrumentation(o =>
        {
            // Capture the SQL text in spans so slow queries are debuggable.
            o.SetDbStatementForText = true;
        })
        .AddSource("MassTransit"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation());

// Add Core Infrastructure (Multitenancy, Interceptors)
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddScoped<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>(sp =>
    new Karamchari.Core.Persistence.Provisioning.TenantProvisioningService(
        sp.GetRequiredService<HRDbContext>(),
        sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.ITenantTableRegistry>(),
        sp.GetRequiredService<Karamchari.Core.Persistence.Provisioning.RlsScriptGenerator>(),
        sp.GetRequiredService<IEnumerable<Karamchari.Core.Persistence.Provisioning.ITenantPostProvisioningTask>>(),
        sp.GetRequiredService<ILogger<Karamchari.Core.Persistence.Provisioning.TenantProvisioningService>>()));

// Configure MassTransit with Transactional Outbox and Module Sagas
builder.Services.AddMassTransit(x =>
{
    // Add Bounded Contexts (now passing MassTransit configurator)
    builder.Services.AddKaramchariHR(builder.Configuration, x);
    builder.Services.AddKaramchariTimeAttendance(builder.Configuration, x);
    builder.Services.AddKaramchariPayroll(builder.Configuration, x);
    builder.Services.AddKaramchariPerformance(builder.Configuration, x);
    builder.Services.AddKaramchariNotifications(builder.Configuration, x);
    builder.Services.AddKaramchariCompensation(builder.Configuration, x);

    // PSA context
    builder.Services.AddDbContext<PSADbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<Karamchari.PSA.Persistence.ProjectResourceRepository>();
    builder.Services.AddScoped<Karamchari.PSA.Services.InvoiceGeneratorService>();
    builder.Services.AddScoped<Karamchari.PSA.Services.EmployeeCostService>();
    builder.Services.AddSingleton<Karamchari.PSA.Services.PricingEngine>();
    builder.Services.AddSingleton<Karamchari.PSA.Services.SimulationService>();
    builder.Services.AddSingleton<Karamchari.PSA.Services.AnomalyDetectionService>();
    builder.Services.AddScoped<Karamchari.PSA.Services.CashFlowService>();
    builder.Services.AddSingleton<Karamchari.PSA.Hubs.AnalyticsBroadcaster>();
    
    // Compliance Generators
    builder.Services.AddScoped<Karamchari.Payroll.Services.Compliance.IEcrGenerator, Karamchari.Payroll.Services.Compliance.EcrGenerator>();
    builder.Services.AddScoped<Karamchari.Payroll.Services.Compliance.IEsicGenerator, Karamchari.Payroll.Services.Compliance.EsicGenerator>();
    builder.Services.AddScoped<Karamchari.Payroll.Services.Compliance.ITdsGenerator, Karamchari.Payroll.Services.Compliance.TdsGenerator>();
    builder.Services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceRiskEngine, Karamchari.Payroll.Services.Compliance.ComplianceRiskEngine>();
    builder.Services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceService, Karamchari.Payroll.Services.Compliance.ComplianceService>();
    
    x.AddConsumer<Karamchari.PSA.Consumers.BillableRevenueConsumer>();
    x.AddConsumer<Karamchari.PSA.Consumers.ProfitCalculationConsumer>();

    // Transactional Outbox — one per DbContext that publishes domain/integration events.
    // Outbox tables (InboxState, OutboxMessage, OutboxState) are pinned to dbo (shared infra, not tenant-owned).
    //
    // Dev: UseBusOutbox() lets MassTransit drain the outbox inline so developers see
    //      immediate delivery without running the relay.
    // Prod: relay service (OutboxRelayService) is the sole drain mechanism; UseBusOutbox()
    //       is intentionally absent to prevent double-delivery.
    var isDev = builder.Environment.IsDevelopment();

    x.AddEntityFrameworkOutbox<HRDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<PayrollDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<TimeAttendanceDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<PSADbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<PerformanceDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });
    x.AddEntityFrameworkOutbox<CompensationDbContext>(o =>
    {
        o.UseSqlServer();
        if (isDev) o.UseBusOutbox();
    });

    builder.Services.AddKaramchariBilling(builder.Configuration);
    builder.Services.AddKaramchariForecasting(builder.Configuration);
    
    x.AddConsumer<Karamchari.Billing.Consumers.BillableEntryConsumer>();
    x.AddConsumer<Karamchari.Billing.Consumers.CollectionCaseConsumer>();
    x.AddConsumer<Karamchari.Forecasting.Consumers.ForecastUpdateConsumer>();

    // TimeAttendance Consumers
    x.AddConsumer<Karamchari.TimeAttendance.Consumers.PunchTranslationConsumer>();
    x.AddConsumer<Karamchari.TimeAttendance.Consumers.LiveAttendanceConsumer>();
    x.AddConsumer<Karamchari.TimeAttendance.Consumers.ReprocessAttendanceConsumer>();

    if (isDev)
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            cfg.ConcurrentMessageLimit = 8;
        });
    }
    else
    {
        // Production: Azure Service Bus transport.
        // OutboxRelayService (registered below) is the sole outbox drain mechanism.
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));
            cfg.ConfigureEndpoints(context);
            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            cfg.ConcurrentMessageLimit = 8;
        });
    }
});

// Production outbox relay — drains dbo.OutboxMessage to Azure Service Bus.
// Not registered in Development: in-memory bus + UseBusOutbox() handles delivery there.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddOutboxRelay(builder.Configuration);
}

// SignalR for real-time analytics and notification updates.
builder.Services.AddSignalR();

// Register NotificationPushService after SignalR so IHubContext<NotificationHub> is available.
builder.Services.AddScoped<INotificationPushService, HubNotificationPushService>();

var app = builder.Build();

app.UseRateLimiter();

app.MapHub<Karamchari.TimeAttendance.Hubs.AttendanceHub>("/hubs/attendance");
app.MapHub<Karamchari.PSA.Hubs.AnalyticsHub>("/hubs/analytics"); // Just in case it was missing

// --- Tenant Provisioning API ---

// Provisions a new tenant, creates their schema/tables, and seeds default data.
app.MapPost("/api/tenants", async (
    ProvisionTenantRequest request, 
    Karamchari.Core.Persistence.Provisioning.TenantProvisioningService provisioningService,
    IPublishEndpoint publishEndpoint) =>
{
    // 1. Generate unique tenant and schema identifiers
    var tenantId = request.CompanyName.ToLowerInvariant().Replace(" ", "_");
    var schemaName = $"tenant_{tenantId}";
    
    // 2. Physical Provisioning (Schema, Table structures, RLS)
    await provisioningService.ProvisionTenantAsync(tenantId, schemaName);
    
    // 3. Trigger Distributed Module Seeding (Asynchronous)
    await publishEndpoint.Publish(new TenantProvisionedIntegrationEvent(
        tenantId, 
        request.CompanyName, 
        request.AdminEmail));
    
    return Results.Created($"/api/tenants/{tenantId}", new { TenantId = tenantId, SchemaName = schemaName });
});

// --- IoT Biometric Edge API ---

// High-throughput biometric ingestion endpoint
app.MapPost("/api/iot/push", async (
    HttpRequest request,
    Karamchari.TimeAttendance.Edge.DeviceAuthService authService,
    Karamchari.TimeAttendance.Edge.IngestionPublisher publisher) =>
{
    var apiKey = request.Headers["x-api-key"].ToString();
    var authResult = await authService.ValidateAsync(apiKey);

    if (!authResult.IsValid || authResult.Device == null)
        return Results.Unauthorized();

    using var reader = new StreamReader(request.Body);
    var rawPayload = await reader.ReadToEndAsync();

    // Fire & Forget into the MassTransit buffer
    await publisher.PublishAsync(rawPayload, authResult.Device.DeviceId, authResult.Device.TenantId, isMobile: false);

    return Results.Ok();
});

// High-throughput mobile attendance endpoint (with GPS)
app.MapPost("/api/iot/mobile-push", async (
    HttpRequest request,
    Karamchari.TimeAttendance.Edge.DeviceAuthService authService,
    Karamchari.TimeAttendance.Edge.IngestionPublisher publisher) =>
{
    var apiKey = request.Headers["x-api-key"].ToString();
    var authResult = await authService.ValidateAsync(apiKey);

    if (!authResult.IsValid || authResult.Device == null)
        return Results.Unauthorized();

    using var reader = new StreamReader(request.Body);
    var rawPayload = await reader.ReadToEndAsync();

    // Fire & Forget into the MassTransit buffer
    await publisher.PublishAsync(rawPayload, authResult.Device.DeviceId, authResult.Device.TenantId, isMobile: true);

    return Results.Ok();
});

// --- TimeAttendance Operational API (DLQ, Fraud, Reprocessing) ---

// Fetch Pending DLQ Items
app.MapGet("/api/attendance/dlq", async (TimeAttendanceDbContext dbContext) =>
{
    var pending = await dbContext.InvalidPunches
        .Where(p => p.Status == Karamchari.TimeAttendance.Domain.IoT.InvalidPunchStatus.Pending)
        .OrderByDescending(p => p.TimestampUtc)
        .ToListAsync();
    return Results.Ok(pending);
});

// Map DLQ to Employee and Trigger Reprocess (Transactional Outbox)
app.MapPost("/api/attendance/dlq/{id}/map", async (
    Guid id,
    MapDlqRequest request,
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ITenantProvider tenantProvider) =>
{
    var punch = await dbContext.InvalidPunches.FindAsync(id);
    if (punch == null) return Results.NotFound();

    var tenant = tenantProvider.GetTenant();
    
    // Robustly parse the biometric payload
    string? biometricId = null;
    try {
        using var document = System.Text.Json.JsonDocument.Parse(punch.Payload);
        biometricId = document.RootElement.TryGetProperty("biometricId", out var prop) ? prop.GetString() : null;
    } catch { }

    if (string.IsNullOrEmpty(biometricId)) return Results.BadRequest("Invalid punch payload: missing biometricId");

    // Begin Transaction
    using var tx = await dbContext.Database.BeginTransactionAsync();

    try
    {
        // 1. Save Mapping
        var mapping = Karamchari.TimeAttendance.Domain.IoT.BiometricMapping.Create(
            tenant.TenantId, biometricId ?? string.Empty, request.EmployeeId);
        dbContext.BiometricMappings.Add(mapping);

        // 2. Update DLQ Status
        punch.TransitionTo(Karamchari.TimeAttendance.Domain.IoT.InvalidPunchStatus.Mapped);

        // 3. Create Background Job & Emit Reprocess Command (Outbox)
        var job = new Karamchari.TimeAttendance.Domain.IoT.BackgroundJob { 
            TenantId = tenant.TenantId, 
            JobType = "ReprocessAttendance" 
        };
        dbContext.BackgroundJobs.Add(job);

        await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.ReprocessAttendanceCommandV1
        {
            JobId = job.Id,
            TenantId = tenant.TenantId,
            EmployeeId = request.EmployeeId,
            FromDateUtc = punch.TimestampUtc.Date,
            ToDateUtc = punch.TimestampUtc.Date
        });

        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Ok(new { JobId = job.Id });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
});

// Fetch Active Fraud Flags
app.MapGet("/api/attendance/fraud", async (TimeAttendanceDbContext dbContext) =>
{
    var flags = await dbContext.FraudFlags
        .Where(f => f.Status == Karamchari.TimeAttendance.Domain.IoT.FraudStatus.Detected || f.Status == Karamchari.TimeAttendance.Domain.IoT.FraudStatus.UnderReview)
        .OrderByDescending(f => f.SeverityScore)
        .ThenByDescending(f => f.DetectedAtUtc)
        .ToListAsync();
    return Results.Ok(flags);
});

// Review Fraud Flag
app.MapPut("/api/attendance/fraud/{id}/review", async (
    Guid id,
    ReviewFraudRequest request,
    TimeAttendanceDbContext dbContext,
    ClaimsPrincipal user) =>
{
    var flag = await dbContext.FraudFlags.FindAsync(id);
    if (flag == null) return Results.NotFound();

    var reviewer = user.Identity?.Name ?? "Admin";
    flag.Review(request.Status, reviewer, request.Notes);

    await dbContext.SaveChangesAsync();
    return Results.Ok();
});

// Trigger Bulk Reprocessing
app.MapPost("/api/attendance/reprocess", async (
    ReprocessAttendanceRequest request,
    IPublishEndpoint publishEndpoint,
    ITenantProvider tenantProvider) =>
{
    if (!DateTime.TryParse(request.FromDate, out var fromDate) || 
        !DateTime.TryParse(request.ToDate, out var toDate))
    {
        return Results.BadRequest("Invalid date format. Use ISO8601.");
    }

    var tenant = tenantProvider.GetTenant();
    var jobId = Guid.NewGuid();

    await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.ReprocessAttendanceCommandV1
    {
        JobId = jobId,
        TenantId = tenant.TenantId,
        EmployeeId = request.EmployeeId ?? Guid.Empty, // Assuming Empty means all for this sprint scope
        FromDateUtc = fromDate.ToUniversalTime(),
        ToDateUtc = toDate.ToUniversalTime()
    });

    return Results.Ok(new { JobId = jobId, Message = "Reprocessing queued successfully." });
});

app.MapGet("/api/attendance/reprocess/{jobId}", async (Guid jobId, TimeAttendanceDbContext db) =>
{
    var job = await db.BackgroundJobs.FindAsync(jobId);
    return job != null ? Results.Ok(job) : Results.NotFound();
});

// --- Payroll API ---
// Initiates a new payroll run saga.
app.MapPost("/api/payroll/runs", async (StartPayrollRunRequest request, IPublishEndpoint publishEndpoint, ITenantProvider tenantProvider) =>
{
    // Generate a COMB GUID for better SQL Server indexing performance
    var runId = NewId.NextGuid();

    // Resolve tenant from the authenticated request (JWT or gateway header)
    var tenant = tenantProvider.GetTenant();

    await publishEndpoint.Publish(new StartPayrollRunCommand(runId, tenant.TenantId, request.PeriodName));

    return Results.Accepted($"/api/payroll/runs/{runId}", new { RunId = runId });
});

// Returns all payroll runs for the current tenant.
// RLS and the Schema Interceptor handle the filtering automatically.
app.MapGet("/api/payroll/runs", async (PayrollDbContext dbContext) =>
{
    var runs = await dbContext.PayrollRunStates
        .OrderByDescending(x => x.StartedAt)
        .ToListAsync();
        
    return Results.Ok(runs);
});

// Returns a high-level summary of a specific payroll run (The "CFO View").
app.MapGet("/api/payroll/runs/{id}/summary", async (Guid id, PayrollDbContext dbContext) =>
{
    var run = await dbContext.PayrollRunStates.FirstOrDefaultAsync(r => r.CorrelationId == id);
    if (run == null) return Results.NotFound();

    // Calculate actual tax from the ledger for double-verification
    var ledgerTax = await dbContext.PayrollLedger
        .Where(e => e.RunId == id)
        .SumAsync(e => e.TdsDeducted);

    return Results.Ok(new 
    {
        run.CorrelationId,
        run.PeriodName,
        run.CurrentState,
        run.TotalEmployeesToProcess,
        run.ProcessedEmployees,
        run.TotalGross,
        run.TotalNet,
        TotalTax = ledgerTax,
        run.StartedAt,
        run.LockedAt,
        run.LockedBy
    });
});

// Returns a detailed, itemized list of all employees in the run with anomaly detection.
app.MapGet("/api/payroll/runs/{id}/details", async (Guid id, PayrollDbContext dbContext) =>
{
    var run = await dbContext.PayrollRunStates.FirstOrDefaultAsync(r => r.CorrelationId == id);
    if (run == null) return Results.NotFound();

    // Fetch current run entries
    var currentEntries = await dbContext.PayrollLedger
        .Where(e => e.RunId == id)
        .ToListAsync();

    var employeeIds = currentEntries.Select(e => e.EmployeeId).ToList();

    // Fetch previous run entries for variance calculation. 
    // Uses the most recent record per employee outside of the current run context.
    // For now, let's just find the most recent entry for these employees that is NOT this run
    var previousEntries = await dbContext.PayrollLedger
        .Where(e => employeeIds.Contains(e.EmployeeId) && e.RunId != id)
        .GroupBy(e => e.EmployeeId)
        .Select(g => g.OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).First())
        .ToListAsync();

    var previousMap = previousEntries.ToDictionary(e => e.EmployeeId, e => e);

    var details = currentEntries.Select(e => 
    {
        var prev = previousMap.GetValueOrDefault(e.EmployeeId);
        decimal variance = 0;
        if (prev != null && prev.NetPay != 0)
        {
            variance = (e.NetPay - prev.NetPay) / prev.NetPay;
        }

        return new
        {
            e.EmployeeId,
            e.MonthlyGross,
            e.NetPay,
            TDS = e.TdsDeducted,
            VariancePercentage = variance,
            IsAnomaly = Math.Abs(variance) > 0.10m // 10% threshold
        };
    });

    return Results.Ok(details);
});

// Locks a payroll run and triggers payslip generation.
app.MapPut("/api/payroll/runs/{id}/lock", async (Guid id, LockPayrollRunRequest request, IPublishEndpoint publishEndpoint) =>
{
    // The Saga handles the state transition and integration event firing
    await publishEndpoint.Publish(new LockPayrollRunCommand(id, request.ApprovedBy));
    
    return Results.Accepted();
});

// --- Salary Structure API ---

// Creates a new master salary component.
app.MapPost("/api/payroll/salary-components", async (CreateSalaryComponentRequest request, PayrollDbContext dbContext) =>
{
    var component = SalaryComponent.Create(request.Name, request.Type, request.Rounding);
    dbContext.SalaryComponents.Add(component);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/payroll/salary-components/{component.Id}", component);
});

// Returns all salary components for the current tenant.
app.MapGet("/api/payroll/salary-components", async (PayrollDbContext dbContext) =>
{
    var components = await dbContext.SalaryComponents.ToListAsync();
    return Results.Ok(components);
});

// Creates a new salary template.
app.MapPost("/api/payroll/salary-templates", async (CreateSalaryTemplateRequest request, PayrollDbContext dbContext) =>
{
    var template = SalaryTemplate.Create(request.Name);
    foreach (var comp in request.Components)
    {
        template.AddComponent(comp);
    }
    dbContext.SalaryTemplates.Add(template);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/payroll/salary-templates/{template.Id}", template);
});

// Dry-runs a CTC breakdown calculation based on a template.
app.MapPost("/api/payroll/salary-templates/{id}/calculate", async (
    Guid id, 
    CalculateCTCBreakdownRequest request, 
    PayrollDbContext dbContext) =>
{
    var template = await dbContext.SalaryTemplates.FindAsync(id);
    if (template == null) return Results.NotFound("Template not found");
    
    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    
    // In a high-throughput system, this CompiledExecutionPlan would be cached in Redis/Memory.
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var result = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);
    
    return Results.Ok(result);
});

// Performs a full salary calculation including statutory deductions (EPF, ESIC, PT, TDS).
app.MapPost("/api/payroll/salary-templates/{id}/calculate-statutory", async (
    Guid id,
    CalculateStatutoryRequest request,
    PayrollDbContext dbContext,
    IProfessionalTaxProvider ptProvider,
    IIncomeProjectionService projectionService,
    IExemptionCalculator exemptionCalculator,
    ITaxSlabProvider taxSlabProvider,
    IITDeclarationRepository declarationRepository) =>
{
    var template = await dbContext.SalaryTemplates.FindAsync(id);
    if (template == null) return Results.NotFound("Template not found");

    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var breakdown = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);

    // Load profile or use defaults for the dry-run
    var profile = request.EmployeeId.HasValue
        ? await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId.Value)
        : PayrollProfile.CreateDraft(Guid.Empty);

    // In a real app, the rule set would be resolved via a factory based on the current date
    var ruleSet = new FY20262027RuleSet(
        request.EpfBaseComponentIds,
        ptProvider,
        projectionService,
        exemptionCalculator,
        taxSlabProvider,
        declarationRepository);

    var statutoryContext = new StatutoryContext(breakdown, profile!, ruleSet.Year, DateTime.UtcNow.Month);
    
    var result = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);
    
    return Results.Ok(new 
    {
        Breakdown = breakdown,
        Statutory = result
    });
});

// --- Statutory Compliance API ---

// Download EPF ECR File
app.MapGet("/api/compliance/epf/{payrollRunId}", async (
    Guid payrollRunId,
    Karamchari.Payroll.Services.Compliance.IComplianceService complianceService) =>
{
    var (fileName, content) = await complianceService.GetEpfEcrAsync(payrollRunId);
    return Results.File(content, "text/plain", fileName);
});

// Download ESIC Monthly Return
app.MapGet("/api/compliance/esic/{payrollRunId}", async (
    Guid payrollRunId,
    Karamchari.Payroll.Services.Compliance.IComplianceService complianceService) =>
{
    var (fileName, content) = await complianceService.GetEsicMonthlyAsync(payrollRunId);
    return Results.File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
});

// Download TDS Form 24Q
app.MapGet("/api/compliance/tds/{payrollRunId}", async (
    Guid payrollRunId,
    Karamchari.Payroll.Services.Compliance.IComplianceService complianceService) =>
{
    var (fileName, content) = await complianceService.GetTdsForm24QAsync(payrollRunId);
    return Results.File(content, "text/plain", fileName);
});

// Mark a return as filed
app.MapPost("/api/compliance/mark-filed", async (
    MarkFiledRequest request,
    Karamchari.Payroll.Services.Compliance.IComplianceService complianceService,
    ClaimsPrincipal user) =>
{
    var filedBy = user.Identity?.Name ?? "Admin";
    await complianceService.MarkAsFiledAsync(request.RunId, request.Type, request.ReferenceNumber, filedBy);
    return Results.Ok();
});

// Get Compliance Health Summary (CFO Dashboard)
app.MapGet("/api/compliance/health", async (
    Karamchari.Payroll.Services.Compliance.IComplianceService complianceService) =>
{
    var health = await complianceService.GetComplianceHealthAsync();
    return Results.Ok(health);
});

// --- Time & Attendance API ---

// Field Agent Mobile Punch
app.MapPost("/api/mobile/punch", async (
    MobilePunchRequest request,
    TimeAttendanceDbContext dbContext,
    Karamchari.TimeAttendance.Services.Geofencing.IGeofenceService geofenceService,
    Karamchari.TimeAttendance.Services.Fraud.IFraudRiskEngine fraudEngine,
    ILogger<Program> logger) =>
{
    // 1. Idempotency Check (Essential for offline sync retries)
    var existing = await dbContext.RawPunches
        .AnyAsync(p => p.ExternalId == request.ClientId);
    
    if (existing)
    {
        return Results.Ok(new { message = "Punch already processed (idempotent)" });
    }

    // 2. Resolve employee
    if (!Guid.TryParse(request.EmployeeId, out var employeeId))
    {
        return Results.BadRequest("Invalid Employee ID format");
    }

    // 3. Time Skew Validation (Detect if device clock is manipulated)
    var timeSkew = Math.Abs((DateTime.UtcNow - request.Timestamp.ToUniversalTime()).TotalMinutes);
    if (timeSkew > 1440) // > 24 hours (allowing for long offline periods, but flagging)
    {
        logger.LogWarning("Significant time skew detected for {EmployeeId}: {Skew} minutes", employeeId, timeSkew);
    }

    // 4. Spatial Geofence Validation
    var (isInside, boundary, distance) = await geofenceService.CheckGeofenceAsync(employeeId, request.Lat, request.Lon);
    
    if (!isInside)
    {
        logger.LogWarning("Punch rejected: Outside geofence for {EmployeeId} (Distance: {Distance:F0}m)", 
            request.EmployeeId, distance);
        return Results.BadRequest(new { 
            message = "Outside geofence boundary", 
            distanceMeters = Math.Round(distance),
            requiredBoundary = boundary?.Name ?? "Assigned Site"
        });
    }

    // 5. Create RawPunch record
    var punch = Karamchari.TimeAttendance.Domain.IoT.RawPunch.Create(
        "system", // Tenant ID fallback
        employeeId,
        "MOBILE_APP",
        request.ClientId, // Use ClientId as ExternalId for idempotency
        request.Timestamp.ToUniversalTime(),
        request.Timestamp, // Local timestamp
        "UTC",
        DateTime.UtcNow,
        request.Type,
        Karamchari.TimeAttendance.Domain.IoT.PunchSource.Mobile,
        request.Lat,
        request.Lon
    );

    dbContext.RawPunches.Add(punch);
    await dbContext.SaveChangesAsync();

    // 6. Fraud Detection (Asynchronous risk assessment)
    var fraudFlag = await fraudEngine.EvaluatePunchAsync(punch, request.Accuracy);

    if (logger.IsEnabled(LogLevel.Information))
        logger.LogInformation("Mobile punch accepted for {EmployeeId} (Accuracy: {Accuracy}m, Risk: {Risk})",
            request.EmployeeId, request.Accuracy, fraudFlag.Severity);

    return Results.Ok(new { 
        message = "Punch recorded successfully", 
        id = punch.Id,
        riskLevel = fraudFlag.Severity,
        explanation = fraudFlag.Explanation
    });
});

// Returns all holidays for the current tenant.
app.MapGet("/api/time/holidays", async (TimeAttendanceDbContext dbContext) =>
{
    // Fetch all holidays across all calendars (RLS automatically filters by tenant).
    // In a mature system, we would filter by the specific calendar assigned to the user's region.
    var holidays = await dbContext.HolidayCalendars
        .SelectMany(c => c.Holidays)
        .OrderBy(h => h.Date)
        .ToListAsync();
        
    return Results.Ok(holidays);
});

// Adds a new holiday to the default organizational calendar.
app.MapPost("/api/time/holidays", async (AddHolidayRequest request, TimeAttendanceDbContext dbContext) =>
{
    // Find or create the "Default Calendar" for the tenant.
    var calendar = await dbContext.HolidayCalendars
        .Include(c => c.Holidays)
        .FirstOrDefaultAsync(c => c.Name == "Default Calendar");
        
    if (calendar == null)
    {
        calendar = HolidayCalendar.Create("Default Calendar", "Standard Organizational Calendar");
        dbContext.HolidayCalendars.Add(calendar);
    }
    
    calendar.AddHoliday(request.Date, request.Name);
    await dbContext.SaveChangesAsync();
    
    // Find the newly added holiday to return its ID
    var newHoliday = calendar.Holidays.First(h => h.Date == request.Date);
    
    return Results.Created($"/api/time/holidays/{newHoliday.Id}", newHoliday);
});

// Returns all leave policies for the current tenant.
app.MapGet("/api/time/leave-policies", async (TimeAttendanceDbContext dbContext) =>
{
    var policies = await dbContext.LeavePolicies
        .Where(p => p.IsActive)
        .ToListAsync();
        
    return Results.Ok(policies);
});

// Creates a new leave policy.
app.MapPost("/api/time/leave-policies", async (CreateLeavePolicyRequest request, TimeAttendanceDbContext dbContext) =>
{
    var policy = LeavePolicy.Create(request.Name, request.Description, request.Rules);
    dbContext.LeavePolicies.Add(policy);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/time/leave-policies/{policy.Id}", policy);
});

// Returns leave balances for the current user (simulated).
app.MapGet("/api/time/leave-balances", async (TimeAttendanceDbContext dbContext) =>
{
    // In this sprint, we resolve EmployeeId from context (mocked for now).
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var balances = await dbContext.LeaveBalances
        .Where(b => b.EmployeeId == employeeId)
        .ToListAsync();
        
    return Results.Ok(balances);
});

// Calculates the actual leave days for a given range (dry-run for UI feedback).
app.MapPost("/api/time/leave-requests/calculate", async (
    CalculateLeaveDaysRequest request, 
    TimeAttendanceDbContext dbContext,
    LeaveRequestService leaveService) =>
{
    var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
    if (policy == null) return Results.NotFound("Policy not found");

    var holidays = await dbContext.HolidayCalendars
        .SelectMany(c => c.Holidays)
        .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
        .Select(h => h.Date)
        .ToListAsync();

    var days = LeaveRequestService.CalculateActualLeaveDays(
        request.StartDate, 
        request.EndDate, 
        holidays, 
        policy.Rules.AllowHalfDays);

    return Results.Ok(new { ActualDays = days });
});

// Submits a new leave request.
app.MapPost("/api/time/leave-requests", async (
    SubmitLeaveRequestRequest request, 
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    LeaveRequestService leaveService) =>
{
    var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
    if (policy == null) return Results.BadRequest("Policy not found");

    var holidays = await dbContext.HolidayCalendars
        .SelectMany(c => c.Holidays)
        .Where(h => h.Date >= request.StartDate && h.Date <= request.EndDate)
        .Select(h => h.Date)
        .ToListAsync();

    var actualDays = LeaveRequestService.CalculateActualLeaveDays(
        request.StartDate, 
        request.EndDate, 
        holidays, 
        policy.Rules.AllowHalfDays);
        
    if (actualDays <= 0) return Results.BadRequest("No working days in range");

    // Mock current user
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); 
    
    var balance = await dbContext.LeaveBalances
        .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.PolicyId == request.PolicyId);
        
    if (balance == null)
    {
        balance = LeaveBalance.Create(employeeId, request.PolicyId, policy.Rules.AnnualAllowance);
        dbContext.LeaveBalances.Add(balance);
    }

    if (balance.RemainingBalance < actualDays) 
    {
        return Results.BadRequest($"Insufficient balance. Required: {actualDays}, Available: {balance.RemainingBalance}");
    }

    // 1. Deduct balance immediately (Reserve)
    balance.Deduct(actualDays);

    var leaveRequest = LeaveRequest.Create(
        employeeId, 
        request.PolicyId, 
        request.StartDate, 
        request.EndDate, 
        actualDays, 
        request.Reason);

    // 2. If no approval needed, move to Approved and emit event
    if (!policy.Rules.RequiresManagerApproval)
    {
        leaveRequest.Approve();
        
        await publishEndpoint.Publish(new LeaveRequestApprovedIntegrationEvent(
            leaveRequest.Id, 
            employeeId, 
            leaveRequest.StartDate, 
            leaveRequest.EndDate, 
            actualDays, 
            policy.Rules.Category == LeaveCategory.Paid));
    }

    dbContext.LeaveRequests.Add(leaveRequest);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/time/leave-requests/{leaveRequest.Id}", leaveRequest);
});

// Returns all pending leave requests for the current tenant.
app.MapGet("/api/time/leave-requests/pending", async (TimeAttendanceDbContext dbContext) =>
{
    var requests = await dbContext.LeaveRequests
        .Where(r => r.Status == LeaveRequestStatus.Pending)
        .OrderByDescending(r => r.RequestedOnUtc)
        .ToListAsync();
        
    return Results.Ok(requests);
});

// Approves a leave request.
app.MapPut("/api/time/leave-requests/{id}/approve", async (
    Guid id, 
    TimeAttendanceDbContext dbContext,
    IPublishEndpoint publishEndpoint) =>
{
    var request = await dbContext.LeaveRequests.FindAsync(id);
    if (request == null) return Results.NotFound();

    var policy = await dbContext.LeavePolicies.FindAsync(request.PolicyId);
    if (policy == null) return Results.BadRequest("Policy not found.");

    request.Approve();
    
    // Publish integration event for Payroll module
    await publishEndpoint.Publish(new LeaveRequestApprovedIntegrationEvent(
        request.Id,
        request.EmployeeId,
        request.StartDate,
        request.EndDate,
        request.ActualDays,
        policy.Rules.Category == LeaveCategory.Paid));

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Rejects a leave request.
app.MapPut("/api/time/leave-requests/{id}/reject", async (
    Guid id, 
    TimeAttendanceDbContext dbContext) =>
{
    var request = await dbContext.LeaveRequests.FindAsync(id);
    if (request == null) return Results.NotFound();

    // Refund the balance that was reserved upon submission
    var balance = await dbContext.LeaveBalances
        .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.PolicyId == request.PolicyId);
        
    balance?.Refund(request.ActualDays);

    request.Reject();
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Returns the current week's timesheet for the authenticated employee.
app.MapGet("/api/time/timesheets/current-week", async (
    TimeAttendanceDbContext dbContext,
    TimeProvider timeProvider) =>
{
    // Mock current user
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
    // Find Monday of the current week
    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
    var weekStart = today.AddDays(-1 * diff);
    
    var timesheet = await dbContext.Timesheets
        .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart);
        
    if (timesheet != null)
    {
        return Results.Ok(timesheet);
    }
    
    // Return a blank draft template if no timesheet exists for this week
    var blankEntries = Enumerable.Range(0, 7)
        .Select(i => new TimeEntry { Date = weekStart.AddDays(i), Hours = 0, Description = null })
        .ToList();
        
    return Results.Ok(new 
    { 
        EmployeeId = employeeId,
        WeekStartDate = weekStart,
        Status = TimesheetStatus.Draft,
        Entries = blankEntries,
        TotalHours = 0
    });
});

// Submits or updates a weekly timesheet.
app.MapPost("/api/time/timesheets", async (
    SubmitTimesheetRequest request,
    ClaimsPrincipal user,
    TimeAttendanceDbContext dbContext) =>
{
    var employeeId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
        ? uid
        : Guid.Parse("00000000-0000-0000-0000-000000000001"); // dev fallback only

    var timesheet = await dbContext.Timesheets
        .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == request.WeekStartDate);

    if (timesheet == null)
    {
        timesheet = Timesheet.Create(employeeId, request.WeekStartDate);
        dbContext.Timesheets.Add(timesheet);
    }

    var entries = request.Entries.Select(e => new TimeEntry
    {
        Date = e.Date,
        Hours = e.Hours,
        Description = e.Description
    });

    timesheet.UpdateEntries(entries);
    timesheet.Submit(employeeId);

    await dbContext.SaveChangesAsync();
    
    return Results.Ok(timesheet);
});

// Returns all pending timesheets for manager review.
app.MapGet("/api/time/timesheets/pending", async (TimeAttendanceDbContext dbContext) =>
{
    var timesheets = await dbContext.Timesheets
        .Where(t => t.Status == TimesheetStatus.Submitted)
        .OrderByDescending(t => t.WeekStartDate)
        .ToListAsync();
        
    return Results.Ok(timesheets);
});

// Approves a weekly timesheet.
// Domain events are captured by the MassTransit outbox inside SaveChangesAsync —
// no direct IPublishEndpoint call here; TimesheetService owns the full flow.
app.MapPut("/api/time/timesheets/{id}/approve", async (
    Guid id,
    ClaimsPrincipal user,
    Karamchari.TimeAttendance.Services.TimesheetService timesheetService) =>
{
    // In dev the JWT sub is absent; use Guid.Empty as the system approver.
    var approverId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid)
        ? uid
        : Guid.Empty;

    try
    {
        await timesheetService.ApproveAsync(id, approverId);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
});

// Rejects a weekly timesheet.
app.MapPut("/api/time/timesheets/{id}/reject", async (
    Guid id, 
    RejectTimesheetRequest request,
    TimeAttendanceDbContext dbContext) =>
{
    var timesheet = await dbContext.Timesheets.FindAsync(id);
    if (timesheet == null) return Results.NotFound();

    timesheet.Reject(request.Reason, Guid.Empty);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// --- Employee Self-Service (ESS) API ---

// Returns all finalized payslips for the authenticated employee.
app.MapGet("/api/ess/payslips", async (PayrollDbContext dbContext) =>
{
    // In a production app, this would be resolved from the JWT claim.
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    var payslips = await dbContext.PayrollLedger
        .Where(e => e.EmployeeId == employeeId)
        .OrderByDescending(e => e.Year)
        .ThenByDescending(e => e.Month)
        .Select(e => new
        {
            e.Id,
            e.Year,
            e.Month,
            e.PeriodName,
            e.MonthlyGross,
            e.NetPay,
            e.TdsDeducted
        })
        .ToListAsync();

    return Results.Ok(payslips);
});

// Streams a payslip PDF for the authenticated employee.
// Security: Deterministic path prevents IDOR/Enumeration attacks.
app.MapGet("/api/ess/payslips/{year}/{month}/download", async (
    int year, 
    int month, 
    PayrollDbContext dbContext,
    IPayslipStorage storage) =>
{
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // 1. Verify the ledger entry exists for this specific user (Ownership Check)
    var ledger = await dbContext.PayrollLedger
        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.Year == year && e.Month == month);

    if (ledger == null)
    {
        return Results.NotFound("Payslip not found or access denied.");
    }

    try
    {
        // 2. Fetch from storage via proxy (Security: Never expose internal storage paths)
        var pdfBytes = await storage.GetAsync(employeeId.ToString(), ledger.PeriodName);
        
        var fileName = $"Payslip_{ledger.PeriodName.Replace(" ", "_")}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound("Physical payslip file missing.");
    }
});

// Returns the Year-to-Date (YTD) summary for the current financial year.
app.MapGet("/api/ess/payslips/ytd", async (PayrollDbContext dbContext) =>
{
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    // Determine the current Indian Financial Year (April 1st to March 31st)
    var now = DateTime.UtcNow;
    var fyStartYear = now.Month >= 4 ? now.Year : now.Year - 1;

    // Batch query with aggregation (Performance: DB-side summation)
    var ytd = await dbContext.PayrollLedger
        .Where(e => e.EmployeeId == employeeId && e.FinancialYearStart == fyStartYear)
        .GroupBy(e => e.EmployeeId)
        .Select(g => new
        {
            GrossYtd = g.Sum(x => x.MonthlyGross),
            NetYtd = g.Sum(x => x.NetPay),
            TaxYtd = g.Sum(x => x.TdsDeducted),
            Count = g.Count()
        })
        .FirstOrDefaultAsync();

    return Results.Ok(ytd ?? new { GrossYtd = 0m, NetYtd = 0m, TaxYtd = 0m, Count = 0 });
});

// --- Tax Simulator API ---

// Performs a stateless "What-If" tax projection comparison.
app.MapPost("/api/ess/tax-simulator/dry-run", async (
    TaxSimulationRequest request,
    PayrollDbContext dbContext,
    IServiceProvider sp) =>
{
    // 1. Security & Validation
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    if (request.Section80C > 150000) return Results.BadRequest("Section 80C declaration cannot exceed ₹1,50,000.");
    if (request.MonthlyRent < 0) return Results.BadRequest("Monthly rent cannot be negative.");

    // 2. Load Base Context (Profile + Salary Template)
    var profile = await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.EmployeeId == employeeId);
    if (profile == null) return Results.NotFound("Employee payroll profile not found.");

    var template = await dbContext.SalaryTemplates.FindAsync(profile.SalaryTemplateId);
    if (template == null) return Results.NotFound("Salary template not found.");

    var masterComponents = await dbContext.SalaryComponents.ToListAsync();
    var plan = CTCTemplateCompiler.Compile(template, masterComponents);
    var breakdown = CTCBreakdownService.Calculate(profile.AnnualCTC, plan);

    // 3. Build Ephemeral Declarations per category for the simulator
    var fy = new FinancialYear(2026, 2027); // Standard for simulator
    var ephemeralDeclarations = new List<ITDeclaration>();
    if (request.Section80C > 0)
        ephemeralDeclarations.Add(ITDeclaration.Create(employeeId, fy.StartYear, "80C", request.Section80C, "simulator", "simulator"));
    if (request.Section80D > 0)
        ephemeralDeclarations.Add(ITDeclaration.Create(employeeId, fy.StartYear, "80D", request.Section80D, "simulator", "simulator"));
    if (request.MonthlyRent > 0)
        ephemeralDeclarations.Add(ITDeclaration.Create(employeeId, fy.StartYear, "HRA", request.MonthlyRent * 12, "simulator", "simulator"));

    // 4. Run Projections (Old vs New Regime)
    var ptProvider = sp.GetRequiredService<IProfessionalTaxProvider>();
    var projectionService = sp.GetRequiredService<IIncomeProjectionService>();
    var exemptionCalculator = sp.GetRequiredService<IExemptionCalculator>();
    var taxSlabProvider = sp.GetRequiredService<ITaxSlabProvider>();

    // Inject the ephemeral declarations via a static repository
    var staticRepo = new StaticDeclarationRepository(ephemeralDeclarations);
    
    // RuleSet with the static repo
    var ruleSet = new FY20262027RuleSet(
        new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") }, // Basic ID
        ptProvider, projectionService, exemptionCalculator, taxSlabProvider, staticRepo);

    // Run Old Regime
    var oldProfile = profile.CloneWithRegime(TaxRegime.Old);
    var oldContext = new StatutoryContext(breakdown, oldProfile, fy, DateTime.UtcNow.Month);
    var oldResult = await StatutoryPipelineEngine.ExecuteAsync(oldContext, ruleSet);

    // Run New Regime
    var newProfile = profile.CloneWithRegime(TaxRegime.New);
    var newContext = new StatutoryContext(breakdown, newProfile, fy, DateTime.UtcNow.Month);
    var newResult = await StatutoryPipelineEngine.ExecuteAsync(newContext, ruleSet);

    decimal oldTax = oldResult.Deductions.GetValueOrDefault("TDS", 0);
    decimal newTax = newResult.Deductions.GetValueOrDefault("TDS", 0);

    return Results.Ok(new TaxSimulationResult(
        oldTax,
        newTax,
        Math.Abs(oldTax - newTax),
        oldTax < newTax ? "Old Regime" : "New Regime",
        oldTax < newTax ? "Based on your deductions, the Old Regime provides higher savings." : "The New Regime is more beneficial due to lower overall tax rates."
    ));
})
.RequireRateLimiting("ess");

// Uploads and analyzes a tax investment proof using Azure AI Document Intelligence.
app.MapPost("/api/ess/declarations/analyze", async (
    IFormFile file,
    IDocumentAnalyzer analyzer,
    IProofStorage storage) =>
{
    // 1. Security: Enforce strict file validation
    if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");
    if (file.Length > 5 * 1024 * 1024) return Results.BadRequest("File size exceeds 5MB limit.");
    
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!ProofAllowedExtensions.Contains(extension)) return Results.BadRequest("Invalid file type. Only PDF, JPG, and PNG are supported.");

    // Context (Mocked for now)
    var employeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var tenantId = "tenant_oakridge";
    var financialYear = 2026;

    using var stream = file.OpenReadStream();

    // 2. Store: Save to secure storage BEFORE analysis (Security & Compliance)
    // Path: /tax-proofs/{tenant}/{year}/{employeeId}/{guid}.pdf
    var storageUri = await storage.SaveAsync(stream, file.FileName, tenantId, employeeId, financialYear);

    // 3. Analyze: Run AI extraction (Security: Backend-only SDK call)
    stream.Position = 0; // Reset stream for analyzer
    var result = await analyzer.AnalyzeAsync(stream, file.FileName);

    // 4. Return: Return normalized DTO with Storage URI for audit linkage
    return Results.Ok(result with { StorageUri = storageUri });
})
.DisableAntiforgery()
.RequireRateLimiting("ai"); 

// --- Admin Tax Verification API ---

// Returns a queue of all pending tax declarations for HR review.
app.MapGet("/api/admin/declarations/pending", async (IITDeclarationRepository repo) =>
{
    var items = await repo.GetPendingReviewAsync();
    return Results.Ok(items.Select(x => new
    {
        x.Id,
        x.EmployeeId,
        x.Category,
        x.ClaimedAmount,
        x.SubmittedAt,
        x.ProofUri
    }));
});

// Streams a proof document to the HR split-view dashboard.
app.MapGet("/api/admin/declarations/{id}/document", async (Guid id, IITDeclarationRepository repo, IProofStorage storage) =>
{
    var declaration = await repo.GetByIdAsync(id);
    if (declaration == null) return Results.NotFound();

    var stream = await storage.GetStreamAsync(declaration.ProofUri);
    // Determine content type based on extension
    var extension = Path.GetExtension(declaration.ProofUri).ToLowerInvariant();
    var contentType = extension == ".pdf" ? "application/pdf" : "image/jpeg";
    
    return Results.File(stream, contentType);
});

// Approves a tax declaration with an explicit approved amount.
app.MapPut("/api/admin/declarations/{id}/approve", async (Guid id, [FromBody] decimal amount, ITDeclarationService service) =>
{
    // In a real app, get user from ClaimsPrincipal
    var approver = "HR_Admin_01";
    await service.ApproveAsync(id, amount, approver);
    return Results.NoContent();
});

// Rejects a tax declaration with a reason.
app.MapPut("/api/admin/declarations/{id}/reject", async (Guid id, [FromBody] string reason, ITDeclarationService service) =>
{
    var rejectedBy = "HR_Admin_01"; // In real app, resolve from JWT
    await service.RejectAsync(id, reason, rejectedBy);
    return Results.NoContent();
});

// ════════════════════════════════════════════════════════════════════════════
// PSA — Professional Services Automation API
// Client → Project → Resource → Revenue → Invoice
// ════════════════════════════════════════════════════════════════════════════

// Creates a new external client.
app.MapPost("/api/psa/clients", async ([FromBody] CreateClientRequest req, PSADbContext db) =>
{
    var client = Client.Create(req.Name, req.Gstin, req.BillingAddress, req.Currency);
    db.Clients.Add(client);
    await db.SaveChangesAsync();
    return Results.Created($"/api/psa/clients/{client.Id}", new { client.Id });
});

// Lists all active clients.
app.MapGet("/api/psa/clients", async (PSADbContext db) =>
{
    var clients = await db.Clients
        .Where(c => c.IsActive)
        .Select(c => new { c.Id, c.Name, c.Gstin, c.Currency })
        .ToListAsync();
    return Results.Ok(clients);
});

// Creates a project under a client.
app.MapPost("/api/psa/projects", async ([FromBody] CreateProjectRequest req, PSADbContext db) =>
{
    var project = ClientProject.Create(
        req.ClientId, req.Name,
        Enum.Parse<BillingType>(req.BillingType),
        DateOnly.Parse(req.StartDate),
        req.EndDate != null ? DateOnly.Parse(req.EndDate) : null);
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/psa/projects/{project.Id}", new { project.Id });
});

// Lists all projects for a client.
app.MapGet("/api/psa/projects", async (Guid clientId, PSADbContext db) =>
{
    var projects = await db.Projects
        .Where(p => p.ClientId == clientId && p.IsActive)
        .Select(p => new { p.Id, p.Name, p.BillingType, p.StartDate, p.EndDate })
        .ToListAsync();
    return Results.Ok(projects);
});

// Assigns an employee to a project with a billable rate.
app.MapPost("/api/psa/projects/{projectId}/resources", async (
    Guid projectId,
    [FromBody] AssignResourceRequest req,
    PSADbContext db) =>
{
    var resource = ProjectResource.Assign(
        req.EmployeeId, projectId,
        req.BillableRate, req.Currency,
        DateOnly.Parse(req.EffectiveFrom),
        req.IsBillable);
    db.ProjectResources.Add(resource);
    await db.SaveChangesAsync();
    return Results.Created($"/api/psa/resources/{resource.Id}", new { resource.Id });
});

// Returns projects the employee is currently assigned to (for timesheet dropdown).
app.MapGet("/api/psa/employees/{employeeId}/projects", async (
    Guid employeeId,
    PSADbContext db,
    ProjectResourceRepository repo) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var projects = await repo.GetAssignedProjectsAsync(employeeId, today);
    return Results.Ok(projects.Select(p => new { p.Id, p.Name, p.BillingType }));
});

// Generates a draft invoice for all unbilled revenue up to the cutoff date.
app.MapPost("/api/psa/invoices/generate", async (
    [FromBody] GenerateInvoiceRequest req,
    InvoiceGeneratorService svc,
    PSADbContext db) =>
{
    var cutoff = DateOnly.Parse(req.CutoffDate);
    var invoice = await svc.GenerateAsync(
        req.ClientId, cutoff, req.InvoiceNumberSeed, req.IsInterState);

    if (invoice == null) return Results.NoContent();

    // Generate and store PDF
    var client = await db.Clients.FindAsync(req.ClientId);
    if (client != null)
    {
        var pdfDoc = new InvoicePdfDocument(invoice, client);
        var pdfBytes = pdfDoc.ToPdfBytes();
        var pdfPath = Path.Combine("artifacts", "invoices", $"{invoice.InvoiceNumber}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
        await File.WriteAllBytesAsync(pdfPath, pdfBytes);
        invoice.AttachPdf(pdfPath);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.TotalAmount,
        invoice.Currency,
        LineCount = invoice.Lines.Count
    });
});

// Streams an invoice PDF.
app.MapGet("/api/psa/invoices/{id}/download", async (Guid id, PSADbContext db) =>
{
    var invoice = await db.Invoices.FindAsync(id);
    if (invoice == null) return Results.NotFound();
    if (string.IsNullOrEmpty(invoice.PdfPath) || !File.Exists(invoice.PdfPath))
        return Results.Problem("Invoice PDF not yet generated.");

    var pdfBytes = await File.ReadAllBytesAsync(invoice.PdfPath);
    return Results.File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
});

// ════════════════════════════════════════════════════════════════════════════
// Analytics — Profitability Engine API
// Real-time financial intelligence for CFOs
// ════════════════════════════════════════════════════════════════════════════

// Project profitability overview (reads from pre-aggregated table ONLY).
app.MapGet("/api/analytics/projects", async (int? year, PSADbContext db) =>
{
    var targetYear = year ?? DateTime.UtcNow.Year;

    var metrics = await db.MonthlyMetrics
        .Where(m => m.Year == targetYear)
        .GroupBy(m => m.ProjectId)
        .Select(g => new
        {
            ProjectId = g.Key,
            Revenue = g.Sum(m => m.TotalRevenue),
            Cost = g.Sum(m => m.TotalCost),
            BillableHours = g.Sum(m => m.BillableHours),
            TotalHours = g.Sum(m => m.TotalHours)
        })
        .ToListAsync();

    var result = metrics.Select(m =>
    {
        var profit = m.Revenue - m.Cost;
        return new
        {
            m.ProjectId,
            m.Revenue,
            m.Cost,
            Profit = profit,
            Margin = m.Revenue == 0 ? 0 : Math.Round(profit / m.Revenue * 100, 2),
            m.BillableHours,
            Utilization = m.TotalHours == 0 ? 0 : Math.Round(m.BillableHours / m.TotalHours * 100, 2)
        };
    });

    return Results.Ok(result);
});

// Monthly trend for a specific project.
app.MapGet("/api/analytics/projects/{projectId}/trend", async (Guid projectId, PSADbContext db) =>
{
    var trend = await db.MonthlyMetrics
        .Where(m => m.ProjectId == projectId)
        .OrderBy(m => m.Year).ThenBy(m => m.Month)
        .Select(m => new
        {
            m.Year,
            m.Month,
            m.TotalRevenue,
            m.TotalCost,
            Profit = m.TotalRevenue - m.TotalCost,
            m.BillableHours
        })
        .ToListAsync();
    return Results.Ok(trend);
});

// What-if simulation (stateless — never persisted).
app.MapPost("/api/analytics/simulate", ([FromBody] SimulateRequest req) =>
{
    var result = SimulationService.Simulate(req.CurrentRevenue, req.CurrentCost, req.RateChangePercent, req.CostChangePercent);
    return Results.Ok(result);
});

// Pricing recommendation for a project.
app.MapGet("/api/analytics/pricing/{projectId}", async (
    Guid projectId,
    PSADbContext db,
    PricingEngine engine,
    EmployeeCostService costSvc) =>
{
    // Get the average billable rate for the project.
    var avgRate = await db.ProjectResources
        .Where(r => r.ProjectId == projectId && r.IsBillable)
        .AverageAsync(r => (decimal?)r.BillableRate) ?? 0;

    // Get the average cost-per-hour for employees on this project.
    var employeeIds = await db.ProjectResources
        .Where(r => r.ProjectId == projectId)
        .Select(r => r.EmployeeId)
        .Distinct()
        .ToListAsync();

    var now = DateTime.UtcNow;
    decimal totalCost = 0;
    int costCount = 0;
    foreach (var empId in employeeIds)
    {
        var cph = await costSvc.GetCostPerHourAsync(empId, now.Year, now.Month);
        if (cph.HasValue) { totalCost += cph.Value; costCount++; }
    }
    decimal avgCost = costCount > 0 ? totalCost / costCount : 0;

    var recommendation = PricingEngine.Recommend(avgCost, avgRate);
    return Results.Ok(recommendation);
});

// Anomaly detection.
app.MapGet("/api/analytics/anomalies", async (PSADbContext db) =>
{
    var now = DateTime.UtcNow;
    var currentMonth = now.Month;
    var currentYear = now.Year;
    var prevMonth = currentMonth == 1 ? 12 : currentMonth - 1;
    var prevYear = currentMonth == 1 ? currentYear - 1 : currentYear;

    var current = await db.MonthlyMetrics
        .Where(m => m.Year == currentYear && m.Month == currentMonth)
        .ToListAsync();

    var previous = await db.MonthlyMetrics
        .Where(m => m.Year == prevYear && m.Month == prevMonth)
        .ToListAsync();

    var comparisons = current.Select(c =>
    {
        var p = previous.FirstOrDefault(x => x.ProjectId == c.ProjectId);
        return new MonthComparison(
            c.ProjectId,
            c.TotalRevenue, c.TotalCost, c.TotalRevenue - c.TotalCost,
            p?.TotalRevenue ?? 0, p?.TotalCost ?? 0,
            p != null ? p.TotalRevenue - p.TotalCost : 0);
    });

    var anomalies = AnomalyDetectionService.Detect(comparisons);
    return Results.Ok(anomalies);
});

// AR Aging report.
app.MapGet("/api/analytics/cashflow/aging", async (CashFlowService svc) =>
{
    var aging = await svc.GetAgingAsync();
    return Results.Ok(aging);
});

// Cash flow forecast.
app.MapGet("/api/analytics/cashflow/forecast", async (CashFlowService svc) =>
{
    var forecast = await svc.ForecastAsync();
    return Results.Ok(forecast);
});

// Client profitability (joins through projects).
app.MapGet("/api/analytics/clients", async (int? year, PSADbContext db) =>
{
    var targetYear = year ?? DateTime.UtcNow.Year;

    var projectClients = await db.Projects
        .Select(p => new { p.Id, p.ClientId, p.Name })
        .ToListAsync();

    var metrics = await db.MonthlyMetrics
        .Where(m => m.Year == targetYear)
        .ToListAsync();

    var clientMetrics = metrics
        .Join(projectClients, m => m.ProjectId, p => p.Id, (m, p) => new { p.ClientId, m.TotalRevenue, m.TotalCost })
        .GroupBy(x => x.ClientId)
        .Select(g => new
        {
            ClientId = g.Key,
            Revenue = g.Sum(x => x.TotalRevenue),
            Cost = g.Sum(x => x.TotalCost),
            Profit = g.Sum(x => x.TotalRevenue - x.TotalCost)
        })
        .ToList();

    return Results.Ok(clientMetrics);
});

// --- Billing & Invoice API ---

app.MapPost("/api/billing/contracts", async (
    CreateBillingContractRequest request,
    Karamchari.Billing.Persistence.BillingDbContext db,
    ITenantProvider tenantProvider) =>
{
    var tenant = tenantProvider.GetTenant();
    var contract = Karamchari.Billing.Domain.Contracts.BillingContract.Create(
        tenant.TenantId, request.ClientId, request.ProjectId, request.BillingType);
    
    contract.Currency = request.Currency;
    contract.BillingCycle = request.Cycle;
    if (!string.IsNullOrEmpty(request.StartDate))
        contract.StartDate = DateOnly.Parse(request.StartDate);

    db.Contracts.Add(contract);
    await db.SaveChangesAsync();
    return Results.Created($"/api/billing/contracts/{contract.Id}", contract);
});

app.MapPost("/api/billing/contracts/{id}/rates", async (
    Guid id,
    AddRateCardRequest request,
    Karamchari.Billing.Persistence.BillingDbContext db) =>
{
    var contract = await db.Contracts.Include(c => c.RateCards).FirstOrDefaultAsync(c => c.Id == id);
    if (contract == null) return Results.NotFound();

    contract.AddRate(
        request.RoleId, 
        request.Rate, 
        request.Unit, 
        DateOnly.Parse(request.EffectiveFrom), 
        request.EffectiveTo != null ? DateOnly.Parse(request.EffectiveTo) : null);

    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/billing/roles", async (
    MapEmployeeRoleRequest request,
    Karamchari.Billing.Persistence.BillingDbContext db,
    ITenantProvider tenantProvider) =>
{
    var tenant = tenantProvider.GetTenant();
    var roleMapping = new Karamchari.Billing.Domain.Contracts.EmployeeRole
    {
        TenantId = tenant.TenantId,
        EmployeeId = request.EmployeeId,
        RoleId = request.RoleId,
        ProjectId = request.ProjectId,
        EffectiveFrom = DateOnly.Parse(request.EffectiveFrom),
        EffectiveTo = request.EffectiveTo != null ? DateOnly.Parse(request.EffectiveTo) : null
    };

    db.EmployeeRoles.Add(roleMapping);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/billing/invoices/generate", async (
    GenerateInvoiceRunRequest request,
    Karamchari.Billing.Services.InvoiceGeneratorService generator) =>
{
    var invoice = await generator.GenerateDraftAsync(
        request.ContractId, 
        DateOnly.Parse(request.StartDate), 
        DateOnly.Parse(request.EndDate));

    return invoice != null ? Results.Ok(invoice) : Results.NoContent();
});

app.MapPost("/api/billing/invoices/{id}/finalize", async (
    Guid id,
    FinalizeInvoiceRequest request,
    Karamchari.Billing.Persistence.BillingDbContext db,
    IPublishEndpoint publishEndpoint) =>
{
    var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);
    if (invoice == null) return Results.NotFound();

    // Generate snapshot for legal freeze
    var snapshot = System.Text.Json.JsonSerializer.Serialize(new {
        invoice.InvoiceNumber,
        invoice.PeriodStart,
        invoice.PeriodEnd,
        Lines = invoice.Lines.Select(l => new { l.Description, l.Quantity, l.Rate, l.Amount }),
        invoice.TotalAmount,
        invoice.TaxAmount,
        invoice.GrandTotal
    });

    invoice.Finalize(request.InvoiceNumber, "Admin", snapshot);
    
    await db.SaveChangesAsync();

    // Emit event to update analytics loop
    await publishEndpoint.Publish(new Karamchari.Billing.Contracts.InvoiceIssuedIntegrationEvent(
        Guid.NewGuid(),
        invoice.Id,
        invoice.ContractId,
        invoice.TenantId,
        invoice.TotalAmount,
        invoice.TaxAmount,
        DateTimeOffset.UtcNow));

    return Results.Ok(new { invoice.InvoiceNumber, Status = "Finalized" });
});

app.MapGet("/api/billing/ar/summary", async (
    Karamchari.Billing.Services.ARAnalyticsService arService,
    ITenantProvider tenantProvider) =>
{
    var tenant = tenantProvider.GetTenant();
    var summary = await arService.GetSummaryAsync(tenant.TenantId);
    return Results.Ok(summary);
});

app.MapPost("/api/billing/invoices/{id}/payment", async (
    Guid id,
    RecordPaymentRequest request,
    Karamchari.Billing.Persistence.BillingDbContext db,
    IPublishEndpoint publishEndpoint) =>
{
    var invoice = await db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id);
    if (invoice == null) return Results.NotFound();

    invoice.RecordPayment(request.Amount, request.PaidAt);
    await db.SaveChangesAsync();

    // Emit payment event
    await publishEndpoint.Publish(new Karamchari.Billing.Contracts.PaymentReceivedIntegrationEvent(
        Guid.NewGuid(),
        Guid.NewGuid(),
        invoice.Id,
        invoice.ContractId,
        invoice.TenantId,
        request.Amount,
        DateTimeOffset.UtcNow));

    return Results.Ok();
});

// --- Collections API ---

app.MapGet("/api/collections", async (
    [FromQuery] Karamchari.Billing.Domain.Collections.CollectionStatus? status,
    Karamchari.Billing.Persistence.BillingDbContext db,
    ITenantProvider tenantProvider) =>
{
    var tenant = tenantProvider.GetTenant();
    var query = db.CollectionCases
        .Where(c => c.TenantId == tenant.TenantId);

    if (status.HasValue)
        query = query.Where(c => c.Status == status.Value);

    var cases = await query
        .OrderByDescending(c => c.DaysOutstanding)
        .ToListAsync();

    return Results.Ok(cases);
});

app.MapPut("/api/collections/{id}/dispute", async (
    Guid id,
    Karamchari.Billing.Persistence.BillingDbContext db) =>
{
    var @case = await db.CollectionCases.FindAsync(id);
    if (@case == null) return Results.NotFound();

    @case.MarkDisputed();
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/collections/{id}/remind", async (
    Guid id,
    Karamchari.Billing.Persistence.BillingDbContext db,
    ILoggerFactory loggerFactory) =>
{
    var @case = await db.CollectionCases.FindAsync(id);
    if (@case == null) return Results.NotFound();

    // Trigger manual reminder
    @case.RecordReminder("manual");
    await db.SaveChangesAsync();
    
    return Results.Ok(new { LastActionAt = @case.LastActionAt });
});

// --- Forecasting API ---

app.MapGet("/api/forecast/summary", async (
    Karamchari.Forecasting.Services.ForecastingEngine engine,
    ITenantProvider tenantProvider) =>
{
    var tenant = tenantProvider.GetTenant();
    var summary = await engine.GetSummaryAsync(tenant.TenantId);
    return Results.Ok(summary);
});

// --- CFO / Project Analytics (Pre-aggregated Columnstore) ---

// High-performance daily project metrics for CFO dashboards.
// Queries against Analytics_ProjectMetrics (clustered columnstore indexed).
app.MapGet("/api/analytics/projects/daily", async (
    [FromQuery] Guid? projectId,
    [FromQuery] string? startDate,
    [FromQuery] string? endDate,
    TimeAttendanceDbContext db) =>
{
    // Default to last 30 days if no range provided
    var start = string.IsNullOrEmpty(startDate) 
        ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)) 
        : DateOnly.Parse(startDate);
        
    var end = string.IsNullOrEmpty(endDate) 
        ? DateOnly.FromDateTime(DateTime.UtcNow) 
        : DateOnly.Parse(endDate);

    var query = db.ProjectMetrics
        .Where(m => m.Date >= start && m.Date <= end);

    if (projectId.HasValue)
    {
        query = query.Where(m => m.ProjectId == projectId.Value);
    }

    var metrics = await query
        .OrderByDescending(m => m.Date)
        .ToListAsync();

    // metadata for "Updated X seconds ago" UI trust
    var lastUpdated = metrics.Max(m => (DateTimeOffset?)m.LastUpdatedAt) ?? DateTimeOffset.UtcNow;

    return Results.Ok(new 
    { 
        Data = metrics, 
        LastUpdatedAt = lastUpdated 
    });
});


// SignalR hub endpoint.
app.MapHub<AnalyticsHub>("/hubs/analytics");

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

// ── BFF Workspace APIs (ADR-0011) ─────────────────────────────────────────────
app.MapManagerWorkspace();
app.MapEmployeeWorkspace();
app.MapHRWorkspace();
app.MapExecutiveWorkspace();
app.MapNotificationCenter();
app.MapExportJobs();
app.MapSearch();

app.Run();

namespace Karamchari.Api
{
    internal static class ProofAllowedExtensions
    {
        private static readonly string[] Values = [".pdf", ".jpg", ".jpeg", ".png"];
        public static bool Contains(string ext) => Array.IndexOf(Values, ext) >= 0;
    }

    /// <summary>Request body for mapping a DLQ punch to an employee.</summary>
    public record MapDlqRequest(Guid EmployeeId);

    /// <summary>Request body for reviewing a fraud flag.</summary>
    public record ReviewFraudRequest(Karamchari.TimeAttendance.Domain.IoT.FraudStatus Status, string Notes);

    /// <summary>Request body for triggering bulk attendance reprocessing.</summary>
    public record ReprocessAttendanceRequest(Guid? EmployeeId, string FromDate, string ToDate);

    /// <summary>Request body for marking a compliance return as filed.</summary>
    /// <param name="RunId">The unique identifier of the payroll run.</param>
    /// <param name="Type">The type of compliance return (EPF, ESIC, TDS).</param>
    /// <param name="ReferenceNumber">The government-issued reference or challan number.</param>
    public record MarkFiledRequest(Guid RunId, Karamchari.Payroll.Domain.Compliance.ComplianceType Type, string ReferenceNumber);

    /// <summary>Represents a punch request from the mobile application.</summary>
    public record MobilePunchRequest(
        string EmployeeId,
        DateTime Timestamp,
        double Lat,
        double Lon,
        double Accuracy,
        bool IsOfflineCaptured,
        string ClientId,
        Karamchari.TimeAttendance.Domain.IoT.PunchType Type);

    /// <summary>Ephemeral repository for stateless tax simulations.</summary>
    public class StaticDeclarationRepository : IITDeclarationRepository
    {
        private readonly IReadOnlyList<ITDeclaration> _declarations;

        public StaticDeclarationRepository(IEnumerable<ITDeclaration> declarations) =>
            _declarations = declarations.ToList();

        public Task<IReadOnlyList<ITDeclaration>> GetApprovedDeclarationsAsync(Guid employeeId, int financialYear)
            => Task.FromResult<IReadOnlyList<ITDeclaration>>(_declarations);

        public Task<ITDeclaration?> GetLatestAsync(Guid employeeId, int financialYear, string category)
            => Task.FromResult(_declarations.FirstOrDefault(d => d.Category == category));

        public Task<List<ITDeclaration>> GetPendingReviewAsync() => Task.FromResult(new List<ITDeclaration>());
        public Task<ITDeclaration?> GetByIdAsync(Guid id) => Task.FromResult<ITDeclaration?>(null);
        public Task UpsertAsync(ITDeclaration declaration) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    public record TaxSimulationRequest(decimal Section80C, decimal Section80D, decimal MonthlyRent, bool IsMetro, int FinancialYear);
    public record TaxSimulationResult(decimal OldRegimeTax, decimal NewRegimeTax, decimal Difference, string RecommendedRegime, string Reason);
    /// <summary>
    /// Request contract for starting a payroll run.
    /// </summary>
    /// <param name="PeriodName">The name of the payroll period (e.g., "April 2027").</param>
    public record StartPayrollRunRequest(string PeriodName);

    /// <summary>
    /// Request contract for adding a new holiday.
    /// </summary>
    /// <param name="Name">The name of the holiday (e.g., "Independence Day").</param>
    /// <param name="Date">The date of the holiday.</param>
    public record AddHolidayRequest(string Name, DateOnly Date);

    /// <summary>
    /// Request contract for creating a new leave policy.
    /// </summary>
    /// <param name="Name">The name.</param>
    /// <param name="Description">The description.</param>
    /// <param name="Rules">The rules.</param>
    public record CreateLeavePolicyRequest(string Name, string Description, LeavePolicyRules Rules);

    /// <summary>
    /// Request contract for submitting a leave request.
    /// </summary>
    public record SubmitLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);

    /// <summary>
    /// Request contract for dry-running leave day calculation.
    /// </summary>
    /// <param name="PolicyId">The policy identifier.</param>
    /// <param name="StartDate">The start date.</param>
    /// <param name="EndDate">The end date.</param>
    public record CalculateLeaveDaysRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate);

    /// <summary>
    /// Request contract for provisioning a new tenant.
    /// </summary>
    /// <param name="CompanyName">The name of the company.</param>
    /// <param name="AdminEmail">The email address of the primary administrator.</param>
    public record ProvisionTenantRequest(string CompanyName, string AdminEmail);

    /// <summary>
    /// Request contract for submitting a weekly timesheet.
    /// </summary>
    /// <param name="WeekStartDate">The start of the week.</param>
    /// <param name="Entries">The list of daily time entries.</param>
    public record SubmitTimesheetRequest(DateOnly WeekStartDate, List<TimeEntryDto> Entries);

    /// <summary>
    /// DTO for a single time entry in a request.
    /// </summary>
    /// <param name="Date">The date.</param>
    /// <param name="Hours">The billable hours.</param>
    /// <param name="Description">The work description.</param>
    public record TimeEntryDto(DateOnly Date, decimal Hours, string? Description);

    /// <summary>
    /// Request contract for rejecting a timesheet.
    /// </summary>
    /// <param name="Reason">The reason for rejection.</param>
    public record RejectTimesheetRequest(string Reason);

    /// <summary>
    /// Request contract for creating a master salary component.
    /// </summary>
    public record CreateSalaryComponentRequest(string Name, ComponentType Type, RoundingStrategy Rounding);

    /// <summary>
    /// Request contract for creating a salary template.
    /// </summary>
    public record CreateSalaryTemplateRequest(string Name, List<SalaryTemplateComponent> Components);

    /// <summary>
    /// Request contract for calculating a CTC breakdown.
    /// </summary>
    public record CalculateCTCBreakdownRequest(decimal AnnualCTC, Dictionary<Guid, decimal>? Overrides);

    /// <summary>
    /// Request contract for a full statutory pay calculation.
    /// </summary>
    public record CalculateStatutoryRequest(
        decimal AnnualCTC, 
        Guid? EmployeeId, 
        List<Guid> EpfBaseComponentIds, 
        Dictionary<Guid, decimal>? Overrides);

    /// <summary>
    /// Request contract for locking a payroll run.
    /// </summary>
    /// <param name="ApprovedBy">The name of the approver.</param>
    public record LockPayrollRunRequest(string ApprovedBy);

    // ── PSA Request DTOs ──────────────────────────────────────────────────────

    /// <summary>Creates a new external billing client.</summary>
    public record CreateClientRequest(
        string Name,
        string Gstin,
        string BillingAddress,
        string Currency = "INR");

    /// <summary>Creates a project under a client.</summary>
    public record CreateProjectRequest(
        Guid ClientId,
        string Name,
        string BillingType,     // "TimeAndMaterial" | "FixedBid" | "NonBillable"
        string StartDate,       // ISO 8601: "2026-04-01"
        string? EndDate = null);

    /// <summary>Assigns an employee to a project with a time-bound billable rate.</summary>
    public record AssignResourceRequest(
        Guid EmployeeId,
        decimal BillableRate,
        string Currency,
        string EffectiveFrom,   // ISO 8601: "2026-04-01"
        bool IsBillable = true);

    /// <summary>Triggers invoice generation for a client up to a cutoff date.</summary>
    public record GenerateInvoiceRequest(
        Guid ClientId,
        string CutoffDate,          // ISO 8601: "2026-04-30"
        string InvoiceNumberSeed,   // e.g. "INV-2026-042"
        bool IsInterState = false);

    // ── Billing Request DTOs ──────────────────────────────────────────────────

    public record CreateBillingContractRequest(
        Guid ClientId,
        Guid ProjectId,
        Karamchari.Billing.Domain.Contracts.BillingType BillingType,
        string Currency = "INR",
        string? StartDate = null,
        Karamchari.Billing.Domain.Contracts.BillingCycle Cycle = Karamchari.Billing.Domain.Contracts.BillingCycle.Monthly);

    public record AddRateCardRequest(
        Guid RoleId,
        decimal Rate,
        Karamchari.Billing.Domain.Contracts.RateUnit Unit,
        string EffectiveFrom,
        string? EffectiveTo = null);

    public record MapEmployeeRoleRequest(
        Guid EmployeeId,
        Guid RoleId,
        Guid? ProjectId,
        string EffectiveFrom,
        string? EffectiveTo = null);

    public record GenerateInvoiceRunRequest(
        Guid ContractId,
        string StartDate,
        string EndDate);

    public record FinalizeInvoiceRequest(string InvoiceNumber);

    public record RecordPaymentRequest(decimal Amount, DateTimeOffset PaidAt);

    /// <summary>What-if simulation inputs. Never persisted.</summary>
    public record SimulateRequest(
        decimal CurrentRevenue,
        decimal CurrentCost,
        decimal RateChangePercent = 0,
        decimal CostChangePercent = 0);
}
