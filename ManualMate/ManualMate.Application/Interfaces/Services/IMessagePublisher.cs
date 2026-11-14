namespace ManualMate.Application.Interfaces.Services;

public interface IMessagePublisher
{
    Task PublishAsync(string payload, string messageType, string routingKey);
}
