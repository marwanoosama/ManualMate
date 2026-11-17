using ManualMate.Application.Enums;
using ManualMate.Application.Responses;

namespace ManualMate.Application.Interfaces.Services
{
    public interface IEmbeddingService
    {
        Task<Result<Pgvector.Vector>> GetEmbeddingAsync(string text, EmbeddingInputType inputType);

        Task<Result<List<Pgvector.Vector>>> GetEmbeddingsAsync(IReadOnlyList<string> texts, EmbeddingInputType inputType, CancellationToken cancellationToken = default);
    }
}
