// -----------------------------------------------------------------------
// <copyright file="LearningEnrollmentTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Primitives;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class LearningEnrollmentTests
{
    private static LearningEnrollment NewEnrollment(bool mandatory = true)
        => LearningEnrollment.Assign("tenant-1", Guid.NewGuid(), Guid.NewGuid(), mandatory);

    [Fact]
    public void LearningEnrollment_Assign_StatusIsAssigned()
    {
        var enrollment = NewEnrollment();

        enrollment.Status.Should().Be(EnrollmentStatus.Assigned);
    }

    [Fact]
    public void LearningEnrollment_Assign_SetsProperties()
    {
        var employeeId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        var enrollment = LearningEnrollment.Assign("tenant-1", employeeId, moduleId, true);

        enrollment.TenantId.Should().Be("tenant-1");
        enrollment.EmployeeId.Should().Be(employeeId);
        enrollment.ModuleId.Should().Be(moduleId);
        enrollment.IsMandatory.Should().BeTrue();
        enrollment.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void LearningEnrollment_Start_StatusIsInProgress()
    {
        var enrollment = NewEnrollment();

        enrollment.Start();

        enrollment.Status.Should().Be(EnrollmentStatus.InProgress);
    }

    [Fact]
    public void LearningEnrollment_Start_WhenNotAssigned_ThrowsInvalidOperationException()
    {
        var enrollment = NewEnrollment();
        enrollment.Start();

        var act = () => enrollment.Start();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LearningEnrollment_MarkCompleted_SetsStatusToCompleted()
    {
        var enrollment = NewEnrollment();
        enrollment.Start();

        enrollment.MarkCompleted("key-abc");

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletionIdempotencyKey.Should().Be("key-abc");
        enrollment.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void LearningEnrollment_MarkCompleted_FromAssigned_SetsStatusToCompleted()
    {
        // MarkCompleted is allowed from Assigned state too per source
        var enrollment = NewEnrollment();

        enrollment.MarkCompleted("key-xyz");

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public void LearningEnrollment_MarkCompleted_SameKey_IsIdempotent()
    {
        var enrollment = NewEnrollment();
        enrollment.MarkCompleted("key-idempotent");

        // Second call with same key should be a no-op
        enrollment.MarkCompleted("key-idempotent");

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public void LearningEnrollment_MarkCompleted_DifferentKeyWhenCompleted_ThrowsDomainException()
    {
        var enrollment = NewEnrollment();
        enrollment.MarkCompleted("key-first");

        // Completed with a different key → should throw
        var act = () => enrollment.MarkCompleted("key-different");

        act.Should().Throw<InvalidOperationException>();
    }
}
