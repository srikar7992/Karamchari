// -----------------------------------------------------------------------
// <copyright file="CTCBreakdownTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;

/// <summary>
/// Unit tests for <see cref="CTCTemplateCompiler"/> and <see cref="CTCBreakdownService"/>.
/// These tests verify the DAG-based salary breakdown engine.
/// </summary>
public class CTCBreakdownTests
{
    // â”€â”€ Shared component IDs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static readonly SalaryComponent BasicComp =
        SalaryComponent.Create("Basic", ComponentType.Earning);

    private static readonly SalaryComponent HraComp =
        SalaryComponent.Create("HRA", ComponentType.Earning);

    private static readonly SalaryComponent SpecialAllowanceComp =
        SalaryComponent.Create("Special Allowance", ComponentType.Earning);

    // â”€â”€ CTCTemplateCompiler â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compile_LinearDependency_ShouldReturnOrderedSteps()
    {
        // Arrange: Basic (40% of CTC) â†’ HRA (40% of Basic)
        var template = SalaryTemplate.Create("Standard");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = BasicComp.Id,
            CalculationType = ComponentCalculationType.PercentageOfCTC,
            Value = 40m,
            ExecutionPriority = 1
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = HraComp.Id,
            CalculationType = ComponentCalculationType.PercentageOfComponents,
            Value = 40m,
            DependsOnComponentIds = new List<Guid> { BasicComp.Id },
            ExecutionPriority = 2
        });

        var masters = new List<SalaryComponent> { BasicComp, HraComp };

        // Act
        var plan = CTCTemplateCompiler.Compile(template, masters);

        // Assert
        Assert.Equal(template.Id, plan.TemplateId);
        Assert.Equal(2, plan.OrderedSteps.Count);
        Assert.Null(plan.RemainderStep);

        // Basic must come before HRA (it's a dependency)
        var basicIdx = plan.OrderedSteps.FindIndex(s => s.ComponentId == BasicComp.Id);
        var hraIdx = plan.OrderedSteps.FindIndex(s => s.ComponentId == HraComp.Id);
        Assert.True(basicIdx < hraIdx, "Basic must be evaluated before HRA");
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compile_WithRemainderComponent_ShouldIsolateItAsRemainderStep()
    {
        // Arrange
        var template = SalaryTemplate.Create("With Remainder");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = BasicComp.Id,
            CalculationType = ComponentCalculationType.PercentageOfCTC,
            Value = 40m,
            ExecutionPriority = 1
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = SpecialAllowanceComp.Id,
            CalculationType = ComponentCalculationType.Remainder,
            ExecutionPriority = 99
        });

        var masters = new List<SalaryComponent> { BasicComp, SpecialAllowanceComp };

        // Act
        var plan = CTCTemplateCompiler.Compile(template, masters);

        // Assert â€” Special Allowance is the remainder step, not in OrderedSteps
        Assert.Single(plan.OrderedSteps);
        Assert.NotNull(plan.RemainderStep);
        Assert.Equal(SpecialAllowanceComp.Id, plan.RemainderStep!.ComponentId);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compile_CircularDependency_ShouldThrow()
    {
        // Arrange: A depends on B, B depends on A â†’ cycle
        var compA = SalaryComponent.Create("CompA", ComponentType.Earning);
        var compB = SalaryComponent.Create("CompB", ComponentType.Earning);

        var template = SalaryTemplate.Create("Circular");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = compA.Id,
            CalculationType = ComponentCalculationType.PercentageOfComponents,
            Value = 50m,
            DependsOnComponentIds = new List<Guid> { compB.Id }
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = compB.Id,
            CalculationType = ComponentCalculationType.PercentageOfComponents,
            Value = 50m,
            DependsOnComponentIds = new List<Guid> { compA.Id }
        });

        var masters = new List<SalaryComponent> { compA, compB };

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => CTCTemplateCompiler.Compile(template, masters));
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compile_MultipleRemainderComponents_ShouldThrow()
    {
        // Arrange
        var r1 = SalaryComponent.Create("Remainder1", ComponentType.Earning);
        var r2 = SalaryComponent.Create("Remainder2", ComponentType.Earning);

        var template = SalaryTemplate.Create("Bad Template");
        template.AddComponent(new SalaryTemplateComponent { ComponentId = r1.Id, CalculationType = ComponentCalculationType.Remainder });
        template.AddComponent(new SalaryTemplateComponent { ComponentId = r2.Id, CalculationType = ComponentCalculationType.Remainder });

        var masters = new List<SalaryComponent> { r1, r2 };

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => CTCTemplateCompiler.Compile(template, masters));
    }

    // â”€â”€ CTCBreakdownService â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Calculate_PercentageOfCTC_ShouldDistributeCorrectly()
    {
        // Arrange: Basic = 40% CTC, HRA = 40% Basic, Special = Remainder
        var template = SalaryTemplate.Create("Standard 40-40-R");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = BasicComp.Id,
            CalculationType = ComponentCalculationType.PercentageOfCTC,
            Value = 40m,
            ExecutionPriority = 1
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = HraComp.Id,
            CalculationType = ComponentCalculationType.PercentageOfComponents,
            Value = 40m,
            DependsOnComponentIds = new List<Guid> { BasicComp.Id },
            ExecutionPriority = 2
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = SpecialAllowanceComp.Id,
            CalculationType = ComponentCalculationType.Remainder,
            ExecutionPriority = 99
        });

        var masters = new List<SalaryComponent> { BasicComp, HraComp, SpecialAllowanceComp };
        var plan = CTCTemplateCompiler.Compile(template, masters);
        const decimal annualCTC = 1_200_000m; // â‚¹12L

        // Act
        var result = CTCBreakdownService.Calculate(annualCTC, plan);

        // Assert annual values
        Assert.Equal(480_000m, result.AnnualBreakdown[BasicComp.Id]);      // 40% of 12L
        Assert.Equal(192_000m, result.AnnualBreakdown[HraComp.Id]);         // 40% of 4.8L

        decimal specialAnnual = annualCTC - 480_000m - 192_000m;            // 528k
        Assert.Equal(specialAnnual, result.AnnualBreakdown[SpecialAllowanceComp.Id]);

        // Assert MonthlyGross includes Basic + HRA + SpecialAllowance
        Assert.Equal(annualCTC, result.AnnualCTC);
        Assert.True(result.MonthlyGross > 0);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Calculate_RemainderOverflow_ShouldThrow()
    {
        // Arrange: two fixed amounts that together exceed CTC
        var comp1 = SalaryComponent.Create("Comp1", ComponentType.Earning);
        var comp2 = SalaryComponent.Create("Comp2", ComponentType.Earning);
        var rem = SalaryComponent.Create("Remainder", ComponentType.Earning);

        var template = SalaryTemplate.Create("Overflow");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = comp1.Id,
            CalculationType = ComponentCalculationType.FixedAmount,
            Value = 700_000m,
            ExecutionPriority = 1
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = comp2.Id,
            CalculationType = ComponentCalculationType.FixedAmount,
            Value = 600_000m,
            ExecutionPriority = 2
        });
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = rem.Id,
            CalculationType = ComponentCalculationType.Remainder,
            ExecutionPriority = 99
        });

        var masters = new List<SalaryComponent> { comp1, comp2, rem };
        var plan = CTCTemplateCompiler.Compile(template, masters);

        // Act + Assert â€” 700k + 600k = 1.3M > 1M CTC â†’ overflow
        Assert.Throws<InvalidOperationException>(() => CTCBreakdownService.Calculate(1_000_000m, plan));
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Calculate_MonthlyBreakdownShouldSumToApproximatelyMonthlyGross()
    {
        // Arrange: single earning component = 100% of CTC, no deductions, no remainder
        var single = SalaryComponent.Create("Full Pay", ComponentType.Earning);

        var template = SalaryTemplate.Create("Simple");
        template.AddComponent(new SalaryTemplateComponent
        {
            ComponentId = single.Id,
            CalculationType = ComponentCalculationType.PercentageOfCTC,
            Value = 100m,
            ExecutionPriority = 1
        });

        var masters = new List<SalaryComponent> { single };
        var plan = CTCTemplateCompiler.Compile(template, masters);

        // Act
        var result = CTCBreakdownService.Calculate(1_200_000m, plan);

        // Assert
        Assert.Equal(100_000m, result.MonthlyBreakdown[single.Id]); // 12L / 12
        Assert.Equal(100_000m, result.MonthlyGross);
    }
}
