namespace ServiceDesk.Domain.Exceptions;
/// <summary>
/// Returns 409 (Conflict) - Occurs when the request you tried to change was modified by someone else after you loaded it, so your change conflicts with the current state of the resource.
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}
