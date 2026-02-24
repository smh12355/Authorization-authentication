using Asp.Versioning;
using Authorization_authentication.Common.Constants;
using Authorization_authentication.Features.UserManagement.Models;
using Authorization_authentication.Features.UserManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Features.UserManagement.Controllers;

[ApiController]
[Route(WebConstants.ApiControllerRoute)]
[ApiVersion("1.0")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        IUserService userService,
        ILogger<UserManagementController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user with optional roles.
    /// </summary>
    [HttpPost("Create")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var createdBy = User.Identity?.Name ?? "Unknown";
        var user = await _userService.CreateUserAsync(request, createdBy, ct);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Gets a user by ID with roles.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserWithRolesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserWithRolesDto>> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserWithRolesAsync(id, ct);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Gets a user by ID with explicit loading demonstration.
    /// </summary>
    [HttpGet("{id:guid}/explicit-loading")]
    [ProducesResponseType(typeof(UserWithRolesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserWithRolesDto>> GetUserWithExplicitLoading(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetUserWithExplicitLoadingAsync(id, ct);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Gets paginated and filtered list of users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] UserFilterRequest filter,
        CancellationToken ct)
    {
        var result = await _userService.GetUsersAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var updatedBy = User.Identity?.Name ?? "Unknown";
        var user = await _userService.UpdateUserAsync(id, request, updatedBy, ct);
        return Ok(user);
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        Guid roleId,
        CancellationToken ct)
    {
        var assignedBy = User.Identity?.Name ?? "Unknown";
        await _userService.AssignRoleAsync(userId, roleId, assignedBy, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(
        Guid userId,
        Guid roleId,
        CancellationToken ct)
    {
        await _userService.RemoveRoleAsync(userId, roleId, ct);
        return NoContent();
    }

    /// <summary>
    /// Soft deletes a user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var deletedBy = User.Identity?.Name ?? "Unknown";
        await _userService.SoftDeleteUserAsync(id, deletedBy, ct);
        return NoContent();
    }

    /// <summary>
    /// Demonstrates N+1 problem (BAD approach - multiple queries).
    /// Watch the SQL logs to see multiple database queries.
    /// </summary>
    [HttpGet("demo/n-plus-one-bad")]
    [ProducesResponseType(typeof(List<UserWithRolesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserWithRolesDto>>> DemoNPlusOneBad(CancellationToken ct)
    {
        var result = await _userService.GetUsersWithRoles_NPlusOne_BAD(ct);
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates optimized approach (GOOD - single query with Include).
    /// Watch the SQL logs to see a single efficient query.
    /// </summary>
    [HttpGet("demo/n-plus-one-good")]
    [ProducesResponseType(typeof(List<UserWithRolesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserWithRolesDto>>> DemoNPlusOneGood(CancellationToken ct)
    {
        var result = await _userService.GetUsersWithRoles_Optimized_GOOD(ct);
        return Ok(result);
    }
}
