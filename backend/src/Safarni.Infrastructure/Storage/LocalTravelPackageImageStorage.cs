using Safarni.Application.Abstractions.Storage;

namespace Safarni.Infrastructure.Storage;

public sealed class LocalTravelPackageImageStorage : ITravelPackageImageStorage
{
    private const string DirectoryRelativePath = "wwwroot/uploads/travel-packages";
    private const string UrlBasePath = "/uploads/travel-packages";

    public async Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var safeExtension = fileExtension.StartsWith('.') ? fileExtension.ToLowerInvariant() : $".{fileExtension.ToLowerInvariant()}";
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var directory = Path.Combine(Directory.GetCurrentDirectory(), DirectoryRelativePath);
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        await using var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(output, cancellationToken);
        return $"{UrlBasePath}/{fileName}";
    }

    public Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return Task.CompletedTask;
        var normalized = imageUrl.Replace('\\', '/').Trim();
        if (!normalized.StartsWith(UrlBasePath, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName)) return Task.CompletedTask;
        var path = Path.Combine(Directory.GetCurrentDirectory(), DirectoryRelativePath, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
