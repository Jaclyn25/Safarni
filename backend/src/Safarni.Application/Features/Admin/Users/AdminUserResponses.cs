using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Users;

public sealed record AdminUserSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    bool IsEmailVerified,
    string? Provider,
    DateTime CreatedAt,
    DateTime? LastActiveAt);

public sealed record AdminUserDetailResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    bool IsEmailVerified,
    string? PhoneNumber,
    string? Country,
    string? City,
    string? Bio,
    string? ProfilePhotoUrl,
    string? Provider,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int TripsCount,
    int ConversationsCount,
    int MessagesCount);

public sealed record ForcePasswordResetResponse(
    bool EmailSent,
    string Message);

public static class AdminUserResponseMapper
{
    public static AdminUserSummaryResponse ToSummary(User user)
    {
        return new AdminUserSummaryResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.IsEmailVerified,
            user.Provider,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static AdminUserDetailResponse ToDetail(
        User user,
        int tripsCount,
        int conversationsCount,
        int messagesCount)
    {
        return new AdminUserDetailResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.IsEmailVerified,
            user.PhoneNumber,
            user.Country,
            user.City,
            user.Bio,
            user.ProfilePhotoUrl,
            user.Provider,
            user.CreatedAt,
            user.UpdatedAt,
            tripsCount,
            conversationsCount,
            messagesCount);
    }
}
