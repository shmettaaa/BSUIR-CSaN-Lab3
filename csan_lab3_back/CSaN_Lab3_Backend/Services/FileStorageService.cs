using Microsoft.Extensions.Configuration;
using CSaN_Lab3_Backend.Data;
using CSaN_Lab3_Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;

namespace CSaN_Lab3_Backend.Services;

public class FileStorageService
{
    private readonly string _storageRoot;
    private readonly AppDbContext _context;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public FileStorageService(IConfiguration configuration, AppDbContext context)
    {
        _storageRoot = configuration["StoragePath"] ?? "Storage";
        Directory.CreateDirectory(_storageRoot);
        _context = context;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }


    private string GetFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Путь к файлу не может быть пустым");

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, relativePath));
        var storageRootFull = Path.GetFullPath(_storageRoot);

        if (!fullPath.StartsWith(storageRootFull, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Попытка выхода за пределы хранилища");

        return fullPath;
    }

    private string GetContentType(string fileName)
    {
        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";
        return contentType;
    }


    public Stream GetFileStream(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Файл не найден: {relativePath}");

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task SaveFileAsync(string relativePath, Stream inputStream)
    {
        var fullPath = GetFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await inputStream.CopyToAsync(fileStream);

        var fileInfo = new FileInfo(fullPath);
        var contentType = GetContentType(relativePath);

        var existing = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == relativePath);
        if (existing == null)
        {
            var metadata = new FileMetadata
            {
                RelativePath = relativePath,
                FileName = Path.GetFileName(relativePath),
                Size = fileInfo.Length,
                ContentType = contentType,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = fileInfo.LastWriteTimeUtc
            };
            await _context.Files.AddAsync(metadata);
        }
        else
        {
            existing.FileName = Path.GetFileName(relativePath);
            existing.Size = fileInfo.Length;
            existing.ContentType = contentType;
            existing.ModifiedAt = fileInfo.LastWriteTimeUtc;
            _context.Files.Update(existing);
        }
        await _context.SaveChangesAsync();
    }

    public async Task AppendToFileAsync(string relativePath, Stream inputStream)
    {
        var fullPath = GetFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.None, 81920, true);
        await inputStream.CopyToAsync(fileStream);

        var fileInfo = new FileInfo(fullPath);
        var existing = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == relativePath);
        if (existing != null)
        {
            existing.Size = fileInfo.Length;
            existing.ModifiedAt = fileInfo.LastWriteTimeUtc;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException();

        var metadata = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == relativePath);
        if (metadata != null)
        {
            _context.Files.Remove(metadata);
            await _context.SaveChangesAsync();
        }

        File.Delete(fullPath);
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        var sourceFullPath = GetFullPath(sourcePath);
        var destFullPath = GetFullPath(destinationPath);

        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException();

        var dir = Path.GetDirectoryName(destFullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.Copy(sourceFullPath, destFullPath, true);

        var sourceMetadata = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == sourcePath);
        var destFileInfo = new FileInfo(destFullPath);
        var destMetadata = new FileMetadata
        {
            RelativePath = destinationPath,
            FileName = Path.GetFileName(destinationPath),
            Size = destFileInfo.Length,
            ContentType = GetContentType(destinationPath),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = destFileInfo.LastWriteTimeUtc
        };
        await _context.Files.AddAsync(destMetadata);
        await _context.SaveChangesAsync();
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        var sourceFullPath = GetFullPath(sourcePath);
        var destFullPath = GetFullPath(destinationPath);

        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException();

        var dir = Path.GetDirectoryName(destFullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.Move(sourceFullPath, destFullPath, true);

        var metadata = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == sourcePath);
        if (metadata != null)
        {
            metadata.RelativePath = destinationPath;
            metadata.FileName = Path.GetFileName(destinationPath);
            metadata.ModifiedAt = File.GetLastWriteTimeUtc(destFullPath);
            metadata.ContentType = GetContentType(destinationPath);
            await _context.SaveChangesAsync();
        }
    }


    public async Task<IEnumerable<string>> GetAllFilesAsync()
    {
        return await _context.Files
            .Select(f => f.RelativePath)
            .OrderBy(p => p)
            .ToListAsync();
    }


    public async Task SyncWithDiskAsync()
    {
        var rootFull = Path.GetFullPath(_storageRoot);
        var allDiskFiles = Directory.GetFiles(rootFull, "*", SearchOption.AllDirectories)
            .Select(fullPath => Path.GetRelativePath(rootFull, fullPath).Replace('\\', '/'));

        foreach (var relPath in allDiskFiles)
        {
            var existsInDb = await _context.Files.AnyAsync(f => f.RelativePath == relPath);
            if (!existsInDb)
            {
                var fullPath = GetFullPath(relPath);
                var fileInfo = new FileInfo(fullPath);
                var metadata = new FileMetadata
                {
                    RelativePath = relPath,
                    FileName = Path.GetFileName(relPath),
                    Size = fileInfo.Length,
                    ContentType = GetContentType(relPath),
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc
                };
                await _context.Files.AddAsync(metadata);
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string relativePath)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.RelativePath == relativePath);
    }


}