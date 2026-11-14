using ManualMate.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ManualMate.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<Item> Items { get; }
        public DbSet<FileEmbedding> FileEmbeddings { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}