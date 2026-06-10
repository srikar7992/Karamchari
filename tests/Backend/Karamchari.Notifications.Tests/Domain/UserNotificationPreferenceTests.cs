// -----------------------------------------------------------------------
// <copyright file="UserNotificationPreferenceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Notifications.Domain;
using Xunit;

namespace Karamchari.Notifications.Tests.Domain;

public sealed class UserNotificationPreferenceTests
{
    // ─── CreateDefault ───────────────────────────────────────────────────────

    [Fact]
    public void UserPreference_CreateDefault_InAppIsTrue()
    {
        var pref = UserNotificationPreference.CreateDefault(
            tenantId: "tenant-001",
            employeeId: Guid.NewGuid(),
            category: NotificationCategory.ReviewWorkflow);

        pref.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public void UserPreference_CreateDefault_EmailIsTrue()
    {
        // Email defaults to true for all categories except SystemAlerts
        var pref = UserNotificationPreference.CreateDefault(
            tenantId: "tenant-001",
            employeeId: Guid.NewGuid(),
            category: NotificationCategory.ReviewWorkflow);

        pref.EmailEnabled.Should().BeTrue();
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public void UserPreference_Update_ChangesPreferences()
    {
        var pref = UserNotificationPreference.CreateDefault(
            tenantId: "tenant-001",
            employeeId: Guid.NewGuid(),
            category: NotificationCategory.GoalManagement);

        pref.Update(
            inApp: false,
            email: false,
            push: true,
            digest: true,
            frequency: DigestFrequency.Weekly,
            quietHoursStart: "22:00",
            quietHoursEnd: "07:00");

        pref.InAppEnabled.Should().BeFalse();
        pref.EmailEnabled.Should().BeFalse();
        pref.PushEnabled.Should().BeTrue();
        pref.DigestEnabled.Should().BeTrue();
        pref.DigestFrequency.Should().Be(DigestFrequency.Weekly);
        pref.QuietHoursStart.Should().Be("22:00");
        pref.QuietHoursEnd.Should().Be("07:00");
    }

    // ─── Quiet hours midnight wrap ────────────────────────────────────────────

    [Fact]
    public void UserPreference_QuietHours_MidnightWrap_StartsAt23EndsAt06()
    {
        var pref = UserNotificationPreference.CreateDefault(
            tenantId: "tenant-001",
            employeeId: Guid.NewGuid(),
            category: NotificationCategory.ReviewWorkflow);

        pref.Update(
            inApp: true,
            email: true,
            push: false,
            digest: false,
            frequency: DigestFrequency.Daily,
            quietHoursStart: "23:00",
            quietHoursEnd: "06:00");

        // Domain stores the strings as provided — midnight-wrap is a
        // concern for the orchestrator, not the preference aggregate.
        pref.QuietHoursStart.Should().Be("23:00");
        pref.QuietHoursEnd.Should().Be("06:00");
    }
}
