
namespace Safarni.Application.Abstractions.Storage;

public interface IProfilePhotoStorage
{
    Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    Task DeleteAsync(string? photoUrl, CancellationToken cancellationToken = default);
}
