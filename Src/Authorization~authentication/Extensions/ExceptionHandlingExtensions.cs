using Authorization_authentication.Middleware;

namespace Authorization_authentication.Extensions;

public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds global exception handling middleware. Must be registered early in the pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
