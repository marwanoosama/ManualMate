namespace ManualMate.Application.Interfaces.Services;

public interface IOutboxService
{
    void Enqueue<TMessage>(TMessage message, string routingKey);
}
