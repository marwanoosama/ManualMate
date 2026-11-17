using ManualMate.Application.Responses;

namespace ManualMate.Application.Interfaces.Services;

public interface ITextChunker
{
    Result<List<string>> ChunkText(string text, int maxChars = 1000, int overlapChars = 150);
}