using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

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
                document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Введите JWT токен в формате: Bearer {token}"
                    }
                };

                document.SecurityRequirements = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    }
                };

                return Task.CompletedTask;
            });
        });
    }
}
