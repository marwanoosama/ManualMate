namespace ManualMate.Infrastructure.Messaging.Outbox;

public class OutboxMessage
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}
