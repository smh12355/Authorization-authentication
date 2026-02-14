using Amazon.S3;
using Amazon.S3.Model;
using Authorization_authentication.Options;
using Microsoft.Extensions.Options;

namespace Authorization_authentication.HostedServices;

public class MinioInitializer : IHostedService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<MinioOptions> _options;
    private readonly ILogger<MinioInitializer> _logger;

    public MinioInitializer(
        IAmazonS3 s3Client,
        IOptions<MinioOptions> options,
        ILogger<MinioInitializer> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _options = options ?? throw new ArgumentNullException(nameof(options)); ;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucketName = _options.Value.BucketName;

        var bucketsToCreate = new[] { bucketName };

        foreach (var bucket in bucketsToCreate)
        {
            await EnsureBucketExistsAsync(bucket, cancellationToken);
        }
    }

    private async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var exists = await Amazon.S3.Util.AmazonS3Util
                    .DoesS3BucketExistV2Async(_s3Client, bucketName);

                if (!exists)
                {
                    _logger.LogInformation(
                        "Creating bucket '{BucketName}' (attempt {Attempt}/{MaxRetries})",
                        bucketName, attempt, maxRetries);

                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    }, cancellationToken);

                    _logger.LogInformation(
                        "Bucket '{BucketName}' created successfully",
                        bucketName);
                }
                else
                {
                    _logger.LogInformation(
                        "Bucket '{BucketName}' already exists",
                        bucketName);
                }

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Failed to initialize bucket '{BucketName}' (attempt {Attempt}/{MaxRetries}). Retrying...",
                    bucketName, attempt, maxRetries);

                await Task.Delay(delay * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to initialize bucket '{BucketName}' after {MaxRetries} attempts",
                    bucketName, maxRetries);

                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
