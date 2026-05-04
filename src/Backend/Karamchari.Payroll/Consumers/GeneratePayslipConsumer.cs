namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts;
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

        // 1. Fetch Employee Details
        var profile = await _dbContext.PayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == message.EmployeeId);

        // 2. Fetch YTD Totals from Ledger
        var ledgerEntries = await _dbContext.PayrollLedger
            .Where(e => e.EmployeeId == message.EmployeeId)
            .ToListAsync();

        var ytdTotals = new Dictionary<string, decimal>
        {
            { "Gross", ledgerEntries.Sum(e => e.MonthlyGross) + message.Gross },
            { "PF", ledgerEntries.Sum(e => e.Deductions.GetValueOrDefault("EPF_Employee", 0)) + message.Deductions.GetValueOrDefault("EPF_Employee", 0) },
            { "TDS", ledgerEntries.Sum(e => e.TdsDeducted) + message.Deductions.GetValueOrDefault("TDS", 0) }
        };

        // 3. Map to PayslipData DTO
        var payslipData = new PayslipData(
            EmployeeName: string.Concat("Employee ", message.EmployeeId.ToString().AsSpan(0, 8)),
            EmployeeId: message.EmployeeId.ToString().AsSpan(0, 8).ToString(),
            Month: message.PeriodName,
            Gross: message.Gross,
            NetPay: message.NetPay,
            Earnings: message.Earnings,
            Deductions: message.Deductions,
            YtdTotals: ytdTotals,
            TaxRegime: message.TaxRegime
        );

        // 4. Generate PDF
        var pdfBytes = _generator.Generate(payslipData);

        // 5. Store PDF
        await _storage.SaveAsync(message.EmployeeId.ToString(), message.PeriodName, pdfBytes);
    }
}
