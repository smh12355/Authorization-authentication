using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.ValueObjects;

public record PasswordLoginRequest(
    [Required] string Username,
    [Required] string Password);
