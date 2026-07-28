using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IChatRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);
    void Delete(Conversation conversation);
    Task<List<TravelKnowledge>> GetTravelKnowledgesAsync(CancellationToken cancellationToken = default);
}
