using ManualMate.Application.Contracts;
using RabbitMQ.Client;

namespace ManualMate.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static Task DeclareExchangeAsync(
        IChannel channel,
        RabbitMqOptions settings,
        CancellationToken cancellationToken = default)
    {
        return channel.ExchangeDeclareAsync(
            exchange: settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public static async Task DeclareItemCreatedQueueAsync(
        IChannel channel,
        RabbitMqOptions settings,
        CancellationToken cancellationToken = default)
    {
        await channel.QueueDeclareAsync(
            queue: settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: settings.QueueName,
            exchange: settings.ExchangeName,
            routingKey: ItemCreatedMessage.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
