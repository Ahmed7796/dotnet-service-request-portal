namespace ServiceDesk.Domain.Exceptions;
/// <summary>
/// Returns 403 - Forbidden (When a user is not allowed to approve/reject/cancel a request. Or a request that already has been approved/rejected/fulfilled.) (Could also be applicable to other scenarios.)
/// </summary>
public sealed class ApprovalNotAllowedException : DomainException
{
    public ApprovalNotAllowedException(string message) : base(message) { }
}
