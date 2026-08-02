using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Ai;

public sealed record AiActivityResponse(
    Guid Id,
    Guid? UserId,
    Guid? ConversationId,
    Guid? ItineraryJobId,
    string Provider,
    string Model,
    string Operation,
    string Status,
    long LatencyMs,
    int? PromptTokens,
    int? CompletionTokens,
    string? ErrorCode,
    DateTime CreatedAt);

public sealed record ListAiActivityQuery(AiActivityCriteria Criteria)
    : IRequest<Result<PagedResult<AiActivityResponse>>>;

public sealed class ListAiActivityQueryHandler
    : IRequestHandler<ListAiActivityQuery, Result<PagedResult<AiActivityResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public ListAiActivityQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<PagedResult<AiActivityResponse>>> Handle(
        ListAiActivityQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _adminRepository.SearchAiActivityAsync(
            request.Criteria,
            cancellationToken);

        return Result<PagedResult<AiActivityResponse>>.Success(
            new PagedResult<AiActivityResponse>(
                result.Items
                    .Select(log => new AiActivityResponse(
                        log.Id,
                        log.UserId,
                        log.ConversationId,
                        log.ItineraryJobId,
                        log.Provider,
                        log.Model,
                        log.Operation,
                        log.Status,
                        log.LatencyMs,
                        log.PromptTokens,
                        log.CompletionTokens,
                        log.ErrorCode,
                        log.CreatedAt))
                    .ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount));
    }
}

public sealed record AdminConversationResponse(
    Guid Id,
    string Title,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    DateTime CreatedAt,
    bool IsPinned,
    IReadOnlyList<AdminConversationMessageResponse> Messages);

public sealed record AdminConversationMessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record GetAiConversationQuery(Guid ConversationId)
    : IRequest<Result<AdminConversationResponse>>;

public sealed class GetAiConversationQueryHandler
    : IRequestHandler<GetAiConversationQuery, Result<AdminConversationResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly Safarni.Application.Abstractions.Auditing.IAuditLogWriter _auditLogWriter;
    private readonly Safarni.Application.Abstractions.Authentication.ICurrentUserService _currentUser;

    public GetAiConversationQueryHandler(
        IAdminRepository adminRepository,
        Safarni.Application.Abstractions.Auditing.IAuditLogWriter auditLogWriter,
        Safarni.Application.Abstractions.Authentication.ICurrentUserService currentUser)
    {
        _adminRepository = adminRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminConversationResponse>> Handle(
        GetAiConversationQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await _adminRepository.GetConversationForAdminAsync(
            request.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            return Result<AdminConversationResponse>.Failure(
                AdminErrors.ConversationNotFound);
        }

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.AIConversationViewedByAdmin,
            "Conversation",
            request.ConversationId.ToString(),
            cancellationToken: cancellationToken);

        return Result<AdminConversationResponse>.Success(
            new AdminConversationResponse(
                conversation.Id,
                conversation.Title,
                conversation.UserId,
                conversation.UserEmail,
                conversation.UserFullName,
                conversation.CreatedAt,
                conversation.IsPinned,
                conversation.Messages
                    .Select(message => new AdminConversationMessageResponse(
                        message.Id,
                        message.Role,
                        message.Content,
                        message.CreatedAt))
                    .ToArray()));
    }
}
