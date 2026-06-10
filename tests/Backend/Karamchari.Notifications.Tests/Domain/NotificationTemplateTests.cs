// -----------------------------------------------------------------------
// <copyright file="NotificationTemplateTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Notifications.Domain;
using Xunit;

namespace Karamchari.Notifications.Tests.Domain;

public sealed class NotificationTemplateTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static NotificationTemplate BuildDefaultTemplate(
        string? tenantId = null,
        string? code = null)
    {
        return NotificationTemplate.CreateDefault(
            tenantId: tenantId ?? "tenant-001",
            code: code ?? "review.deadline.reminder",
            category: NotificationCategory.ReviewWorkflow,
            channel: NotificationChannel.Email,
            locale: "en-US",
            subject: "Your review is due on {{DueDate}}",
            bodyHtml: "<p>Hello {{EmployeeName}}, your review is due.</p>",
            bodyText: "Hello {{EmployeeName}}, your review is due.",
            previewText: "Review due soon");
    }

    // ─── CreateDefault ───────────────────────────────────────────────────────

    [Fact]
    public void NotificationTemplate_CreateDefault_SetsIsCustomFalse()
    {
        var template = BuildDefaultTemplate();

        template.IsCustom.Should().BeFalse();
    }

    [Fact]
    public void NotificationTemplate_CreateDefault_IsActive()
    {
        var template = BuildDefaultTemplate();

        template.IsActive.Should().BeTrue();
    }

    // ─── Customize ───────────────────────────────────────────────────────────

    [Fact]
    public void NotificationTemplate_Customize_SetsIsCustomTrue()
    {
        var template = BuildDefaultTemplate();

        template.Customize(
            subject: "Custom: Review Due {{DueDate}}",
            bodyHtml: "<p>Custom body for {{EmployeeName}}</p>",
            bodyText: "Custom body for {{EmployeeName}}",
            previewText: "Custom preview");

        template.IsCustom.Should().BeTrue();
    }

    [Fact]
    public void NotificationTemplate_Customize_UpdatesSubjectAndBody()
    {
        var template = BuildDefaultTemplate();
        const string newSubject = "Updated subject {{DueDate}}";
        const string newHtml = "<p>Updated HTML</p>";
        const string newText = "Updated text";

        template.Customize(newSubject, newHtml, newText, previewText: null);

        template.Subject.Should().Be(newSubject);
        template.BodyHtml.Should().Be(newHtml);
        template.BodyText.Should().Be(newText);
        template.PreviewText.Should().BeNull();
    }

    // ─── Deactivate / Activate ───────────────────────────────────────────────

    [Fact]
    public void NotificationTemplate_Deactivate_SetsIsActiveFalse()
    {
        var template = BuildDefaultTemplate();

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void NotificationTemplate_Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var template = BuildDefaultTemplate();
        template.Deactivate();

        template.Activate();

        template.IsActive.Should().BeTrue();
    }
}
