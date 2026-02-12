using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Options;

public record AuthOptions
{
    [Required(ErrorMessage = "Issuer is required")]
    [Url(ErrorMessage = "Issuer must be a valid URL")]
    public required string Issuer { get; init; }

    [Required(ErrorMessage = "ClientId is required")]
    public required string ClientId { get; init; }

    [Required(ErrorMessage = "RealmClientId is required")]
    public required string RealmClientId { get; init; }

    public bool RequireHttpsMetadata { get; init; }

    [Required(ErrorMessage = "ClientSecret is required for confidential client")]
    public required string ClientSecret { get; init; }
}
