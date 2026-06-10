// -----------------------------------------------------------------------
// <copyright file="TdsGenerator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Karamchari.Payroll.Domain.Compliance;

namespace Karamchari.Payroll.Services.Compliance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface ITdsGenerator
{
    string Generate(IEnumerable<TdsRecord> records);
}

/// <summary>
/// Generates the fixed-width text format for TDS Form 24Q.
/// </summary>
public sealed class TdsGenerator : ITdsGenerator
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Generate(IEnumerable<TdsRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            Validate(record);

            var line =
                FormatField(record.Pan, 10) +
                FormatField(record.EmployeeName.ToUpperInvariant(), 75) +
                FormatField(Math.Round(record.AnnualIncome, 0).ToString("0", CultureInfo.InvariantCulture), 12) +
                FormatField(Math.Round(record.TotalTax, 0).ToString("0", CultureInfo.InvariantCulture), 12) +
                FormatField(Math.Round(record.TdsDeducted, 0).ToString("0", CultureInfo.InvariantCulture), 12) +
                FormatField(Math.Round(record.RemainingTax, 0).ToString("0", CultureInfo.InvariantCulture), 12);

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static void Validate(TdsRecord r)
    {
        if (string.IsNullOrWhiteSpace(r.Pan) || !Regex.IsMatch(r.Pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"))
            throw new InvalidOperationException($"Invalid PAN format for employee {r.EmployeeName}.");
    }

    private static string FormatField(string? value, int length)
    {
        value ??= string.Empty;
        if (value.Length > length) return value[..length];
        return value.PadRight(length);
    }
}
