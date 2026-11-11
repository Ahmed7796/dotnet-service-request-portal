namespace ServiceDesk.Domain.Exceptions;
/// <summary>
/// Generic Exception (500) Internal Server Error
/// </summary>
public abstract class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
