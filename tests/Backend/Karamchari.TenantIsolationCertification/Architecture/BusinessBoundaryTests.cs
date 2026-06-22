// -----------------------------------------------------------------------
// <copyright file="BusinessBoundaryTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Karamchari.HR.Persistence;
using Karamchari.Payroll.Data;
using NetArchTest.Rules;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Architecture;

/// <summary>
/// Architectural fitness functions that ensure strict isolation between business domains 
/// and the underlying distributed platform infrastructure.
/// </summary>
public class BusinessBoundaryTests
{
    // We sample a few business bounded contexts for this architectural test.
    private static readonly Assembly HrAssembly = typeof(HRDbContext).Assembly;
    private static readonly Assembly PayrollAssembly = typeof(PayrollDbContext).Assembly;

    // The core platform assemblies containing execution and isolation primitives
    private static readonly string[] PlatformNamespaces = new[]
    {
        "Karamchari.Core.Multitenancy.Execution",
        "Karamchari.Core.Persistence.Tenant",
        "Karamchari.Core.Messaging.Tenant",
        "Karamchari.Core.BackgroundJobs.Tenant",
        "Karamchari.Core.Observability.Tenant"
    };

    /// <summary>
    /// Enforces that business domains (HR, Payroll, etc.) have zero awareness of 
    /// platform execution mechanics (scopes, connection guards, etc.).
    /// </summary>
    [Fact]
    public void Fitness_BusinessDomains_MustNotDependOnPlatformExecutionMechanics()
    {
        var assemblies = new[] { HrAssembly, PayrollAssembly };

        var result = Types.InAssemblies(assemblies)
            .That()
            .ResideInNamespaceStartingWith("Karamchari")
            .ShouldNot()
            .HaveDependencyOnAny(PlatformNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Business domains (HR, Payroll, etc.) must have 0% awareness of platform execution scopes, " +
            "replay internals, connection guards, or tracing mechanics. They should only depend on " +
            "abstract domain interfaces and ITenantProvider for identity, but NEVER execution internals. " +
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    /// <summary>
    /// Enforces that business logic does not interact with the TenantExecutionEnvelope directly.
    /// Identity should be consumed via high-level ID strings from ITenantProvider.
    /// </summary>
    [Fact]
    public void Fitness_BusinessDomains_MustNotDependOnTenantExecutionEnvelope()
    {
        var assemblies = new[] { HrAssembly, PayrollAssembly };

        var result = Types.InAssemblies(assemblies)
            .That()
            .ResideInNamespaceStartingWith("Karamchari")
            .And().DoNotResideInNamespaceMatching(".*Migrations.*")
            .And().DoNotResideInNamespaceMatching(@".*\.Messaging")
            .ShouldNot()
            .HaveDependencyOn("Karamchari.Core.Multitenancy.TenantExecutionEnvelope")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Business logic should not handle TenantExecutionEnvelope directly. " +
            "It is an infrastructure primitive for propagation and replay, not a business concept. " +
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
