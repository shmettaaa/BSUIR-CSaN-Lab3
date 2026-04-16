using Microsoft.AspNetCore.Mvc;
using CSaN_Lab3_Backend.Services;

namespace CSaN_Lab3_Backend.Controllers;

[ApiController]
[Route("api/files/{*path}")]
public class FileController : ControllerBase
{
    private readonly FileStorageService _service;

    public FileController(FileStorageService service)
    {
        _service = service;
    }

    [HttpGet]
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
        catch (Exception)
        {
            return StatusCode(500, "Ошибка при чтении файла");
        }
    }

    [HttpPut]
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
        catch (Exception)
        {
            return StatusCode(500, "Ошибка при сохранении файла");
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


    [HttpPost]
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
        catch (Exception)
        {
            return StatusCode(500, "Ошибка при записи в файл");
        }
    }


}