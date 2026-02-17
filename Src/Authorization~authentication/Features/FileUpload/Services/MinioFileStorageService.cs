using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Authorization_authentication.Common.Options;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace Authorization_authentication.Features.FileUpload.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly string _bucketName;

    public MinioFileStorageService(
        IAmazonS3 s3Client,
        ILogger<MinioFileStorageService> logger,
        IOptions<MinioOptions> options)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = options.Value.BucketName
            ?? throw new InvalidOperationException("MinIO BucketName is not configured.");
    }

    public async Task<string> UploadFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting streaming upload: {ObjectName} to bucket {BucketName}. Content-Type: {ContentType}",
            objectName, _bucketName, contentType);

        // КЛЮЧЕВОЙ МОМЕНТ: stream передается напрямую без загрузки в память
        // MinIO SDK сам читает по частям (chunked upload)
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectName,
            InputStream = stream,          // Это и есть streaming!
            ContentType = contentType,
            AutoCloseStream = false        // Мы сами закроем stream
        };

        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        _logger.LogInformation(
            "Successfully uploaded {ObjectName}. ETag: {ETag}, Size: {Size} bytes",
            objectName, response.ETag, response.Size);

        // Возвращаем путь к файлу
        return $"{_bucketName}/{objectName}";
    }

    public async Task<string> UploadGzipedFileAsync(string objectName, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> UploadFileCompressedAsync(
        string bucketName,
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting streaming upload: {ObjectName})",
                objectName);

            Stream uploadStream = stream;

            // Создаем временный поток для сжатых данных
            var compressedStream = new MemoryStream(); // Буфер для сжатых данных

            // Создаем сжимающий поток
            await using (var gzipStream = new GZipStream(
                compressedStream,
                CompressionLevel.Optimal,
                leaveOpen: true)) // ⭐ Оставляем compressedStream открытым
            {
                // Копируем данные через GZip
                await stream.CopyToAsync(gzipStream, 81920, cancellationToken);
                // 81920 = 80 KB буфер для копирования
            }

            // Возвращаемся в начало сжатого потока
            compressedStream.Position = 0;
            uploadStream = compressedStream;

            // Добавляем .gz к имени файла
            objectName = $"{objectName}.gz";

            _logger.LogInformation(
                "Compressed: {OriginalSize} → {CompressedSize} bytes",
                stream.Length, compressedStream.Length);

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                InputStream = uploadStream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            putRequest.Headers["Content-Encoding"] = "gzip";
            putRequest.Metadata["x-amz-meta-original-name"] = objectName.Replace(".gz", "");

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded {ObjectName}. ETag: {ETag}",
                objectName, response.ETag);

            return $"{bucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectName}", objectName);
            throw;
        }
    }

    public async Task<string> UploadRawFileAsync(
        string bucketName,
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting streaming upload: {ObjectName} to bucket {BucketName}",
                objectName, bucketName);


            // Для истинного streaming используем TransferUtility
            // Он НЕ требует знать размер заранее!
            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucketName,
                Key = objectName,
                InputStream = stream,
                ContentType = contentType,
                PartSize = 10 * 1024 * 1024, // 10 MB части
                AutoCloseStream = false
            };

            // Отслеживание прогресса (опционально)
            uploadRequest.UploadProgressEvent += (sender, args) =>
            {
                if (args.TotalBytes > 0)
                {
                    var percent = (args.TransferredBytes * 100.0) / args.TotalBytes;
                    _logger.LogDebug("Upload progress: {Percent:F2}%", percent);
                }
            };

            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            _logger.LogInformation("Successfully uploaded {ObjectName}", objectName);

            return $"{bucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {ObjectName}", objectName);
            throw;
        }
    }
}
