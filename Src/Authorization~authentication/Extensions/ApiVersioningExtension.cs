using Asp.Versioning;

namespace Authorization_authentication.Extensions;

public static class ApiVersioningExtension
{
    public static IServiceCollection AddOwnApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                // версия по умолчанию
                options.DefaultApiVersion = new ApiVersion(1, 0);
                // если версия не указана, использовать версию по умолчанию
                options.AssumeDefaultVersionWhenUnspecified = true;
                // добавлять заголовки api-supported-versions / api-deprecated-versions
                options.ReportApiVersions = true;
                // читаем версию из URL-сегмента: /api/v{version}/...
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // v1, v1.1, v2 и т.п.
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
