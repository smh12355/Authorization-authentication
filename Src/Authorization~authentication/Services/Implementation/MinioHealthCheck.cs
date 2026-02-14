using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Authorization_authentication.Services.Implementation;

public class MinioHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<MinioHealthCheck> _logger;

    public MinioHealthCheck(IAmazonS3 s3Client, ILogger<MinioHealthCheck> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Простая проверка доступности - список buckets
            await _s3Client.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy("MinIO is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO health check failed");
            return HealthCheckResult.Unhealthy(
                "MinIO is not accessible",
                ex);
        }
    }
}
