using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Options;

public record MinioOptions
{
    public const string SectionName = "MinIO";

    [Required(ErrorMessage = "MinIO endpoint is required")]
    [Url(ErrorMessage = "Endpoint must be a valid URL")]
    public required string Endpoint { get; init; }

    [Required(ErrorMessage = "MinIO AccessKey is required")]
    public required string AccessKey { get; init; }

    [Required(ErrorMessage = "MinIO SecretKey is required")]
    public required string SecretKey { get; init; }

    [Required(ErrorMessage = "MinIO BucketName is required")]
    public required string BucketName { get; init; }

    public bool UseSsl { get; init; } = false;

    public string? Region { get; init; }
}
