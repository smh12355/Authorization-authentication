using Authorization_authentication.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace Authorization_authentication.Extensions;

public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
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

    private static void MapKeycloakRolesToClaims(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity claimsIdentity)
            return;

        // Извлекаем роли из realm_access.roles
        var realmAccessClaim = context.Principal.FindFirst("realm_access");
        if (realmAccessClaim == null)
            return;

        var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
        if (!realmAccess.RootElement.TryGetProperty("roles", out var roles))
            return;

        foreach (var role in roles.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (!string.IsNullOrEmpty(roleValue))
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
            }
        }
    }
}
