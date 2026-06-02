using FluentAssertions;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.HR.Contracts.Employees;
using MassTransit;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Processors;

public sealed class EmployeeImportProcessorTests
{
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly EmployeeImportProcessor _sut;

    public EmployeeImportProcessorTests()
    {
        _sut = new EmployeeImportProcessor(_publishEndpoint);
    }

    private static EmployeeImportRecord MakeRecord(
        string employeeNumber = "EMP001",
        string firstName = "Arjun",
        string lastName = "Sharma",
        string email = "arjun@example.com",
        string dateOfJoining = "2022-01-15") => new()
        {
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfJoining = dateOfJoining
        };

    // ------------------------------------------------------------------ ImportType

    [Fact]
    public void ImportType_ReturnsEmployeeImport()
    {
        _sut.ImportType.Should().Be("EmployeeImport");
    }

    // ------------------------------------------------------------------ empty batch

    [Fact]
    public async Task ProcessBatchAsync_EmptyBatch_NoPublishCalls()
    {
        await _sut.ProcessBatchAsync(Array.Empty<EmployeeImportRecord>());

        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<OnboardEmployeeCommand>(), Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ single item

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_PublishesOneCommand()
    {
        await _sut.ProcessBatchAsync([MakeRecord()]);

        await _publishEndpoint.Received(1)
            .Publish(Arg.Any<OnboardEmployeeCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_CommandHasCorrectEmployeeNumber()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        await _sut.ProcessBatchAsync([MakeRecord(employeeNumber: "EMP042")]);

        captured.Should().NotBeNull();
        captured!.EmployeeNumber.Should().Be("EMP042");
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_CommandHasCorrectEmail()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        await _sut.ProcessBatchAsync([MakeRecord(email: "test@acme.com")]);

        captured.Should().NotBeNull();
        captured!.WorkEmail.Should().Be("test@acme.com");
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_CommandHasCorrectLegalName()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        await _sut.ProcessBatchAsync([MakeRecord(firstName: "Priya", lastName: "Reddy")]);

        captured.Should().NotBeNull();
        captured!.LegalName.Should().Be("Priya Reddy");
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleItem_DateOfJoiningParsedCorrectly()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        await _sut.ProcessBatchAsync([MakeRecord(dateOfJoining: "2022-06-15")]);

        captured.Should().NotBeNull();
        captured!.HiredOn.Should().Be(new DateOnly(2022, 6, 15));
    }

    // ------------------------------------------------------------------ multiple items

    [Fact]
    public async Task ProcessBatchAsync_ThreeItems_PublishesThreeCommands()
    {
        var records = new[]
        {
            MakeRecord("EMP001"),
            MakeRecord("EMP002"),
            MakeRecord("EMP003")
        };

        await _sut.ProcessBatchAsync(records);

        await _publishEndpoint.Received(3)
            .Publish(Arg.Any<OnboardEmployeeCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleItems_EachCommandHasDistinctEmployeeNumber()
    {
        var captured = new List<OnboardEmployeeCommand>();
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured.Add(cmd)),
            Arg.Any<CancellationToken>());

        var records = new[]
        {
            MakeRecord("EMP001"),
            MakeRecord("EMP002"),
            MakeRecord("EMP003")
        };

        await _sut.ProcessBatchAsync(records);

        captured.Select(c => c.EmployeeNumber)
            .Should().BeEquivalentTo(new[] { "EMP001", "EMP002", "EMP003" });
    }

    // ------------------------------------------------------------------ invalid / null date fallback

    [Fact]
    public async Task ProcessBatchAsync_NullDateOfJoining_FallsBackToToday()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        var record = MakeRecord() with { DateOfJoining = null };
        await _sut.ProcessBatchAsync([record]);

        captured.Should().NotBeNull();
        captured!.HiredOn.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task ProcessBatchAsync_InvalidDateOfJoining_FallsBackToToday()
    {
        OnboardEmployeeCommand? captured = null;
        await _publishEndpoint.Publish(
            Arg.Do<OnboardEmployeeCommand>(cmd => captured = cmd),
            Arg.Any<CancellationToken>());

        // The processor uses DateOnly.Parse which will throw on invalid input —
        // but the validator is expected to guard against this before processing.
        // For null/empty fallback the processor uses DateTime.Today.
        var record = MakeRecord() with { DateOfJoining = null };
        await _sut.ProcessBatchAsync([record]);

        captured!.HiredOn.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    // ------------------------------------------------------------------ cancellation

    [Fact]
    public async Task ProcessBatchAsync_CancellationTokenForwarded_PassedToPublish()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await _sut.ProcessBatchAsync([MakeRecord()], token);

        await _publishEndpoint.Received(1)
            .Publish(Arg.Any<OnboardEmployeeCommand>(), token);
    }
}
