namespace Authorization_authentication.Infrastructure.Database.Entities;

/// <summary>
/// Represents a role entity in the database.
/// </summary>
public class Role
{
    /// <summary>
    /// Primary key for the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role name (e.g., Admin, User, Manager).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created this role.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for user roles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
