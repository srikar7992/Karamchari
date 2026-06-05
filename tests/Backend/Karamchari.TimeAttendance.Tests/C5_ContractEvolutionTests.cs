namespace Karamchari.TimeAttendance.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves.Events;
using Xunit;

/// <summary>
/// Concern 5 — Contract Evolution: unknown fields, versioning, missing optional fields.
/// Producer adds new fields → existing consumers must not break.
/// Consumer must tolerate forward-compatible payloads.
/// </summary>
public sealed class C5_ContractEvolutionTests
{
    private static readonly JsonSerializerOptions LenientOptions = new()
    {
        // Mirrors MassTransit default: ignores unknown properties on deserialization
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Forward compatibility: unknown fields tolerated ───────────────────

    [Fact]
    public void Consumer_LeaveRequestApproved_WithExtraField_DeserializesWithoutError()
    {
        // ProducerV2 adds "NewField" → V1 consumer must not throw
        var payloadV2 = """
        {
          "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "TenantId": "tenant-1",
          "EmployeeId": "9b2e8f3a-4c1d-4e2b-a5f6-7c8d9e0f1a2b",
          "StartDate": "2026-06-01",
          "EndDate": "2026-06-05",
          "TotalDays": 5.0,
          "NewField": "some-future-value",
          "AnotherNewField": 42
        }
        """;

        // Deserializing with a forgiving consumer DTO (subset of fields)
        var act = () => JsonSerializer.Deserialize<LeaveApprovedConsumerDto>(payloadV2, LenientOptions);
        act.Should().NotThrow("consumer must tolerate unknown fields added by producer V2");

        var dto = JsonSerializer.Deserialize<LeaveApprovedConsumerDto>(payloadV2, LenientOptions)!;
        dto.RequestId.Should().NotBe(Guid.Empty);
        dto.TotalDays.Should().Be(5.0);
    }

    [Fact]
    public void Consumer_LeaveRequestApproved_MissingOptionalField_Tolerated()
    {
        // V1 producer didn't have "Notes" field — V2 consumer expects it (optional)
        var payloadV1 = """
        {
          "RequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "TenantId": "tenant-1",
          "EmployeeId": "9b2e8f3a-4c1d-4e2b-a5f6-7c8d9e0f1a2b",
          "StartDate": "2026-06-01",
          "EndDate": "2026-06-05",
          "TotalDays": 5.0
        }
        """;

        var dto = JsonSerializer.Deserialize<LeaveApprovedConsumerDtoV2>(payloadV1, LenientOptions)!;
        dto.RequestId.Should().NotBe(Guid.Empty);
        dto.Notes.Should().BeNull("missing optional field defaults to null — consumer handles gracefully");
    }

    [Fact]
    public void Consumer_EmptyPayload_Tolerated_WithDefaults()
    {
        // Edge case: broker delivers empty JSON object (network glitch or routing issue)
        var emptyPayload = "{}";

        var act = () => JsonSerializer.Deserialize<LeaveApprovedConsumerDto>(emptyPayload, LenientOptions);
        act.Should().NotThrow("empty payload deserializes to default DTO — consumer validates before processing");

        var dto = JsonSerializer.Deserialize<LeaveApprovedConsumerDto>(emptyPayload, LenientOptions)!;
        dto.RequestId.Should().Be(Guid.Empty, "missing field → default value");
    }

    // ── Domain event serialization round-trip ─────────────────────────────

    [Fact]
    public void LeaveRequestApproved_SerializeDeserialize_RoundTrip_Lossless()
    {
        var original = new LeaveRequestApproved(
            Guid.NewGuid(), "tenant-1", Guid.NewGuid(),
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), 5.0);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<LeaveRequestApproved>(json, LenientOptions);

        restored!.RequestId.Should().Be(original.RequestId);
        restored.TenantId.Should().Be(original.TenantId);
        restored.EmployeeId.Should().Be(original.EmployeeId);
        restored.TotalDays.Should().Be(original.TotalDays);
        restored.StartDate.Should().Be(original.StartDate);
        restored.EndDate.Should().Be(original.EndDate);
    }

    [Fact]
    public void LeaveRequestCancelled_WasApproved_SerializationPreserved()
    {
        var evt = new LeaveRequestCancelled(Guid.NewGuid(), "t1", Guid.NewGuid(), true);
        var json = JsonSerializer.Serialize(evt);
        var restored = JsonSerializer.Deserialize<LeaveRequestCancelled>(json, LenientOptions)!;

        restored.WasApproved.Should().BeTrue("WasApproved=true must survive serialization round-trip");
    }

    // ── Versioning: V2 consumer reading V1 message ────────────────────────

    [Fact]
    public void ConsumerV2_ReadingV1Message_UsesDefaultForNewField()
    {
        // V2 adds "ActualDurationHours" field. V1 messages don't have it.
        // V2 consumer must treat missing field as 0 (or null) and function correctly.
        var v1Message = """
        {
          "RequestId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          "TenantId": "t1",
          "EmployeeId": "ffffffff-0000-1111-2222-333333333333",
          "StartDate": "2026-07-01",
          "EndDate": "2026-07-05",
          "TotalDays": 5.0
        }
        """;

        var dto = JsonSerializer.Deserialize<LeaveApprovedConsumerDtoV2WithHours>(v1Message, LenientOptions)!;
        dto.TotalDays.Should().Be(5.0);
        dto.ActualDurationHours.Should().Be(0, "missing field defaults to 0 — V2 consumer safe");
    }

    // ── Event EventId uniqueness ──────────────────────────────────────────

    [Fact]
    public void DomainEvents_EachEventId_UniqueAcrossEventTypes()
    {
        var e1 = new LeaveRequestCreated(Guid.NewGuid(), "t1", Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow), 1);
        var e2 = new LeaveRequestApproved(Guid.NewGuid(), "t1", Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow), 1);
        var e3 = new LeaveRequestCancelled(Guid.NewGuid(), "t1", Guid.NewGuid(), false);

        var ids = new[] { e1.EventId, e2.EventId, e3.EventId };
        ids.Should().OnlyHaveUniqueItems("each event instance must have a unique EventId for inbox dedup");
    }
}

// ── Consumer DTOs (simulate consumer-side deserialization target) ──────────

/// <summary>V1 consumer DTO — only reads fields it knows about.</summary>
file sealed class LeaveApprovedConsumerDto
{
    public Guid RequestId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public double TotalDays { get; set; }
}

/// <summary>V2 consumer DTO — adds optional Notes field.</summary>
file sealed class LeaveApprovedConsumerDtoV2
{
    public Guid RequestId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public double TotalDays { get; set; }
    public string? Notes { get; set; } // Optional — V1 messages won't have this
}

/// <summary>V2 consumer DTO with new ActualDurationHours field (default 0).</summary>
file sealed class LeaveApprovedConsumerDtoV2WithHours
{
    public Guid RequestId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public double TotalDays { get; set; }
    public double ActualDurationHours { get; set; } // New in V2
}
