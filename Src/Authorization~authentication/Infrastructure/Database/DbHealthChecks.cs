using Authorization_authentication.Infrastructure.Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Authorization_authentication.Infrastructure.Database;

public class DbHealthChecks : IHealthCheck
{
    private readonly AppDbContext _db;
    private readonly ILogger<DbHealthChecks> _logger;

    public DbHealthChecks(AppDbContext db, ILogger<DbHealthChecks> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("MainDb is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MainDb health check failed");
            return HealthCheckResult.Unhealthy(
                "MainDb is not accessible",
                ex);
        }
    }
}
