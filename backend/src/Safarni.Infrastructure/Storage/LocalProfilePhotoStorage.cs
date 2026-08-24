using Safarni.Application.Abstractions.Storage;

using Microsoft.Extensions.Hosting;

namespace Safarni.Infrastructure.Storage;

public sealed class LocalProfilePhotoStorage : IProfilePhotoStorage
{
    private readonly string _directory;
    private const string PhotoUrlBasePath = "/uploads/profile-photos";

    public LocalProfilePhotoStorage(IHostEnvironment environment)
    {
        _directory = Path.Combine(environment.ContentRootPath, "App_Data", "uploads", "profile-photos");
    }

    public async Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        Directory.CreateDirectory(_directory);

        var filePath = Path.Combine(_directory, fileName);

        await using var output = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(output, cancellationToken);

        return $"{PhotoUrlBasePath}/{fileName}";
    }

    public Task DeleteAsync(string? photoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            return Task.CompletedTask;
        }

        var normalizedUrl = photoUrl.Replace('\\', '/').Trim();

        if (!normalizedUrl.StartsWith(PhotoUrlBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var fileName = Path.GetFileName(normalizedUrl);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.CompletedTask;
        }

        var filePath = Path.Combine(_directory, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
