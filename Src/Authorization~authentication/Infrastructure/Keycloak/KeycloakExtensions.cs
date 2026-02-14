using Authorization_authentication.Common.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Authorization_authentication.Infrastructure.Keycloak;

public static class KeycloakExtensions
{
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
                });

            services.AddAuthorization();

            return services;
        }
    }
}
