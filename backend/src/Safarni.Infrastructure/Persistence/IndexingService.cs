using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Infrastructure.Persistence;

namespace ChatBotApi.Infrastructure.Persistence;

public class IndexingService : IIndexingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexingService> _logger;

    public IndexingService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        IConfiguration configuration,
        ILogger<IndexingService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _configuration = configuration;
        _logger = logger;
    }

    private int ChunkSize => int.TryParse(_configuration["RAG:ChunkSize"], out var size) ? size : 800;
    private int ChunkOverlap => int.TryParse(_configuration["RAG:ChunkOverlap"], out var overlap) ? overlap : 150;

    public async Task<int> IndexAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting travel database indexing pipeline for TravelKnowledges...");

        var rawDocuments = new List<RawDocument>();

        var items = await _context.TravelKnowledges
            .AsNoTracking()
            .Where(item => !item.IsArchived)
            .ToListAsync();
        foreach (var item in items)
        {
            var text = $"Category: {item.Category}. Title: {item.Title}. Location: {item.City}, {item.Country}. Content: {item.Content}";
            rawDocuments.Add(new RawDocument(item.Id.ToString(), item.Category, text, item.City ?? "", item.Country ?? "", item.SourceUrl));
        }

        await _qdrantService.CreateCollectionAsync();

        if (rawDocuments.Count == 0)
        {
            _logger.LogWarning("No TravelKnowledge records found in database to index.");
            return 0;
        }

        _logger.LogInformation("Loaded {Count} TravelKnowledge records from database. Chunking...", rawDocuments.Count);

        var chunks = new List<DocumentChunk>();
        foreach (var doc in rawDocuments)
        {
            chunks.AddRange(ChunkText(doc));
        }

        _logger.LogInformation("Created {ChunkCount} chunks from {DocCount} records. Ensuring vector collection exists...", chunks.Count, rawDocuments.Count);
        
        await EmbedChunksAsync(chunks);

        _logger.LogInformation("Upserting vectors into Qdrant...");
        await _qdrantService.SaveChunksAsync(chunks);

        stopwatch.Stop();
        _logger.LogInformation("Indexing pipeline completed. Processed {DocCount} records, saved {ChunkCount} vector chunks in {Time}ms.", 
            rawDocuments.Count, chunks.Count, stopwatch.ElapsedMilliseconds);

        return chunks.Count;
    }

    public async Task<int> ReindexAllAsync()
    {
        _logger.LogInformation("Reindexing triggered. Deleting existing Qdrant collection...");
        await _qdrantService.DeleteCollectionAsync();
        return await IndexAllAsync();
    }

    public async Task<int> SyncKnowledgeAsync(int knowledgeId)
    {
        var item = await _context.TravelKnowledges
            .AsNoTracking()
            .SingleOrDefaultAsync(knowledge => knowledge.Id == knowledgeId);

        await _qdrantService.CreateCollectionAsync();
        await _qdrantService.DeleteChunksByEntityIdAsync(knowledgeId.ToString());

        if (item is null || item.IsArchived)
        {
            _logger.LogInformation(
                "Removed vector chunks for missing or archived knowledge item {KnowledgeId}.",
                knowledgeId);
            return 0;
        }

        var text = $"Category: {item.Category}. Title: {item.Title}. Location: {item.City}, {item.Country}. Content: {item.Content}";
        var chunks = ChunkText(new RawDocument(
            item.Id.ToString(),
            item.Category,
            text,
            item.City ?? string.Empty,
            item.Country ?? string.Empty,
            item.SourceUrl));

        await EmbedChunksAsync(chunks);
        await _qdrantService.SaveChunksAsync(chunks);

        _logger.LogInformation(
            "Synchronized knowledge item {KnowledgeId} to {ChunkCount} Qdrant chunks.",
            knowledgeId,
            chunks.Count);

        return chunks.Count;
    }

    private async Task EmbedChunksAsync(List<DocumentChunk> chunks)
    {
        const int batchSize = 20;
        for (var i = 0; i < chunks.Count; i += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, chunks.Count - i);
            var batch = chunks.GetRange(i, currentBatchSize);
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
                batch.Select(chunk => chunk.Content).ToList());

            if (embeddings.Count != batch.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding provider returned {embeddings.Count} vectors for a batch of {batch.Count} chunks.");
            }

            for (var j = 0; j < batch.Count; j++)
            {
                batch[j].Embedding = embeddings[j];
            }
        }
    }

    private List<DocumentChunk> ChunkText(RawDocument doc)
    {
        var chunks = new List<DocumentChunk>();
        var text = doc.Content;
        var limit = ChunkSize;
        var overlap = ChunkOverlap;

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        var chunkIndex = 0;

        if (text.Length <= limit)
        {
            chunks.Add(new DocumentChunk
            {
                Id = CreateStableChunkId(doc.EntityId, chunkIndex),
                EntityId = doc.EntityId,
                EntityType = doc.EntityType,
                Content = text,
                Source = "SQL Server",
                SourceUrl = doc.SourceUrl,
                City = doc.City,
                Country = doc.Country,
                CreatedDate = DateTime.UtcNow
            });
            return chunks;
        }

        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + limit, text.Length);

            if (end < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', end, end - start);
                if (lastSpace > start + (limit / 2))
                {
                    end = lastSpace;
                }
            }

            var chunkText = text.Substring(start, end - start).Trim();
            if (!string.IsNullOrEmpty(chunkText))
            {
                chunks.Add(new DocumentChunk
                {
                    Id = CreateStableChunkId(doc.EntityId, chunkIndex++),
                    EntityId = doc.EntityId,
                    EntityType = doc.EntityType,
                    Content = chunkText,
                    Source = "SQL Server",
                    SourceUrl = doc.SourceUrl,
                    City = doc.City,
                    Country = doc.Country,
                    CreatedDate = DateTime.UtcNow
                });
            }

            int step = end - start;
            start += step - overlap;

            if (step <= overlap || overlap >= limit)
            {
                start = end;
            }
            if (start >= text.Length || end >= text.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static string CreateStableChunkId(string entityId, int chunkIndex)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"TravelKnowledge:{entityId}:{chunkIndex}"));
        return new Guid(hash).ToString();
    }

    private record RawDocument(string EntityId, string EntityType, string Content, string City, string Country, string? SourceUrl);
}
