namespace Safarni.Application.Abstractions.Storage;

public interface IAgencyVerificationDocumentStorage
{
    Task<string> SaveAsync(
        Guid agencyId,
        Stream fileStream,
        string fileExtension,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageReference,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? storageReference,
        CancellationToken cancellationToken = default);
}
