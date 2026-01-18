using System.Text.Json;

public class IncidentEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = default!;
    public JsonDocument Payload { get; set; } = default!;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}