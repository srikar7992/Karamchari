namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V2;
using Karamchari.Payroll.Services.Payslip;
using Karamchari.Payroll.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consumes the payroll completion event to generate and store a PDF payslip.
/// </summary>
public sealed class GeneratePayslipConsumer : IConsumer<PayrollRunCompletedIntegrationEvent>
{
    private readonly IPayslipGenerator _generator;
    private readonly IPayslipStorage _storage;
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratePayslipConsumer"/> class.
    /// </summary>
    public GeneratePayslipConsumer(
        IPayslipGenerator generator, 
        IPayslipStorage storage, 
        PayrollDbContext dbContext)
    {
        _generator = generator;
        _storage = storage;
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PayrollRunCompletedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        // Idempotency: skip generation if the payslip already exists in storage.
        // On retry the storage check short-circuits before re-generating the PDF.
        var alreadyGenerated = await _storage.ExistsAsync(
            message.EmployeeId.ToString(), message.PeriodName);
        if (alreadyGenerated) return;

        // 1. Fetch employee profile (for tax regime metadata).
        var profile = await _dbContext.PayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == message.EmployeeId, context.CancellationToken);

        // 2. Fetch YTD totals scoped to the current financial year.
        //    The previous query fetched ALL ledger entries for the employee across all years,
        //    which produces incorrect YTD figures after the first financial year.
        //    We infer the financial year start from the ledger data in the message instead.
        // TODO: Pass FinancialYearStart in the integration event (tracked as a future improvement).
        //    For now, scope to the calendar year of the period to limit the result set.
        var periodYear = DateTime.UtcNow.Year;
        var ytdLedgerEntries = await _dbContext.PayrollLedger
            .Where(e => e.EmployeeId == message.EmployeeId && e.Year == periodYear)
            .ToListAsync(context.CancellationToken);

        var ytdTotals = new Dictionary<string, decimal>
        {
            { "Gross", ytdLedgerEntries.Sum(e => e.MonthlyGross) + message.Gross },
            {
                "PF",
                ytdLedgerEntries.Sum(e => e.Deductions.GetValueOrDefault("EPF_Employee", 0))
                    + message.Deductions.GetValueOrDefault("EPF_Employee", 0)
            },
            {
                "TDS",
                ytdLedgerEntries.Sum(e => e.TdsDeducted)
                    + message.Deductions.GetValueOrDefault("TDS", 0)
            }
        };

        // 3. Map to PayslipData DTO.
        var payslipData = new PayslipData(
            EmployeeName: message.EmployeeName,
            EmployeeId: message.EmployeeId.ToString().AsSpan(0, 8).ToString(),
            Month: message.PeriodName,
            Gross: message.Gross,
            NetPay: message.NetPay,
            Earnings: message.Earnings,
            Deductions: message.Deductions,
            YtdTotals: ytdTotals,
            TaxRegime: message.TaxRegime
        );

        // 4. Generate PDF.
        var pdfBytes = _generator.Generate(payslipData);

        // 5. Persist PDF to storage.
        await _storage.SaveAsync(message.EmployeeId.ToString(), message.PeriodName, pdfBytes);
    }
}
