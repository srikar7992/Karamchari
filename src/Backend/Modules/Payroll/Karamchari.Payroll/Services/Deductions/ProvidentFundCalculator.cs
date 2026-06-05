using Karamchari.Payroll.Domain.DeductionRules;

namespace Karamchari.Payroll.Services.Deductions;

/// <summary>
/// PF: 12% of Basic+DA, wage ceiling ₹15,000 unless voluntary PF opted.
/// Employer also contributes 12% (3.67% to EPF, 8.33% to EPS capped at ₹1,250).
/// </summary>
public sealed class ProvidentFundCalculator : IDeductionCalculator
{
    private const decimal WageCeiling = 15_000m;
    private const decimal EmployeeRate = 0.12m;
    private const decimal EmployerEpfRate = 0.0367m;
    private const decimal EmployerEpsMax = 1_250m;

    public StatutoryDeductionType DeductionType => StatutoryDeductionType.ProvidentFund;

    public decimal Calculate(DeductionContext context)
    {
        var pfWage = context.OptedVoluntaryPf
            ? context.BasicPay + context.DaAmount
            : Math.Min(context.BasicPay + context.DaAmount, WageCeiling);

        return Math.Round(pfWage * EmployeeRate, 0);
    }

    public decimal EmployerContribution(DeductionContext context)
    {
        var pfWage = Math.Min(context.BasicPay + context.DaAmount, WageCeiling);
        var epf = Math.Round(pfWage * EmployerEpfRate, 0);
        var eps = Math.Min(Math.Round(pfWage * 0.0833m, 0), EmployerEpsMax);
        return epf + eps;
    }
}
