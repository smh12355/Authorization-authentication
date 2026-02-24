using Authorization_authentication.Features.UserManagement.Exceptions;
using Authorization_authentication.Features.UserManagement.Models;
using Authorization_authentication.Infrastructure.Database;
using Authorization_authentication.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authorization_authentication.Features.UserManagement.Services;

/// <summary>
/// Service implementation for user management using direct EF Core DbContext.
/// Demonstrates various EF Core patterns and scenarios.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdBy, CancellationToken ct = default)
    {
        // Check if username already exists
        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == request.Username, ct);

        if (usernameExists)
            throw new ValidationException($"Username '{request.Username}' already exists");

        // Check if email already exists
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailExists)
            throw new ValidationException($"Email '{request.Email}' already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Users.Add(user);

        // Assign roles if provided
        if (request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId, ct);
                if (!roleExists)
                    throw new NotFoundException($"Role {roleId} not found");

                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = createdBy
                };

                _context.UserRoles.Add(userRole);
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {Username} created by {CreatedBy}", user.Username, createdBy);

        return MapToUserDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user == null ? null : MapToUserDto(user);
    }

    public async Task<UserWithRolesDto?> GetUserWithRolesAsync(Guid id, CancellationToken ct = default)
    {
        // Eager loading with Include and ThenInclude
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user == null ? null : MapToUserWithRolesDto(user);
    }

    public async Task<UserWithRolesDto?> GetUserWithExplicitLoadingAsync(Guid id, CancellationToken ct = default)
    {
        // First, load the user
        var user = await _context.Users.FindAsync(new object[] { id }, ct);

        if (user == null)
            return null;

        // Explicitly load related data
        await _context.Entry(user)
            .Collection(u => u.UserRoles)
            .Query()
            .Include(ur => ur.Role)
            .LoadAsync(ct);

        return MapToUserWithRolesDto(user);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserFilterRequest filter, CancellationToken ct = default)
    {
        // AsNoTracking for read-only queries
        var query = _context.Users.AsNoTracking();

        // Filtering
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(u =>
                u.Username.Contains(filter.SearchTerm) ||
                u.Email.Contains(filter.SearchTerm) ||
                u.FirstName.Contains(filter.SearchTerm) ||
                u.LastName.Contains(filter.SearchTerm));
        }

        var total = await query.CountAsync(ct);

        // Projection - selecting only needed fields
        var users = await query
            .OrderBy(u => u.Username)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>(users, total, filter.Page, filter.PageSize);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, string updatedBy, CancellationToken ct = default)
    {
        // Transaction example with optimistic concurrency
        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var user = await _context.Users.FindAsync(new object[] { id }, ct);

            if (user == null)
                throw new NotFoundException($"User {id} not found");

            // Check if email is being changed and if it's already taken
            if (user.Email != request.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.Id != id, ct);

                if (emailExists)
                    throw new ValidationException($"Email '{request.Email}' is already taken");
            }

            // Update properties
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = updatedBy;

            // SaveChanges will throw DbUpdateConcurrencyException if RowVersion changed
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("User {UserId} updated by {UpdatedBy}", id, updatedBy);

            return MapToUserDto(user);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Concurrency conflict updating user {UserId}", id);
            throw new ConcurrencyException("User was modified by another process. Please refresh and try again.");
        }
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, string assignedBy, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
            throw new NotFoundException($"User {userId} not found");

        var role = await _context.Roles.FindAsync(new object[] { roleId }, ct);
        if (role == null)
            throw new NotFoundException($"Role {roleId} not found");

        // Check if role is already assigned
        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (existingUserRole != null)
            throw new ValidationException($"Role '{role.Name}' is already assigned to user '{user.Username}'");

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Role {RoleName} assigned to user {Username} by {AssignedBy}",
            role.Name, user.Username, assignedBy);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (userRole == null)
            throw new NotFoundException($"User {userId} does not have role {roleId}");

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
    }

    public async Task SoftDeleteUserAsync(Guid id, string deletedBy, CancellationToken ct = default)
    {
        var user = await _context.Users
            .IgnoreQueryFilters() // Bypass soft delete filter
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null)
            throw new NotFoundException($"User {id} not found");

        if (user.IsDeleted)
            throw new ValidationException($"User {id} is already deleted");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} soft deleted by {DeletedBy}", id, deletedBy);
    }

    public async Task<List<UserWithRolesDto>> GetUsersWithRoles_NPlusOne_BAD(CancellationToken ct = default)
    {
        _logger.LogWarning("Executing N+1 query pattern (BAD) - watch the SQL logs!");

        // This loads users first (1 query)
        var users = await _context.Users.ToListAsync(ct);

        var result = new List<UserWithRolesDto>();

        // Then for EACH user, makes a separate query for roles (N queries!)
        foreach (var user in users)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .ToListAsync(ct);

            var roles = userRoles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description
            }).ToList();

            result.Add(new UserWithRolesDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles
            });
        }

        return result;
    }

    public async Task<List<UserWithRolesDto>> GetUsersWithRoles_Optimized_GOOD(CancellationToken ct = default)
    {
        _logger.LogInformation("Executing optimized query with Include (GOOD) - single query!");

        // Single query with Include - loads everything at once
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync(ct);

        return users.Select(MapToUserWithRolesDto).ToList();
    }

    // Mapping helpers
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private static UserWithRolesDto MapToUserWithRolesDto(User user)
    {
        return new UserWithRolesDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description
            }).ToList()
        };
    }
}
