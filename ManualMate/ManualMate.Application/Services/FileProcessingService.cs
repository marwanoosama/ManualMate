using ManualMate.Application.Interfaces;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using ManualMate.Application.Responses;

namespace ManualMate.Application.Services
{
    public class FileProcessingService(
        IApplicationDbContext dbContext,
        IEmbeddingService embeddingService,
        IPdfTextExtractor pdfTextExtractor,
        ITextChunker textChunker)
        : IFileProcessingService
    {
        public async Task<Result<bool>> ProcessFileAsync(Guid itemId)
        {
            var item = await dbContext.Items.FindAsync(itemId);
            if (item is null)
            {
                return Result<bool>.Fail($"Item with id : {itemId} not found", HttpStatusCode.NotFound);
            }

            var pdfText = pdfTextExtractor.ExtractTextFromPdf(item.FilePath).Value;
            var chunks = textChunker.ChunkText(pdfText).Value
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            const int batchSize = 100;

            for (var offset = 0; offset < chunks.Count; offset += batchSize)
            {
                var batch = chunks.Skip(offset).Take(batchSize).ToList();

                var embeddings = await embeddingService.GetEmbeddingsAsync(batch);
                if (!embeddings.Success || embeddings.Value is null)
                {
                    return Result<bool>.Fail(embeddings.Error ?? "Error generating embeddings", embeddings.StatusCode);
                }

                for (var i = 0; i < batch.Count; i++)
                {
                    var newEmbedding = new FileEmbedding
                    {
                        ItemId = itemId,
                        TextChunk = batch[i],
                        Embedding = embeddings.Value[i],
                        ChunkIndex = offset + i
                    };

                    await dbContext.FileEmbeddings.AddAsync(newEmbedding);
                }

                await dbContext.SaveChangesAsync();
            }

            return Result<bool>.Ok(true);

            return Result<bool>.Ok(true);
        }

        public async Task<Result<bool>> DeleteFileEmbeddingsAsync(Guid itemId)
        {
            var item = await dbContext.Items.FindAsync(itemId);

            if (item is null)
            {
                return Result<bool>.Fail($"Item with id : {itemId} not found", HttpStatusCode.NotFound);
            }

            await dbContext.FileEmbeddings.Where(fe => fe.ItemId == itemId).ExecuteDeleteAsync();
          
            return Result<bool>.Ok(true);
        }
    }
}