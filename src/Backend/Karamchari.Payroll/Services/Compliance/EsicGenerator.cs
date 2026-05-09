using ClosedXML.Excel;
using Karamchari.Payroll.Domain.Compliance;

namespace Karamchari.Payroll.Services.Compliance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface IEsicGenerator
{
    byte[] Generate(IEnumerable<EsicRecord> records);
}

/// <summary>
/// Generates the ESIC Monthly Contribution Excel file.
/// Only employees with Gross &lt;= 21000 are eligible.
/// </summary>
public sealed class EsicGenerator : IEsicGenerator
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] Generate(IEnumerable<EsicRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("ESIC_Monthly_Contribution");

        // Headers
        worksheet.Cell(1, 1).Value = "IP Number";
        worksheet.Cell(1, 2).Value = "Employee Name";
        worksheet.Cell(1, 3).Value = "Gross Wages";
        worksheet.Cell(1, 4).Value = "Employee Contribution";
        worksheet.Cell(1, 5).Value = "Employer Contribution";
        worksheet.Cell(1, 6).Value = "Days Worked";

        // Styling headers
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var record in records)
        {
            Validate(record);

            worksheet.Cell(row, 1).Value = record.InsuranceNumber;
            worksheet.Cell(row, 2).Value = record.EmployeeName;
            worksheet.Cell(row, 3).Value = record.GrossWages;
            worksheet.Cell(row, 4).Value = record.EmployeeContribution;
            worksheet.Cell(row, 5).Value = record.EmployerContribution;
            worksheet.Cell(row, 6).Value = record.DaysWorked;

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void Validate(EsicRecord r)
    {
        if (string.IsNullOrWhiteSpace(r.InsuranceNumber))
            throw new InvalidOperationException($"Missing ESIC Insurance Number for {r.EmployeeName}.");

        if (r.GrossWages > 21000 && (r.EmployeeContribution > 0 || r.EmployerContribution > 0))
            throw new InvalidOperationException($"Employee {r.EmployeeName} exceeds the ESIC threshold of 21000 but has contributions.");
    }
}
