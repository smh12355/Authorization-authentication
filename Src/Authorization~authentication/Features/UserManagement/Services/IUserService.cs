using Authorization_authentication.Features.UserManagement.Models;

namespace Authorization_authentication.Features.UserManagement.Services;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user with optional roles.
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by ID without roles.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by ID with roles (eager loading demonstration).
    /// </summary>
    Task<UserWithRolesDto?> GetUserWithRolesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by ID with explicit loading demonstration.
    /// </summary>
    Task<UserWithRolesDto?> GetUserWithExplicitLoadingAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets paginated and filtered list of users.
    /// </summary>
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilterRequest filter, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, string updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task AssignRoleAsync(Guid userId, Guid roleId, string assignedBy, CancellationToken ct = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a user.
    /// </summary>
    Task SoftDeleteUserAsync(Guid id, string deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Demonstrates N+1 problem (BAD approach - multiple queries).
    /// </summary>
    Task<List<UserWithRolesDto>> GetUsersWithRoles_NPlusOne_BAD(CancellationToken ct = default);

    /// <summary>
    /// Demonstrates optimized approach (GOOD - single query with Include).
    /// </summary>
    Task<List<UserWithRolesDto>> GetUsersWithRoles_Optimized_GOOD(CancellationToken ct = default);
}
