using ManualMate.Application.Interfaces.Services;
using ManualMate.Domain.Enums;
using ManualMate.Infrastructure.Presistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManualMate.Infrastructure.Jobs;

public sealed class ItemProcessingJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ItemProcessingJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAsync(int itemId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ManualMateDbContext>();
        var fileProcessingService = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();

        var item = await dbContext.Items.FindAsync(itemId, cancellationToken);

        if (item is null)
        {
            logger.LogWarning("Item with ID {ItemId} not found. Skipping processing.", itemId);
            return;
        }

        try
        {
            item.Status = ItemStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            var result = await fileProcessingService.ProcessFileAsync(itemId);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error ?? "File processing failed.");
            }

            item.Status = ItemStatus.Completed;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully processed file for Item ID: {ItemId}", itemId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process file for Item ID: {ItemId}", itemId);

            item.Status = ItemStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);

            throw;
        }
    }
}
