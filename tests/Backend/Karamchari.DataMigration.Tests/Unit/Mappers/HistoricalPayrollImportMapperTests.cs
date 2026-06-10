// -----------------------------------------------------------------------
// <copyright file="HistoricalPayrollImportMapperTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Features.HistoricalPayroll;
using Karamchari.DataMigration.Services;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Mappers;

public sealed class HistoricalPayrollImportMapperTests
{
    private readonly HistoricalPayrollImportMapper _sut = new();

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
            ("employee number", "EMP030"),
            ("month", "3"),
            ("year", "2024"),
            ("gross pay", "60000.00"),
            ("deductions", "5000.00"),
            ("net pay", "55000.00"));

        var result = _sut.Map(row);

        result.EmployeeNumber.Should().Be("EMP030");
        result.Month.Should().Be(3);
        result.Year.Should().Be(2024);
        result.GrossPay.Should().Be(60000.00m);
        result.Deductions.Should().Be(5000.00m);
        result.NetPay.Should().Be(55000.00m);
    }

    // ------------------------------------------------------------------ alternate column names

    [Fact]
    public void Map_AlternateEmpId_MapsEmployeeNumber()
    {
        var row = MakeRow(2, ("emp id", "EMP031"));
        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP031");
    }

    [Fact]
    public void Map_AlternateGross_MapsGrossPay()
    {
        var row = MakeRow(3, ("gross", "70000"));
        var result = _sut.Map(row);
        result.GrossPay.Should().Be(70000m);
    }

    [Fact]
    public void Map_AlternateNet_MapsNetPay()
    {
        var row = MakeRow(4, ("net", "65000"));
        var result = _sut.Map(row);
        result.NetPay.Should().Be(65000m);
    }

    [Fact]
    public void Map_AlternateTotalDeductions_MapsDeductions()
    {
        var row = MakeRow(5, ("total deductions", "4500.50"));
        var result = _sut.Map(row);
        result.Deductions.Should().Be(4500.50m);
    }

    [Fact]
    public void Map_PrimaryGrossPayTakesPrecedenceOverGross()
    {
        var row = MakeRow(6,
            ("gross pay", "80000"),
            ("gross", "70000"));

        var result = _sut.Map(row);
        result.GrossPay.Should().Be(80000m);
    }

    [Fact]
    public void Map_PrimaryNetPayTakesPrecedenceOverNet()
    {
        var row = MakeRow(7,
            ("net pay", "75000"),
            ("net", "65000"));

        var result = _sut.Map(row);
        result.NetPay.Should().Be(75000m);
    }

    // ------------------------------------------------------------------ month / year parsing

    [Fact]
    public void Map_Month_ParsesAsInt()
    {
        var row = MakeRow(8, ("month", "12"));
        var result = _sut.Map(row);
        result.Month.Should().Be(12);
    }

    [Fact]
    public void Map_Year_ParsesAsInt()
    {
        var row = MakeRow(9, ("year", "2023"));
        var result = _sut.Map(row);
        result.Year.Should().Be(2023);
    }

    [Fact]
    public void Map_NonIntegerMonth_ReturnsNull()
    {
        var row = MakeRow(10,
            ("employee number", "EMP032"),
            ("month", "March"));

        var result = _sut.Map(row);
        result.Month.Should().BeNull();
    }

    [Fact]
    public void Map_NonIntegerYear_ReturnsNull()
    {
        var row = MakeRow(11,
            ("employee number", "EMP033"),
            ("year", "twenty-twenty-four"));

        var result = _sut.Map(row);
        result.Year.Should().BeNull();
    }

    // ------------------------------------------------------------------ decimal parsing

    [Fact]
    public void Map_NonNumericNetPay_ReturnsNull()
    {
        var row = MakeRow(12,
            ("employee number", "EMP034"),
            ("net pay", "N/A"));

        var result = _sut.Map(row);
        result.NetPay.Should().BeNull();
    }

    [Fact]
    public void Map_NetPayWithDecimalPlaces_ParsesCorrectly()
    {
        var row = MakeRow(13, ("net pay", "48250.99"));
        var result = _sut.Map(row);
        result.NetPay.Should().Be(48250.99m);
    }

    // ------------------------------------------------------------------ missing / empty handling

    [Fact]
    public void Map_AllEmptyRow_ReturnsRecordWithAllNulls_WithoutThrowing()
    {
        var row = new ImportRow(14, new Dictionary<string, string>());

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().BeNull();
        result.Month.Should().BeNull();
        result.Year.Should().BeNull();
        result.GrossPay.Should().BeNull();
        result.Deductions.Should().BeNull();
        result.NetPay.Should().BeNull();
    }

    [Fact]
    public void Map_ExtraUnrecognisedColumns_AreIgnored()
    {
        var row = MakeRow(15,
            ("employee number", "EMP035"),
            ("month", "6"),
            ("year", "2024"),
            ("net pay", "50000"),
            ("some_custom_field", "data"));

        var act = () => _sut.Map(row);
        act.Should().NotThrow();

        var result = _sut.Map(row);
        result.EmployeeNumber.Should().Be("EMP035");
        result.Month.Should().Be(6);
    }
}
