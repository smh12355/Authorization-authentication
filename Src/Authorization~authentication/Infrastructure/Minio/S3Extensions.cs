using Amazon.Runtime;
using Amazon.S3;
using Authorization_authentication.Common.Options;
using Microsoft.Extensions.Options;

namespace Authorization_authentication.Infrastructure.Minio;

public static class S3Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMinIO(IConfiguration configuration)
        {
            services.AddOptions<MinioOptions>()
                .Bind(configuration.GetSection(MinioOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

                var config = new AmazonS3Config
                {
                    ServiceURL = options.Endpoint,
                    ForcePathStyle = true,
                    UseHttp = !options.UseSsl,
                };

                if (!string.IsNullOrEmpty(options.Region))
                {
                    config.AuthenticationRegion = options.Region;
                }

                var credentials = new BasicAWSCredentials(
                    options.AccessKey,
                    options.SecretKey);

                return new AmazonS3Client(credentials, config);
            });

            services.AddHealthChecks()
                .AddCheck<MinioHealthCheck>("minio", tags: new[] { "storage", "minio" });

            services.AddHostedService<MinioInitializer>();

            return services;
        }
    }
}
