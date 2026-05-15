using Microsoft.AspNetCore.Mvc;
using CSaN_Lab3_Backend.Services;
using CSaN_Lab3_Backend.Dtos;

namespace CSaN_Lab3_Backend.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly FileStorageService _service;

    public FileController(FileStorageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        try
        {
            var files = await _service.GetAllFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении списка: {ex.Message}");
        }
    }

    [HttpGet("content")]
    public async Task<IActionResult> GetFileContent([FromQuery] string path, [FromQuery] string? mode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            var stream = _service.GetFileStream(path);
            var contentType = GetContentType(path);

            if (mode == "open")
                return File(stream, contentType);
            else
                return File(stream, contentType, Path.GetFileName(path));
        }
        catch (FileNotFoundException)
        {
            return NotFound($"Файл '{path}' не найден");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при чтении файла: {ex.Message}");
        }
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetFileMetadata([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            var metadata = await _service.GetFileMetadataAsync(path);
            if (metadata == null)
                return NotFound($"Файл '{path}' не найден в базе данных");

            var dto = new FileMetadataDto
            {
                RelativePath = metadata.RelativePath,
                FileName = metadata.FileName,
                Size = metadata.Size,
                ContentType = metadata.ContentType,
                CreatedAt = metadata.CreatedAt,
                ModifiedAt = metadata.ModifiedAt
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка получения метаданных: {ex.Message}");
        }
    }

    [HttpPut]
    public async Task<IActionResult> PutFile([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            await _service.SaveFileAsync(path, Request.Body);
            return Ok($"Файл '{path}' успешно сохранён");
        }
        catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("занят"))
        {
            return Conflict($"Файл '{path}' занят другим пользователем. Попробуйте позже.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при сохранении файла: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AppendToFile([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            await _service.AppendToFileAsync(path, Request.Body);
            return Ok($"Данные успешно добавлены в файл '{path}'");
        }
        catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("занят"))
        {
            return Conflict($"Файл '{path}' занят другим пользователем. Попробуйте позже.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при записи в файл: {ex.Message}");
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            await _service.DeleteFileAsync(path);
            return Ok($"Файл '{path}' удалён");
        }
        catch (FileNotFoundException)
        {
            return NotFound($"Файл '{path}' не найден");
        }
        catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("занят"))
        {
            return Conflict($"Файл '{path}' занят другим пользователем. Попробуйте позже.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при удалении: {ex.Message}");
        }
    }

    [AcceptVerbs("COPY")]
    [Route("copy")]
    public async Task<IActionResult> CopyFile([FromBody] FileTransferRequestDto request)
    {
        try
        {
            await _service.CopyFileAsync(request.SourcePath, request.DestinationPath);
            return Ok("Файл успешно скопирован");
        }
        catch (FileNotFoundException)
        {
            return NotFound("Исходный файл не найден");
        }
        catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("занят"))
        {
            return Conflict("Файл занят другим пользователем. Попробуйте позже.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка копирования: {ex.Message}");
        }
    }

    [AcceptVerbs("MOVE")]
    [Route("move")]
    public async Task<IActionResult> MoveFile([FromBody] FileTransferRequestDto request)
    {
        try
        {
            await _service.MoveFileAsync(request.SourcePath, request.DestinationPath);
            return Ok("Файл успешно перемещён");
        }
        catch (FileNotFoundException)
        {
            return NotFound("Исходный файл не найден");
        }
        catch (IOException ex) when (ex.Message.Contains("being used") || ex.Message.Contains("занят"))
        {
            return Conflict("Файл занят другим пользователем. Попробуйте позже.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка перемещения: {ex.Message}");
        }
    }

    private string GetContentType(string fileName)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";
        return contentType;
    }
}