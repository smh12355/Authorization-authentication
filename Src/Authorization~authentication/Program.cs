using Authorization_authentication.Extensions;
using Authorization_authentication.Features;
using Authorization_authentication.Features.FileUpload.Services;
using Authorization_authentication.Infrastructure.Database;
using Authorization_authentication.Infrastructure.Keycloak;
using Authorization_authentication.Infrastructure.Minio;
using Authorization_authentication.Infrastructure.OpenApi;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Аутенфикация/авторизация
builder.Services.AddKeycloakAuthentication(builder.Configuration);
// S3 хранилище
builder.Services.AddMinIO(builder.Configuration);
// Database
builder.Services.AddDbContext(builder.Configuration);
// Регистрация сервисов каталога Features
builder.Services.RegisterFeaturesServices();
// Версенирование апи
builder.Services.AddOwnApiVersioning();
// Мапинг схемы опенапи в свагер ui
builder.Services.AddOwnOpenApi();

// HttpClient фабрика
builder.Services.AddHttpClient();
builder.Services.AddControllers();

var app = builder.Build();

app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .CacheOutput();
    //app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Authorization API (all versions)");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
