using Microsoft.AspNetCore.Mvc;
using CSaN_Lab3_Backend.Services;

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

    [HttpGet("list")]
    public IActionResult GetFiles()
    {
        try
        {
            var files = _service.GetAllFiles();
            return Ok(files);                    
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении списка: {ex.Message}");
        }
    }

    [HttpGet("{*path}")]                     
    public async Task<IActionResult> GetFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            var stream = await _service.GetFileStreamAsync(path);

            var contentType = GetContentType(path);

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

    [HttpPut("{*path}")]
    public async Task<IActionResult> PutFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            await _service.SaveFileAsync(path, Request.Body);

            return Ok($"Файл '{path}' успешно сохранён");
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

    [HttpPost("{*path}")]
    public async Task<IActionResult> AppendToFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            await _service.AppendToFileAsync(path, Request.Body);

            return Ok($"Данные успешно добавлены в файл '{path}'");
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

    [HttpDelete("{*path}")]
    public IActionResult DeleteFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Путь к файлу не указан");

            _service.DeleteFile(path);

            return Ok($"Файл '{path}' удалён");
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
            return StatusCode(500, $"Ошибка при удалении: {ex.Message}");
        }
    }



    private string GetContentType(string fileName)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return contentType;
    }
}