// -----------------------------------------------------------------------
// <copyright file="EcrGenerator.cs" company="Karamchari">
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
public interface IEcrGenerator
{
    string Generate(IEnumerable<EcrRecord> records);
}

/// <summary>
/// Generates the EPF ECR (Electronic Challan cum Return) pipe-separated text file.
/// Adheres to strict EPFO formatting rules.
/// </summary>
public sealed class EcrGenerator : IEcrGenerator
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Generate(IEnumerable<EcrRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            Validate(record);

            var line = string.Join("|",
                record.Uan,
                SanitizeName(record.MemberName),
                Format(record.GrossWages),
                Format(record.EpfWages),
                Format(record.EpsWages),
                Format(record.EdliWages),
                Format(record.EpfContribution),
                Format(record.EpsContribution),
                Format(record.EpfEpsDiff),
                record.NcpDays.ToString(CultureInfo.InvariantCulture),
                Format(record.RefundOfAdvances)
            );

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static void Validate(EcrRecord r)
    {
        if (string.IsNullOrWhiteSpace(r.Uan) || r.Uan.Length != 12 || !r.Uan.All(char.IsDigit))
            throw new InvalidOperationException($"Invalid UAN for member {r.MemberName}. Must be 12 digits.");

        if (r.EpsContribution > 1250.01m)
            throw new InvalidOperationException($"EPS contribution exceeds statutory limit of 1250 for member {r.MemberName}.");

        if (r.EpfContribution < r.EpsContribution)
            throw new InvalidOperationException($"EPF contribution cannot be less than EPS contribution for member {r.MemberName}.");
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "UNKNOWN";
        var sanitized = Regex.Replace(name.ToUpperInvariant(), @"[^A-Z\s]", " ");
        return Regex.Replace(sanitized, @"\s+", " ").Trim();
    }

    /// <summary>
    /// EPFO rejects decimals. Values must be rounded to nearest whole integer.
    /// </summary>
    private static string Format(decimal value)
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
    }
}
