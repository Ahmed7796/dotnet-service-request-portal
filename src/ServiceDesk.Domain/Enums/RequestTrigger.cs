namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Approval step states, a request step can be approved and moved forward
/// to the next member of the workflow, or rejected, fulfilled, or even cancelled.
/// </summary>
public enum RequestTrigger
{
    ApproveStep = 0,
    ApproveFinalStep = 1,
    Reject = 2,
    Fulfil = 3,
    Cancel = 4,
}