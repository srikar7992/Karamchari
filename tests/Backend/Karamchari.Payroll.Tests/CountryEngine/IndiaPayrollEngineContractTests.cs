// -----------------------------------------------------------------------
// <copyright file="IndiaPayrollEngineContractTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Tests.CountryEngine;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.CountryEngine;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Calculation;
using Karamchari.Payroll.Services.Compliance;
using Karamchari.Payroll.Services.CountryEngine;
using Karamchari.Payroll.Services.Statutory;
using NSubstitute;

/// <summary>
/// Runs the full <see cref="PayrollCountryEngineContractTests"/> contract against <see cref="IndiaPayrollEngine"/>.
/// All engine dependencies are substituted so these tests remain fast and pure.
/// </summary>
public sealed class IndiaPayrollEngineContractTests : PayrollCountryEngineContractTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid BasicComponentId = Guid.NewGuid();
    private static readonly Guid HraComponentId = Guid.NewGuid();

    protected override IPayrollCountryEngine CreateEngine()
    {
        var factory = Substitute.For<IStatutoryRuleSetFactory>();
        var ruleSet = Substitute.For<IStatutoryRuleSet>();

        ruleSet.Year.Returns(new FinancialYear(2026, 2027));
        ruleSet.Rules.Returns(new List<IStatutoryRuleMetadata>());
        factory
            .Create(
                Arg.Any<FinancialYear>(),
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<IReadOnlyList<Guid>>())
            .Returns(ruleSet);

        var taxCalc = Substitute.For<ITaxCalculatorService>();
        taxCalc
            .Calculate(Arg.Any<TaxCalculationInput>())
            .Returns(new TaxCalculationOutput(
                TaxableIncome: 525_000m,
                TaxLiability: 22_500m,
                TdsThisMonth: 1_875m,
                RuleVersionId: "FY2026-27-v1"));

        return new IndiaPayrollEngine(
            factory,
            taxCalc,
            Substitute.For<ITdsGenerator>(),
            Substitute.For<IEcrGenerator>(),
            Substitute.For<IEsicGenerator>());
    }

    protected override PayrollContext BuildValidContext()
    {
        var profile = PayrollProfile.CreateDraft(EmployeeId, "Arjun Sharma");
        profile.AssignCompensation(600_000m, TemplateId);
        profile.UpdateStatutoryInfo("ABCDE1234F", "123456789012", string.Empty);

        var breakdown = new CTCBreakdownResult(
            AnnualCTC: 600_000m,
            MonthlyGross: 50_000m,
            MonthlyBreakdown: new Dictionary<Guid, decimal>
            {
                { BasicComponentId, 20_000m },
                { HraComponentId, 10_000m }
            },
            AnnualBreakdown: new Dictionary<Guid, decimal>
            {
                { BasicComponentId, 240_000m },
                { HraComponentId, 120_000m }
            });

        return new PayrollContext(
            Profile: profile,
            Breakdown: breakdown,
            FinancialYear: new FinancialYear(2026, 2027),
            PayrollMonth: 4,
            EffectiveDate: new DateOnly(2026, 4, 30),
            YearToDateGross: 0m,
            YearToDateTds: 0m,
            DeclaredInvestments: 0m,
            MonthlyRentPaid: 8_000m,
            EpfWageComponentIds: [BasicComponentId],
            BasicComponentIds: [BasicComponentId],
            HraComponentIds: [HraComponentId]);
    }
}
