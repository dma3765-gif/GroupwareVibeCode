using Engine.Application.Common.Interfaces;

namespace Engine.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var dir = Path.Combine(_basePath, datePath);
        Directory.CreateDirectory(dir);

        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(dir, storedName);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);

        return Path.Combine(datePath, storedName).Replace("\\", "/");
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("파일을 찾을 수 없습니다.", storagePath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
