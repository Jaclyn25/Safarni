using System.Threading.Tasks;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IIndexingService
{
    Task<int> IndexAllAsync();
    Task<int> ReindexAllAsync();
    Task<int> SyncKnowledgeAsync(int knowledgeId);
}
