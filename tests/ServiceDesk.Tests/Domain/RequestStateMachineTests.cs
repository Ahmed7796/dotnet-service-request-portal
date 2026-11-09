using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Tests.Domain;

public class RequestStateMachineTests
{
    // ---------- Legal transitions ----------

    [Theory]
    [InlineData(RequestStatus.Submitted, RequestTrigger.ApproveStep,      RequestStatus.InReview)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.ApproveFinalStep, RequestStatus.Approved)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.ApproveStep,      RequestStatus.InReview)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.ApproveFinalStep, RequestStatus.Approved)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Reject,           RequestStatus.Rejected)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.Reject,           RequestStatus.Rejected)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Cancel,           RequestStatus.Cancelled)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.Cancel,           RequestStatus.Cancelled)]
    [InlineData(RequestStatus.Approved,  RequestTrigger.Fulfil,           RequestStatus.Fulfilled)]
    public void Fire_LegalTransition_ReturnsNextStatus(
        RequestStatus from,
        RequestTrigger trigger,
        RequestStatus expected)
    {
        var actual = RequestStateMachine.Fire(from, trigger);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(RequestStatus.Submitted, RequestTrigger.ApproveStep)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.ApproveFinalStep)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.ApproveStep)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.ApproveFinalStep)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Reject)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.Reject)]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Cancel)]
    [InlineData(RequestStatus.InReview,  RequestTrigger.Cancel)]
    [InlineData(RequestStatus.Approved,  RequestTrigger.Fulfil)]
    public void CanFire_LegalTransition_ReturnsTrue(RequestStatus from, RequestTrigger trigger)
    {
        Assert.True(RequestStateMachine.CanFire(from, trigger));
    }

    // ---------- Illegal transitions ----------

    [Theory]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Fulfil)]       // can't skip approval
    [InlineData(RequestStatus.InReview,  RequestTrigger.Fulfil)]
    [InlineData(RequestStatus.Approved,  RequestTrigger.Reject)]       // can't undo approval
    [InlineData(RequestStatus.Approved,  RequestTrigger.Cancel)]
    [InlineData(RequestStatus.Approved,  RequestTrigger.ApproveStep)]
    [InlineData(RequestStatus.Rejected,  RequestTrigger.ApproveStep)]  // terminal states are final
    [InlineData(RequestStatus.Cancelled, RequestTrigger.ApproveStep)]
    [InlineData(RequestStatus.Fulfilled, RequestTrigger.Cancel)]
    public void Fire_IllegalTransition_Throws(RequestStatus from, RequestTrigger trigger)
    {
        Assert.Throws<InvalidStateTransitionException>(() => RequestStateMachine.Fire(from, trigger));
    }

    [Theory]
    [InlineData(RequestStatus.Submitted, RequestTrigger.Fulfil)]
    [InlineData(RequestStatus.Approved,  RequestTrigger.Reject)]
    [InlineData(RequestStatus.Rejected,  RequestTrigger.ApproveStep)]
    [InlineData(RequestStatus.Fulfilled, RequestTrigger.Cancel)]
    public void CanFire_IllegalTransition_ReturnsFalse(RequestStatus from, RequestTrigger trigger)
    {
        Assert.False(RequestStateMachine.CanFire(from, trigger));
    }

    // ---------- Terminal states ----------

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Cancelled)]
    [InlineData(RequestStatus.Fulfilled)]
    public void CanFire_TerminalStatus_AllowsNoTrigger(RequestStatus terminal)
    {
        foreach (var trigger in Enum.GetValues<RequestTrigger>())
            Assert.False(RequestStateMachine.CanFire(terminal, trigger));
    }
}