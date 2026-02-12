using Asp.Versioning;
using Authorization_authentication.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;

namespace Authorization_authentication.Extensions;

public static class ServiceCollectionExtensions
{
    // Instance extension для IServiceCollection
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKeycloakAuthentication(IConfiguration configuration)
        {
            var authOptions = configuration
                .GetSection(nameof(AuthOptions))
                .Get<AuthOptions>() ?? throw new InvalidOperationException(
                        "AuthOptions section is missing in appsettings.json");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authOptions.Issuer;
                    options.Audience = authOptions.ClientId;
                    options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = authOptions.ClientId,

                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidateIssuer = true,
                        ValidIssuer = authOptions.Issuer,

                        NameClaimType = "preferred_username",
                        RoleClaimType = ClaimTypes.Role,

                        ClockSkew = TimeSpan.Zero
                    };

                    //// Маппинг ролей из realm_access.roles в Claims
                    //options.Events = new JwtBearerEvents
                    //{
                    //    OnTokenValidated = context =>
                    //    {
                    //        MapKeycloakRolesToClaims(context);
                    //        return Task.CompletedTask;
                    //    }
                    //};
                });

            // Добавляем вложенность ролей через composition-root в keycloak
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("UserPolicy", policy =>
            //        policy.RequireAssertion(context =>
            //            context.User.IsInRole("User") ||
            //            context.User.IsInRole("Admin")
            //        ));
            //});
            services.AddAuthorization();


            return services;
        }
        public IServiceCollection AddOwnOpenApi()
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
#if false
        public IServiceCollection AddOwnOpenApi()
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
#endif
        public IServiceCollection AddOwnApiVersioning()
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }
    }
}
