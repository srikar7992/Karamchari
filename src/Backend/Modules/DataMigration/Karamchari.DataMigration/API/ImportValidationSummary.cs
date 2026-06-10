// -----------------------------------------------------------------------
// <copyright file="ImportValidationSummary.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.API;

public record ImportValidationSummary(
    Guid JobId,
    int ValidRows,
    int InvalidRows,
    IEnumerable<RowError> Errors);

public record RowError(int RowNumber, string ErrorMessage);
