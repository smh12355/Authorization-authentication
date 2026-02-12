using Authorization_authentication.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddControllers();

// Версионирование API
builder.Services.AddOwnApiVersioning();

// OpenAPI/Swagger с Bearer-аутентификацией
builder.Services.AddOwnOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseAuthentication(); // Сначала аутентификация
app.UseAuthorization();  // Потом авторизация
app.UseHttpsRedirection();

app.MapControllers();

app.Run();

