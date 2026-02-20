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
    private readonly IContentInspector _contentInspector;

    // Максимальный размер файла (например, 10 GB)
    private const long MaxFileSize = 10L * 1024 * 1024 * 1024;

    // Максимальный размер файла (например, 500 mb)
    private const long MaxBufferedFileSize = 500 * 1024 * 1024;

    // Максимальный размер файла (например, 10 GB)
    private const long MaxRawFileSize = 10L * 1024 * 1024 * 1024;

    public FileUploadController(
        IFileStorageService fileStorage,
        ILogger<FileUploadController> logger,
        IContentInspector contentInspector)
    {
        _fileStorage = fileStorage;
        _logger = logger;
        _contentInspector = contentInspector;
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
    public async Task<IActionResult> BufferedUploadFile(
                    IFormFile file,
                    CancellationToken cancellationToken = default)
    {
        var prepared = PrepareFile(file);

        if (prepared.Error is not null)
            return prepared.Error;

        await using var stream = prepared.Stream;

        var filePath = await _fileStorage.UploadRawFileAsync(
            prepared.ObjectName,
            stream,
            prepared.Mime,
            cancellationToken);

        return Ok(FileUploadResponse.Create(file.FileName, prepared, filePath, file.Length));
    }

    /// <summary>
    /// Buffered upload с RAM-сжатием
    /// </summary>
    [HttpPost("buffered-ram-compressed-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBufferedFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBufferedFileSize)]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BufferedRamCompressedUploadFile(
                    IFormFile file,
                    CancellationToken cancellationToken = default)
    {
        var prepared = PrepareFile(file);
        if (prepared.Error is not null)
            return prepared.Error;

        await using var stream = prepared.Stream;

        var filePath = await _fileStorage.UploadRamCompressedFileAsync(
            prepared.ObjectName,
            stream,
            prepared.Mime,
            cancellationToken);

        return Ok(FileUploadResponse.Create(file.FileName, prepared, filePath, file.Length));
    }

    /// <summary>
    /// Buffered upload с дисковым сжатием
    /// </summary>
    [HttpPost("buffered-compressed-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBufferedFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBufferedFileSize)]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BufferedCompressedUploadFile(
                    IFormFile file,
                    CancellationToken cancellationToken = default)
    {
        var prepared = PrepareFile(file);
        if (prepared.Error is not null)
            return prepared.Error;

        await using var stream = prepared.Stream;

        var filePath = await _fileStorage.UploadCompressedFileAsync(
            prepared.ObjectName,
            stream,
            prepared.Mime,
            cancellationToken);

        return Ok(FileUploadResponse.Create(file.FileName, prepared, filePath, file.Length));
    }

    private PreparedFile PrepareFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return PreparedFile.Fail(BadRequest("No file uploaded"));

        _logger.LogInformation(
            "Receiving file: {FileName}, Size: {Size} bytes ({SizeMB:F2} MB)",
            file.FileName, file.Length, file.Length / (1024.0 * 1024.0));

        var stream = file.OpenReadStream();

        if (!stream.CanSeek)
        {
            stream.Dispose();
            throw new NotSupportedException($"Stream for '{file.FileName}' does not support seeking");
        }

        var inspectResult = _contentInspector.Inspect(stream);
        var extension = inspectResult.ByFileExtension().FirstOrDefault()?.Extension ?? "bin";
        var mime = inspectResult.ByMimeType().FirstOrDefault()?.MimeType ?? "application/octet-stream";

        stream.Seek(0, SeekOrigin.Begin);

        var objectName = $"{Guid.NewGuid()}.{extension}";

        return new PreparedFile(stream, mime, objectName);
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
