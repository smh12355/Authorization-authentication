using Asp.Versioning;
using Authorization_authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Версионирование API
builder.Services
    .AddApiVersioning(options =>
    {
        // версия по умолчанию
        options.DefaultApiVersion = new ApiVersion(1, 0);
        // если версия не указана, использовать версию по умолчанию
        options.AssumeDefaultVersionWhenUnspecified = true;
        // добавлять заголовки api-supported-versions / api-deprecated-versions
        options.ReportApiVersions = true;
        // читаем версию из URL-сегмента: /api/v{version}/...
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    // Для Swagger / ApiExplorer
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV"; // v1, v1.1, v2 и т.п.
        options.SubstituteApiVersionInUrl = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // Сначала аутентификация
app.UseAuthorization();  // Потом авторизация
app.UseHttpsRedirection();

app.MapControllers();

app.Run();

