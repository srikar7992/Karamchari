// -----------------------------------------------------------------------
// <copyright file="SchemaDefinitionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Governance.Domain.Contracts;
using Xunit;

namespace Karamchari.Governance.Tests.Domain;

public class SchemaDefinitionTests
{
    // ---------------------------------------------------------------
    // Register
    // ---------------------------------------------------------------

    [Fact]
    public void SchemaDefinition_Register_StatusIsActive()
    {
        // Arrange / Act
        var schema = SchemaDefinition.Register(
            tenantId: "tenant-1",
            contractName: "EmployeeHiredEvent",
            version: "v1.0",
            jsonSchema: "{}",
            compatibilityRule: SchemaCompatibilityRule.BackwardCompatible,
            ownerModule: "HR");

        // Assert
        schema.Status.Should().Be(SchemaStatus.Active);
        schema.ContractName.Should().Be("EmployeeHiredEvent");
        schema.Version.Should().Be("v1.0");
        schema.CompatibilityRule.Should().Be(SchemaCompatibilityRule.BackwardCompatible);
        schema.OwnerModule.Should().Be("HR");
        schema.Id.Should().NotBeEmpty();
    }

    // ---------------------------------------------------------------
    // Deprecate
    // ---------------------------------------------------------------

    [Fact]
    public void SchemaDefinition_Deprecate_FromActive_StatusIsDeprecated()
    {
        // Arrange
        var schema = BuildActiveSchema();

        // Act
        schema.Deprecate();

        // Assert
        schema.Status.Should().Be(SchemaStatus.Deprecated);
        schema.DeprecatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SchemaDefinition_Deprecate_FromDeprecated_ThrowsDomainException()
    {
        // Arrange
        var schema = BuildActiveSchema();
        schema.Deprecate();

        // Act
        var act = () => schema.Deprecate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot deprecate*");
    }

    // ---------------------------------------------------------------
    // Archive
    // ---------------------------------------------------------------

    [Fact]
    public void SchemaDefinition_Archive_FromDeprecated_StatusIsArchived()
    {
        // Arrange
        var schema = BuildActiveSchema();
        schema.Deprecate();

        // Act
        schema.Archive();

        // Assert
        schema.Status.Should().Be(SchemaStatus.Archived);
        schema.ArchivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SchemaDefinition_Archive_FromActive_ThrowsDomainException()
    {
        // Arrange — schema is Active, not yet Deprecated
        var schema = BuildActiveSchema();

        // Act
        var act = () => schema.Archive();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only deprecated schemas can be archived*");
    }

    // ---------------------------------------------------------------
    // One-way transition guard
    // ---------------------------------------------------------------

    [Fact]
    public void SchemaDefinition_CannotRevertFromDeprecated_ToActive()
    {
        // Arrange
        var schema = BuildActiveSchema();
        schema.Deprecate();

        // Act — attempting to deprecate again (no "reactivate" method exists; the
        // only mutation is Deprecate, which requires Active → Deprecated is the
        // guard that prevents re-activating from a non-Active state).
        var act = () => schema.Deprecate(); // must throw because state is Deprecated

        // Assert
        act.Should().Throw<InvalidOperationException>();
        schema.Status.Should().Be(SchemaStatus.Deprecated);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static SchemaDefinition BuildActiveSchema() =>
        SchemaDefinition.Register(
            tenantId: "tenant-1",
            contractName: "PayslipGeneratedEvent",
            version: "v2.0",
            jsonSchema: "{}",
            compatibilityRule: SchemaCompatibilityRule.FullCompatible,
            ownerModule: "Payroll");
}
