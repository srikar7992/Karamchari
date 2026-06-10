// -----------------------------------------------------------------------
// <copyright file="TemplateRendererTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Rendering;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.Notifications.Tests.Services;

public sealed class TemplateRendererTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static TemplateRenderer CreateRenderer()
    {
        var logger = Substitute.For<ILogger<TemplateRenderer>>();
        return new TemplateRenderer(logger);
    }

    private static NotificationTemplate BuildTemplate(
        string subject,
        string bodyHtml,
        string bodyText)
    {
        return NotificationTemplate.CreateDefault(
            tenantId: "tenant-001",
            code: "test.template",
            category: NotificationCategory.ReviewWorkflow,
            channel: NotificationChannel.Email,
            locale: "en-US",
            subject: subject,
            bodyHtml: bodyHtml,
            bodyText: bodyText,
            previewText: null);
    }

    // ─── Variable substitution ───────────────────────────────────────────────

    [Fact]
    public void TemplateRenderer_Render_SubstitutesVariables()
    {
        var renderer = CreateRenderer();
        var template = BuildTemplate(
            subject: "Hello {{EmployeeName}}",
            bodyHtml: "<p>Dear {{EmployeeName}}, your review is on {{DueDate}}.</p>",
            bodyText: "Dear {{EmployeeName}}, your review is on {{DueDate}}.");

        var variables = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Alice",
            ["DueDate"] = "2026-06-15",
        };

        var (subject, bodyHtml, bodyText) = renderer.Render(template, variables);

        subject.Should().Be("Hello Alice");
        bodyHtml.Should().Be("<p>Dear Alice, your review is on 2026-06-15.</p>");
        bodyText.Should().Be("Dear Alice, your review is on 2026-06-15.");
    }

    [Fact]
    public void TemplateRenderer_Render_MissingVariable_LeavesPlaceholder()
    {
        // According to the implementation, missing variables are replaced with
        // an empty string (and a warning is logged) — not left as {{VarName}}.
        var renderer = CreateRenderer();
        var template = BuildTemplate(
            subject: "Hello {{EmployeeName}}",
            bodyHtml: "<p>Body</p>",
            bodyText: "Body");

        var (subject, _, _) = renderer.Render(template, new Dictionary<string, string>());

        // Missing variable → substituted with empty string
        subject.Should().Be("Hello ");
    }

    [Fact]
    public void TemplateRenderer_Render_MultipleVariables_AllSubstituted()
    {
        var renderer = CreateRenderer();
        var template = BuildTemplate(
            subject: "{{A}} and {{B}} and {{C}}",
            bodyHtml: "<p>{{A}}{{B}}{{C}}</p>",
            bodyText: "{{A}}{{B}}{{C}}");

        var variables = new Dictionary<string, string>
        {
            ["A"] = "Alpha",
            ["B"] = "Beta",
            ["C"] = "Gamma",
        };

        var (subject, bodyHtml, bodyText) = renderer.Render(template, variables);

        subject.Should().Be("Alpha and Beta and Gamma");
        bodyHtml.Should().Be("<p>AlphaBetaGamma</p>");
        bodyText.Should().Be("AlphaBetaGamma");
    }

    [Fact]
    public void TemplateRenderer_Render_CaseSensitiveVariables()
    {
        // The regex matches [A-Za-z0-9_.]+, and dictionary lookups are case-sensitive
        // by default. {{employeeName}} != {{EmployeeName}}.
        var renderer = CreateRenderer();
        var template = BuildTemplate(
            subject: "{{EmployeeName}} vs {{employeeName}}",
            bodyHtml: "<p>body</p>",
            bodyText: "body");

        var variables = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Alice",
            // "employeeName" is intentionally absent
        };

        var (subject, _, _) = renderer.Render(template, variables);

        // First placeholder resolved; second missing → empty string
        subject.Should().Be("Alice vs ");
    }
}
