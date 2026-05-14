using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Reimbursements;

/// <summary>
/// Consumer for the SubmitExpenseClaim command.
/// Handles the persistence of new expense claims and triggers subsequent workflow events.
/// </summary>
public sealed class SubmitExpenseClaimConsumer : IConsumer<SubmitExpenseClaim>
{
    private readonly FinancialOpsDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SubmitExpenseClaimConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitExpenseClaimConsumer"/> class.
    /// </summary>
    /// <param name="db">The financial ops DB context.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    public SubmitExpenseClaimConsumer(
        FinancialOpsDbContext db,
        ITenantProvider tenantProvider,
        ILogger<SubmitExpenseClaimConsumer> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SubmitExpenseClaim> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var command = context.Message;
        var tenant = _tenantProvider.GetTenant()
            ?? throw new InvalidOperationException("Tenant context is required to submit an expense claim.");

        var tenantId = tenant.TenantId;

        _logger.LogInformation(
            "Processing Expense Claim Submission: {ClaimNumber} for Employee {EmployeeId} in Tenant {TenantId}",
            command.ClaimNumber, command.EmployeeId, tenantId);

        // Domain Logic: Map command to Aggregate Root
        var claim = ExpenseClaim.Submit(
            tenantId,
            new EmployeeId(command.EmployeeId),
            new ExpenseCategoryId(command.CategoryId),
            command.ClaimNumber,
            command.Amount,
            new CurrencyCode(command.Currency),
            command.ExpenseDate,
            1, // Initial policy version
            command.TaxTreatment,
            command.PreferredSettlementType);

        _db.Set<ExpenseClaim>().Add(claim);
        await _db.SaveChangesAsync(context.CancellationToken);

        // Publish success event
        await context.Publish(new ExpenseClaimSubmitted(
            claim.Id.Value,
            claim.TenantId,
            claim.EmployeeId.Value,
            claim.ClaimedAmount,
            claim.Currency.Value), context.CancellationToken);
    }
}
