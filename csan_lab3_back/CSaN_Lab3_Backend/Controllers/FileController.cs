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

    [HttpGet()]
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

    [HttpGet("open/{*path}")]
    public async Task<IActionResult> OpenFile(string path)
    {
        try
        {
            var stream = await _service.GetFileStreamAsync(path);
            var contentType = GetContentType(path);

            return File(stream, contentType); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
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

    [AcceptVerbs("COPY")]
    [Route("copy")]
    public IActionResult CopyFile([FromBody] FileTransferRequestDto request)
    {
        try
        {
            _service.CopyFile(request.SourcePath, request.DestinationPath);
            return Ok("Файл успешно скопирован");
        }
        catch (FileNotFoundException)
        {
            return NotFound("Исходный файл не найден");
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
    public IActionResult MoveFile([FromBody] FileTransferRequestDto request)
    {
        try
        {
            _service.MoveFile(request.SourcePath, request.DestinationPath);
            return Ok("Файл успешно перемещён");
        }
        catch (FileNotFoundException)
        {
            return NotFound("Исходный файл не найден");
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
        {
            contentType = "application/octet-stream";
        }

        return contentType;
    }
}