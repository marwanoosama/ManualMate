using ManualMate.Application.Interfaces.Services;
using Hangfire;

namespace ManualMate.Infrastructure.Jobs;

public sealed class ItemProcessingScheduler(IBackgroundJobClient client) : IItemProcessingScheduler
{
    public void Schedule(int itemId)
    {
        client.Enqueue<ItemProcessingJob>(job => job.ProcessAsync(itemId, CancellationToken.None));
    }
}
