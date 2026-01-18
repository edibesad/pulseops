sealed class IncidentStartedEvent
{
    public string IncidentKey { get; set; } = default!;
    public DateTimeOffset StartTime { get; set; }
    public string Source { get; set; } = default!;

}