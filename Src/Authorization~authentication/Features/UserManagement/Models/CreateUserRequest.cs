using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Features.UserManagement.Models;

/// <summary>
/// Request model for creating a new user.
/// </summary>
public record CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    public List<Guid> RoleIds { get; init; } = new();
}
