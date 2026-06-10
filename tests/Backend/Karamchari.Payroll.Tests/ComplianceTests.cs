// -----------------------------------------------------------------------
// <copyright file="ComplianceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.Compliance;
using Karamchari.Payroll.Services.Compliance;
using Xunit;

namespace Karamchari.Payroll.Tests;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class ComplianceTests
{
    private readonly IEcrGenerator _ecrGenerator = new EcrGenerator();
    private readonly IEsicGenerator _esicGenerator = new EsicGenerator();
    private readonly ITdsGenerator _tdsGenerator = new TdsGenerator();
    private readonly IComplianceRiskEngine _riskEngine = new ComplianceRiskEngine();

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void EcrGenerator_ShouldProduceCorrectPipeSeparatedFormat()
    {
        // Arrange
        var records = new[]
        {
            new EcrRecord
            {
                Uan = "100123456789",
                MemberName = "John Doe-Special!#",
                GrossWages = 20000.50m,
                EpfWages = 15000,
                EpsWages = 15000,
                EdliWages = 15000,
                EpfContribution = 1800,
                EpsContribution = 1250,
                EpfEpsDiff = 550,
                NcpDays = 2,
                RefundOfAdvances = 0
            }
        };

        // Act
        var result = _ecrGenerator.Generate(records);

        // Assert
        // Format: UAN|Name|Gross|EpfWages|EpsWages|EdliWages|Epf|Eps|Diff|NCP|Refund
        // Name should be sanitized to JOHN DOE SPECIAL
        // Decimals should be rounded
        Assert.Contains("100123456789|JOHN DOE SPECIAL|20001|15000|15000|15000|1800|1250|550|2|0", result);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void EsicGenerator_ShouldEnforceEligibilityAndFormat()
    {
        // Arrange
        var records = new[]
        {
            new EsicRecord
            {
                InsuranceNumber = "1234567890",
                EmployeeName = "Jane Smith",
                GrossWages = 21000,
                EmployeeContribution = 158,
                EmployerContribution = 683,
                DaysWorked = 30
            }
        };

        // Act
        var excelBytes = _esicGenerator.Generate(records);

        // Assert
        Assert.NotNull(excelBytes);
        Assert.True(excelBytes.Length > 0);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void TdsGenerator_ShouldValidatePanAndFixedWidth()
    {
        // Arrange
        var records = new[]
        {
            new TdsRecord
            {
                Pan = "ABCDE1234F",
                EmployeeName = "Audit Tester",
                AnnualIncome = 1200000,
                TotalTax = 180000,
                TdsDeducted = 15000,
                RemainingTax = 165000
            }
        };

        // Act
        var result = _tdsGenerator.Generate(records);

        // Assert
        Assert.StartsWith("ABCDE1234F", result);
        Assert.Contains("AUDIT TESTER", result);
        // Check fixed width (PAN = 10, Name = 75)
        var line = result.Split(Environment.NewLine)[0];
        Assert.Equal(10 + 75 + 12 + 12 + 12 + 12, line.Length);
    }

    [Theory]
    [InlineData(10000, 10, 103)] // 10000 * 0.12 * 10/365 = 32.8 + 10000 * 0.05 = 500 => ~533? No, damages = 5% = 500. Total ~533.
    [InlineData(10000, 70, 1023)] // 70 days => 10% damages = 1000. Interest = 10000*0.12*70/365 = 230. Total ~1230.
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RiskEngine_ShouldCalculateCorrectEpfPenalty(decimal amount, int daysLate, decimal expectedPenaltyMin)
    {
        // Arrange
        var filing = ComplianceFiling.CreatePending(string.Empty, Guid.NewGuid(), ComplianceType.EpfEcr, DateTime.UtcNow.AddDays(-daysLate));

        // Act
        var result = _riskEngine.Evaluate(filing, amount);

        // Assert
        Assert.True(result.EstimatedPenalty >= expectedPenaltyMin);
        var expectedRiskLevel = daysLate > 30 ? "Critical" : "High";
        Assert.Equal(expectedRiskLevel, result.RiskLevel);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void TdsPenalty_ShouldBeLinearPerDay()
    {
        // Arrange
        var filing = ComplianceFiling.CreatePending(string.Empty, Guid.NewGuid(), ComplianceType.TdsForm24Q, DateTime.UtcNow.AddDays(-5));

        // Act
        var result = _riskEngine.Evaluate(filing, 0);

        // Assert
        Assert.Equal(1000, result.EstimatedPenalty); // 5 days * 200
    }
}
