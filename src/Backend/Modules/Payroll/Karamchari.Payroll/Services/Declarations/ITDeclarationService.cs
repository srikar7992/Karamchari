// -----------------------------------------------------------------------
// <copyright file="ITDeclarationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Declarations;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;
using MassTransit;

/// <summary>
/// Service for managing the tax declaration lifecycle.
/// </summary>
public class ITDeclarationService
{
    private readonly IITDeclarationRepository _repo;
    private readonly IPublishEndpoint _publishEndpoint;

    public ITDeclarationService(IITDeclarationRepository repo, IPublishEndpoint publishEndpoint)
    {
        _repo = repo;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task ApproveAsync(Guid id, decimal approvedAmount, string approver)
    {
        var declaration = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Declaration not found.");

        declaration.Approve(approvedAmount, approver);

        await _repo.SaveChangesAsync();
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task RejectAsync(Guid id, string reason, string rejectedBy)
    {
        var declaration = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Declaration not found.");

        declaration.Reject(reason, rejectedBy);

        await _repo.SaveChangesAsync();

        // Trigger Notification to Employee
        // This event would be handled by a NotificationConsumer to send email/SMS/Push
        await _publishEndpoint.Publish(new ITDeclarationRejectedIntegrationEvent(
            declaration.Id,
            declaration.EmployeeId,
            declaration.Category,
            reason
        ));
    }
}
