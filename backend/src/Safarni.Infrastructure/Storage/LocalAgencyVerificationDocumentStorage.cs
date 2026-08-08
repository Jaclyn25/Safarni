using Safarni.Application.Abstractions.Storage;

namespace Safarni.Infrastructure.Storage;

public sealed class LocalAgencyVerificationDocumentStorage
    : IAgencyVerificationDocumentStorage
{
    private const string StorageDirectoryRelativePath =
        "App_Data/agency-verification-documents";

    public async Task<string> SaveAsync(
        Guid agencyId,
        Stream fileStream,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var agencyFolder = agencyId.ToString("N");
        var fileName = $"{Guid.NewGuid():N}{fileExtension.ToLowerInvariant()}";
        var storageReference = Path.Combine(agencyFolder, fileName).Replace('\\', '/');
        var filePath = ResolvePath(storageReference);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var output = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await fileStream.CopyToAsync(output, cancellationToken);
        return storageReference;
    }

    public Task<Stream?> OpenReadAsync(
        string storageReference,
        CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(storageReference);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string? storageReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageReference))
        {
            return Task.CompletedTask;
        }

        var filePath = ResolvePath(storageReference);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string ResolvePath(string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference) ||
            Path.IsPathRooted(storageReference))
        {
            throw new InvalidOperationException("Invalid verification-document storage reference.");
        }

        var root = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            StorageDirectoryRelativePath));
        var candidate = Path.GetFullPath(Path.Combine(
            root,
            storageReference.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid verification-document storage reference.");
        }

        return candidate;
    }
}
