using Microsoft.OpenApi;

namespace Authorization_authentication.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOwnOpenApi()
        {
            return services.AddOpenApi("v1", options =>
            {
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

                    document.AddComponent("Bearer", bearerScheme);

                    var securityRequirement = new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                    };

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
}
