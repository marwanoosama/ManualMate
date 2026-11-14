using ManualMate.Application.Responses;

namespace ManualMate.Application.Interfaces.Services
{
    public interface IEmbeddingService
    {
        Task<Result<Pgvector.Vector>> GetEmbeddingAsync(string text);

        Task<Result<List<Pgvector.Vector>>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
    }
}
