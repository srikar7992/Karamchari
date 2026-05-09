using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Payroll.Services.FnF;

public record FnFCalculationInput(
    Guid TenantId,
    Guid EmployeeId,
    DateOnly LastWorkingDay,
    FnFExitType ExitType,
    int NoticePeriodDays,
    int ActualNoticeDays,
    decimal PendingSalaryDays,
    decimal LeaveBalance,
    decimal GratuityYearsOfService);

public record FnFCalculationOutput(
    IReadOnlyList<FnFLineItem> LineItems,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal NetSettlement);

/// <summary>
/// Calculates FnF line items from employee exit parameters.
/// Stateless — pure calculation. Caller persists result to FnFSettlement aggregate.
/// </summary>
public sealed class FnFCalculationService
{
    private readonly PayrollDbContext _db;

    public FnFCalculationService(PayrollDbContext db)
    {
        _db = db;
    }

    public async Task<FnFCalculationOutput> CalculateAsync(
        FnFCalculationInput input,
        CancellationToken ct)
    {
        var profile = await _db.PayrollProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == input.EmployeeId, ct)
            ?? throw new InvalidOperationException($"No payroll profile for employee {input.EmployeeId}.");

        var monthlySalary = profile.BaseSalary;
        var dailySalary = monthlySalary / 26m;  // industry standard: 26 working days

        var lineItems = new List<FnFLineItem>();

        // 1. Pending salary
        if (input.PendingSalaryDays > 0)
        {
            lineItems.Add(FnFLineItem.Create(
                FnFLineItemType.PendingSalary,
                $"Pending salary for {input.PendingSalaryDays:0.##} days",
                dailySalary * input.PendingSalaryDays,
                isDeduction: false,
                isTaxable: true));
        }

        // 2. Leave encashment (earned leave balance)
        if (input.LeaveBalance > 0)
        {
            var encashment = dailySalary * input.LeaveBalance;
            lineItems.Add(FnFLineItem.Create(
                FnFLineItemType.LeaveEncashment,
                $"Leave encashment for {input.LeaveBalance:0.##} days",
                encashment,
                isDeduction: false,
                isTaxable: false));  // leave encashment taxability depends on regime/type
        }

        // 3. Gratuity (eligible if >= 5 years of service, Gratuity Act 1972)
        if (input.GratuityYearsOfService >= 5)
        {
            var gratuity = (monthlySalary / 26m) * 15m * input.GratuityYearsOfService;
            lineItems.Add(FnFLineItem.Create(
                FnFLineItemType.Gratuity,
                $"Gratuity for {input.GratuityYearsOfService:0.##} years",
                gratuity,
                isDeduction: false,
                isTaxable: false));
        }

        // 4. Notice period recovery (if short-served)
        if (input.NoticePeriodDays > input.ActualNoticeDays)
        {
            var shortfallDays = input.NoticePeriodDays - input.ActualNoticeDays;
            var recovery = dailySalary * shortfallDays;
            lineItems.Add(FnFLineItem.Create(
                FnFLineItemType.NoticePeriodRecovery,
                $"Notice period shortfall recovery ({shortfallDays} days)",
                recovery,
                isDeduction: true));
        }

        // 5. Outstanding loan recovery
        var activeLoans = await _db.Set<EmployeeLoan>()
            .AsNoTracking()
            .Where(l => l.EmployeeId == input.EmployeeId && l.Status == LoanStatus.Active)
            .ToListAsync(ct);

        foreach (var loan in activeLoans)
        {
            if (loan.OutstandingBalance > 0)
            {
                lineItems.Add(FnFLineItem.Create(
                    FnFLineItemType.LoanRecovery,
                    $"Loan recovery — outstanding balance (Loan #{loan.Id:N})",
                    loan.OutstandingBalance,
                    isDeduction: true,
                    reference: loan.Id.ToString()));
            }
        }

        var earnings = lineItems.Where(l => !l.IsDeduction).Sum(l => l.Amount);
        var deductions = lineItems.Where(l => l.IsDeduction).Sum(l => l.Amount);

        return new FnFCalculationOutput(lineItems, earnings, deductions, earnings - deductions);
    }
}
