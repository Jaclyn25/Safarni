using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
}
