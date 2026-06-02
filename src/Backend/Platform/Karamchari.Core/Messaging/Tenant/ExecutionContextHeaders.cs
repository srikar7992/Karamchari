namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// Defines header key constants for the platform's Execution Context Preservation System.
/// These headers are used to propagate, sign, and validate execution context across
/// asynchronous boundaries.
/// </summary>
public static class ExecutionContextHeaders
{
    /// <summary>The tenant identifier (Tier 1 - Isolation Critical).</summary>
    public const string TenantId = "MT-Tenant-Id";

    /// <summary>The cryptographic signature of the execution context (Tier 1 - Integrity Critical).</summary>
    public const string ExecutionContextSignature = "MT-Context-Signature";

    /// <summary>The correlation identifier for request tracking (Tier 2 - Audit Critical).</summary>
    public const string CorrelationId = "MT-Correlation-Id";

    /// <summary>The conversation identifier for message chains (Tier 2 - Audit Critical).</summary>
    public const string ConversationId = "MT-Conversation-Id";

    /// <summary>The identity of the producing boundary (Tier 2 - Audit Critical).</summary>
    public const string SourceService = "MT-Source-Service";

    /// <summary>The version of the execution context contract (Tier 2 - Governance).</summary>
    public const string ExecutionContextVersion = "MT-Context-Version";

    /// <summary>The distributed trace identifier (Tier 3 - Telemetry).</summary>
    public const string TraceId = "MT-Trace-Id";

    /// <summary>The distributed trace span identifier.</summary>
    public const string SpanId = "MT-Span-Id";

    /// <summary>The request identifier for unique execution identification.</summary>
    public const string RequestId = "MT-Request-Id";

    /// <summary>The timestamp of context creation (used for freshness/replay protection).</summary>
    public const string Timestamp = "MT-Timestamp";

    /// <summary>The serialized execution envelope for legacy/fallback support.</summary>
    public const string ExecutionEnvelope = "MT-Execution-Envelope";

    // Legacy mapping support
    public const string UserIdentity = "MT-User-Identity";
    public const string ExecutionSource = "MT-Execution-Source";
    public const string RetryAttempt = "MT-Retry-Attempt";
    public const string ReplayCount = "MT-Replay-Count";
    public const string ContentHash = "MT-Content-Hash";
    public const string OriginalMessageId = "MT-Original-Message-Id";
    public const string WorkflowInstanceId = "MT-Workflow-Instance-Id";
    public const string WorkflowStepId = "MT-Workflow-Step-Id";
    public const string WorkflowSlaDeadline = "MT-Workflow-Sla-Deadline";
}
