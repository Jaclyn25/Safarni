using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Safarni.Infrastructure.Persistence;

namespace ChatBotApi.Infrastructure.Persistence.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
    }

    public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
    {
        await _context.ConversationMessages.AddAsync(message, cancellationToken);
    }

    public void DeleteMessages(IEnumerable<ConversationMessage> messages)
    {
        _context.ConversationMessages.RemoveRange(messages);
    }

    public void Delete(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
    }

    public async Task<List<TravelKnowledge>> GetTravelKnowledgesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TravelKnowledges
            .AsNoTracking()
            .Where(knowledge => !knowledge.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatFolder>> GetFoldersByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatFolders
            .Include(f => f.Conversations)
            .Where(f => f.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatFolder?> GetFolderByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatFolders
            .Include(f => f.Conversations)
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);
    }

    public async Task AddFolderAsync(ChatFolder folder, CancellationToken cancellationToken = default)
    {
        await _context.ChatFolders.AddAsync(folder, cancellationToken);
    }

    public void DeleteFolder(ChatFolder folder)
    {
        _context.ChatFolders.Remove(folder);
    }
}
