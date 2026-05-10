namespace Karamchari.Core.Caching.Tenant;

public sealed class CachePoisoningDetectionException : Exception
{
    public string? AttackKey { get; }
    public DateTime DetectionTimeUtc { get; }

    public CachePoisoningDetectionException(string message)
        : base(message)
    {
        AttackKey = null;
        DetectionTimeUtc = DateTime.UtcNow;
    }

    public CachePoisoningDetectionException(string message, string attackKey)
        : base(message)
    {
        AttackKey = attackKey;
        DetectionTimeUtc = DateTime.UtcNow;
    }
}
