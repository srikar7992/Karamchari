using FluentAssertions;
using Karamchari.Workflow.Domain;
using Xunit;

namespace Karamchari.Workflow.Tests.Domain;

/// <summary>
/// Tests for the Phase 13 ConditionGroup expression engine (OR/AND/NOT nesting).
/// </summary>
public class WorkflowExpressionGroupTests
{
    // ── Empty group ──────────────────────────────────────────────────────────

    [Fact]
    public void EmptyGroup_AlwaysTrue()
    {
        var group = ConditionGroup.Empty;
        group.Evaluate(new Dictionary<string, object>()).Should().BeTrue();
    }

    [Fact]
    public void EmptyGroup_WithNegate_AlwaysFalse()
    {
        var group = new ConditionGroup { Negate = true };
        group.Evaluate(new Dictionary<string, object>()).Should().BeFalse();
    }

    // ── AND semantics ────────────────────────────────────────────────────────

    [Fact]
    public void AndGroup_AllConditionsTrue_ReturnsTrue()
    {
        var group = new ConditionGroup
        {
            Operator = LogicalOperator.And,
            Conditions =
            [
                new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "5000"),
                new WorkflowCondition("EmployeeType", ConditionOperator.Equals, "Contractor"),
            ],
        };

        var ctx = new Dictionary<string, object>
        {
            ["Amount"] = 10000,
            ["EmployeeType"] = "Contractor",
        };

        group.Evaluate(ctx).Should().BeTrue();
    }

    [Fact]
    public void AndGroup_OneConditionFalse_ReturnsFalse()
    {
        var group = new ConditionGroup
        {
            Operator = LogicalOperator.And,
            Conditions =
            [
                new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "5000"),
                new WorkflowCondition("EmployeeType", ConditionOperator.Equals, "Contractor"),
            ],
        };

        var ctx = new Dictionary<string, object>
        {
            ["Amount"] = 1000,          // fails
            ["EmployeeType"] = "Contractor",
        };

        group.Evaluate(ctx).Should().BeFalse();
    }

    // ── OR semantics ─────────────────────────────────────────────────────────

    [Fact]
    public void OrGroup_OneConditionTrue_ReturnsTrue()
    {
        var group = new ConditionGroup
        {
            Operator = LogicalOperator.Or,
            Conditions =
            [
                new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "10000"),
                new WorkflowCondition("Department", ConditionOperator.Equals, "Finance"),
            ],
        };

        var ctx = new Dictionary<string, object>
        {
            ["Amount"] = 500,           // fails
            ["Department"] = "Finance", // passes
        };

        group.Evaluate(ctx).Should().BeTrue();
    }

    [Fact]
    public void OrGroup_AllConditionsFalse_ReturnsFalse()
    {
        var group = new ConditionGroup
        {
            Operator = LogicalOperator.Or,
            Conditions =
            [
                new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "10000"),
                new WorkflowCondition("Department", ConditionOperator.Equals, "Finance"),
            ],
        };

        var ctx = new Dictionary<string, object>
        {
            ["Amount"] = 500,
            ["Department"] = "Engineering",
        };

        group.Evaluate(ctx).Should().BeFalse();
    }

    // ── NOT semantics ─────────────────────────────────────────────────────────

    [Fact]
    public void Negate_InvertsResult()
    {
        var group = new ConditionGroup
        {
            Operator = LogicalOperator.And,
            Negate = true,
            Conditions = [new WorkflowCondition("Executive", ConditionOperator.Equals, "true")],
        };

        var ctx = new Dictionary<string, object> { ["Executive"] = "true" };
        group.Evaluate(ctx).Should().BeFalse(); // NOT (Executive == true) when Executive=true → false

        var ctxNonExec = new Dictionary<string, object> { ["Executive"] = "false" };
        group.Evaluate(ctxNonExec).Should().BeTrue(); // NOT (Executive == true) when Executive=false → true
    }

    // ── Nested groups (enterprise-grade expressions) ──────────────────────────

    [Fact]
    public void NestedGroup_CompoundExpression_EvaluatesCorrectly()
    {
        // (Amount > 10000 OR Department = Finance)
        // AND NOT (Executive = true)
        var root = new ConditionGroup
        {
            Operator = LogicalOperator.And,
            Groups =
            [
                new ConditionGroup
                {
                    Operator = LogicalOperator.Or,
                    Conditions =
                    [
                        new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "10000"),
                        new WorkflowCondition("Department", ConditionOperator.Equals, "Finance"),
                    ],
                },
                new ConditionGroup
                {
                    Operator = LogicalOperator.And,
                    Negate = true,
                    Conditions = [new WorkflowCondition("Executive", ConditionOperator.Equals, "true")],
                },
            ],
        };

        // Amount=15000, Department=Engineering, Executive=false → Amount satisfies OR; NOT Executive passes → true
        var ctx1 = new Dictionary<string, object>
        {
            ["Amount"] = 15000,
            ["Department"] = "Engineering",
            ["Executive"] = "false",
        };
        root.Evaluate(ctx1).Should().BeTrue();

        // Amount=500, Department=Engineering → OR fails → false
        var ctx2 = new Dictionary<string, object>
        {
            ["Amount"] = 500,
            ["Department"] = "Engineering",
            ["Executive"] = "false",
        };
        root.Evaluate(ctx2).Should().BeFalse();

        // Amount=15000, Executive=true → OR passes but NOT Executive fails → false
        var ctx3 = new Dictionary<string, object>
        {
            ["Amount"] = 15000,
            ["Department"] = "Engineering",
            ["Executive"] = "true",
        };
        root.Evaluate(ctx3).Should().BeFalse();
    }

    [Fact]
    public void ThreeLevelNesting_EvaluatesCorrectly()
    {
        // Level 1: AND [condition, sub-group]
        // Level 2 sub-group: OR [condition, sub-sub-group]
        // Level 3 sub-sub-group: AND [condition]
        var root = new ConditionGroup
        {
            Operator = LogicalOperator.And,
            Conditions = [new WorkflowCondition("Country", ConditionOperator.Equals, "India")],
            Groups =
            [
                new ConditionGroup
                {
                    Operator = LogicalOperator.Or,
                    Conditions = [new WorkflowCondition("Department", ConditionOperator.Equals, "Finance")],
                    Groups =
                    [
                        new ConditionGroup
                        {
                            Operator = LogicalOperator.And,
                            Conditions =
                            [
                                new WorkflowCondition("Tenure", ConditionOperator.LessThan, "90"),
                                new WorkflowCondition("PerformanceRating", ConditionOperator.LessThan, "3"),
                            ],
                        },
                    ],
                },
            ],
        };

        // Country=India, Department=Finance → level1 AND = Country(pass) AND OR(dept pass) = true
        var ctx1 = new Dictionary<string, object>
        {
            ["Country"] = "India",
            ["Department"] = "Finance",
            ["Tenure"] = 200,
            ["PerformanceRating"] = 4,
        };
        root.Evaluate(ctx1).Should().BeTrue();

        // Country=India, Department=Engineering, Tenure=60, Performance=2 → level3 AND = pass → level2 OR = pass
        var ctx2 = new Dictionary<string, object>
        {
            ["Country"] = "India",
            ["Department"] = "Engineering",
            ["Tenure"] = 60,
            ["PerformanceRating"] = 2,
        };
        root.Evaluate(ctx2).Should().BeTrue();

        // Country=USA → level1 AND fails immediately
        var ctx3 = new Dictionary<string, object>
        {
            ["Country"] = "USA",
            ["Department"] = "Finance",
        };
        root.Evaluate(ctx3).Should().BeFalse();
    }

    // ── FromFlatList (backward compat) ────────────────────────────────────────

    [Fact]
    public void FromFlatList_AndSemantics_MatchesPhase12Behavior()
    {
        var conditions = new List<WorkflowCondition>
        {
            new("Amount", ConditionOperator.GreaterThan, "1000"),
            new("EmployeeType", ConditionOperator.Equals, "Contractor"),
        };

        var group = ConditionGroup.FromFlatList(conditions);
        group.Operator.Should().Be(LogicalOperator.And);

        var pass = new Dictionary<string, object> { ["Amount"] = 5000, ["EmployeeType"] = "Contractor" };
        var fail = new Dictionary<string, object> { ["Amount"] = 500, ["EmployeeType"] = "Contractor" };

        group.Evaluate(pass).Should().BeTrue();
        group.Evaluate(fail).Should().BeFalse();
    }

    // ── WorkflowDefinition.SetExpression integration ─────────────────────────

    [Fact]
    public void WorkflowDefinition_SetExpression_MatchesUsesNewExpression()
    {
        var def = WorkflowDefinition.Create("tenant-1", "Expense", "Finance Route");

        // Set an OR expression
        var expr = new ConditionGroup
        {
            Operator = LogicalOperator.Or,
            Conditions =
            [
                new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "10000"),
                new WorkflowCondition("Department", ConditionOperator.Equals, "Finance"),
            ],
        };
        def.SetExpression(expr);

        // Finance dept passes
        var ctxFinance = new Dictionary<string, object>
        {
            ["Amount"] = 100,
            ["Department"] = "Finance",
        };
        def.Matches(ctxFinance).Should().BeTrue();

        // Large amount passes
        var ctxHighAmount = new Dictionary<string, object>
        {
            ["Amount"] = 50000,
            ["Department"] = "Engineering",
        };
        def.Matches(ctxHighAmount).Should().BeTrue();

        // Neither passes
        var ctxNeither = new Dictionary<string, object>
        {
            ["Amount"] = 100,
            ["Department"] = "Engineering",
        };
        def.Matches(ctxNeither).Should().BeFalse();
    }

    [Fact]
    public void WorkflowDefinition_ClearConditions_ResetsToUnconditional()
    {
        var def = WorkflowDefinition.Create("tenant-1", "Expense", "Test");
        def.SetExpression(new ConditionGroup
        {
            Conditions = [new WorkflowCondition("Amount", ConditionOperator.GreaterThan, "10000")],
        });

        def.ClearConditions();
        def.Matches(null).Should().BeTrue(); // no context, no conditions → unconditional match
    }
}
