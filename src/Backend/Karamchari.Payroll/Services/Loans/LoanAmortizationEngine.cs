using Karamchari.Payroll.Domain.Loans;

namespace Karamchari.Payroll.Services.Loans;

/// <summary>
/// Generates amortization schedule for employee loans.
/// Supports flat-rate and reducing-balance interest methods.
/// </summary>
public sealed class LoanAmortizationEngine
{
    public record AmortizationResult(
        IReadOnlyList<LoanInstallment> Installments,
        decimal MonthlyEmi,
        decimal TotalInterest,
        decimal TotalPayable);

    public static AmortizationResult GenerateSchedule(
        EmployeeLoan loan,
        DateOnly disbursedOn)
    {
        ArgumentNullException.ThrowIfNull(loan);
        var installments = new List<LoanInstallment>();
        decimal emi;
        decimal totalInterest = 0m;

        if (loan.InterestType == LoanInterestType.ZeroInterest)
        {
            emi = Math.Round(loan.PrincipalAmount / loan.TenureMonths, 2, MidpointRounding.AwayFromZero);
            decimal outstanding = loan.PrincipalAmount;

            for (var i = 1; i <= loan.TenureMonths; i++)
            {
                var date = disbursedOn.AddMonths(i);
                var principal = i == loan.TenureMonths
                    ? outstanding
                    : emi;
                outstanding -= principal;

                installments.Add(LoanInstallment.Create(
                    number: i,
                    periodName: $"{date.Year}-{date.Month:D2}",
                    year: date.Year,
                    month: date.Month,
                    principal: principal,
                    interest: 0m,
                    outstandingAfter: Math.Max(0, outstanding)));
            }
        }
        else
        {
            // Reducing-balance EMI: EMI = P * r * (1+r)^n / ((1+r)^n - 1)
            var monthlyRate = loan.InterestRatePercent / 100m / 12m;
            var factor = (decimal)Math.Pow((double)(1 + monthlyRate), loan.TenureMonths);
            emi = Math.Round(loan.PrincipalAmount * monthlyRate * factor / (factor - 1), 2, MidpointRounding.AwayFromZero);

            decimal outstanding = loan.PrincipalAmount;

            for (var i = 1; i <= loan.TenureMonths; i++)
            {
                var date = disbursedOn.AddMonths(i);
                var interest = Math.Round(outstanding * monthlyRate, 2, MidpointRounding.AwayFromZero);
                var principal = i == loan.TenureMonths
                    ? outstanding
                    : emi - interest;
                outstanding -= principal;
                totalInterest += interest;

                installments.Add(LoanInstallment.Create(
                    number: i,
                    periodName: $"{date.Year}-{date.Month:D2}",
                    year: date.Year,
                    month: date.Month,
                    principal: principal,
                    interest: interest,
                    outstandingAfter: Math.Max(0, outstanding)));
            }
        }

        return new AmortizationResult(
            installments,
            emi,
            totalInterest,
            loan.PrincipalAmount + totalInterest);
    }
}
