using ChatBotApi.Domain.Entities;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Knowledge;

public sealed record AdminKnowledgeResponse(
    int Id,
    string Title,
    string Category,
    string Content,
    string? Country,
    string? City,
    string? SourceUrl,
    string MetadataJson,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime LastUpdated,
    Guid? UpdatedByUserId);

public sealed record KnowledgeInput(
    string Title,
    string Category,
    string Content,
    string? Country = null,
    string? City = null,
    string? SourceUrl = null,
    string? MetadataJson = null);

public sealed record ListKnowledgeQuery(KnowledgeSearchCriteria Criteria)
    : IRequest<Result<PagedResult<AdminKnowledgeResponse>>>;

public static class AdminKnowledgeResponseMapper
{
    public static AdminKnowledgeResponse ToResponse(TravelKnowledge item)
    {
        return new AdminKnowledgeResponse(
            item.Id,
            item.Title,
            item.Category,
            item.Content,
            item.Country,
            item.City,
            item.SourceUrl,
            item.MetadataJson,
            item.IsArchived,
            item.CreatedAt,
            item.LastUpdated,
            item.UpdatedByUserId);
    }
}

public sealed class ListKnowledgeQueryHandler
    : IRequestHandler<ListKnowledgeQuery, Result<PagedResult<AdminKnowledgeResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public ListKnowledgeQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<PagedResult<AdminKnowledgeResponse>>> Handle(
        ListKnowledgeQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _adminRepository.SearchKnowledgeAsync(
            request.Criteria,
            cancellationToken);

        return Result<PagedResult<AdminKnowledgeResponse>>.Success(
            new PagedResult<AdminKnowledgeResponse>(
                result.Items
                    .Select(AdminKnowledgeResponseMapper.ToResponse)
                    .ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount));
    }
}
