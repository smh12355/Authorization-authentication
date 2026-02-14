using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Features.Auth.Models;

public record PasswordLoginRequest(
    [Required] string Username,
    [Required] string Password);
