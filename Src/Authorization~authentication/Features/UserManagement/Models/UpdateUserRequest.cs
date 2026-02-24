using System.ComponentModel.DataAnnotations;

namespace Authorization_authentication.Features.UserManagement.Models;

/// <summary>
/// Request model for updating an existing user.
/// </summary>
public record UpdateUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
