using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Roles;

public sealed record GetRolesQuery()
    : IRequest<Result<IReadOnlyList<RoleResponse>>>;

public sealed record RoleResponse(
    string Name,
    string Description,
    IReadOnlyList<string> Capabilities);

public sealed class GetRolesQueryHandler
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    public Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new RoleResponse(
                UserRole.Admin.ToString(),
                "Full access to every admin module.",
                new[] { "Users", "Knowledge", "AI activity", "Integrations", "Analytics", "Roles", "Audit logs" }),
            new RoleResponse(
                UserRole.Support.ToString(),
                "View users, inspect AI conversations, force password resets, and browse audit logs.",
                new[] { "Users (view)", "AI conversation inspection", "Force password reset", "Audit logs" }),
            new RoleResponse(
                UserRole.ContentManager.ToString(),
                "Manage the travel knowledge base and trigger RAG reindexes.",
                new[] { "Knowledge base", "Reindex" }),
            new RoleResponse(
                UserRole.Analyst.ToString(),
                "Read-only access to dashboards and analytics.",
                new[] { "Dashboard", "Analytics" }),
            new RoleResponse(
                UserRole.User.ToString(),
                "Normal traveler account. No admin access.",
                Array.Empty<string>())
        };

        return Task.FromResult(
            Result<IReadOnlyList<RoleResponse>>.Success(roles));
    }
}
