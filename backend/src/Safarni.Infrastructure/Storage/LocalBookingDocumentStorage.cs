using Safarni.Application.Abstractions.Storage;

namespace Safarni.Infrastructure.Storage;

public sealed class LocalBookingDocumentStorage : IBookingDocumentStorage
{
    private const string StorageDirectoryRelativePath = "App_Data/booking-documents";

    public async Task<string> SaveAsync(Guid requestId, Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var requestFolder = requestId.ToString("N");
        var fileName = $"{Guid.NewGuid():N}{fileExtension.ToLowerInvariant()}";
        var reference = Path.Combine(requestFolder, fileName).Replace('\\', '/');
        var filePath = ResolvePath(reference);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await fileStream.CopyToAsync(output, cancellationToken);
        return reference;
    }

    public Task<Stream?> OpenReadAsync(string storageReference, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(storageReference);
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string? storageReference, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(storageReference))
        {
            var filePath = ResolvePath(storageReference);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private static string ResolvePath(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || Path.IsPathRooted(reference))
            throw new InvalidOperationException("Invalid booking-document storage reference.");
        var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), StorageDirectoryRelativePath));
        var candidate = Path.GetFullPath(Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid booking-document storage reference.");
        return candidate;
    }
}
