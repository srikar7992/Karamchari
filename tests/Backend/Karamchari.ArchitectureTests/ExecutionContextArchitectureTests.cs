// -----------------------------------------------------------------------
// <copyright file="ExecutionContextArchitectureTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Messaging.Tenant;
using NetArchTest.Rules;
using Xunit;

namespace Karamchari.ArchitectureTests;

public class ExecutionContextArchitectureTests
{
    [Fact]
    public void DomainLayersShouldNotDependOnMessagingTenantInternals()
    {
        var domainAssemblies = new[]
        {
            typeof(Karamchari.HR.Domain.Employees.Employee).Assembly,
            typeof(Karamchari.Payroll.Domain.PayrollProfile).Assembly
        };

        var result = Types.InAssemblies(domainAssemblies)
            .ShouldNot()
            .HaveDependencyOn("Karamchari.Core.Messaging.Tenant")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain layers must not depend on messaging tenant infrastructure.");
    }

    [Fact]
    public void ExecutionContextHeadersShouldOnlyBeReferencedByCoreAndInfrastructure()
    {
        var domainAssemblies = new[]
        {
            typeof(Karamchari.HR.Domain.Employees.Employee).Assembly,
            typeof(Karamchari.Payroll.Domain.PayrollProfile).Assembly
        };

        var result = Types.InAssemblies(domainAssemblies)
            .ShouldNot()
            .HaveDependencyOn(typeof(ExecutionContextHeaders).FullName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("ExecutionContextHeaders must not be referenced by domain layers.");
    }

    [Fact]
    public void OnlyCoreMessagingShouldImplementExecutionContextSigning()
    {
        var result = Types.InCurrentDomain()
            .That().HaveName(nameof(ExecutionContextSigner))
            .Should()
            .ResideInNamespace("Karamchari.Core.Messaging.Tenant")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("ExecutionContextSigner must only reside in Core Messaging namespace.");
    }
}
