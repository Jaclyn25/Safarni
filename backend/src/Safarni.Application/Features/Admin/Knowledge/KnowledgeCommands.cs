using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using ChatBotApi.Domain.Entities;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Admin.Knowledge;

public sealed record CreateKnowledgeCommand(KnowledgeInput Input)
    : IRequest<Result<AdminKnowledgeResponse>>;

public sealed class CreateKnowledgeCommandHandler
    : IRequestHandler<CreateKnowledgeCommand, Result<AdminKnowledgeResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public CreateKnowledgeCommandHandler(
        IAdminRepository adminRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _adminRepository = adminRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminKnowledgeResponse>> Handle(
        CreateKnowledgeCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (string.IsNullOrWhiteSpace(input.Title) ||
            string.IsNullOrWhiteSpace(input.Content))
        {
            return Result<AdminKnowledgeResponse>.Failure(
                AdminErrors.KnowledgeValidationFailed);
        }

        var now = DateTime.UtcNow;
        var item = new TravelKnowledge
        {
            Title = input.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(input.Category)
                ? "General"
                : input.Category.Trim(),
            Content = input.Content.Trim(),
            Country = input.Country?.Trim(),
            City = input.City?.Trim(),
            SourceUrl = input.SourceUrl?.Trim(),
            MetadataJson = input.MetadataJson ?? "{}",
            CreatedAt = now,
            LastUpdated = now,
            UpdatedByUserId = _currentUser.UserId
        };

        await _adminRepository.AddKnowledgeAsync(item, cancellationToken);

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.KnowledgeCreated,
            "Knowledge",
            item.Id.ToString(),
            new { title = item.Title, category = item.Category },
            cancellationToken: cancellationToken);

        return Result<AdminKnowledgeResponse>.Success(
            AdminKnowledgeResponseMapper.ToResponse(item));
    }
}

public sealed record UpdateKnowledgeCommand(int Id, KnowledgeInput Input)
    : IRequest<Result<AdminKnowledgeResponse>>;

public sealed class UpdateKnowledgeCommandHandler
    : IRequestHandler<UpdateKnowledgeCommand, Result<AdminKnowledgeResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public UpdateKnowledgeCommandHandler(
        IAdminRepository adminRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _adminRepository = adminRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminKnowledgeResponse>> Handle(
        UpdateKnowledgeCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (string.IsNullOrWhiteSpace(input.Title) ||
            string.IsNullOrWhiteSpace(input.Content))
        {
            return Result<AdminKnowledgeResponse>.Failure(
                AdminErrors.KnowledgeValidationFailed);
        }

        var item = await _adminRepository.GetKnowledgeByIdAsync(
            request.Id,
            cancellationToken);

        if (item is null)
        {
            return Result<AdminKnowledgeResponse>.Failure(
                AdminErrors.KnowledgeNotFound);
        }

        item.Title = input.Title.Trim();
        item.Category = string.IsNullOrWhiteSpace(input.Category)
            ? item.Category
            : input.Category.Trim();
        item.Content = input.Content.Trim();
        item.Country = input.Country?.Trim();
        item.City = input.City?.Trim();
        item.SourceUrl = input.SourceUrl?.Trim();
        item.MetadataJson = input.MetadataJson ?? item.MetadataJson;
        item.LastUpdated = DateTime.UtcNow;
        item.UpdatedByUserId = _currentUser.UserId;

        await _adminRepository.SaveKnowledgeChangesAsync(cancellationToken);

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.KnowledgeUpdated,
            "Knowledge",
            item.Id.ToString(),
            new { title = item.Title, category = item.Category },
            cancellationToken: cancellationToken);

        return Result<AdminKnowledgeResponse>.Success(
            AdminKnowledgeResponseMapper.ToResponse(item));
    }
}

public sealed record ArchiveKnowledgeCommand(int Id, bool Archived)
    : IRequest<Result<AdminKnowledgeResponse>>;

public sealed class ArchiveKnowledgeCommandHandler
    : IRequestHandler<ArchiveKnowledgeCommand, Result<AdminKnowledgeResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public ArchiveKnowledgeCommandHandler(
        IAdminRepository adminRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _adminRepository = adminRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminKnowledgeResponse>> Handle(
        ArchiveKnowledgeCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _adminRepository.GetKnowledgeByIdAsync(
            request.Id,
            cancellationToken);

        if (item is null)
        {
            return Result<AdminKnowledgeResponse>.Failure(
                AdminErrors.KnowledgeNotFound);
        }

        item.IsArchived = request.Archived;
        item.LastUpdated = DateTime.UtcNow;
        item.UpdatedByUserId = _currentUser.UserId;

        await _adminRepository.SaveKnowledgeChangesAsync(cancellationToken);

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            request.Archived
                ? AdminAuditActions.KnowledgeArchived
                : AdminAuditActions.KnowledgeRestored,
            "Knowledge",
            item.Id.ToString(),
            new { title = item.Title },
            cancellationToken: cancellationToken);

        return Result<AdminKnowledgeResponse>.Success(
            AdminKnowledgeResponseMapper.ToResponse(item));
    }
}
