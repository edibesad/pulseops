namespace EventWorker.Application.Events;

public sealed class IncidentEndedEvent
{
    public string IncidentKey { get; set; } = default!;
    public DateTimeOffset EndTime { get; set; }
    public string? CorrelationId { get; set; }
}
