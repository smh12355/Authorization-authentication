// Features/FileUpload/FileUploadController.cs
using Asp.Versioning;
using Authorization_authentication.Common.Constants;
using Authorization_authentication.Features.FileUpload.Models;
using Authorization_authentication.Features.FileUpload.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using MimeDetective;

namespace Authorization_authentication.Features.FileUpload;

[ApiController]
[Route(WebConstants.ApiControllerRoute)]
[ApiVersion("1.0")]
public class FileUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<FileUploadController> _logger;

    // Максимальный размер файла (например, 10 GB)
    private const long MaxFileSize = 10L * 1024 * 1024 * 1024;

    // Максимальный размер файла (например, 10 GB)
    private const long MaxBufferedFileSize = 2000 * 1024 * 1024;

    // Максимальный размер файла (например, 10 GB)
    private const long MaxRawFileSize = 10L * 1024 * 1024 * 1024;

    public FileUploadController(
        IFileStorageService fileStorage,
        ILogger<FileUploadController> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <summary>
    /// Buffered upload, маленькие файлы в памяти
    /// </summary>
    [HttpPost("buffered-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBufferedFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBufferedFileSize)]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
                    IFormFile file,
                    [FromQuery] string? bucketName = null,
                    CancellationToken cancellationToken = default)
    {
        bucketName ??= "uploads";

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (file.Length > MaxBufferedFileSize)
            {
                return BadRequest($"File size exceeds maximum allowed size of {MaxBufferedFileSize / (1024 * 1024)} MB");
            }

            _logger.LogInformation(
                    "Receiving file: {FileName}, Size: {Size} bytes ({SizeMB} MB)",
                    file.FileName, file.Length, file.Length / (1024.0 * 1024.0));

            var typeInspector = new ContentInspectorBuilder()
            {
                Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
            }
            .Build();

            await using var stream = file.OpenReadStream();

            var result = typeInspector.Inspect(stream);
            var type = result.ByFileExtension().FirstOrDefault()?.Extension;
            var mime = result.ByMimeType().FirstOrDefault()?.MimeType;

            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking");

            stream.Seek(0, SeekOrigin.Begin);

            var objectName = $"{Guid.NewGuid()}.{type}";

            var filePath = await _fileStorage.UploadFileAsync(
                bucketName,
                objectName,
                stream,
                file.ContentType ?? "application/octet-stream",
                cancellationToken);

            return Ok(new FileUploadResponse
            {
                FileName = file.FileName,
                StoredFileName = objectName,
                FilePath = filePath,
                Size = file.Length,
                ContentType = mime,
                UploadedAt = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File upload was cancelled");
            return StatusCode(499, "Upload cancelled"); // Client Closed Request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file upload");
            return StatusCode(500, "Internal server error during upload");
        }
    }

    /// <summary>
    /// Прямая загрузка МАКСИМАЛЬНАЯ эффективность - но низкая безопасность
    /// </summary>
    [HttpPost("direct-upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadDirect(
                    [FromQuery] string fileName,
                    [FromQuery] string? bucketName = null,
                    [FromHeader(Name = "Content-Type")] string? contentType = null,
                    CancellationToken cancellationToken = default)
    {
        bucketName ??= "uploads";
        contentType ??= "application/octet-stream";

        try
        {
            var fileExtension = Path.GetExtension(fileName);
            var objectName = $"{Guid.NewGuid()}{fileExtension}";

            _logger.LogInformation(
                "Starting direct stream upload: {FileName}",
                fileName);

            // ⭐⭐⭐ Request.Body - это ПРЯМОЙ поток от клиента!
            // Клиент → TCP → Request.Body → MinIO
            // НОЛЬ промежуточных буферов!
            var filePath = await _fileStorage.UploadRawFileAsync(
                bucketName,
                objectName,
                Request.Body,
                contentType,
                cancellationToken);

            return Ok(new FileUploadResponse
            {
                FileName = fileName,
                StoredFileName = objectName,
                FilePath = filePath,
                Size = Request.ContentLength ?? 0,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during direct upload");
            return StatusCode(500, "Internal server error during upload");
        }
    }

    /// <summary>
    /// Multipart загрузка
    /// </summary>>
    [HttpPost("multipart-upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadTrueStream(
                        [FromQuery] string? bucketName = null,
                        CancellationToken cancellationToken = default)
    {
        bucketName ??= "uploads";

        if (!Request.HasFormContentType)
        {
            return BadRequest("Expected multipart/form-data request");
        }

        try
        {
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
            var reader = new MultipartReader(boundary, Request.Body);

            MultipartSection? section;

            // Читаем секции multipart по одной
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
            {
                var hasContentDisposition = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition,
                    out var contentDisposition);

                if (!hasContentDisposition || !contentDisposition.IsFileDisposition())
                {
                    continue; // Пропускаем не-файловые поля
                }

                var fileName = contentDisposition.FileName.Value?.Trim('"');
                var fileExtension = Path.GetExtension(fileName);
                var objectName = $"{Guid.NewGuid()}{fileExtension}";
                var contentType = section.ContentType ?? "application/octet-stream";

                _logger.LogInformation(
                    "Receiving file stream: {FileName}, Content-Type: {ContentType}",
                    fileName, contentType);

                // ⭐ КЛЮЧЕВОЙ МОМЕНТ: section.Body - это прямой stream от клиента!
                // Данные идут: Клиент → Request.Body → section.Body → MinIO
                // БЕЗ промежуточной буферизации!
                var filePath = await _fileStorage.UploadRawFileAsync(
                    bucketName,
                    objectName,
                    section.Body, // ⭐ Прямой поток!
                    contentType,
                    cancellationToken);

                return Ok(new FileUploadResponse
                {
                    FileName = fileName ?? "unknown",
                    StoredFileName = objectName,
                    FilePath = filePath,
                    Size = 0, // Размер неизвестен заранее при streaming
                    ContentType = contentType,
                    UploadedAt = DateTime.UtcNow
                });
            }

            return BadRequest("No file found in request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during true streaming upload");
            return StatusCode(500, "Internal server error during upload");
        }
    }

    private static string GetBoundary(MediaTypeHeaderValue contentType)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary");
        }

        return boundary;
    }
}
