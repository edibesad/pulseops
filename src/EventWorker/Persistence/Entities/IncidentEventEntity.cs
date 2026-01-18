namespace EventWorker.Persistence.Entities;

public class IncidentEventEntity
{
    public long Id { get; set; }

    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CorrelationId { get; set; }
    public string? IncidentKey { get; set; }
}
