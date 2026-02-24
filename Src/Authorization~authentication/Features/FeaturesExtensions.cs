using Authorization_authentication.Features.FileUpload.Services;
using Authorization_authentication.Features.UserManagement.Services;

namespace Authorization_authentication.Features;

public static class FeaturesExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterFeaturesServices()
        {
            services.AddScoped<IFileStorageService, MinioFileStorageService>();

            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }

}
