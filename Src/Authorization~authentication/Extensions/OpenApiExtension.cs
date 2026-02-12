using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Authorization_authentication.Extensions;

public static class OpenApiExtension
{
    public static IServiceCollection AddOwnOpenApi(this IServiceCollection services)
    {
        return services.AddOpenApi("v1", options =>
        {
            // Один OpenAPI-документ для всех версий API.
            // Новые версии (v2, v3, ...) будут автоматически попадать в этот документ.
            options.ShouldInclude = description => true;

            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Authorization API",
                    Version = "multi-version"
                };

                document.Components ??= new OpenApiComponents();

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Введите JWT токен в формате: Bearer {token}"
                };

                // Добавление схемы безопасности в компоненты документа
                // В .NET 10 используется AddComponent вместо прямого назначения
                document.AddComponent("Bearer", bearerScheme);

                // Создание требования безопасности с использованием нового API
                var securityRequirement = new OpenApiSecurityRequirement
                {
                    // В .NET 10 используется OpenApiSecuritySchemeReference
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                };

                // Применение требования безопасности ко всем операциям
                foreach (var operation in document.Paths.Values
                    .Where(path => path.Operations is not null)
                    .SelectMany(path => path.Operations!))
                {
                    operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Value.Security.Add(securityRequirement);
                }
                
                return Task.CompletedTask;
            });
        });
    }
}
