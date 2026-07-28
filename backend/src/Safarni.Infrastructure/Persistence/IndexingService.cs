using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        var items = await _context.TravelKnowledges.AsNoTracking().ToListAsync();
        foreach (var item in items)
        {
            var text = $"Category: {item.Category}. Title: {item.Title}. Location: {item.City}, {item.Country}. Content: {item.Content}";
            rawDocuments.Add(new RawDocument(item.Id.ToString(), item.Category, text, item.City ?? "", item.Country ?? ""));
        }

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
        
        await _qdrantService.CreateCollectionAsync();

        const int batchSize = 20;
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            int currentBatchSize = Math.Min(batchSize, chunks.Count - i);
            var batch = chunks.GetRange(i, currentBatchSize);
            
            var textsToEmbed = new List<string>();
            foreach (var chunk in batch)
            {
                textsToEmbed.Add(chunk.Content);
            }

            try
            {
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(textsToEmbed);
                for (int j = 0; j < batch.Count; j++)
                {
                    batch[j].Embedding = embeddings[j];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for batch starting at {Index}.", i);
                throw;
            }
        }

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

    private List<DocumentChunk> ChunkText(RawDocument doc)
    {
        var chunks = new List<DocumentChunk>();
        var text = doc.Content;
        var limit = ChunkSize;
        var overlap = ChunkOverlap;

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        if (text.Length <= limit)
        {
            chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                EntityId = doc.EntityId,
                EntityType = doc.EntityType,
                Content = text,
                Source = "SQL Server",
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
                    Id = Guid.NewGuid().ToString(),
                    EntityId = doc.EntityId,
                    EntityType = doc.EntityType,
                    Content = chunkText,
                    Source = "SQL Server",
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

    private record RawDocument(string EntityId, string EntityType, string Content, string City, string Country);
}
