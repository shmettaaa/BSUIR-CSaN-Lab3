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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

    private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

    public FileStorageService(IConfiguration configuration, AppDbContext context)
    {
        _storageRoot = configuration["StoragePath"] ?? "Storage";
        Directory.CreateDirectory(_storageRoot);
        _context = context;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    private SemaphoreSlim GetFileLock(string relativePath)
    {
        return _fileLocks.GetOrAdd(relativePath, _ => new SemaphoreSlim(1, 1));
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
        var threadId = Environment.CurrentManagedThreadId;
        var now = DateTime.Now.ToString("HH:mm:ss.fff");

        Console.WriteLine($"[{now}][{threadId}] НАЧАЛО: {relativePath}");

        var fileLock = GetFileLock(relativePath);

        Console.WriteLine($"[{now}][{threadId}] ОЖИДАНИЕ БЛОКИРОВКИ: {relativePath}");
        await fileLock.WaitAsync();
        Console.WriteLine($"[{now}][{threadId}] БЛОКИРОВКА ПОЛУЧЕНА: {relativePath}");

        try
        {
            var fullPath = GetFullPath(relativePath);
            Console.WriteLine($"[{now}][{threadId}] ПОЛНЫЙ ПУТЬ: {fullPath}");

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"[{now}][{threadId}] ДИРЕКТОРИЯ СОЗДАНА: {directory}");
            }

            // Важно: позиционируем поток в начало
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            Console.WriteLine($"[{now}][{threadId}] НАЧАЛО ЗАПИСИ ФАЙЛА НА ДИСК");
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await inputStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            Console.WriteLine($"[{now}][{threadId}] ФАЙЛ ЗАПИСАН. РАЗМЕР: {fileStream.Length}");

            var fileInfo = new FileInfo(fullPath);
            var contentType = GetContentType(relativePath);

            Console.WriteLine($"[{now}][{threadId}] ОБНОВЛЕНИЕ БД");
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
                Console.WriteLine($"[{now}][{threadId}] ЗАПИСЬ ДОБАВЛЕНА В БД");
            }
            else
            {
                existing.FileName = Path.GetFileName(relativePath);
                existing.Size = fileInfo.Length;
                existing.ContentType = contentType;
                existing.ModifiedAt = fileInfo.LastWriteTimeUtc;
                _context.Files.Update(existing);
                Console.WriteLine($"[{now}][{threadId}] ЗАПИСЬ ОБНОВЛЕНА В БД");
            }
            await _context.SaveChangesAsync();

            Console.WriteLine($"[{now}][{threadId}] ЗАВЕРШЕНО УСПЕШНО: {relativePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{now}][{threadId}] ОШИБКА: {ex.Message}");
            Console.WriteLine($"[{now}][{threadId}] СТЭК: {ex.StackTrace}");
            throw;
        }
        finally
        {
            fileLock.Release();
            Console.WriteLine($"[{now}][{threadId}] БЛОКИРОВКА ОСВОБОЖДЕНА: {relativePath}");
        }
    }
    public async Task AppendToFileAsync(string relativePath, Stream inputStream)
    {
        var threadId = Environment.CurrentManagedThreadId;
        var now = DateTime.Now.ToString("HH:mm:ss.fff");

        Console.WriteLine($"[{now}][{threadId}] APPEND НАЧАЛО: {relativePath}");

        var fileLock = GetFileLock(relativePath);
        await fileLock.WaitAsync();
        Console.WriteLine($"[{now}][{threadId}] APPEND БЛОКИРОВКА ПОЛУЧЕНА");

        try
        {
            var fullPath = GetFullPath(relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.None, 81920, true);

            using var reader = new StreamReader(inputStream);
            var text = await reader.ReadToEndAsync();
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            await fileStream.WriteAsync(bytes, 0, bytes.Length);
            await fileStream.FlushAsync();

            var fileInfo = new FileInfo(fullPath);

            var existing = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == relativePath);
            if (existing != null)
            {
                existing.Size = fileInfo.Length;
                existing.ModifiedAt = fileInfo.LastWriteTimeUtc;
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"[{now}][{threadId}] APPEND ГОТОВО. НОВЫЙ РАЗМЕР: {fileInfo.Length}");
        }
        finally
        {
            fileLock.Release();
            Console.WriteLine($"[{now}][{threadId}] APPEND БЛОКИРОВКА СНЯТА");
        }
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        var fileLock = GetFileLock(relativePath);
        await fileLock.WaitAsync();
        try
        {
            var fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException();

            await _dbLock.WaitAsync();
            try
            {
                var metadata = await _context.Files.FirstOrDefaultAsync(f => f.RelativePath == relativePath);
                if (metadata != null)
                {
                    _context.Files.Remove(metadata);
                    await _context.SaveChangesAsync();
                }
            }
            finally
            {
                _dbLock.Release();
            }

            File.Delete(fullPath);
        }
        finally
        {
            fileLock.Release();
            _fileLocks.TryRemove(relativePath, out _);
        }
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        var lock1 = GetFileLock(sourcePath);
        var lock2 = GetFileLock(destinationPath);

        var firstLock = string.Compare(sourcePath, destinationPath, StringComparison.Ordinal) < 0 ? lock1 : lock2;
        var secondLock = firstLock == lock1 ? lock2 : lock1;

        await firstLock.WaitAsync();
        try
        {
            await secondLock.WaitAsync();
            try
            {
                var sourceFullPath = GetFullPath(sourcePath);
                var destFullPath = GetFullPath(destinationPath);

                if (!File.Exists(sourceFullPath))
                    throw new FileNotFoundException();

                var dir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(sourceFullPath, destFullPath, true);

                var destFileInfo = new FileInfo(destFullPath);

                await _dbLock.WaitAsync();
                try
                {
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
                finally
                {
                    _dbLock.Release();
                }
            }
            finally
            {
                secondLock.Release();
            }
        }
        finally
        {
            firstLock.Release();
        }
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        var lock1 = GetFileLock(sourcePath);
        var lock2 = GetFileLock(destinationPath);

        var firstLock = string.Compare(sourcePath, destinationPath, StringComparison.Ordinal) < 0 ? lock1 : lock2;
        var secondLock = firstLock == lock1 ? lock2 : lock1;

        await firstLock.WaitAsync();
        try
        {
            await secondLock.WaitAsync();
            try
            {
                var sourceFullPath = GetFullPath(sourcePath);
                var destFullPath = GetFullPath(destinationPath);

                if (!File.Exists(sourceFullPath))
                    throw new FileNotFoundException();

                var dir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.Move(sourceFullPath, destFullPath, true);

                await _dbLock.WaitAsync();
                try
                {
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
                finally
                {
                    _dbLock.Release();
                }
            }
            finally
            {
                secondLock.Release();
            }
        }
        finally
        {
            firstLock.Release();
            _fileLocks.TryRemove(sourcePath, out _);
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
        await _syncLock.WaitAsync();
        try
        {
            var rootFull = Path.GetFullPath(_storageRoot);
            var allDiskFiles = Directory.GetFiles(rootFull, "*", SearchOption.AllDirectories)
                .Select(fullPath => Path.GetRelativePath(rootFull, fullPath).Replace('\\', '/'));

            foreach (var relPath in allDiskFiles)
            {
                var existsInDb = await _context.Files.AnyAsync(f => f.RelativePath == relPath);
                if (!existsInDb)
                {
                    await _dbLock.WaitAsync();
                    try
                    {
                        existsInDb = await _context.Files.AnyAsync(f => f.RelativePath == relPath);
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
                    finally
                    {
                        _dbLock.Release();
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string relativePath)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.RelativePath == relativePath);
    }
}