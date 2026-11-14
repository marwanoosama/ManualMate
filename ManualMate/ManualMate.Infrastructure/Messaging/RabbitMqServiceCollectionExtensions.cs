using ManualMate.Application.Interfaces.Services;
using ManualMate.Infrastructure.Consumers;
using ManualMate.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManualMate.Infrastructure.Messaging;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddHostedService<ItemCreatedConsumer>();

        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}
