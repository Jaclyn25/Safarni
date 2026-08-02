using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Integrations;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Integrations;

public sealed record GetIntegrationsHealthQuery()
    : IRequest<Result<IReadOnlyList<IntegrationHealthCheck>>>;

public sealed class GetIntegrationsHealthQueryHandler
    : IRequestHandler<GetIntegrationsHealthQuery, Result<IReadOnlyList<IntegrationHealthCheck>>>
{
    private readonly IExternalServiceHealthChecker _healthChecker;

    public GetIntegrationsHealthQueryHandler(IExternalServiceHealthChecker healthChecker)
    {
        _healthChecker = healthChecker;
    }

    public async Task<Result<IReadOnlyList<IntegrationHealthCheck>>> Handle(
        GetIntegrationsHealthQuery request,
        CancellationToken cancellationToken)
    {
        var checks = await _healthChecker.CheckAllAsync(cancellationToken);
        return Result<IReadOnlyList<IntegrationHealthCheck>>.Success(checks);
    }
}

public sealed record GetIntegrationsConfigQuery()
    : IRequest<Result<IReadOnlyList<IntegrationConfigEntry>>>;

public sealed class GetIntegrationsConfigQueryHandler
    : IRequestHandler<GetIntegrationsConfigQuery, Result<IReadOnlyList<IntegrationConfigEntry>>>
{
    private readonly IExternalServiceHealthChecker _healthChecker;

    public GetIntegrationsConfigQueryHandler(IExternalServiceHealthChecker healthChecker)
    {
        _healthChecker = healthChecker;
    }

    public Task<Result<IReadOnlyList<IntegrationConfigEntry>>> Handle(
        GetIntegrationsConfigQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Result<IReadOnlyList<IntegrationConfigEntry>>.Success(
                _healthChecker.GetConfigStatus()));
    }
}

public sealed record TestIntegrationCommand(string Name)
    : IRequest<Result<IntegrationHealthCheck>>;

public sealed class TestIntegrationCommandHandler
    : IRequestHandler<TestIntegrationCommand, Result<IntegrationHealthCheck>>
{
    private readonly IExternalServiceHealthChecker _healthChecker;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public TestIntegrationCommandHandler(
        IExternalServiceHealthChecker healthChecker,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _healthChecker = healthChecker;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<IntegrationHealthCheck>> Handle(
        TestIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        var check = await _healthChecker.CheckByNameAsync(
            request.Name,
            cancellationToken);

        if (check is null)
        {
            return Result<IntegrationHealthCheck>.Failure(
                AdminErrors.UnknownIntegration);
        }

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.IntegrationTested,
            "Integration",
            request.Name,
            new { status = check.Status, detail = check.Detail, latencyMs = check.LatencyMs },
            cancellationToken: cancellationToken);

        return Result<IntegrationHealthCheck>.Success(check);
    }
}
