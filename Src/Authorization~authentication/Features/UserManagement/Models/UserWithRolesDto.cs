namespace Authorization_authentication.Features.UserManagement.Models;

/// <summary>
/// Data transfer object for user with roles information.
/// </summary>
public record UserWithRolesDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<RoleDto> Roles { get; init; } = new();
}
