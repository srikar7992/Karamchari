using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using Karamchari.Recruitment.Contracts;
using MassTransit;

namespace Karamchari.HR.Consumers;

public sealed class CreateEmployeeOnCandidateHiredConsumer(HRDbContext dbContext) 
    : IConsumer<CandidateHiredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CandidateHiredIntegrationEvent> context)
    {
        var message = context.Message;

        // In a real system, we'd check if employee already exists (idempotency)
        // and handle departmental mapping, etc.
        
        var employee = Employee.Hire(
            $"EMP-{message.CandidateId.ToString().Substring(0, 8).ToUpperInvariant()}",
            $"{message.FirstName} {message.LastName}",
            message.Email,
            DateOnly.FromDateTime(DateTime.UtcNow));

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();
    }
}
