using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ManualMate.Application.Enums;
using ManualMate.Application.Interfaces.Services;
using ManualMate.Application.Responses;
using Microsoft.Extensions.Configuration;

namespace ManualMate.Infrastructure.Services
{
    public class VoyageAIEmbeddingService(IHttpClientFactory clientFactory, IConfiguration configuration) : IEmbeddingService
    {
        public async Task<Result<Pgvector.Vector>> GetEmbeddingAsync(string text, EmbeddingInputType inputType)
        {
            var result = await GetEmbeddingsAsync([text], inputType);

            if (!result.Success || result.Value is null)
            {
                return Result<Pgvector.Vector>.Fail(result.Error ?? "Error generating embedding", result.StatusCode);
            }

            if (result.Value.Count == 0)
            {
                return Result<Pgvector.Vector>.Fail("Cannot generate question embeddings");
            }

            return Result<Pgvector.Vector>.Ok(result.Value[0]);
        }

        public async Task<Result<List<Pgvector.Vector>>> GetEmbeddingsAsync(IReadOnlyList<string> texts, EmbeddingInputType inputType, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await SendWithRetryAsync(texts, inputType, cancellationToken);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if(TryExtractVoyageErrorMessage(json, out var message))
                    {
                        return Result<List<Pgvector.Vector>>.Fail(message, response.StatusCode);
                    }

                    return Result<List<Pgvector.Vector>>.Fail("Error Generating Answer.", response.StatusCode);
                }

                if(TryExtractVoyageAnswer(json, out List<Pgvector.Vector>? embeddingsResponse))
                {
                    if(embeddingsResponse is null || embeddingsResponse.Count == 0)
                    {
                        return Result<List<Pgvector.Vector>>.Fail("Cannot generate question embeddings");
                    }
                    return Result<List<Pgvector.Vector>>.Ok(embeddingsResponse);
                }

                return Result<List<Pgvector.Vector>>.Fail("Unexpected behaviour from Voyage Json response", response.StatusCode);
            }
            catch (Exception ex)
            {
                return Result<List<Pgvector.Vector>>.Fail("Unexpected error: " + ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(IReadOnlyList<string> texts, EmbeddingInputType inputType, CancellationToken cancellationToken)
        {
            var maxRetries = int.TryParse(configuration["VoyageAI:MaxRetries"], out var retries) ? retries : 5;
            var baseDelaySeconds = double.TryParse(configuration["VoyageAI:RetryBaseDelaySeconds"], out var delay) ? delay : 21;

            for (var attempt = 0; ; attempt++)
            {
                var response = await GetVoyageResponse(texts, inputType, cancellationToken);

                if (response.IsSuccessStatusCode || attempt >= maxRetries || !IsRetryable(response.StatusCode))
                {
                    return response;
                }

                var waitSeconds = GetRetryAfterSeconds(response.Headers.RetryAfter)
                    ?? Math.Min(baseDelaySeconds * Math.Pow(2, attempt), 120);

                response.Dispose();

                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
            }
        }

        private static bool IsRetryable(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.GatewayTimeout;
        }

        private static double? GetRetryAfterSeconds(RetryConditionHeaderValue? retryAfter)
        {
            if (retryAfter?.Delta is { } delta)
            {
                return delta.TotalSeconds;
            }

            if (retryAfter?.Date is { } date)
            {
                return Math.Max((date - DateTimeOffset.UtcNow).TotalSeconds, 0);
            }

            return null;
        }

        private async Task<HttpResponseMessage> GetVoyageResponse(IReadOnlyList<string> texts, EmbeddingInputType inputType, CancellationToken cancellationToken)
        {
            var payload = new
            {
                input = texts,
                model = configuration["VoyageAI:Model"],
                output_dimension = 512,
                input_type = inputType.ToString().ToLowerInvariant()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var client = clientFactory.CreateClient("VoyageAIClient");

            return await client.PostAsync("", content, cancellationToken);
        }

        private static bool TryExtractVoyageAnswer(string json, out List<Pgvector.Vector>? result)
        {
            result = null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement
                    .GetProperty("data");

                var vectors = new List<Pgvector.Vector>();

                foreach (var item in data.EnumerateArray())
                {
                    var embedding = item.GetProperty("embedding");

                    var floatArray = embedding.EnumerateArray()
                        .Select(e => e.GetSingle())
                        .ToArray();

                    if (floatArray.Length == 0)
                    {
                        return false;
                    }

                    vectors.Add(new Pgvector.Vector(floatArray));
                }

                result = vectors;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryExtractVoyageErrorMessage(string json, out string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                message = doc.RootElement
                    .GetProperty("detail")
                    .GetString()!;

                return true;
            }
            catch
            {
                message = "";
                return false;
            }
        }
    }
}
