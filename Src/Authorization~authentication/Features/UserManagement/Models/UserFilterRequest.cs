namespace Authorization_authentication.Features.UserManagement.Models;

/// <summary>
/// Request model for filtering and paginating users.
/// </summary>
public record UserFilterRequest
{
    public string? SearchTerm { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
