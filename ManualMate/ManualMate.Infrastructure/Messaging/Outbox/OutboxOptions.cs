namespace ManualMate.Infrastructure.Messaging.Outbox;

public class OutboxOptions
{
    public int BatchSize { get; set; } = 20;
    public int PollingIntervalSeconds { get; set; } = 1;
    public int MaxAttempts { get; set; } = 10;
}
