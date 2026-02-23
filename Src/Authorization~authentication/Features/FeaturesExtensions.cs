using Authorization_authentication.Features.FileUpload.Services;

namespace Authorization_authentication.Features;

public static class FeaturesExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterFeaturesServices()
        {
            services.AddScoped<IFileStorageService, MinioFileStorageService>();
            return services;
        }
    }

}
