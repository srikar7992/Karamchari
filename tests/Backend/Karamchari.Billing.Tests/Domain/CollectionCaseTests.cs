// -----------------------------------------------------------------------
// <copyright file="CollectionCaseTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Billing.Domain.Collections;
using Xunit;

namespace Karamchari.Billing.Tests.Domain;

public class CollectionCaseTests
{
    private static CollectionCase CreateCase(decimal amount = 5000m, int days = 10)
    {
        return CollectionCase.Create(
            tenantId: "tenant-1",
            invoiceId: Guid.NewGuid(),
            amount: amount,
            days: days);
    }

    [Fact]
    public void CollectionCase_Create_SetsStatusToActive()
    {
        // Act
        var collectionCase = CreateCase();

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Active);
        collectionCase.OutstandingAmount.Should().Be(5000m);
        collectionCase.DaysOutstanding.Should().Be(10);
        collectionCase.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void CollectionCase_UpdateStatus_WhenOutstandingZero_ClosesCase()
    {
        // Arrange
        var collectionCase = CreateCase(amount: 5000m, days: 45);

        // Act
        collectionCase.UpdateStatus(outstanding: 0m, days: 50);

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Closed);
        collectionCase.OutstandingAmount.Should().Be(0m);
        collectionCase.DaysOutstanding.Should().Be(50);
    }

    [Fact]
    public void CollectionCase_UpdateStatus_WhenOutstandingPositive_RemainsActive()
    {
        // Arrange
        var collectionCase = CreateCase(amount: 5000m, days: 15);

        // Act
        collectionCase.UpdateStatus(outstanding: 2500m, days: 20);

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Active);
        collectionCase.OutstandingAmount.Should().Be(2500m);
        collectionCase.DaysOutstanding.Should().Be(20);
    }

    [Fact]
    public void CollectionCase_MarkDisputed_SetsStatusToDisputed()
    {
        // Arrange
        var collectionCase = CreateCase();

        // Act
        collectionCase.MarkDisputed();

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Disputed);
    }

    [Fact]
    public void CollectionCase_RecordReminder_At60dStage_EscalatesCase()
    {
        // Arrange
        var collectionCase = CreateCase(days: 62);

        // Act
        collectionCase.RecordReminder("60d");

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Escalated);
        collectionCase.CurrentStage.Should().Be("60d");
        collectionCase.ReminderCount.Should().Be(1);
        collectionCase.LastActionAt.Should().NotBeNull();
    }

    [Fact]
    public void CollectionCase_RecordReminder_AtEarlierStage_DoesNotEscalate()
    {
        // Arrange
        var collectionCase = CreateCase(days: 32);

        // Act — record a 30d reminder (not the 60d escalation trigger)
        collectionCase.RecordReminder("30d");

        // Assert
        collectionCase.Status.Should().Be(CollectionStatus.Active);
        collectionCase.CurrentStage.Should().Be("30d");
        collectionCase.ReminderCount.Should().Be(1);
    }
}
