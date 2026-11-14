using ManualMate.Application.Interfaces.Services;
using ManualMate.Infrastructure.Presistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace ManualMate.Infrastructure.Messaging.Outbox;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxOptions _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ManualMateDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var batch = await dbContext.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedOnUtc" IS NULL AND "Attempts" < {_settings.MaxAttempts}
                ORDER BY "OccurredOnUtc"
                LIMIT {_settings.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                await publisher.PublishAsync(message.Payload, message.Type, message.RoutingKey);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex) when (IsBrokerUnavailable(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsBrokerUnavailable(Exception ex)
    {
        return ex is BrokerUnreachableException
            or AlreadyClosedException
            or OperationInterruptedException
            or TimeoutException;
    }
}
