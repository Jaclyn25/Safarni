using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Infrastructure.Persistence;

public class QdrantService : IQdrantService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantService> _logger;
    

    public QdrantService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<QdrantService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string QdrantUrl => (_configuration["RAG:QdrantUrl"] ?? "http://localhost:6333").TrimEnd('/');
    private string CollectionName => _configuration["RAG:QdrantCollectionName"] ?? "safarni_knowledge";
    private int VectorSize => int.TryParse(_configuration["RAG:QdrantVectorSize"], out var size) ? size : 384;

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            var url = $"{QdrantUrl}/";
            var response = await _httpClient.GetAsync(url, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Qdrant health check failed at {Url}: {Message}", QdrantUrl, ex.Message);
            return false;
        }
    }

    public async Task<bool> CreateCollectionAsync()
    {
        // Check if Qdrant is healthy first
        if (!await IsHealthyAsync())
        {
            _logger.LogError("Qdrant service is not available at {Url}.", QdrantUrl);
            throw new InvalidOperationException("قاعدة بيانات الـ Vectors (Qdrant) غير متوفرة حالياً. يرجى التأكد من تشغيلها.");
        }

        var checkUrl = $"{QdrantUrl}/collections/{CollectionName}";
        bool recreateNeeded = false;
        bool exists = false;

        try
        {
            var checkResponse = await _httpClient.GetAsync(checkUrl);
            if (checkResponse.IsSuccessStatusCode)
            {
                exists = true;
                var details = await checkResponse.Content.ReadFromJsonAsync<QdrantCollectionDetailsResponse>();
                var size = details?.Result?.Config?.Params?.Vectors?.Size ?? 0;
                if (size != VectorSize)
                {
                    _logger.LogWarning("Qdrant collection {Collection} exists but has dimension {ActualSize} instead of {ExpectedSize}. Re-creating...", CollectionName, size, VectorSize);
                    recreateNeeded = true;
                }
                else
                {
                    _logger.LogInformation("Qdrant collection {Collection} already exists with correct dimension {ExpectedSize}.", CollectionName, VectorSize);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if Qdrant collection {Collection} exists.", CollectionName);
        }

        if (recreateNeeded)
        {
            await DeleteCollectionAsync();
            exists = false;
        }

        if (!exists)
        {
            var createUrl = $"{QdrantUrl}/collections/{CollectionName}";
            
            // Embeddings distance metric.
            var createPayload = new
            {
                vectors = new
                {
                    size = VectorSize,
                    distance = "Cosine"
                }
            };

            try
            {
                var response = await _httpClient.PutAsJsonAsync(createUrl, createPayload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create collection {Collection}. Status: {Status}, Error: {Error}", 
                        CollectionName, response.StatusCode, errorText);
                    throw new HttpRequestException($"فشل إنشاء مجموعة Qdrant: {response.StatusCode} - {errorText}");
                }

                _logger.LogInformation("Successfully created Qdrant collection {Collection} with dimension {ExpectedSize}.", CollectionName, VectorSize);
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "Exception occurred during Qdrant collection creation.");
                throw new InvalidOperationException("حدث خطأ أثناء محاولة إنشاء مجموعة البيانات في Qdrant.", ex);
            }
            
            return true;
        }

        return false;
    }

    public async Task DeleteCollectionAsync()
    {
        _logger.LogInformation("Deleting Qdrant collection: {Collection}", CollectionName);

        var url = $"{QdrantUrl}/collections/{CollectionName}";
        try
        {
            var response = await _httpClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete Qdrant collection {Collection}. Status: {Status}, Error: {Error}", 
                    CollectionName, response.StatusCode, errorText);
                throw new HttpRequestException($"فشل حذف مجموعة Qdrant: {response.StatusCode}");
            }

            _logger.LogInformation("Qdrant collection {Collection} deleted or was not present.", CollectionName);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception occurred during Qdrant collection deletion.");
            throw new InvalidOperationException("حدث خطأ أثناء محاولة حذف مجموعة البيانات من Qdrant.", ex);
        }
    }

    public async Task SaveChunksAsync(List<DocumentChunk> chunks)
    {
        if (chunks == null || chunks.Count == 0)
        {
            _logger.LogWarning("SaveChunksAsync was called with an empty list.");
            return;
        }

        _logger.LogInformation("Upserting {Count} chunks to Qdrant collection {Collection}", chunks.Count, CollectionName);

        var url = $"{QdrantUrl}/collections/{CollectionName}/points?wait=true";

        var pointsList = new List<object>();
        foreach (var chunk in chunks)
        {
            pointsList.Add(new
            {
                id = chunk.Id,
                vector = chunk.Embedding,
                payload = new
                {
                    entityId = chunk.EntityId,
                    entityType = chunk.EntityType,
                    content = chunk.Content,
                    source = chunk.Source,
                    city = chunk.City,
                    country = chunk.Country,
                    createdDate = chunk.CreatedDate.ToString("o")
                }
            });
        }

        var payload = new { points = pointsList };

        try
        {
            var response = await _httpClient.PutAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save chunks to Qdrant. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorText);
                throw new HttpRequestException($"فشل حفظ الـ Vectors في Qdrant: {response.StatusCode} - {errorText}");
            }

            _logger.LogInformation("Successfully saved {Count} chunks in Qdrant.", chunks.Count);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception occurred during saving chunks to Qdrant.");
            throw new InvalidOperationException("حدث خطأ أثناء حفظ بيانات الـ Vectors في Qdrant.", ex);
        }
    }

    public async Task<List<DocumentChunk>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK)
    {
        _logger.LogInformation("Performing similarity search in Qdrant collection {Collection} with topK={TopK}", CollectionName, topK);

        var url = $"{QdrantUrl}/collections/{CollectionName}/points/search";

        var searchPayload = new
        {
            vector = queryEmbedding,
            limit = topK,
            with_payload = true
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, searchPayload);
            
            // Handle missing collection (e.g. status 404)
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Qdrant collection {Collection} not found. Returning empty list.", CollectionName);
                return new List<DocumentChunk>();
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Qdrant similarity search failed. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorText);
                throw new HttpRequestException($"فشل البحث في Qdrant: {response.StatusCode}");
            }

            var searchResponse = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>();
            if (searchResponse?.Result == null)
            {
                _logger.LogInformation("Qdrant search returned no results.");
                return new List<DocumentChunk>();
            }

            var chunks = new List<DocumentChunk>();
            foreach (var result in searchResponse.Result)
            {
                if (result.Payload == null) continue;

                result.Payload.TryGetValue("entityId", out var entityIdObj);
                result.Payload.TryGetValue("entityType", out var entityTypeObj);
                result.Payload.TryGetValue("content", out var contentObj);
                result.Payload.TryGetValue("source", out var sourceObj);
                result.Payload.TryGetValue("city", out var cityObj);
                result.Payload.TryGetValue("country", out var countryObj);
                result.Payload.TryGetValue("createdDate", out var dateObj);

                string entityId = entityIdObj?.ToString() ?? string.Empty;
                string entityType = entityTypeObj?.ToString() ?? string.Empty;
                string content = contentObj?.ToString() ?? string.Empty;
                string source = sourceObj?.ToString() ?? string.Empty;
                string city = cityObj?.ToString() ?? string.Empty;
                string country = countryObj?.ToString() ?? string.Empty;
                
                DateTime createdDate = DateTime.UtcNow;
                if (dateObj != null && DateTime.TryParse(dateObj.ToString(), out var date))
                {
                    createdDate = date;
                }

                chunks.Add(new DocumentChunk
                {
                    Id = result.Id,
                    EntityId = entityId,
                    EntityType = entityType,
                    Content = content,
                    Source = source,
                    City = city,
                    Country = country,
                    CreatedDate = createdDate,
                    Embedding = Array.Empty<float>() // Embedding not required for prompt context building
                });
            }

            _logger.LogInformation("Retrieved {Count} relevant chunks from Qdrant.", chunks.Count);
            foreach (var chunk in chunks)
            {
                _logger.LogDebug("Retrieved Chunk: Source={Source}, Type={Type}, Snippet={Snippet}", 
                    chunk.Source, chunk.EntityType, chunk.Content.Length > 60 ? chunk.Content.Substring(0, 57) + "..." : chunk.Content);
            }

            return chunks;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Exception occurred during Qdrant similarity search.");
            throw new InvalidOperationException("حدث خطأ أثناء إجراء البحث الدلالي في Qdrant.", ex);
        }
    }

    #region JSON Models

    private class QdrantCollectionDetailsResponse
    {
        [JsonPropertyName("result")]
        public QdrantCollectionDetailsResult? Result { get; set; }
    }

    private class QdrantCollectionDetailsResult
    {
        [JsonPropertyName("config")]
        public QdrantCollectionConfig? Config { get; set; }
    }

    private class QdrantCollectionConfig
    {
        [JsonPropertyName("params")]
        public QdrantCollectionParams? Params { get; set; }
    }

    private class QdrantCollectionParams
    {
        [JsonPropertyName("vectors")]
        public QdrantCollectionVectors? Vectors { get; set; }
    }

    private class QdrantCollectionVectors
    {
        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    private class QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public List<QdrantSearchResult>? Result { get; set; }
    }

    private class QdrantSearchResult
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }
    }

    #endregion
}
