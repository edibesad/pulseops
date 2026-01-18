public record IncidentStarted(
    string IncidentKey,
    string Source,
    string ResourceId,
    DateTimeOffset StartTime,
    string? Region,
    string? CorelationId
    );