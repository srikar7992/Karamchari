// -----------------------------------------------------------------------
// <copyright file="RecruitmentArchitectureTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Karamchari.ArchitectureTests;

/// <summary>
/// Enforces architectural layering and isolation rules for the Recruitment module.
/// </summary>
public sealed class RecruitmentArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(Karamchari.Recruitment.Api.RecruitmentEndpoints).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Karamchari.Recruitment.Application.IRecruitmentDbContext).Assembly;
    private static readonly Assembly CoreAssembly = typeof(Karamchari.Recruitment.Domain.JobRequisition).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Karamchari.Recruitment.Contracts.CandidateDto).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Karamchari.Recruitment.Persistence.RecruitmentDbContext).Assembly;
    private static readonly Assembly WorkerAssembly = typeof(Karamchari.Recruitment.Worker.Consumers.RequisitionPublishedAuditConsumer).Assembly;

    private static readonly Assembly[] AllRecruitmentAssemblies =
    {
        ApiAssembly,
        ApplicationAssembly,
        CoreAssembly,
        ContractsAssembly,
        InfrastructureAssembly,
        WorkerAssembly
    };

    [Fact]
    public void ApiShouldOnlyDependOnApplicationAndContracts()
    {
        var result = Types.InAssembly(ApiAssembly)
            .Should().NotHaveDependencyOnAny("Karamchari.Recruitment.Infrastructure", "Karamchari.Recruitment.Core")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment API should only depend on Application and Contracts layers.");
    }

    [Fact]
    public void ApplicationShouldOnlyDependOnCoreAndContracts()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should().NotHaveDependencyOnAny("Karamchari.Recruitment.Api", "Karamchari.Recruitment.Infrastructure", "Karamchari.Recruitment.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment Application should only depend on Core and Contracts layers.");
    }

    [Fact]
    public void InfrastructureShouldOnlyDependOnApplicationCoreAndContracts()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should().NotHaveDependencyOnAny("Karamchari.Recruitment.Api", "Karamchari.Recruitment.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment Infrastructure should only depend on Application, Core and Contracts layers.");
    }

    [Fact]
    public void CoreShouldOnlyDependOnContracts()
    {
        var result = Types.InAssembly(CoreAssembly)
            .Should().NotHaveDependencyOnAny(
                "Karamchari.Recruitment.Api",
                "Karamchari.Recruitment.Application",
                "Karamchari.Recruitment.Infrastructure",
                "Karamchari.Recruitment.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment Core should only depend on the Contracts layer.");
    }

    [Fact]
    public void WorkerShouldOnlyDependOnApplicationAndContracts()
    {
        var result = Types.InAssembly(WorkerAssembly)
            .Should().NotHaveDependencyOnAny("Karamchari.Recruitment.Infrastructure", "Karamchari.Recruitment.Core", "Karamchari.Recruitment.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment Worker should only depend on Application and Contracts layers.");
    }

    [Fact]
    public void RecruitmentShouldNotReferenceHRInternals()
    {
        var result = Types.InAssemblies(AllRecruitmentAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny("Karamchari.HR.Core", "Karamchari.HR.Persistence", "Karamchari.HR.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, "Recruitment module must not reference HR module internals. Communication must be via Contracts or Events.");
    }
}
