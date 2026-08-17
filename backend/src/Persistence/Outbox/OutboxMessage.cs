namespace Persistence.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccuredOn { get; set; }
    public bool IsProcessed { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
}