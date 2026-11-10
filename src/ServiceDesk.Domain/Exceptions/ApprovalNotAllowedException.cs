namespace ServiceDesk.Domain.Exceptions;

public sealed class ApprovalNotAllowedException : DomainException
{
    public ApprovalNotAllowedException(string message) : base(message) { }
}
