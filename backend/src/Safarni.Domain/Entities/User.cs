using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public bool IsEmailVerified { get; set; }

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public string? Provider { get; set; }

    public string? ProviderUserId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public TravelPreference? TravelPreference { get; set; }

    public PassportInfo? PassportInfo { get; set; }

    public TravelAgency? TravelAgency { get; set; }

    public ICollection<VisaApplication> VisaApplications { get; set; } = new List<VisaApplication>();

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public ICollection<UserNotification> Notifications { get; set; } =
        new List<UserNotification>();
}
