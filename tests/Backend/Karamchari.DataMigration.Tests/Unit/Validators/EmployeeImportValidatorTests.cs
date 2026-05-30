using FluentAssertions;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Services;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Validators;

public sealed class EmployeeImportValidatorTests
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly EmployeeImportValidator _sut;

    public EmployeeImportValidatorTests()
    {
        _sut = new EmployeeImportValidator(_resolver);
    }

    private static EmployeeImportRecord ValidRecord() => new()
    {
        EmployeeNumber = "EMP001",
        FirstName = "Arjun",
        LastName = "Sharma",
        Email = "arjun@example.com",
        DateOfJoining = "2022-01-15"
    };

    // ------------------------------------------------------------------ happy path

    [Fact]
    public async Task ValidateAsync_ValidRecord_ReturnsValid()
    {
        var record = ValidRecord();

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_NoDepartmentOrManager_ReturnsValid()
    {
        var record = ValidRecord() with { DepartmentName = null, ManagerEmployeeNumber = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NoDoj_ReturnsValid()
    {
        var record = ValidRecord() with { DateOfJoining = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_DepartmentFound_ReturnsValid()
    {
        var record = ValidRecord() with { DepartmentName = "Engineering" };
        _resolver.ResolveDepartmentIdAsync("Engineering", Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ManagerFound_ReturnsValid()
    {
        var record = ValidRecord() with { ManagerEmployeeNumber = "EMP000" };
        _resolver.ResolveEmployeeIdAsync("EMP000", Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    // ------------------------------------------------------------------ required field errors

    [Fact]
    public async Task ValidateAsync_MissingEmployeeNumber_ReturnsError()
    {
        var record = ValidRecord() with { EmployeeNumber = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    [Fact]
    public async Task ValidateAsync_WhitespaceEmployeeNumber_ReturnsError()
    {
        var record = ValidRecord() with { EmployeeNumber = "   " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    [Fact]
    public async Task ValidateAsync_MissingEmail_ReturnsError()
    {
        var record = ValidRecord() with { Email = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email is required.");
    }

    [Fact]
    public async Task ValidateAsync_MissingFirstName_ReturnsError()
    {
        var record = ValidRecord() with { FirstName = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("First Name is required.");
    }

    // ------------------------------------------------------------------ date of joining

    [Fact]
    public async Task ValidateAsync_InvalidDateOfJoiningFormat_ReturnsError()
    {
        // Verify the validator rejects a clearly non-date string.
        // "not-a-date" cannot be parsed by DateOnly.TryParse under any locale.
        var record = ValidRecord() with { DateOfJoining = "notadate!!" };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Date of Joining format");
        result.ErrorMessage.Should().Contain("notadate!!");
    }

    [Fact]
    public async Task ValidateAsync_DateOfJoiningRandomString_ReturnsError()
    {
        var record = ValidRecord() with { DateOfJoining = "not-a-date" };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Date of Joining format");
    }

    // ------------------------------------------------------------------ reference resolution

    [Fact]
    public async Task ValidateAsync_DepartmentNotFound_ReturnsError()
    {
        var record = ValidRecord() with { DepartmentName = "UnknownDept" };
        _resolver.ResolveDepartmentIdAsync("UnknownDept", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("UnknownDept");
        result.ErrorMessage.Should().Contain("could not be resolved");
    }

    [Fact]
    public async Task ValidateAsync_ManagerNotFound_ReturnsError()
    {
        var record = ValidRecord() with { ManagerEmployeeNumber = "EMP999" };
        _resolver.ResolveEmployeeIdAsync("EMP999", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EMP999");
        result.ErrorMessage.Should().Contain("could not be resolved");
    }

    [Fact]
    public async Task ValidateAsync_EmployeeNumberMissing_NeverCallsResolver()
    {
        var record = ValidRecord() with { EmployeeNumber = null, DepartmentName = "Engineering" };

        await _sut.ValidateAsync(record);

        await _resolver.DidNotReceive().ResolveDepartmentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
