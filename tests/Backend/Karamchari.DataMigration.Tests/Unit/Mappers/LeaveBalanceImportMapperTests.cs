using FluentAssertions;
using Karamchari.DataMigration.Features.LeaveBalance;
using Karamchari.DataMigration.Services;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Mappers;

public sealed class LeaveBalanceImportMapperTests
{
    private readonly LeaveBalanceImportMapper _sut = new();

    private static ImportRow MakeRow(int rowNum, params (string key, string value)[] columns)
    {
        var data = columns.ToDictionary(c => c.key, c => c.value);
        return new ImportRow(rowNum, data);
    }

    // ------------------------------------------------------------------ happy path

    [Fact]
    public void Map_AllPrimaryColumns_MapsAllFieldsCorrectly()
    {
        var row = MakeRow(1,
            ("employee number", "EMP001"),
            ("leave type", "Annual Leave"),
            ("balance", "12.5"),
            ("effective date", "2024-01-01"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP001");
        result.LeaveTypeName.Should().Be("Annual Leave");
        result.Balance.Should().Be(12.5m);
        result.EffectiveDate.Should().Be("2024-01-01");
    }

    // ------------------------------------------------------------------ alternate column names

    [Fact]
    public void Map_AlternateEmpId_MapsEmployeeNumber()
    {
        var row = MakeRow(2, ("emp id", "EMP007"));
        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP007");
    }

    [Fact]
    public void Map_AlternatePolicy_MapsLeaveTypeName()
    {
        var row = MakeRow(3, ("policy", "Sick Leave"));
        var result = _sut.Map(row);
        result.LeaveTypeName.Should().Be("Sick Leave");
    }

    [Fact]
    public void Map_AlternateDate_MapsEffectiveDate()
    {
        var row = MakeRow(4, ("date", "2024-04-01"));
        var result = _sut.Map(row);
        result.EffectiveDate.Should().Be("2024-04-01");
    }

    [Fact]
    public void Map_PrimaryLeaveTypeTakesPrecedenceOverPolicy()
    {
        var row = MakeRow(5,
            ("leave type", "Casual Leave"),
            ("policy", "Annual Leave"));

        var result = _sut.Map(row);
        result.LeaveTypeName.Should().Be("Casual Leave");
    }

    [Fact]
    public void Map_PrimaryEffectiveDateTakesPrecedenceOverDate()
    {
        var row = MakeRow(6,
            ("effective date", "2024-01-01"),
            ("date", "2023-01-01"));

        var result = _sut.Map(row);
        result.EffectiveDate.Should().Be("2024-01-01");
    }

    // ------------------------------------------------------------------ balance parsing

    [Fact]
    public void Map_BalanceAsInteger_ParsesCorrectly()
    {
        var row = MakeRow(7, ("balance", "20"));
        var result = _sut.Map(row);
        result.Balance.Should().Be(20m);
    }

    [Fact]
    public void Map_BalanceAsDecimalWithTwoPlaces_ParsesCorrectly()
    {
        var row = MakeRow(8, ("balance", "7.50"));
        var result = _sut.Map(row);
        result.Balance.Should().Be(7.50m);
    }

    [Fact]
    public void Map_NonNumericBalance_ReturnsNull()
    {
        var row = MakeRow(9,
            ("employee number", "EMP001"),
            ("balance", "N/A"));

        var result = _sut.Map(row);
        result.Balance.Should().BeNull();
    }

    [Fact]
    public void Map_MissingBalanceColumn_ReturnsNullBalance()
    {
        var row = MakeRow(10,
            ("employee number", "EMP001"),
            ("leave type", "Annual Leave"));

        var result = _sut.Map(row);
        result.Balance.Should().BeNull();
    }

    // ------------------------------------------------------------------ missing required fields

    [Fact]
    public void Map_MissingEmployeeNumber_ReturnsNullEmployeeNumber()
    {
        var row = MakeRow(11,
            ("leave type", "Annual Leave"),
            ("balance", "5"));

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
    }

    [Fact]
    public void Map_AllEmptyRow_ReturnsRecordWithAllNulls_WithoutThrowing()
    {
        var row = new ImportRow(12, new Dictionary<string, string>());

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
        result.LeaveTypeName.Should().BeNull();
        result.Balance.Should().BeNull();
        result.EffectiveDate.Should().BeNull();
    }

    [Fact]
    public void Map_ExtraUnrecognisedColumns_AreIgnored()
    {
        var row = MakeRow(13,
            ("employee number", "EMP002"),
            ("balance", "10"),
            ("extra_column", "ignored"));

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP002");
        result.Balance.Should().Be(10m);
    }
}
