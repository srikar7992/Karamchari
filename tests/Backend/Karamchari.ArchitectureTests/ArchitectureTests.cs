// -----------------------------------------------------------------------
// <copyright file="ArchitectureTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Karamchari.ArchitectureTests;

/// <summary>
/// Enforces architectural dependency governance rules.
/// </summary>
public sealed class DependencyGovernanceTests
{
    private static readonly Assembly[] Assemblies = new[]
    {
        typeof(Karamchari.Core.DependencyInjection.CoreServiceCollectionExtensions).Assembly,
        typeof(Karamchari.HR.Persistence.HRDbContext).Assembly,
        typeof(Karamchari.Payroll.Data.PayrollDbContext).Assembly,
        typeof(Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext).Assembly,
        typeof(Karamchari.Performance.Persistence.PerformanceDbContext).Assembly,
        typeof(Karamchari.Recruitment.Persistence.RecruitmentDbContext).Assembly,
        typeof(Karamchari.Compensation.Persistence.CompensationDbContext).Assembly,
        typeof(Karamchari.Capability.Persistence.CapabilityDbContext).Assembly,
        typeof(Karamchari.Intelligence.Persistence.IntelligenceDbContext).Assembly,
        typeof(Karamchari.Governance.Persistence.GovernanceDbContext).Assembly,
        typeof(Karamchari.Billing.Persistence.BillingDbContext).Assembly,
        typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext).Assembly,
        typeof(Karamchari.PSA.Persistence.PSADbContext).Assembly,
        typeof(Karamchari.Notifications.Persistence.NotificationsDbContext).Assembly,
        typeof(Karamchari.Workflow.Persistence.WorkflowDbContext).Assembly,
        typeof(Karamchari.FinancialOps.Persistence.FinancialOpsDbContext).Assembly,
        typeof(Karamchari.Identity.IdentityEndpoints).Assembly,
        typeof(Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext).Assembly
    };

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
        "Karamchari.Workflow",
        "Karamchari.FinancialOps",
        "Karamchari.Identity"
    };

    /// <summary>
    /// Enforces that Platform Core must not depend on any commercial capability pack.
    /// </summary>
    [Fact]
    public void CoreMustNotDependOnCapabilities()
    {
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(CoreNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(CapabilityNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful, "Platform Core must not depend on any commercial capability pack.");
    }

    /// <summary>
    /// Enforces that Forecasting must not reference Billing implementation types directly.
    /// </summary>
    [Fact]
    public void ForecastingCannotReferenceBillingImplementation()
    {
        var billingImplementationTypes = Types.InAssembly(typeof(Karamchari.Billing.Persistence.BillingDbContext).Assembly)
            .GetTypes()
            .Select(t => t.FullName)
            .Where(name => name != null)
            .ToArray();

        var result = Types.InAssembly(typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(billingImplementationTypes)
            .GetResult();

        Assert.True(result.IsSuccessful, "Forecasting must not depend on Billing implementation types.");
    }

    /// <summary>
    /// Enforces that Payroll must not reference Notifications implementation types directly.
    /// </summary>
    [Fact]
    public void PayrollCannotReferenceNotificationsImplementation()
    {
        var notificationsImplementationTypes = Types.InAssembly(typeof(Karamchari.Notifications.Persistence.NotificationsDbContext).Assembly)
            .GetTypes()
            .Select(t => t.FullName)
            .Where(name => name != null)
            .ToArray();

        var result = Types.InAssembly(typeof(Karamchari.Payroll.Data.PayrollDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(notificationsImplementationTypes)
            .GetResult();

        Assert.True(result.IsSuccessful, "Payroll must not depend on Notifications implementation types.");
    }

    /// <summary>
    /// Enforces that Identity application layer must not depend on Identity infrastructure.
    /// </summary>
    [Fact]
    public void IdentityApplicationCannotDependOnIdentityInfrastructure()
    {
        var result = Types.InAssembly(typeof(Karamchari.Identity.IdentityEndpoints).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Karamchari.Identity.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Identity application layer must not depend on Identity infrastructure.");
    }

    /// <summary>
    /// Enforces that Domain layer classes must not depend on EF Core.
    /// </summary>
    [Fact]
    public void DomainLayerMustNotDependOnEFCore()
    {
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespaceEndingWith(".Domain")
            .Or().ResideInNamespace("*.Domain.*")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must not depend on EF Core.");
    }

    /// <summary>
    /// Enforces that Contracts layer projects must not depend on EF Core, Persistence, or Infrastructure.
    /// </summary>
    [Fact]
    public void ContractsMustNotDependOnInfrastructureOrPersistence()
    {
        var forbiddenDependencies = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "*.Persistence",
            "*.Persistence.*",
            "*.Infrastructure",
            "*.Infrastructure.*"
        };

        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespaceEndingWith(".Contracts")
            .Or().ResideInNamespace("*.Contracts.*")
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, "Contracts must not depend on EF Core, Persistence, or Infrastructure.");
    }

    /// <summary>
    /// Enforces that DbContexts do not leak across capability bounds.
    /// </summary>
    [Fact]
    public void DbContextsMustNotLeakAcrossCapabilities()
    {
        var allTypes = Types.InAssemblies(Assemblies).GetTypes();
        var dbContexts = allTypes.Where(t => t.IsSubclassOf(typeof(Microsoft.EntityFrameworkCore.DbContext))
                                        && t.Namespace != null
                                        && CapabilityNamespaces.Any(c => t.Namespace.StartsWith(c, StringComparison.Ordinal))).ToList();

        foreach (var dbContext in dbContexts)
        {
            var ownerCapability = CapabilityNamespaces.First(c => dbContext.Namespace!.StartsWith(c, StringComparison.Ordinal));
            var otherCapabilities = CapabilityNamespaces.Where(c => c != ownerCapability).ToArray();

            foreach (var otherCapability in otherCapabilities)
            {
                var result = Types.InAssemblies(Assemblies)
                    .That().ResideInNamespace(otherCapability)
                    .Or().ResideInNamespace($"{otherCapability}.*")
                    .ShouldNot()
                    .HaveDependencyOn(dbContext.FullName)
                    .GetResult();

                Assert.True(result.IsSuccessful, $"DbContext {dbContext.Name} is illegally accessed by {otherCapability}. Use projections/contracts instead.");
            }
        }
    }
}
