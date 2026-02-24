namespace Authorization_authentication.Infrastructure.Database.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Users and Roles.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Foreign key to User.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Foreign key to Role.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation property to Role.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Timestamp when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// User who assigned this role.
    /// </summary>
    public string AssignedBy { get; set; } = string.Empty;
}
