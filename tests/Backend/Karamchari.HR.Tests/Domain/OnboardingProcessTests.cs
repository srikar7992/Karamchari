// -----------------------------------------------------------------------
// <copyright file="OnboardingProcessTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.HR.Domain.Employees;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class OnboardingProcessTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static OnboardingProcess StartDefault(
        string tenantId = "tenant-acme",
        Guid? employeeId = null,
        string employeeName = "John Smith")
        => OnboardingProcess.Start(tenantId, employeeId ?? Guid.NewGuid(), employeeName);

    // ─── Start ───────────────────────────────────────────────────────────────

    [Fact]
    public void OnboardingProcess_Start_SetsStatusToNotStarted()
    {
        var process = StartDefault();

        // Start() creates the process; the status begins at NotStarted per the spec
        process.Status.Should().Be(OnboardingStatus.NotStarted);
    }

    [Fact]
    public void OnboardingProcess_Start_SetsEmployeeId()
    {
        var employeeId = Guid.NewGuid();

        var process = StartDefault(employeeId: employeeId);

        process.EmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public void OnboardingProcess_Start_SetsEmployeeName()
    {
        var process = StartDefault(employeeName: "Alice Wonderland");

        process.EmployeeName.Should().Be("Alice Wonderland");
    }

    [Fact]
    public void OnboardingProcess_Start_SetsTenantId()
    {
        var process = StartDefault(tenantId: "tenant-xyz");

        process.TenantId.Should().Be("tenant-xyz");
    }

    [Fact]
    public void OnboardingProcess_Start_SetsDocumentsVerifiedFalse()
    {
        var process = StartDefault();

        process.DocumentsVerified.Should().BeFalse();
    }

    [Fact]
    public void OnboardingProcess_Start_SetsAssetsAssignedFalse()
    {
        var process = StartDefault();

        process.AssetsAssigned.Should().BeFalse();
    }

    [Fact]
    public void OnboardingProcess_Start_SetsSystemAccountsCreatedFalse()
    {
        var process = StartDefault();

        process.SystemAccountsCreated.Should().BeFalse();
    }

    [Fact]
    public void OnboardingProcess_Start_SetsCreatedAtUtcToApproximatelyNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var process = StartDefault();

        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        process.CreatedAtUtc.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void OnboardingProcess_Start_AssignsNewGuidId()
    {
        var process = StartDefault();

        process.Id.Should().NotBe(Guid.Empty);
    }

    // ─── VerifyDocuments ─────────────────────────────────────────────────────

    [Fact]
    public void OnboardingProcess_VerifyDocuments_SetsDocumentsVerifiedTrue()
    {
        var process = StartDefault();

        process.VerifyDocuments();

        process.DocumentsVerified.Should().BeTrue();
    }

    [Fact]
    public void OnboardingProcess_VerifyDocuments_TransitionsStatusToInProcess()
    {
        var process = StartDefault();

        process.VerifyDocuments();

        process.Status.Should().Be(OnboardingStatus.InProcess);
    }

    [Fact]
    public void OnboardingProcess_VerifyDocuments_DoesNotAffectAssetsOrSystems()
    {
        var process = StartDefault();

        process.VerifyDocuments();

        process.AssetsAssigned.Should().BeFalse();
        process.SystemAccountsCreated.Should().BeFalse();
    }

    // ─── AssignAssets ────────────────────────────────────────────────────────

    [Fact]
    public void OnboardingProcess_AssignAssets_SetsAssetsAssignedTrue()
    {
        var process = StartDefault();

        process.AssignAssets();

        process.AssetsAssigned.Should().BeTrue();
    }

    [Fact]
    public void OnboardingProcess_AssignAssets_TransitionsStatusToInProcess()
    {
        var process = StartDefault();

        process.AssignAssets();

        process.Status.Should().Be(OnboardingStatus.InProcess);
    }

    [Fact]
    public void OnboardingProcess_AssignAssets_DoesNotAffectDocumentsOrSystems()
    {
        var process = StartDefault();

        process.AssignAssets();

        process.DocumentsVerified.Should().BeFalse();
        process.SystemAccountsCreated.Should().BeFalse();
    }

    // ─── ProvisionSystems ────────────────────────────────────────────────────

    [Fact]
    public void OnboardingProcess_ProvisionSystems_SetsSystemAccountsCreatedTrue()
    {
        var process = StartDefault();

        process.ProvisionSystems();

        process.SystemAccountsCreated.Should().BeTrue();
    }

    [Fact]
    public void OnboardingProcess_ProvisionSystems_TransitionsStatusToInProcess()
    {
        var process = StartDefault();

        process.ProvisionSystems();

        process.Status.Should().Be(OnboardingStatus.InProcess);
    }

    [Fact]
    public void OnboardingProcess_ProvisionSystems_DoesNotAffectDocumentsOrAssets()
    {
        var process = StartDefault();

        process.ProvisionSystems();

        process.DocumentsVerified.Should().BeFalse();
        process.AssetsAssigned.Should().BeFalse();
    }

    // ─── CompleteOnboarding ──────────────────────────────────────────────────

    [Fact]
    public void OnboardingProcess_CompleteOnboarding_SetsStatusToCompleted()
    {
        var process = StartDefault();
        process.VerifyDocuments();
        process.AssignAssets();
        process.ProvisionSystems();

        process.CompleteOnboarding();

        process.Status.Should().Be(OnboardingStatus.Completed);
    }

    [Fact]
    public void OnboardingProcess_CompleteOnboarding_SetsCompletedAtUtc()
    {
        var process = StartDefault();
        process.VerifyDocuments();
        process.AssignAssets();
        process.ProvisionSystems();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        process.CompleteOnboarding();

        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        process.CompletedAtUtc.Should().NotBeNull();
        process.CompletedAtUtc!.Value.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void OnboardingProcess_CompleteOnboarding_WithoutAllSteps_StillCompletes()
    {
        // The domain allows completing without all steps (no guard in CompleteOnboarding itself).
        // This test explicitly validates the actual domain behavior.
        var process = StartDefault();

        // Not calling VerifyDocuments, AssignAssets, ProvisionSystems

        var act = () => process.CompleteOnboarding();

        // Current implementation allows completion without step guards
        act.Should().NotThrow();
        process.Status.Should().Be(OnboardingStatus.Completed);
    }

    [Fact]
    public void OnboardingProcess_CompleteOnboarding_WhenAlreadyCompleted_IsIdempotent()
    {
        var process = StartDefault();
        process.VerifyDocuments();
        process.AssignAssets();
        process.ProvisionSystems();
        process.CompleteOnboarding();
        var firstCompletedAt = process.CompletedAtUtc;

        // Second call should not throw and should not change CompletedAtUtc
        process.CompleteOnboarding();

        process.Status.Should().Be(OnboardingStatus.Completed);
        process.CompletedAtUtc.Should().Be(firstCompletedAt);
    }

    [Fact]
    public void OnboardingProcess_FullLifecycle_StepByStep_AllFlagsTrue()
    {
        var process = StartDefault();

        process.VerifyDocuments();
        process.AssignAssets();
        process.ProvisionSystems();
        process.CompleteOnboarding();

        process.DocumentsVerified.Should().BeTrue();
        process.AssetsAssigned.Should().BeTrue();
        process.SystemAccountsCreated.Should().BeTrue();
        process.Status.Should().Be(OnboardingStatus.Completed);
        process.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void OnboardingProcess_StatusProgresses_NotStarted_ToInProcess_OnFirstStep()
    {
        var process = StartDefault();
        process.Status.Should().Be(OnboardingStatus.NotStarted);

        process.VerifyDocuments();

        process.Status.Should().Be(OnboardingStatus.InProcess);
    }

    [Fact]
    public void OnboardingProcess_StatusRemainsInProcess_AfterMultipleSteps()
    {
        var process = StartDefault();
        process.VerifyDocuments();
        process.AssignAssets();

        process.Status.Should().Be(OnboardingStatus.InProcess);
    }
}
