// -----------------------------------------------------------------------
// <copyright file="GeneratePayslipConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V2;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Services.Payslip;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consumes the payroll completion event to generate and store a PDF payslip.
/// </summary>
public sealed class GeneratePayslipConsumer : IConsumer<PayrollRunCompletedIntegrationEventV1>
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
    public async Task Consume(ConsumeContext<PayrollRunCompletedIntegrationEventV1> context)
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

        // 2. Fetch YTD totals scoped to the financial year of this run. The event now carries
        //    FinancialYearStart, so YTD aggregates the correct April-March window rather than the
        //    calendar year. Legacy messages (FinancialYearStart == 0, produced before the field
        //    existed) fall back to the calendar year to limit the result set.
        var ytdLedgerEntries = message.FinancialYearStart > 0
            ? await _dbContext.PayrollLedger
                .Where(e => e.EmployeeId == message.EmployeeId && e.FinancialYearStart == message.FinancialYearStart)
                .ToListAsync(context.CancellationToken)
            : await _dbContext.PayrollLedger
                .Where(e => e.EmployeeId == message.EmployeeId && e.Year == DateTime.UtcNow.Year)
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
