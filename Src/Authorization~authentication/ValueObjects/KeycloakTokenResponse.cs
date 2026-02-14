namespace Authorization_authentication.ValueObjects;

public record KeycloakTokenResponse(
    string access_token,
    int expires_in,
    int refresh_expires_in,
    string refresh_token,
    string token_type,
    int not_before_policy,
    string session_state,
    string scope,
    string id_token);
