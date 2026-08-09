namespace Safarni.Application.Abstractions.Storage;

public interface ITravelPackageImageStorage
{
    Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken = default);
}
