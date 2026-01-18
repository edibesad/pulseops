namespace Shared.Contracts.Events
{
    public record IncidentEnded(
        string IncidentKey,
        string Source,
        string ResourceId,
        DateTimeOffset EndTime,
        string? Region,
        string? CorelationId
        );
}
