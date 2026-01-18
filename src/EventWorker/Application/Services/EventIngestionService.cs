using System.Text.Json;
using EventWorker.Application;

public sealed class EventIngestionService : IEventIngestionService
{
    private readonly IIncidentEventRepository _repo;

    public EventIngestionService(IIncidentEventRepository repo)
    {
        _repo = repo;
    }

    public async Task IngestRawEventAsync(string eventType, string payloadJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(payloadJson);

        if (string.IsNullOrWhiteSpace(eventType) || eventType == "Unknown")
            throw new DomainValidationException("Missing event type header: type");

        if (eventType == "IncidentStarted")
        {
            if (!doc.RootElement.TryGetProperty("IncidentKey", out var keyEl) ||
                string.IsNullOrWhiteSpace(keyEl.GetString()))
            {
                throw new DomainValidationException("incidentKey is required for IncidentStarted");
            }
        }

        await _repo.AddAsync(eventType, payloadJson, ct);
    }
}