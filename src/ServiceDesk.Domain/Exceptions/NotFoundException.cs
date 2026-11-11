namespace ServiceDesk.Domain.Exceptions;
/// <summary>
/// Returns 404 (Not Found)
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}
