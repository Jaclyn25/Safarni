using FluentValidation;

namespace Safarni.Application.Features.Profile.UploadProfilePhoto;

public sealed class UploadProfilePhotoCommandValidator : AbstractValidator<UploadProfilePhotoCommand>
{
    public const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public UploadProfilePhotoCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(contentType => AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only image/jpeg, image/png, and image/webp are allowed.");

        RuleFor(command => command.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxPhotoSizeBytes);

        RuleFor(command => command.FileContent)
            .NotNull()
            .Must(fileContent => fileContent.Length > 0)
            .WithMessage("Profile photo content is required.");
    }
}
