using Authorization_authentication.Extensions;
using Authorization_authentication.Features.FileUpload.Services;
using Authorization_authentication.Infrastructure.Keycloak;
using Authorization_authentication.Infrastructure.Minio;
using Authorization_authentication.Infrastructure.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddMinIO(builder.Configuration);

builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddOwnApiVersioning();
builder.Services.AddOwnOpenApi();

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
