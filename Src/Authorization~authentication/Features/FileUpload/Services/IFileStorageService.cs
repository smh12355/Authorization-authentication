namespace Authorization_authentication.Features.FileUpload.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Загружает файл в хранилище используя streaming и оптимизарованно используя оперативку
    /// </summary>
    /// <param name="objectName">Имя файла в хранилище</param>
    /// <param name="stream">Поток данных файла</param>
    /// <param name="contentType">MIME тип файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL загруженного файла или идентификатор</returns>
    Task<string> UploadRawFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл в хранилище используя streaming и сжимая данные, но весь файл в оперативке
    /// </summary>
    /// <param name="objectName">Имя файла в хранилище</param>
    /// <param name="stream">Поток данных файла</param>
    /// <param name="contentType">MIME тип файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL загруженного файла или идентификатор</returns>
    Task<string> UploadRamCompressedFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл в хранилище используя streaming и сжимая данные, оптимизарованно используя оперативку
    /// </summary>
    /// <param name="objectName">Имя файла в хранилище</param>
    /// <param name="stream">Поток данных файла</param>
    /// <param name="contentType">MIME тип файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL загруженного файла или идентификатор</returns>
    Task<string> UploadCompressedFileAsync(
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл в хранилище используя streaming
    /// </summary>
    /// <param name="bucketName">Имя bucket</param>
    /// <param name="objectName">Имя файла в хранилище</param>
    /// <param name="stream">Поток данных файла</param>
    /// <param name="contentType">MIME тип файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL загруженного файла или идентификатор</returns>
    Task<string> UploadRawFileAsync(
        string bucketName,
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл в хранилище используя streaming
    /// </summary>
    /// <param name="bucketName">Имя bucket</param>
    /// <param name="objectName">Имя файла в хранилище</param>
    /// <param name="stream">Поток данных файла</param>
    /// <param name="contentType">MIME тип файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL загруженного файла или идентификатор</returns>
    Task<string> UploadFileCompressedAsync(
        string bucketName,
        string objectName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);
}
