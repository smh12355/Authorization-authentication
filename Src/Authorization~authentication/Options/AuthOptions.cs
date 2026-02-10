using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Options;

public record AuthOptions
{
    [Required(ErrorMessage = "Issuer is required")]
    [Url(ErrorMessage = "Issuer must be a valid URL")]
    public required string Issuer { get; init; }

    [Required(ErrorMessage = "ClientId is required")]
    public required string ClientId { get; init; }

    public bool RequireHttpsMetadata { get; init; }
}
