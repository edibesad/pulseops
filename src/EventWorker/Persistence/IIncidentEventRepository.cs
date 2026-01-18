public interface IIncidentEventRepository
{
    Task AddAsync(string eventType, string payloadJson, CancellationToken ct);
}
