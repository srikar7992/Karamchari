using System;
using System.Linq;
using Karamchari.Core.Multitenancy;
using NetArchTest.Rules;
using Xunit;

namespace Karamchari.ArchitectureTests;

/// <summary>
/// Enforces architectural dependency governance rules.
/// </summary>
public sealed class DependencyGovernanceTests
{
    private const string CoreNamespace = "Karamchari.Core";

    private static readonly string[] CapabilityNamespaces = 
    {
        "Karamchari.HR",
        "Karamchari.Payroll",
        "Karamchari.TimeAttendance",
        "Karamchari.Performance",
        "Karamchari.Recruitment",
        "Karamchari.Compensation",
        "Karamchari.Capability",
        "Karamchari.Intelligence",
        "Karamchari.Governance",
        "Karamchari.Billing",
        "Karamchari.Forecasting",
        "Karamchari.PSA",
        "Karamchari.Notifications",
        "Karamchari.Workflow"
    };

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    [Fact]
    public void CoreMustNotDependOnCapabilities()
    {
        var result = Types.InNamespace(CoreNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(CapabilityNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful, "Platform Core must not depend on any commercial capability pack.");
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    [Fact]
    public void CapabilitiesMustNotDependOnEachOtherExceptThroughContracts()
    {
        // Enforce that capabilities do not cross-reference each other's internals.
        // They should only reference Karamchari.Core or Karamchari.*.Contracts.

        foreach (var capability in CapabilityNamespaces)
        {
            var otherCapabilities = CapabilityNamespaces.Where(c => c != capability).ToArray();

            var result = Types.InNamespace(capability)
                .That().DoNotResideInNamespace($"{capability}.Contracts")
                .ShouldNot()
                .HaveDependencyOnAny(otherCapabilities)
                .GetResult();

            Assert.True(result.IsSuccessful, $"Capability {capability} has forbidden dependencies on other capabilities. Use Contracts or ReadModels.");
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    [Fact]
    public void DomainAggregatesMustNotDependOnWorkflowCoordination()
    {
        // Enforces: "Domain Owns Business Truth, Workflow Owns Coordination"
        // Domain entities should not know about Workflow execution, condition compilation, or MassTransit orchestration.

        foreach (var capability in CapabilityNamespaces)
        {
            var result = Types.InNamespace(capability)
                .That().Inherit(typeof(Karamchari.Core.Domain.Primitives.AggregateRoot<Guid>))
                .ShouldNot()
                .HaveDependencyOn("Karamchari.Workflow.Services")
                .GetResult();

            Assert.True(result.IsSuccessful, $"Domain aggregates in {capability} must not depend on workflow coordination services.");
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    [Fact]
    public void DbContextsMustNotLeakAcrossCapabilities()
    {
        var allTypes = Types.InCurrentDomain().GetTypes();
        var dbContexts = allTypes.Where(t => t.IsSubclassOf(typeof(Microsoft.EntityFrameworkCore.DbContext)) 
                                        && t.Namespace != null 
                                        && CapabilityNamespaces.Any(c => t.Namespace.StartsWith(c, StringComparison.Ordinal))).ToList();

        foreach (var dbContext in dbContexts)
        {
            var ownerCapability = CapabilityNamespaces.First(c => dbContext.Namespace!.StartsWith(c, StringComparison.Ordinal));
            var otherCapabilities = CapabilityNamespaces.Where(c => c != ownerCapability).ToArray();

            foreach (var otherCapability in otherCapabilities)
            {
                var result = Types.InNamespace(otherCapability)
                    .ShouldNot()
                    .HaveDependencyOn(dbContext.FullName)
                    .GetResult();

                Assert.True(result.IsSuccessful, $"DbContext {dbContext.Name} is illegally accessed by {otherCapability}. Use projections/contracts instead.");
            }
        }
    }
}
