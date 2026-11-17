using ManualMate.Application.Enums;
using ManualMate.Application.Interfaces;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using ManualMate.Application.Responses;

namespace ManualMate.Application.Services
{
    public class FileProcessingService(
        IApplicationDbContext dbContext,
        IEmbeddingService embeddingService,
        IPdfTextExtractor pdfTextExtractor,
        ITextChunker textChunker,
        IConfiguration configuration)
        : IFileProcessingService
    {
        private readonly int _batchSize = int.TryParse(configuration["VoyageAI:DocumentBatchSize"], out var size) && size > 0 ? size : 64;

        public async Task<Result<bool>> ProcessFileAsync(int itemId)
        {
            var item = await dbContext.Items.FindAsync(itemId);
            if (item is null)
            {
                return Result<bool>.Fail($"Item with id : {itemId} not found", HttpStatusCode.NotFound);
            }

            var pdfTextResult = pdfTextExtractor.ExtractTextFromPdf(item.FilePath);
            if (!pdfTextResult.Success || String.IsNullOrWhiteSpace(pdfTextResult.Value))
            {
                return Result<bool>.Fail(pdfTextResult.Error ?? "No text found in PDF.", HttpStatusCode.UnprocessableEntity);
            }
            var pdfText = pdfTextResult.Value;
            var chunks = textChunker.ChunkText(pdfText).Value
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
            

            for (var offset = 0; offset < chunks.Count; offset += _batchSize)
            {
                var batch = chunks.Skip(offset).Take(_batchSize).ToList();

                var embeddings = await embeddingService.GetEmbeddingsAsync(batch, EmbeddingInputType.Document);
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
        }

        public async Task<Result<bool>> DeleteFileEmbeddingsAsync(int itemId)
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