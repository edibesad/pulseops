namespace Shared.Contracts.Events
{
    public record SlaViolated(
        string IncidentKey,
        string ServiceName,
        TimeSpan Duration,
        string RuleName,
        DateTimeOffset DetectedAt,
        string? CorelationId
        );
}
