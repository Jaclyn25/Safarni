using ChatBotApi.Application.Common.Interfaces;
using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Knowledge;

public sealed record ReindexKnowledgeCommand()
    : IRequest<Result<ReindexKnowledgeResponse>>;

public sealed record ReindexKnowledgeResponse(
    bool Success,
    string Message,
    int ChunkCount,
    bool QdrantHealthy);

public sealed class ReindexKnowledgeCommandHandler
    : IRequestHandler<ReindexKnowledgeCommand, Result<ReindexKnowledgeResponse>>
{
    private readonly IQdrantService _qdrantService;
    private readonly IIndexingService _indexingService;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public ReindexKnowledgeCommandHandler(
        IQdrantService qdrantService,
        IIndexingService indexingService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _qdrantService = qdrantService;
        _indexingService = indexingService;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<ReindexKnowledgeResponse>> Handle(
        ReindexKnowledgeCommand request,
        CancellationToken cancellationToken)
    {
        bool qdrantHealthy;
        try
        {
            qdrantHealthy = await _qdrantService.IsHealthyAsync();
        }
        catch
        {
            qdrantHealthy = false;
        }

        if (!qdrantHealthy)
        {
            return Result<ReindexKnowledgeResponse>.Failure(
                AdminErrors.QdrantUnavailable);
        }

        try
        {
            var chunkCount = await _indexingService.ReindexAllAsync();

            await _auditLogWriter.WriteAsync(
                _currentUser.UserId,
                AdminAuditActions.KnowledgeReindexed,
                "Knowledge",
                null,
                new { chunkCount, qdrantHealthy = true },
                cancellationToken: cancellationToken);

            return Result<ReindexKnowledgeResponse>.Success(
                new ReindexKnowledgeResponse(
                    true,
                    $"Reindex completed. {chunkCount} chunks written to Qdrant.",
                    chunkCount,
                    true));
        }
        catch (Exception ex)
        {
            await _auditLogWriter.WriteAsync(
                _currentUser.UserId,
                AdminAuditActions.KnowledgeReindexed,
                "Knowledge",
                null,
                new { succeeded = false, error = ex.Message },
                severity: Safarni.Domain.Enums.AuditSeverity.Error,
                cancellationToken: cancellationToken);

            return Result<ReindexKnowledgeResponse>.Success(
                new ReindexKnowledgeResponse(
                    false,
                    $"Reindex failed: {ex.Message}",
                    0,
                    true));
        }
    }
}
