using System.Text.Json;
using ManualMate.Application.Contracts;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Domain.Enums;
using ManualMate.Infrastructure.Messaging;
using ManualMate.Infrastructure.Presistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ManualMate.Infrastructure.Consumers;

public sealed class ItemCreatedConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<ItemCreatedConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ consumer stopped. Reconnecting in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken);

        await RabbitMqTopology.DeclareExchangeAsync(channel, _settings, cancellationToken);
        await RabbitMqTopology.DeclareItemCreatedQueueAsync(channel, _settings, cancellationToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        var channelShutdown = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        channel.ChannelShutdownAsync += (_, _) =>
        {
            channelShutdown.TrySetResult(true);
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);

        // what gets fired when there is a message received (declaring the consumer)
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<ItemCreatedMessage>(args.Body.Span)
                    ?? throw new InvalidOperationException("Item created message could not be deserialized.");

                await ProcessAsync(message, cancellationToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await channel.BasicNackAsync(args.DeliveryTag, false, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process item created message.");
                await channel.BasicNackAsync(args.DeliveryTag, false, false, CancellationToken.None);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        await Task.WhenAny(
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
            channelShutdown.Task);
    }

    private async Task ProcessAsync(ItemCreatedMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ManualMateDbContext>();
        var fileProcessingService = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();

        var item = await dbContext.Items.FindAsync(message.ItemId, cancellationToken);

        if (item is null)
        {
            logger.LogWarning("Item with ID {ItemId} not found. Skipping processing.", message.ItemId);
            return;
        }

        try
        {
            item.Status = ItemStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            var result = await fileProcessingService.ProcessFileAsync(message.ItemId);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error ?? "File processing failed.");
            }

            item.Status = ItemStatus.Completed;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully processed file for Item ID: {ItemId}", message.ItemId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process file for Item ID: {ItemId}", message.ItemId);

            item.Status = ItemStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);

            throw;
        }
    }
}
