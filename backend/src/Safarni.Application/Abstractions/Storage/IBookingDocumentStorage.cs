namespace Safarni.Application.Abstractions.Storage;

public interface IBookingDocumentStorage
{
    Task<string> SaveAsync(Guid requestId, Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageReference, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? storageReference, CancellationToken cancellationToken = default);
}
