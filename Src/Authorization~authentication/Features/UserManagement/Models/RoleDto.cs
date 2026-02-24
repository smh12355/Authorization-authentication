namespace Authorization_authentication.Features.UserManagement.Models;

/// <summary>
/// Data transfer object for role information.
/// </summary>
public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
