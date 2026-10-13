namespace BusinessOperations.Domain.Common;

/// <summary>
/// Common base class providing unique identification and auditing timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    public void MarkUpdated(DateTime? timestampUtc = null)
    {
        UpdatedAtUtc = timestampUtc ?? DateTime.UtcNow;
    }
}
