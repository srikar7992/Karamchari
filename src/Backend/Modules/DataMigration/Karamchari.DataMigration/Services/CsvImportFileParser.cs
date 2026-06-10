// -----------------------------------------------------------------------
// <copyright file="CsvImportFileParser.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Implementation of <see cref="IImportFileParser"/> for CSV files.
/// </summary>
public sealed class CsvImportFileParser : IImportFileParser
{
    public string SupportedExtension => ".csv";

    public async IAsyncEnumerable<ImportRow> ParseAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.ToLower(CultureInfo.InvariantCulture),
            TrimOptions = TrimOptions.Trim
        });

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers == null) yield break;

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                data[header] = csv.GetField(header) ?? string.Empty;
            }

            yield return new ImportRow(++rowNumber, data);
        }
    }
}
