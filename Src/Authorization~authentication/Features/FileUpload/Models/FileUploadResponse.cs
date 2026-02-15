// Features/FileUpload/Models/FileUploadResponse.cs
namespace Authorization_authentication.Features.FileUpload.Models;

public record FileUploadResponse
{
    public string FileName { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long Size { get; init; }
    public string? ContentType { get; init; }
    public DateTime UploadedAt { get; init; }
}
