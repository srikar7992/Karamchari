namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using NSubstitute;

public class PipelineTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Fact]
    public async Task PipelineShouldExecuteRulesAndDeductFromGross()
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

        var rules = new List<IStatutoryRuleMetadata>
        {
            new EpfStatutoryRule(new List<Guid> { BasicId }),
            new EsicStatutoryRule()
        };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = await StatutoryPipelineEngine.ExecuteAsync(context, ruleSet);

        // Assert
        Assert.Equal(1200, result.Deductions["EPF_Employee"]);
        Assert.Equal(90, result.Deductions["ESIC_Employee"]);

        decimal totalDeductions = 1200 + 90;
        Assert.Equal(grossMonthly - totalDeductions, result.NetPay);
    }

    [Fact]
    public async Task PipelineShouldIncludeProfessionalTaxIfConfigured()
    {
        // Arrange
        var grossMonthly = 25000m;
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(300000, grossMonthly, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);

        var ptProvider = Substitute.For<IProfessionalTaxProvider>();
        ptProvider.GetTaxAmount(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<FinancialYear>())
            .Returns(new ProfessionalTaxResult(200m, true, "Test Slab"));

        var rules = new List<IStatutoryRuleMetadata> { new ProfessionalTaxRule(ptProvider) };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = await StatutoryPipelineEngine.ExecuteAsync(context, ruleSet);

        // Assert
        Assert.Equal(200, result.Deductions["ProfessionalTax"]);
        Assert.Equal(grossMonthly - 200, result.NetPay);
    }

    [Fact]
    public async Task PipelineShouldBeDeterministic()
    {
        // Arrange
        var context = CreateDefaultContext();
        var rules = new List<IStatutoryRuleMetadata> { new EpfStatutoryRule(new List<Guid>()) };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result1 = await StatutoryPipelineEngine.ExecuteAsync(context, ruleSet);

        // Reset ComputedValues by re-creating context (it is mutable during pipeline execution)
        var context2 = CreateDefaultContext();
        var result2 = await StatutoryPipelineEngine.ExecuteAsync(context2, ruleSet);

        // Assert
        Assert.Equal(result1.NetPay, result2.NetPay);
        Assert.Equal(result1.Deductions, result2.Deductions);
    }

    [Fact]
    public async Task PipelineShouldHandleMixedSyncAndAsyncRules()
    {
        // Arrange: EPF is sync (IStatutoryRule), TDS is async (IAsyncStatutoryRule)
        var grossMonthly = 50000m;
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(600000, grossMonthly, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);

        var projectionService = Substitute.For<IIncomeProjectionService>();
        projectionService.ProjectAnnualIncomeAsync(Arg.Any<StatutoryContext>())
            .Returns(Task.FromResult(new AnnualProjection(0, 0, 600000, 12)));

        var exemptionCalc = Substitute.For<IExemptionCalculator>();
        exemptionCalc.CalculateHraExemption(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<bool>())
            .Returns(0m);

        var taxSlabs = Substitute.For<ITaxSlabProvider>();
        taxSlabs.CalculateAnnualTax(Arg.Any<decimal>(), Arg.Any<TaxRegime>())
            .Returns(60000m); // Flat mock: ₹60k annual tax → ₹5000/mo

        var declarationRepo = Substitute.For<IITDeclarationRepository>();
        declarationRepo.GetApprovedDeclarationsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<ITDeclaration>>(Array.Empty<ITDeclaration>()));

        var rules = new List<IStatutoryRuleMetadata>
        {
            new EpfStatutoryRule(new List<Guid>()),                          // sync rule
            new TdsStatutoryRule(projectionService, exemptionCalc, taxSlabs, declarationRepo) // async rule
        };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = await StatutoryPipelineEngine.ExecuteAsync(context, ruleSet);

        // Assert — both rules fired
        Assert.True(result.Deductions.ContainsKey("EPF_Employee"));
        Assert.True(result.Deductions.ContainsKey("TDS"));
        Assert.Equal(5000m, result.Deductions["TDS"]);
    }

    [Fact]
    public async Task PipelineShouldSkipRulesThatReturnNotApplicable()
    {
        // Arrange — ESIC on a salary above the ₹21,000 wage ceiling
        var grossMonthly = 25000m; // above ESIC ceiling → should return IsApplicable = false
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(300000, grossMonthly, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);

        var rules = new List<IStatutoryRuleMetadata> { new EsicStatutoryRule() };
        var ruleSet = new MockRuleSet(FY2026, rules);

        // Act
        var result = await StatutoryPipelineEngine.ExecuteAsync(context, ruleSet);

        // Assert — ESIC not deducted when gross > 21,000
        Assert.False(result.Deductions.ContainsKey("ESIC_Employee"));
        Assert.Equal(grossMonthly, result.NetPay);
    }

    private static StatutoryContext CreateDefaultContext()
    {
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(120000, 10000, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        return new StatutoryContext(breakdown, profile, FY2026, 4);
    }

    private sealed class MockRuleSet : IStatutoryRuleSet
    {
        public MockRuleSet(FinancialYear year, IReadOnlyList<IStatutoryRuleMetadata> rules)
        {
            Year = year;
            Rules = rules;
        }

        public FinancialYear Year { get; }
        public IReadOnlyList<IStatutoryRuleMetadata> Rules { get; }
    }
}
