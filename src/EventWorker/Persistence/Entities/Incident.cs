public class Incident
{
    public long Id { get; set; }
    public string IncidentKey { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public int? DurationSeconds { get; set; }


    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum IncidentStatus
{
    Open = 1, Closed = 2
}