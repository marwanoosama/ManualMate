using System.Text;
using ManualMate.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ManualMate.Infrastructure.Messaging;

public sealed class RabbitMqMessagePublisher(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessagePublisher> logger) : IMessagePublisher
{
    private readonly RabbitMqOptions _settings = options.Value;

    public async Task PublishAsync(string payload, string messageType, string routingKey)
    {
        var body = Encoding.UTF8.GetBytes(payload);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel = await connection.CreateChannelAsync(channelOptions);

        await RabbitMqTopology.DeclareExchangeAsync(channel, _settings);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Type = messageType
        };

        try
        {
            await channel.BasicPublishAsync(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
        catch (PublishException ex)
        {
            logger.LogError(ex, "RabbitMQ returned message {MessageType} published with routing key {RoutingKey}.", messageType, routingKey);
            throw;
        }
    }
}
