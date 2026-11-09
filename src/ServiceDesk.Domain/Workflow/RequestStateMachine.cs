using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Workflow;

public static class RequestStateMachine
{
    private static readonly Dictionary<(RequestStatus From, RequestTrigger Trigger), RequestStatus> _transitions = new()
    {
        [(RequestStatus.Submitted, RequestTrigger.ApproveStep)] = RequestStatus.InReview,
        [(RequestStatus.Submitted, RequestTrigger.ApproveFinalStep)] = RequestStatus.Approved,
        [(RequestStatus.InReview, RequestTrigger.ApproveStep)] = RequestStatus.InReview,
        [(RequestStatus.InReview, RequestTrigger.ApproveFinalStep)] = RequestStatus.Approved,
        [(RequestStatus.Submitted, RequestTrigger.Reject)] = RequestStatus.Rejected,
        [(RequestStatus.InReview, RequestTrigger.Reject)] = RequestStatus.Rejected,
        [(RequestStatus.Submitted, RequestTrigger.Cancel)] = RequestStatus.Cancelled,
        [(RequestStatus.InReview, RequestTrigger.Cancel)] = RequestStatus.Cancelled,
        [(RequestStatus.Approved, RequestTrigger.Fulfil)] = RequestStatus.Fulfilled,
    };

    // CanFire Method checks whether the dictionary contains the key (in this case, a tuple) and then returns True/False.
    public static bool CanFire(RequestStatus from, RequestTrigger trigger) => _transitions.ContainsKey((from, trigger));

    public static RequestStatus Fire(RequestStatus from, RequestTrigger trigger)
    {
        if(_transitions.TryGetValue((from ,trigger), out var next))
            return next;
        else throw new InvalidStateTransitionException($"The trigger {trigger} cannot be applied to {from}");
    }
}
