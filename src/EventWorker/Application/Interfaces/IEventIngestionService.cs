public interface IEventIngestionService
{
    Task IngestRawEventAsync(string eventType, string payloadJson, CancellationToken ct);
}
