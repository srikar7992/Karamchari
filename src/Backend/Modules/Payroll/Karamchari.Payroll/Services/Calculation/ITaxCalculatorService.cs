namespace Karamchari.Payroll.Services.Calculation;

public sealed record TaxCalculationInput(
    Guid EmployeeId,
    string TaxRegime,
    int Month,
    int FinancialYear,
    decimal YearToDateGross,
    decimal YearToDateTds,
    decimal CurrentMonthGross,
    decimal PfDeduction,
    decimal ProfessionalTax,
    decimal DeclaredInvestments);

public sealed record TaxCalculationOutput(
    decimal TaxableIncome,
    decimal TaxLiability,
    decimal TdsThisMonth,
    string RuleVersionId);

/// <summary>
/// Service interface for tax calculation. Loaded with the FY-appropriate TaxRuleVersion.
/// </summary>
public interface ITaxCalculatorService
{
    TaxCalculationOutput Calculate(TaxCalculationInput input);
}
