// -----------------------------------------------------------------------
// <copyright file="IImportPipeline.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.API;
using Karamchari.DataMigration.Domain.Importing;

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Type-erased pipeline that coordinates map → validate → process for a specific import type.
/// Register one implementation per import type and resolve via IEnumerable&lt;IImportPipeline&gt;.
/// </summary>
public interface IImportPipeline
{
    string ImportType { get; }

    Task<IEnumerable<object>> PreviewAsync(
        Stream stream,
        IImportFileParser parser,
        int maxRows = 10,
        CancellationToken ct = default);

    Task<(int validCount, int invalidCount, List<RowError> errors)> ValidateAsync(
        Stream stream,
        IImportFileParser parser,
        CancellationToken ct = default);

    Task<(int success, int failed, int skipped)> ProcessAsync(
        Stream stream,
        IImportFileParser parser,
        ImportJob job,
        CancellationToken ct = default);
}
