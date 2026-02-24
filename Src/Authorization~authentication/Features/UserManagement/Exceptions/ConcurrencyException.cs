namespace Authorization_authentication.Features.UserManagement.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict occurs during update.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
