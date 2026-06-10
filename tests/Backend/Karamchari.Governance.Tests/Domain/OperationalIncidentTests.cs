// -----------------------------------------------------------------------
// <copyright file="OperationalIncidentTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Governance.Domain.Incidents;
using Xunit;

namespace Karamchari.Governance.Tests.Domain;

public class OperationalIncidentTests
{
    // ---------------------------------------------------------------
    // Declare
    // ---------------------------------------------------------------

    [Fact]
    public void OperationalIncident_Declare_StatusIsInvestigating()
    {
        // Act
        var incident = OperationalIncident.Declare(
            tenantId: "tenant-1",
            title: "Database latency spike",
            severity: IncidentSeverity.Sev2High,
            component: "CoreDB");

        // Assert
        incident.Status.Should().Be(IncidentStatus.Investigating);
        incident.Title.Should().Be("Database latency spike");
        incident.Severity.Should().Be(IncidentSeverity.Sev2High);
        incident.AffectedComponent.Should().Be("CoreDB");
        incident.Id.Should().NotBeEmpty();
        incident.ResolvedAtUtc.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // Resolve
    // ---------------------------------------------------------------

    [Fact]
    public void OperationalIncident_Resolve_SetsResolvedAt()
    {
        // Arrange
        var incident = BuildSev3Incident();

        // Act
        incident.Resolve();

        // Assert
        incident.ResolvedAtUtc.Should().NotBeNull();
        incident.Status.Should().Be(IncidentStatus.Resolved);
    }

    // ---------------------------------------------------------------
    // Sev1/Sev2 — postmortem required before close
    // ---------------------------------------------------------------

    [Fact]
    public void OperationalIncident_Sev1_CompletePostmortemRequired_BeforeClose()
    {
        // Arrange
        var incident = OperationalIncident.Declare(
            "tenant-1", "Platform Down", IncidentSeverity.Sev1Critical, "Auth");

        // Act
        incident.Resolve();

        // Assert — Sev1 transitions to PostmortemRequired, not Resolved
        incident.Status.Should().Be(IncidentStatus.PostmortemRequired);
    }

    [Fact]
    public void OperationalIncident_Sev2_CompletePostmortemRequired_BeforeClose()
    {
        // Arrange
        var incident = OperationalIncident.Declare(
            "tenant-1", "Major feature degraded", IncidentSeverity.Sev2High, "Billing");

        // Act
        incident.Resolve();

        // Assert — Sev2 also requires postmortem
        incident.Status.Should().Be(IncidentStatus.PostmortemRequired);
    }

    // ---------------------------------------------------------------
    // Sev3/Sev4 — can close without postmortem
    // ---------------------------------------------------------------

    [Fact]
    public void OperationalIncident_Sev3_CanCloseWithoutPostmortem()
    {
        // Arrange
        var incident = BuildSev3Incident();

        // Act
        incident.Resolve();

        // Assert — Sev3 goes directly to Resolved, NOT PostmortemRequired
        incident.Status.Should().Be(IncidentStatus.Resolved);
        // Verify no postmortem call required (it would throw if called now)
        var act = () => incident.CompletePostmortem("root", "[]");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Postmortem is not currently required*");
    }

    // ---------------------------------------------------------------
    // CompletePostmortem — transitions PostmortemRequired → Closed
    // ---------------------------------------------------------------

    [Fact]
    public void OperationalIncident_CompletePostmortem_SetsStatusToClosed()
    {
        // Arrange
        var incident = OperationalIncident.Declare(
            "tenant-1", "Platform Down", IncidentSeverity.Sev1Critical, "Auth");
        incident.Resolve(); // → PostmortemRequired

        // Act
        incident.CompletePostmortem(
            rootCause: "Memory leak in auth service",
            actionItemsJson: "[{\"item\":\"Add memory monitoring\"}]");

        // Assert
        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.RootCauseSummary.Should().Be("Memory leak in auth service");
        incident.ActionItemsJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OperationalIncident_CompletePostmortem_WhenNotRequired_Throws()
    {
        // Arrange
        var incident = BuildSev3Incident(); // Sev3 → Resolved, not PostmortemRequired

        incident.Resolve();

        // Act
        var act = () => incident.CompletePostmortem("cause", "[]");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Postmortem is not currently required*");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static OperationalIncident BuildSev3Incident() =>
        OperationalIncident.Declare(
            tenantId: "tenant-1",
            title: "UI glitch on dashboard",
            severity: IncidentSeverity.Sev3Medium,
            component: "Dashboard");
}
