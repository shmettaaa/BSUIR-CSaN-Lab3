using Microsoft.Extensions.Configuration;

namespace CSaN_Lab3_Backend.Services;

public class FileStorageService
{
    private readonly string _storageRoot;

    public FileStorageService(IConfiguration configuration)
    {
        _storageRoot = configuration["StoragePath"] ?? "Storage";
        Directory.CreateDirectory(_storageRoot);
    }

    public string GetFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Путь к файлу не может быть пустым");

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, relativePath));
        var storageRootFull = Path.GetFullPath(_storageRoot);

        if (!fullPath.StartsWith(storageRootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Попытка выхода за пределы хранилища");
        }

        return fullPath;
    }

    public bool FileExists(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        return System.IO.File.Exists(fullPath);
    }

    public Task<Stream> GetFileStreamAsync(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);

        if (!System.IO.File.Exists(fullPath))
            throw new FileNotFoundException($"Файл не найден: {relativePath}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public async Task SaveFileAsync(string relativePath, Stream inputStream)
    {
        var fullPath = GetFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await inputStream.CopyToAsync(fileStream);
    }


    public async Task AppendToFileAsync(string relativePath, Stream inputStream)
    {
        var fullPath = GetFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(
            fullPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await inputStream.CopyToAsync(fileStream);
    }

    public void DeleteFile(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException();

        File.Delete(fullPath);
    }

    public IEnumerable<string> GetAllFiles()
    {
        var rootFullPath = Path.GetFullPath(_storageRoot);

        var files = Directory.GetFiles(rootFullPath, "*", SearchOption.AllDirectories);

        return files.Select(fullPath =>
        {
            var relativePath = Path.GetRelativePath(rootFullPath, fullPath);

            return relativePath.Replace("\\", "/");
        });
    }

}