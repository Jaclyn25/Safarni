using System.Collections.Generic;
using System.Threading.Tasks;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IQdrantService
{
    Task CreateCollectionAsync();
    Task DeleteCollectionAsync();
    Task SaveChunksAsync(List<DocumentChunk> chunks);
    Task<List<DocumentChunk>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK);
    Task<bool> IsHealthyAsync();
}
