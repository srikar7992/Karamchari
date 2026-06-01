using FluentAssertions;
using Karamchari.Billing.Domain.BillableEntries;
using Xunit;

namespace Karamchari.Billing.Tests.Domain;

public class BillableEntryTests
{
    private static BillableEntry CreateEntry(decimal hours = 8m, decimal rate = 100m, bool isVoided = false, bool isBilled = false, Guid? invoiceId = null)
    {
        var entry = new BillableEntry
        {
            TenantId = "tenant-1",
            TimesheetEntryId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            WorkDate = new DateOnly(2025, 1, 15),
            Hours = hours,
            Rate = rate,
            Amount = hours * rate
        };

        // Apply state via methods if needed
        if (isBilled && invoiceId.HasValue)
            entry.MarkBilled(invoiceId.Value);
        else if (isVoided)
            entry.Void();

        return entry;
    }

    [Fact]
    public void BillableEntry_Create_SetsCorrectAmountHoursMultipliedByRate()
    {
        // Arrange
        const decimal hours = 8m;
        const decimal rate = 125m;

        // Act
        var entry = new BillableEntry
        {
            TenantId = "tenant-1",
            TimesheetEntryId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            WorkDate = new DateOnly(2025, 1, 15),
            Hours = hours,
            Rate = rate,
            Amount = hours * rate
        };

        // Assert
        entry.Amount.Should().Be(1000m);
        entry.Hours.Should().Be(hours);
        entry.Rate.Should().Be(rate);
    }

    [Fact]
    public void BillableEntry_Void_SetsIsVoidedTrue()
    {
        // Arrange
        var entry = CreateEntry();

        // Act
        entry.Void();

        // Assert
        entry.IsVoided.Should().BeTrue();
    }

    [Fact]
    public void BillableEntry_Void_WhenAlreadyBilled_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var entry = CreateEntry(isBilled: true, invoiceId: invoiceId);

        // Act
        var act = () => entry.Void();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*billed*");
    }

    [Fact]
    public void BillableEntry_Void_WhenAlreadyVoided_IsIdempotent()
    {
        // Arrange
        var entry = CreateEntry();
        entry.Void();

        // Act — calling Void a second time on an already-voided (but not billed) entry
        var act = () => entry.Void();

        // Assert — should not throw, IsVoided stays true
        act.Should().NotThrow();
        entry.IsVoided.Should().BeTrue();
    }

    [Fact]
    public void BillableEntry_MarkBilled_SetsIsBilledAndInvoiceId()
    {
        // Arrange
        var entry = CreateEntry();
        var invoiceId = Guid.NewGuid();

        // Act
        entry.MarkBilled(invoiceId);

        // Assert
        entry.IsBilled.Should().BeTrue();
        entry.InvoiceId.Should().Be(invoiceId);
    }
}
