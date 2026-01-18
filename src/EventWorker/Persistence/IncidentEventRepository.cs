using System.Text.Json;


public sealed class IncidentEventRepository : IIncidentEventRepository
{
    private readonly PulseOpsDbContext _db;

    public IncidentEventRepository(PulseOpsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(string eventType, string payloadJson, CancellationToken ct)
    {
        _db.IncidentEvents.Add(new IncidentEvent
        {
            EventType = eventType,
            Payload = JsonDocument.Parse(payloadJson),
        });

        await _db.SaveChangesAsync(ct);
    }
}
