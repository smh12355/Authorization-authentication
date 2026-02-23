using Authorization_authentication.Common.Options;
using Authorization_authentication.Infrastructure.Minio;
using Microsoft.EntityFrameworkCore;

namespace Authorization_authentication.Infrastructure.Database;

public static class DbExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MainDb")
                ?? throw new ArgumentNullException("MainDb section is missing in appsettings.json, cannot connect to database");

            var isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString);

                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging(); // показывает значения параметров в логах
                    options.EnableDetailedErrors();
                }
            });

            services.AddHealthChecks()
                .AddCheck<DbHealthChecks>("MainDb", tags: new[] { "MainDb", "postgres" });

            return services;
        }
    }
}
