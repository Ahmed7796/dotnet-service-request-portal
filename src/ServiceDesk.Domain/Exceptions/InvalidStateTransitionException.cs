namespace ServiceDesk.Domain.Exceptions;
/// <summary>
///  Invokes 409 - Conflict : Often occurs when a resource transitions from a state to another in an illegal / illogical manner.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string message) : base(message) { }
}
