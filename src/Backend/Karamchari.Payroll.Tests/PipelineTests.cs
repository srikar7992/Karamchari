namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using NSubstitute;

public class PipelineTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Fact]
    public void PipelineShouldExecuteRulesAndDeductFromGross()
    {
        // Arrange
        var basicMonthly = 10000m; // EPF = 1200
        var grossMonthly = 12000m; // ESIC = Ceil(12000 * 0.0075) = 90
        
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(
            AnnualCTC: 144000,
            MonthlyGross: grossMonthly,
            MonthlyBreakdown: new Dictionary<Guid, decimal> { { BasicId, basicMonthly } },
            AnnualBreakdown: new Dictionary<Guid, decimal>());

        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        
        // Mock RuleSet
        var rules = new List<IStatutoryRule>
        {
            new EpfStatutoryRule(new List<Guid> { BasicId }),
            new EsicStatutoryRule()
        };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = StatutoryPipelineEngine.Execute(context, ruleSet);

        // Assert
        Assert.Equal(1200, result.Deductions["EPF_Employee"]);
        Assert.Equal(90, result.Deductions["ESIC_Employee"]);
        
        decimal totalDeductions = 1200 + 90;
        Assert.Equal(grossMonthly - totalDeductions, result.NetPay);
    }

    [Fact]
    public void PipelineShouldIncludeProfessionalTaxIfConfigured()
    {
        // Arrange
        var grossMonthly = 25000m;
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(300000, grossMonthly, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);

        var ptProvider = Substitute.For<IProfessionalTaxProvider>();
        ptProvider.GetTaxAmount(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<FinancialYear>())
            .Returns(new ProfessionalTaxResult(200m, true, "Test Slab"));

        var rules = new List<IStatutoryRule> { new ProfessionalTaxRule(ptProvider) };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = StatutoryPipelineEngine.Execute(context, ruleSet);

        // Assert
        Assert.Equal(200, result.Deductions["ProfessionalTax"]);
        Assert.Equal(grossMonthly - 200, result.NetPay);
    }

    [Fact]
    public void PipelineShouldBeDeterministic()
    {
        // Arrange
        var context = CreateDefaultContext();
        var rules = new List<IStatutoryRule> { new EpfStatutoryRule(new List<Guid>()) };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result1 = StatutoryPipelineEngine.Execute(context, ruleSet);
        var result2 = StatutoryPipelineEngine.Execute(context, ruleSet);

        // Assert
        Assert.Equal(result1.NetPay, result2.NetPay);
        Assert.Equal(result1.Deductions, result2.Deductions);
    }

    private static StatutoryContext CreateDefaultContext()
    {
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(120000, 10000, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        return new StatutoryContext(breakdown, profile, FY2026, 4);
    }

    private sealed class MockRuleSet : IStatutoryRuleSet
    {
        public MockRuleSet(FinancialYear year, IReadOnlyList<IStatutoryRule> rules)
        {
            Year = year;
            Rules = rules;
        }
        public FinancialYear Year { get; }
        public IReadOnlyList<IStatutoryRule> Rules { get; }
    }
}
