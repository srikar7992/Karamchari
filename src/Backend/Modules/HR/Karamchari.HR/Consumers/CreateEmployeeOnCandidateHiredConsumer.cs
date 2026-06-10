// -----------------------------------------------------------------------
// <copyright file="CreateEmployeeOnCandidateHiredConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using Karamchari.Recruitment.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Consumers;

public sealed class CreateEmployeeOnCandidateHiredConsumer(HRDbContext dbContext)
    : IConsumer<CandidateHiredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CandidateHiredIntegrationEvent> context)
    {
        var message = context.Message;
        var employeeNumber = $"EMP-{message.CandidateId.ToString()[..8].ToUpperInvariant()}";

        // Idempotency guard: same CandidateId → same EmployeeNumber → skip if already materialized.
        // Without this, duplicate delivery (broker retry, replay attack, manual re-publish) would
        // hit the UX_Employees_TenantId_EmployeeNumber unique index, throw SqlException, and spin
        // the consumer in an infinite MassTransit retry loop.
        var alreadyExists = await dbContext.Employees
            .AnyAsync(e => e.EmployeeNumber == employeeNumber, context.CancellationToken);

        if (alreadyExists)
        {
            return; // idempotent — exactly-once semantics enforced
        }

        var employee = Employee.Hire(
            employeeNumber,
            $"{message.FirstName} {message.LastName}",
            message.Email,
            DateOnly.FromDateTime(message.HiredAt.UtcDateTime));

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
