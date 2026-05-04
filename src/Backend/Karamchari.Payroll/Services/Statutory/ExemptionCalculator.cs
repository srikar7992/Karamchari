namespace Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Implements standard Indian tax exemptions (Section 10).
/// </summary>
public sealed class ExemptionCalculator : IExemptionCalculator
{
    /// <inheritdoc/>
    public decimal CalculateHraExemption(decimal annualBasic, decimal annualHra, decimal annualRentPaid, bool isMetro)
    {
        if (annualHra <= 0 || annualRentPaid <= 0) return 0;

        // HRA Exemption is MIN of:
        // 1. Actual HRA received
        // 2. 50% of Basic (Metro) or 40% (Non-Metro)
        // 3. Actual Rent Paid - 10% of Basic

        decimal percentageOfBasic = isMetro ? 0.5m : 0.4m;
        decimal baseExemption1 = annualHra;
        decimal baseExemption2 = annualBasic * percentageOfBasic;
        decimal baseExemption3 = Math.Max(0, annualRentPaid - (annualBasic * 0.1m));

        return Math.Min(baseExemption1, Math.Min(baseExemption2, baseExemption3));
    }
}
