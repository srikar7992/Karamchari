using System.Text;
using System.Text.RegularExpressions;
using Karamchari.Payroll.Domain.Compliance;

namespace Karamchari.Payroll.Services.Compliance;

public interface ITdsGenerator
{
    string Generate(IEnumerable<TdsRecord> records);
}

/// <summary>
/// Generates the fixed-width text format for TDS Form 24Q.
/// </summary>
public sealed class TdsGenerator : ITdsGenerator
{
    public string Generate(IEnumerable<TdsRecord> records)
    {
        var sb = new StringBuilder();

        foreach (var record in records)
        {
            Validate(record);

            var line = 
                FormatField(record.Pan, 10) +
                FormatField(record.EmployeeName.ToUpper(), 75) +
                FormatField(Math.Round(record.AnnualIncome, 0).ToString("0"), 12) +
                FormatField(Math.Round(record.TotalTax, 0).ToString("0"), 12) +
                FormatField(Math.Round(record.TdsDeducted, 0).ToString("0"), 12) +
                FormatField(Math.Round(record.RemainingTax, 0).ToString("0"), 12);

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private void Validate(TdsRecord r)
    {
        if (string.IsNullOrWhiteSpace(r.Pan) || !Regex.IsMatch(r.Pan, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$"))
            throw new InvalidOperationException($"Invalid PAN format for employee {r.EmployeeName}.");
    }

    private string FormatField(string value, int length)
    {
        if (value == null) value = string.Empty;
        if (value.Length > length) return value.Substring(0, length);
        return value.PadRight(length);
    }
}
