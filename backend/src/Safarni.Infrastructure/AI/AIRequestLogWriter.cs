using Safarni.Application.Abstractions.AI;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.AI;

public sealed class AIRequestLogWriter : IAIRequestLogWriter
{
    private readonly ApplicationDbContext _dbContext;

    public AIRequestLogWriter(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        AIRequestLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AIRequestLogs.Add(new AIRequestLog
        {
            Id = Guid.NewGuid(),
            UserId = entry.UserId,
            ConversationId = entry.ConversationId,
            ItineraryJobId = entry.ItineraryJobId,
            Provider = entry.Provider,
            Model = entry.Model,
            Operation = entry.Operation,
            Status = entry.Status,
            LatencyMs = entry.LatencyMs,
            PromptTokens = entry.PromptTokens,
            CompletionTokens = entry.CompletionTokens,
            ErrorCode = entry.ErrorCode,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
