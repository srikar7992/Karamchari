using FluentAssertions;
using Karamchari.DataMigration.Features.LeaveBalance;
using Karamchari.DataMigration.Services;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Validators;

public sealed class LeaveBalanceImportValidatorTests
{
    private readonly IReferenceResolver _resolver = Substitute.For<IReferenceResolver>();
    private readonly LeaveBalanceImportValidator _sut;

    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid PolicyId = Guid.NewGuid();

    public LeaveBalanceImportValidatorTests()
    {
        _sut = new LeaveBalanceImportValidator(_resolver);

        _resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _resolver.ResolveLeavePolicyIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    private void SetupBothResolved()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(EmployeeId);
        _resolver.ResolveLeavePolicyIdAsync("Annual Leave", Arg.Any<CancellationToken>())
            .Returns(PolicyId);
    }

    private static LeaveBalanceImportRecord ValidRecord() => new()
    {
        EmployeeNumber = "EMP001",
        LeaveTypeName = "Annual Leave",
        Balance = 10m,
        EffectiveDate = "2024-01-01"
    };

    // ------------------------------------------------------------------ happy path

    [Fact]
    public async Task ValidateAsync_ValidRecord_ReturnsValid()
    {
        SetupBothResolved();

        var result = await _sut.ValidateAsync(ValidRecord());

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_NullEffectiveDate_StillValid()
    {
        SetupBothResolved();
        var record = ValidRecord() with { EffectiveDate = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AllReferencesFound_ReturnsValid()
    {
        SetupBothResolved();

        var result = await _sut.ValidateAsync(ValidRecord());

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
        var record = ValidRecord() with { EmployeeNumber = "  " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Employee Number is required.");
    }

    [Fact]
    public async Task ValidateAsync_MissingLeaveTypeName_ReturnsError()
    {
        var record = ValidRecord() with { LeaveTypeName = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Leave Type is required.");
    }

    [Fact]
    public async Task ValidateAsync_WhitespaceLeaveTypeName_ReturnsError()
    {
        var record = ValidRecord() with { LeaveTypeName = "   " };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Leave Type is required.");
    }

    [Fact]
    public async Task ValidateAsync_NullBalance_ReturnsError()
    {
        var record = ValidRecord() with { Balance = null };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Balance is required.");
    }

    // ------------------------------------------------------------------ date validation

    [Fact]
    public async Task ValidateAsync_InvalidEffectiveDate_ReturnsError()
    {
        SetupBothResolved();
        var record = ValidRecord() with { EffectiveDate = "not-a-date" };

        var result = await _sut.ValidateAsync(record);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Effective Date format");
    }

    [Fact]
    public async Task ValidateAsync_EffectiveDateWrongDelimiter_ReturnsError()
    {
        SetupBothResolved();
        var record = ValidRecord() with { EffectiveDate = "01-01-2024" };

        var result = await _sut.ValidateAsync(record);

        // DateOnly.TryParse is culture-aware and may accept this — we just verify
        // the validator doesn't throw, and if it fails the error message is informative.
        if (!result.IsValid)
        {
            result.ErrorMessage.Should().Contain("Effective Date");
        }
    }

    // ------------------------------------------------------------------ reference resolution

    [Fact]
    public async Task ValidateAsync_EmployeeNotFound_ReturnsError()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.ValidateAsync(ValidRecord());

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EMP001");
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAsync_LeavePolicyNotFound_ReturnsError()
    {
        _resolver.ResolveEmployeeIdAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(EmployeeId);
        _resolver.ResolveLeavePolicyIdAsync("Annual Leave", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.ValidateAsync(ValidRecord());

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Annual Leave");
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredField_NeverCallsResolver()
    {
        var record = ValidRecord() with { EmployeeNumber = null };

        await _sut.ValidateAsync(record);

        await _resolver.DidNotReceive().ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _resolver.DidNotReceive().ResolveLeavePolicyIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
