using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Authorization_authentication.Common.Options;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Mime;

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

    public async Task<string> UploadRawFileAsync(
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

    public async Task<string> UploadRamCompressedFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting streaming upload: {ObjectName} to bucket {BucketName}. Content-Type: {ContentType}",
            objectName, _bucketName, contentType);

        // Сжимаем данные и флашим буфер через Dispose
        await using var compressedStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(
            compressedStream,
            CompressionLevel.Optimal,
            leaveOpen: true))
        {
            await stream.CopyToAsync(gzipStream, cancellationToken);
            await gzipStream.FlushAsync(cancellationToken);
        } // gzipStream.Dispose() вызывает Flush() - записывает остаток данных

        // Сбрасываем позицию для чтения с начала
        if (compressedStream.CanSeek)
            compressedStream.Seek(0, SeekOrigin.Begin);

        // Загрузка в MinIO
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"{objectName}.gz",
            InputStream = compressedStream,
            ContentType = "application/gzip",
            AutoCloseStream = false,
            Metadata =
            {
                ["x-original-content-type"] = contentType
            },
            Headers =
            {
                ["Content-Encoding"] = "gzip"
            }
        };

        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        _logger.LogInformation(
            "Successfully uploaded {ObjectName}. ETag: {ETag}, CompressedSize: {Size} bytes",
            objectName, response.ETag, response.Size);

        return $"{_bucketName}/{objectName}.gz";
    }

    public async Task<string> UploadCompressedFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting streaming upload: {ObjectName} to bucket {BucketName}. Content-Type: {ContentType}",
            objectName, _bucketName, contentType);

        var pipe = new Pipe();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Task 1: Читаем из source и пишем сжатое в pipe
        var compressionTask = Task.Run(async () =>
        {
            Exception? error = null;
            try
            {
                await using var pipeStream = pipe.Writer.AsStream();
                await using var gzipStream = new GZipStream(
                    pipeStream,
                    CompressionLevel.Optimal);
                await stream.CopyToAsync(gzipStream, cts.Token);
                await gzipStream.FlushAsync(cts.Token);
            }
            catch (Exception ex)
            {
                error = ex;
                throw;
            }
            finally
            {
                await pipe.Writer.CompleteAsync(error);
            }

        }, cts.Token);

        var uploadTask = Task.Run(async () =>
        {
            Exception? error = null;
            try
            {
                // await using автоматически вызовет CompleteAsync при Dispose!
                await using var pipeStream = pipe.Reader.AsStream();

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"{objectName}.gz",
                    InputStream = pipeStream,
                    ContentType = "application/gzip",
                    AutoCloseStream = false,

                    Metadata =
                {
                    ["x-original-content-type"] = contentType
                },
                    Headers =
                {
                    ["Content-Encoding"] = "gzip"
                },
                };

                return await _s3Client.PutObjectAsync(putRequest, cts.Token);
            }
            catch(Exception ex) 
            {
                error = ex;
                throw;
            }
            finally
            {
                await pipe.Reader.CompleteAsync(error);
            }

        }, cts.Token);

        try
        {
            await Task.WhenAll(compressionTask, uploadTask);
        }
        catch (Exception)
        {
            cts.Cancel();

            await Task.WhenAll(
                compressionTask.ContinueWith(_ => { }),
                uploadTask.ContinueWith(_ => { }));
            throw;
        }

        var response = uploadTask.Result;

        _logger.LogInformation(
            "Successfully uploaded {ObjectName}. ETag: {ETag}, CompressedSize: {Size} bytes",
            objectName, response.ETag, response.Size);

        return $"{_bucketName}/{objectName}.gz";
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
