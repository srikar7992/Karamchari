// -----------------------------------------------------------------------
// <copyright file="EmployeeImportProcessor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;
using Karamchari.HR.Contracts.Employees;
using MassTransit;

namespace Karamchari.DataMigration.Features.Employees;

/// <summary>
/// Implementation of <see cref="IImportProcessor{T}"/> for Employee records.
/// Executes the import by publishing onboarding commands to the bus.
/// </summary>
public sealed class EmployeeImportProcessor : IImportProcessor<EmployeeImportRecord>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EmployeeImportProcessor(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public string ImportType => "EmployeeImport";

    public async Task ProcessBatchAsync(IEnumerable<EmployeeImportRecord> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            // We map the import record to the HR onboarding command.
            // Note: In a production scenario, we'd also handle Department/Designation/Manager 
            // mapping here if the OnboardEmployeeCommand supported them.
            var doj = !string.IsNullOrWhiteSpace(item.DateOfJoining)
                ? DateOnly.Parse(item.DateOfJoining)
                : DateOnly.FromDateTime(DateTime.Today);

            var command = new OnboardEmployeeCommand(
                item.EmployeeNumber!,
                $"{item.FirstName} {item.LastName}",
                item.Email,
                doj);

            await _publishEndpoint.Publish(command, ct);
        }
    }
}
