using Asp.Versioning;
using Authorization_authentication.Common.Constants;
using Authorization_authentication.Common.Options;
using Authorization_authentication.Features.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Authorization_authentication.Features.IdToken;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route(WebConstants.ApiControllerRoute)]
public class IdTokenController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public IdTokenController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    ///     Получение ID Token из Keycloak по логину и паролю (OIDC, grant_type=password).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<KeycloakTokenResponse>> GetIdToken(
        [FromBody] PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var options = _configuration
            .GetSection(nameof(AuthOptions))
            .Get<AuthOptions>() ?? throw new InvalidOperationException(
                    "AuthOptions section is missing in appsettings.json");

        var issuer = options.Issuer.TrimEnd('/');
        var tokenEndpoint = $"{issuer}/protocol/openid-connect/token";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = options.ClientId,
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["scope"] = "openid"
        };

        if (!string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            form["client_secret"] = options.ClientSecret!;
        }

        var client = _httpClientFactory.CreateClient();

        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(tokenEndpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, responseBody);
        }

        try
        {
            var result = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseBody);
            return Ok(result);
        }
        catch
        {
            return Ok(responseBody);
        }
    }
}
