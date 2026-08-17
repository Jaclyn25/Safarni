namespace Safarni.Application.Abstractions.Storage;

public interface ICommunityMediaStorage
{
    Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    Task DeleteAsync(string? mediaUrl, CancellationToken cancellationToken = default);
}
