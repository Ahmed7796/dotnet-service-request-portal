using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Tests.Domain;

public class ServiceRequestTests
{
    private static readonly Guid FinanceDeptId = Guid.NewGuid();
    private static readonly Guid ItDeptId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    private readonly User _requester = User.CreateRequester("user@mail.com", "0001", "hash", "Ali", FinanceDeptId);
    private readonly User _itHead = User.CreateApprover("ithead@mail.com", "0002", "hash", "Jonas", ItDeptId, ApproverPosition.DepartmentHead);
    private readonly User _itManager = User.CreateApprover("itmanager@mail.com", "0003", "hash", "Eobard", ItDeptId, ApproverPosition.ItManager);
    private readonly User _financeHead = User.CreateApprover("financehead@mail.com", "0004", "hash", "Bruce", FinanceDeptId, ApproverPosition.DepartmentHead);
    private readonly User _financeManager = User.CreateApprover("financemanager@mail.com", "0005", "hash", "Selina", FinanceDeptId, ApproverPosition.FinanceManager);
    private readonly User _generalManager = User.CreateApprover("gm@mail.com", "0006", "hash", "Diana", FinanceDeptId, ApproverPosition.GeneralManager);
    private readonly User _admin = User.CreateAdmin("admin@mail.com", "0007", "hash", "Alfred", ItDeptId);

    private readonly WorkflowTemplate _equipmentWorkflow = new WorkflowTemplate(RequestType.Equipment, "Equipment Approval", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.ItManager, ApprovalScope.Global)]);


    private readonly WorkflowTemplate _purchaseWorkflow = new WorkflowTemplate(RequestType.Equipment, "Purchase Approval", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.ItManager, ApprovalScope.Global), (ApproverPosition.FinanceManager, ApprovalScope.Global)]);

    private readonly WorkflowTemplate _gmWorkflow = new WorkflowTemplate(RequestType.Equipment, "GM Sign-off", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.GeneralManager, ApprovalScope.Global)]);

    private ServiceRequest SubmitValid()
    {
        return ServiceRequest.Submit(_requester, "New laptop", "Current one is old", _equipmentWorkflow, Now);
    }

    private ServiceRequest SubmitAs(User requester, WorkflowTemplate workflow)
    {
        return ServiceRequest.Submit(requester, "New laptop", "Current one is old", workflow, Now);
    }

    private ServiceRequest SubmitApproved()
    {
        var request = SubmitValid();
        request.Approve(_financeHead, null, Now);
        request.Approve(_itManager, null, Now);
        return request;
    }

    // GM submits _gmWorkflow, step 1 approved by the finance head, so the GM step (held by the requester) is current.
    private ServiceRequest SubmitGmRequestAwaitingGm()
    {
        var request = SubmitAs(_generalManager, _gmWorkflow);
        request.Approve(_financeHead, null, Now);
        return request;
    }

    private sealed record Snapshot(RequestStatus Status, StepDecision[] Decisions, int AuditCount, DateTimeOffset UpdatedAt);

    private static Snapshot Capture(ServiceRequest request) =>
        new(request.Status, request.ApprovalSteps.Select(s => s.Decision).ToArray(), request.AuditLogs.Count, request.UpdatedAt);

    private static void AssertNothingChanged(Snapshot before, ServiceRequest request)
    {
        Assert.Equal(before.Status, request.Status);
        Assert.Equal(before.Decisions, request.ApprovalSteps.Select(s => s.Decision).ToArray());
        Assert.Equal(before.AuditCount, request.AuditLogs.Count);
        Assert.Equal(before.UpdatedAt, request.UpdatedAt);
    }

    [Fact]
    public void Submit_ValidInput_StatusIsSubmitted()
    {
        var request = SubmitValid();
        Assert.Equal(RequestStatus.Submitted, request.Status);
    }

    [Fact]
    public void Submit_SetsRequesterDepartmentAndType()
    {
        var request = SubmitValid();
        Assert.Equal(_requester.Id, request.RequesterId);
        Assert.Equal(FinanceDeptId, request.DepartmentId);
        Assert.Equal(RequestType.Equipment, request.Type);
    }

    [Fact]
    public void Submit_SetsTimeStamps()
    {
        var request = SubmitValid();
        Assert.Equal(Now, request.CreatedAt);
        Assert.Equal(Now, request.UpdatedAt);
    }

    [Fact]
    public void Submit_CopiesWorkflowSteps_InOrder()
    {
        var request = SubmitValid();
        Assert.Collection(request.ApprovalSteps,
        first =>
        {
            Assert.Equal(1, first.Order);
            Assert.Equal(ApproverPosition.DepartmentHead, first.Position);
            Assert.Equal(ApprovalScope.RequesterDepartment, first.Scope);
        },
        second =>
        {
            Assert.Equal(2, second.Order);
            Assert.Equal(ApproverPosition.ItManager, second.Position);
            Assert.Equal(ApprovalScope.Global, second.Scope);
        }
        );
    }

    [Fact]
    public void Submit_AllStepsArePending()
    {
        var request = SubmitValid();
        Assert.All(request.ApprovalSteps,
        step =>
        {
            Assert.Equal(StepDecision.Pending, step.Decision);
        });
    }

    [Fact]
    public void Submit_CurrentStepIsFirstStep()
    {
        var request = SubmitValid();
        var currentStep = request.CurrentStep;

        Assert.NotNull(currentStep);
        Assert.Equal(1, currentStep.Order);
    }

    [Fact]
    public void Submit_AddsSubmittedAuditEntry()
    {
        var request = SubmitValid();
        var log = Assert.Single(request.AuditLogs);
        Assert.Equal(AuditAction.Submitted, log.Action);
        Assert.Equal(_requester.Id, log.ActorId);
        Assert.Null(log.StatusBefore);
        Assert.Equal(RequestStatus.Submitted, log.StatusAfter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_BlankTitle_Throws(string title)
    {
        var ex = Assert.Throws<ArgumentException>(() => ServiceRequest.Submit(_requester, title, "desc", _equipmentWorkflow, Now));
        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void Submit_TitleTooLong_Throws()
    {
        string title = new string('x', ServiceRequest.TitleMaxLength + 1);

        var ex = Assert.Throws<ArgumentException>(()
        => ServiceRequest.Submit(_requester, title, "desc", _equipmentWorkflow, Now));
        Assert.Equal("title", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_BlankDescription_Throws(string description)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        ServiceRequest.Submit(_requester, "new laptop", description, _equipmentWorkflow, Now));
        Assert.Equal("description", ex.ParamName);
    }

    [Fact]
    public void Approve_FirstStepOfTwo_StatusInReview()
    {
        var request = SubmitValid();

        request.Approve(_financeHead, null, Now);
        Assert.Equal(RequestStatus.InReview, request.Status);
        Assert.Equal(StepDecision.Approved, request.ApprovalSteps[0].Decision);

        var current = request.CurrentStep;

        Assert.NotNull(current);
        Assert.Equal(2, current.Order);
    }

    [Fact]
    public void Approve_FinalStep_StatusApproved()
    {
        var request = SubmitValid();
        request.Approve(_financeHead, null, Now);
        request.Approve(_itManager, null, Now);

        Assert.Equal(RequestStatus.Approved, request.Status);


        Assert.Null(request.CurrentStep);

        Assert.All(request.ApprovalSteps, step =>
        {
            Assert.Equal(StepDecision.Approved, step.Decision);
        }
        );
    }

    [Fact]
    public void Approve_ByRequester_Throws()
    {
        var request = SubmitValid();
        Assert.Throws<ApprovalNotAllowedException>(() =>
        request.Approve(_requester, null, Now)
        );

        Assert.Equal(RequestStatus.Submitted, request.Status);
        Assert.Equal(StepDecision.Pending, request.ApprovalSteps[0].Decision);
        Assert.Single(request.AuditLogs);
    }

    // ---------- Escalation ----------

    [Fact]
    public void Submit_RequesterHoldsStepPosition_StepEscalatedToGeneralManager()
    {
        var request = SubmitAs(_financeHead, _equipmentWorkflow);

        var first = request.ApprovalSteps[0];
        Assert.Equal(ApproverPosition.GeneralManager, first.Position);
        Assert.Equal(ApprovalScope.Global, first.Scope);
        Assert.Equal(ApproverPosition.DepartmentHead, first.EscalatedFrom);

        var second = request.ApprovalSteps[1];
        Assert.Equal(ApproverPosition.ItManager, second.Position);
        Assert.Null(second.EscalatedFrom);
    }

    [Fact]
    public void Submit_RegularRequester_NoStepsEscalated()
    {
        var request = SubmitValid();

        Assert.All(request.ApprovalSteps, step => Assert.Null(step.EscalatedFrom));
        Assert.DoesNotContain(request.AuditLogs, log => log.Action == AuditAction.StepEscalated);
    }

    [Fact]
    public void Submit_RequesterHoldsStepPosition_AddsEscalationAuditEntry()
    {
        var request = SubmitAs(_financeHead, _equipmentWorkflow);

        var log = Assert.Single(request.AuditLogs, l => l.Action == AuditAction.StepEscalated);
        Assert.Equal(_financeHead.Id, log.ActorId);
        Assert.Equal(RequestStatus.Submitted, log.StatusBefore);
        Assert.Equal(RequestStatus.Submitted, log.StatusAfter);
        Assert.Equal("Step 1 escalated from DepartmentHead to GeneralManager (requester holds DepartmentHead)", log.Details);
    }

    [Fact]
    public void Submit_GeneralManagersOwnRequest_GmStepNotEscalated()
    {
        var request = SubmitAs(_generalManager, _gmWorkflow);

        var gmStep = request.ApprovalSteps[1];
        Assert.Equal(ApproverPosition.GeneralManager, gmStep.Position);
        Assert.Null(gmStep.EscalatedFrom);
        Assert.Equal(ApproverPosition.GeneralManager, request.RequesterPosition);
        Assert.DoesNotContain(request.AuditLogs, log => log.Action == AuditAction.StepEscalated);
    }

    // ---------- Approve ----------

    [Fact]
    public void Approve_WrongPosition_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_itManager, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_DepartmentHeadFromOtherDepartment_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_itHead, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_ClosedRequest_Throws()
    {
        var request = SubmitApproved();
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Approve(_itManager, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_CancelledRequest_Throws()
    {
        var request = SubmitValid();
        request.Cancel(_requester, Now);
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Approve(_financeHead, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_ThreeStepWorkflow_StaysInReviewUntilLastStep()
    {
        var request = SubmitAs(_requester, _purchaseWorkflow);

        request.Approve(_financeHead, null, Now);
        Assert.Equal(RequestStatus.InReview, request.Status);

        request.Approve(_itManager, null, Now);
        Assert.Equal(RequestStatus.InReview, request.Status);
        Assert.Equal(3, request.CurrentStep!.Order);

        request.Approve(_financeManager, null, Later);
        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Null(request.CurrentStep);
        Assert.Equal(Later, request.UpdatedAt);
        Assert.Equal(3, request.AuditLogs.Count(l => l.Action == AuditAction.StepApproved));
    }

    [Fact]
    public void Approve_ApproverApprovesOwnRequest_Throws()
    {
        var request = SubmitAs(_financeHead, _equipmentWorkflow);
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_financeHead, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_EscalatedStep_ApprovedByGeneralManager()
    {
        var request = SubmitAs(_financeHead, _equipmentWorkflow);

        request.Approve(_generalManager, null, Now);

        Assert.Equal(StepDecision.Approved, request.ApprovalSteps[0].Decision);
        Assert.Equal(RequestStatus.InReview, request.Status);
    }

    [Fact]
    public void Approve_AddsStepApprovedAuditEntry()
    {
        var request = SubmitValid();

        request.Approve(_financeHead, "ok", Later);

        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.StepApproved, log.Action);
        Assert.Equal(_financeHead.Id, log.ActorId);
        Assert.Equal(RequestStatus.Submitted, log.StatusBefore);
        Assert.Equal(RequestStatus.InReview, log.StatusAfter);
        Assert.Equal("Step 1 (DepartmentHead) approved.", log.Details);
    }

    // ---------- Admin fallback ----------

    [Fact]
    public void Approve_AdminOnStepHeldByRequester_Approves()
    {
        var request = SubmitGmRequestAwaitingGm();

        request.Approve(_admin, null, Later);

        Assert.Equal(RequestStatus.Approved, request.Status);
        var gmStep = request.ApprovalSteps[1];
        Assert.Equal(StepDecision.Approved, gmStep.Decision);
        Assert.Equal(_admin.Id, gmStep.DecidedById);
    }

    [Fact]
    public void Approve_AdminFallback_AuditSaysOnBehalf()
    {
        var request = SubmitGmRequestAwaitingGm();

        request.Approve(_admin, null, Later);

        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.StepApproved, log.Action);
        Assert.Equal(_admin.Id, log.ActorId);
        Assert.Contains("decided by Admin on behalf of GeneralManager", log.Details);
    }

    [Fact]
    public void Approve_GeneralManagerOnOwnRequest_Throws()
    {
        var request = SubmitGmRequestAwaitingGm();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_generalManager, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_AdminOnStepNotHeldByRequester_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_admin, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Approve_AdminOnEarlierStepOfGmRequest_Throws()
    {
        // Step 1 (DepartmentHead) is not held by the GM requester, so the Admin fallback does not apply.
        var request = SubmitAs(_generalManager, _gmWorkflow);
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Approve(_admin, null, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_AdminOnStepHeldByRequester_Rejects()
    {
        var request = SubmitGmRequestAwaitingGm();

        request.Reject(_admin, "Over budget", Later);

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(StepDecision.Rejected, request.ApprovalSteps[1].Decision);
        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.StepRejected, log.Action);
        Assert.Contains("decided by Admin on behalf of GeneralManager", log.Details);
    }

    [Fact]
    public void Reject_AdminOnStepNotHeldByRequester_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Reject(_admin, "No", Later));

        AssertNothingChanged(before, request);
    }

    // ---------- Reject ----------

    [Fact]
    public void Reject_FirstStep_RejectsAndSkipsRemaining()
    {
        var request = SubmitAs(_requester, _purchaseWorkflow);

        request.Reject(_financeHead, "  Not needed  ", Later);

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(Later, request.UpdatedAt);
        Assert.Null(request.CurrentStep);
        Assert.Collection(request.ApprovalSteps,
            first =>
            {
                Assert.Equal(StepDecision.Rejected, first.Decision);
                Assert.Equal("Not needed", first.Comment);
                Assert.Equal(_financeHead.Id, first.DecidedById);
                Assert.Equal(Later, first.DecidedAt);
            },
            second => Assert.Equal(StepDecision.Skipped, second.Decision),
            third => Assert.Equal(StepDecision.Skipped, third.Decision));
    }

    [Fact]
    public void Reject_LaterStep_KeepsEarlierApproval()
    {
        var request = SubmitAs(_requester, _purchaseWorkflow);
        request.Approve(_financeHead, null, Now);

        request.Reject(_itManager, "No stock", Later);

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(StepDecision.Approved, request.ApprovalSteps[0].Decision);
        Assert.Equal(StepDecision.Rejected, request.ApprovalSteps[1].Decision);
        Assert.Equal(StepDecision.Skipped, request.ApprovalSteps[2].Decision);
    }

    [Fact]
    public void Reject_AddsStepRejectedAuditEntry()
    {
        var request = SubmitValid();
        request.Approve(_financeHead, null, Now);

        request.Reject(_itManager, "No stock", Later);

        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.StepRejected, log.Action);
        Assert.Equal(_itManager.Id, log.ActorId);
        Assert.Equal(RequestStatus.InReview, log.StatusBefore);
        Assert.Equal(RequestStatus.Rejected, log.StatusAfter);
        Assert.Equal("Step 2 (ItManager) rejected: No stock", log.Details);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Reject_BlankReason_Throws(string? reason)
    {
        var request = SubmitValid();
        var before = Capture(request);

        var ex = Assert.Throws<ArgumentException>(() => request.Reject(_financeHead, reason!, Later));

        Assert.Equal("reason", ex.ParamName);
        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_ReasonTooLong_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        var ex = Assert.Throws<ArgumentException>(() =>
            request.Reject(_financeHead, new string('x', ApprovalStep.CommentMaxLength + 1), Later));

        Assert.Equal("reason", ex.ParamName);
        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_WrongPosition_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Reject(_itManager, "No", Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_DepartmentHeadFromOtherDepartment_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Reject(_itHead, "No", Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_OwnRequest_Throws()
    {
        var request = SubmitAs(_financeHead, _equipmentWorkflow);
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Reject(_financeHead, "No", Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Reject_ClosedRequest_Throws()
    {
        var request = SubmitApproved();
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Reject(_itManager, "Changed my mind", Later));

        AssertNothingChanged(before, request);
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_ByRequester_CancelsAndSkipsPendingSteps()
    {
        var request = SubmitValid();

        request.Cancel(_requester, Later);

        Assert.Equal(RequestStatus.Cancelled, request.Status);
        Assert.Equal(Later, request.UpdatedAt);
        Assert.Null(request.CurrentStep);
        Assert.All(request.ApprovalSteps, step => Assert.Equal(StepDecision.Skipped, step.Decision));
    }

    [Fact]
    public void Cancel_InReview_KeepsApprovedSteps()
    {
        var request = SubmitAs(_requester, _purchaseWorkflow);
        request.Approve(_financeHead, null, Now);

        request.Cancel(_requester, Later);

        Assert.Equal(RequestStatus.Cancelled, request.Status);
        Assert.Equal(StepDecision.Approved, request.ApprovalSteps[0].Decision);
        Assert.Equal(StepDecision.Skipped, request.ApprovalSteps[1].Decision);
        Assert.Equal(StepDecision.Skipped, request.ApprovalSteps[2].Decision);
    }

    [Fact]
    public void Cancel_AddsCancelledAuditEntry()
    {
        var request = SubmitValid();

        request.Cancel(_requester, Later);

        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.Cancelled, log.Action);
        Assert.Equal(_requester.Id, log.ActorId);
        Assert.Equal(RequestStatus.Submitted, log.StatusBefore);
        Assert.Equal(RequestStatus.Cancelled, log.StatusAfter);
        Assert.Equal(Later, log.OccurredAt);
    }

    [Fact]
    public void Cancel_ByOtherUser_Throws()
    {
        var request = SubmitValid();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Cancel(_financeHead, Later));
        Assert.Throws<ApprovalNotAllowedException>(() => request.Cancel(_admin, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Cancel_ApprovedRequest_Throws()
    {
        var request = SubmitApproved();
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Cancel(_requester, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var request = SubmitValid();
        request.Cancel(_requester, Now);
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Cancel(_requester, Later));

        AssertNothingChanged(before, request);
    }

    // ---------- Fulfil ----------

    [Fact]
    public void Fulfil_ByAdminFromApproved_StatusFulfilled()
    {
        var request = SubmitApproved();

        request.Fulfil(_admin, Later);

        Assert.Equal(RequestStatus.Fulfilled, request.Status);
        Assert.Equal(Later, request.UpdatedAt);
    }

    [Fact]
    public void Fulfil_AddsFulfilledAuditEntry()
    {
        var request = SubmitApproved();

        request.Fulfil(_admin, Later);

        var log = request.AuditLogs[^1];
        Assert.Equal(AuditAction.Fulfilled, log.Action);
        Assert.Equal(_admin.Id, log.ActorId);
        Assert.Equal(RequestStatus.Approved, log.StatusBefore);
        Assert.Equal(RequestStatus.Fulfilled, log.StatusAfter);
    }

    [Fact]
    public void Fulfil_ByNonAdmin_Throws()
    {
        var request = SubmitApproved();
        var before = Capture(request);

        Assert.Throws<ApprovalNotAllowedException>(() => request.Fulfil(_itManager, Later));
        Assert.Throws<ApprovalNotAllowedException>(() => request.Fulfil(_requester, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Fulfil_NotApproved_Throws()
    {
        var request = SubmitValid();
        request.Approve(_financeHead, null, Now);
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Fulfil(_admin, Later));

        AssertNothingChanged(before, request);
    }

    [Fact]
    public void Fulfil_AlreadyFulfilled_Throws()
    {
        var request = SubmitApproved();
        request.Fulfil(_admin, Now);
        var before = Capture(request);

        Assert.Throws<InvalidStateTransitionException>(() => request.Fulfil(_admin, Later));

        AssertNothingChanged(before, request);
    }
}
