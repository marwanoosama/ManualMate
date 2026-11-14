using System.Text.Json;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Infrastructure.Presistence;

namespace ManualMate.Infrastructure.Messaging.Outbox;

public sealed class OutboxService(ManualMateDbContext dbContext) : IOutboxService
{
    public void Enqueue<TMessage>(TMessage message, string routingKey)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(TMessage).Name,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(message),
            OccurredOnUtc = DateTime.UtcNow
        });
    }
}
