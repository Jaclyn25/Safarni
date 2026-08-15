using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IChatRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);
    void DeleteMessages(IEnumerable<ConversationMessage> messages);
    void Delete(Conversation conversation);
    Task<List<TravelKnowledge>> GetTravelKnowledgesAsync(CancellationToken cancellationToken = default);

    // Folder methods
    Task<List<ChatFolder>> GetFoldersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatFolder?> GetFolderByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task AddFolderAsync(ChatFolder folder, CancellationToken cancellationToken = default);
    void DeleteFolder(ChatFolder folder);
}
