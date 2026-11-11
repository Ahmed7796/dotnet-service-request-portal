using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Domain.Entities;

public sealed class ServiceRequest : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private readonly List<ApprovalStep> _approvalSteps = [];
    public IReadOnlyList<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();

    private readonly List<AuditLog> _auditLogs = [];
    public IReadOnlyList<AuditLog> AuditLogs => _auditLogs.AsReadOnly();

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public RequestType Type { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Submitted;
    public Guid RequesterId { get; private set; }
    public ApproverPosition? RequesterPosition { get; private set; } // Snapshot at submission, used for the Admin fallback.
    public Guid DepartmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public ApprovalStep? CurrentStep => _approvalSteps.Where(s => s.Decision == StepDecision.Pending).OrderBy(s => s.Order).FirstOrDefault();


    private ServiceRequest() { } // For Core EF

    public static ServiceRequest Submit(User requester, string title, string description, WorkflowTemplate workflow, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(requester);
        ArgumentNullException.ThrowIfNull(workflow);

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription == null)
            throw new ArgumentException("Description is required.", nameof(description));

        if (trimmedDescription.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description can be only {DescriptionMaxLength} characters at most.", nameof(description));


        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > TitleMaxLength)
            throw new ArgumentException($"Title can be {TitleMaxLength} characters at most.", nameof(title));

        ServiceRequest request = new ServiceRequest();
        request.RequesterId = requester.Id;
        request.RequesterPosition = requester.IsApprover ? requester.Position : null;
        request.Title = trimmedTitle;
        request.Description = trimmedDescription;
        request.DepartmentId = requester.DepartmentId;
        request.Status = RequestStatus.Submitted;
        request.Type = workflow.RequestType;
        request.CreatedAt = now;
        request.UpdatedAt = now;

        request._auditLogs.Add(new AuditLog(request.Id, request.RequesterId, AuditAction.Submitted, null, RequestStatus.Submitted, "Submitted.", now));

        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            var requesterHoldsStep = requester.IsApprover && requester.Position == step.Position;

            ApproverPosition? escalated = requesterHoldsStep ? EscalationPolicy.Escalate(step.Position) : null;

            if (escalated is not null)
            {
                request._approvalSteps.Add(new ApprovalStep(
                    request.Id,
                    step.Order,
                    escalated.Value,
                    ApprovalScope.Global,
                    escalatedFrom: step.Position));
                request._auditLogs.Add(new AuditLog(
                    request.Id,
                    requester.Id,
                    AuditAction.StepEscalated,
                    RequestStatus.Submitted,
                    RequestStatus.Submitted,
                    $"Step {step.Order} escalated from {step.Position} to {escalated} (requester holds {step.Position})",
                    now
                ));
            }
            else
            {
                request._approvalSteps.Add(new ApprovalStep(request.Id, step.Order, step.Position, step.Scope));
            }

        }
        return request;
    }

    public void Approve(User approver, string? comment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(approver);

        var step = EnsureCanDecideCurrentStep(approver, RequestTrigger.ApproveStep);

        var before = Status;
        step.Approve(approver.Id, comment, now);

        RequestTrigger trigger = CurrentStep == null ? RequestTrigger.ApproveFinalStep : RequestTrigger.ApproveStep;

        Status = RequestStateMachine.Fire(before, trigger);
        UpdatedAt = now;

        _auditLogs.Add(
            new AuditLog(Id,
            approver.Id,
            AuditAction.StepApproved,
            before,
            Status,
            $"Step {step.Order} ({step.Position}) approved{OnBehalfNote(approver, step)}.",
            now));
    }

    public void Reject(User approver, string reason, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(approver);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required to reject a request.", nameof(reason));

        var trimmedReason = reason.Trim();
        if (trimmedReason.Length > ApprovalStep.CommentMaxLength)
            throw new ArgumentException($"Reason can be {ApprovalStep.CommentMaxLength} characters at most.", nameof(reason));

        var step = EnsureCanDecideCurrentStep(approver, RequestTrigger.Reject);

        var before = Status;
        step.Reject(approver.Id, trimmedReason, now);
        SkipPendingSteps();

        Status = RequestStateMachine.Fire(before, RequestTrigger.Reject);
        UpdatedAt = now;

        _auditLogs.Add(
            new AuditLog(Id,
            approver.Id,
            AuditAction.StepRejected,
            before,
            Status,
            $"Step {step.Order} ({step.Position}) rejected{OnBehalfNote(approver, step)}: {trimmedReason}",
            now));
    }

    public void Cancel(User requester, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(requester);

        if (!RequestStateMachine.CanFire(Status, RequestTrigger.Cancel))
            throw new InvalidStateTransitionException($"Cannot cancel a request that is {Status}");

        if (requester.Id != RequesterId)
            throw new ApprovalNotAllowedException("Only the original requester can cancel this request.");

        var before = Status;
        SkipPendingSteps();

        Status = RequestStateMachine.Fire(before, RequestTrigger.Cancel);
        UpdatedAt = now;

        _auditLogs.Add(new AuditLog(Id, requester.Id, AuditAction.Cancelled, before, Status, "Cancelled by requester.", now));
    }

    public void Fulfil(User admin, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(admin);

        if (!RequestStateMachine.CanFire(Status, RequestTrigger.Fulfil))
            throw new InvalidStateTransitionException($"Cannot fulfil a request that is {Status}");

        if (admin.Role != UserRole.Admin)
            throw new ApprovalNotAllowedException("Only an Admin can fulfil a request.");

        var before = Status;
        Status = RequestStateMachine.Fire(before, RequestTrigger.Fulfil);
        UpdatedAt = now;

        _auditLogs.Add(new AuditLog(Id, admin.Id, AuditAction.Fulfilled, before, Status, "Fulfilled.", now));
    }

    private ApprovalStep EnsureCanDecideCurrentStep(User actor, RequestTrigger trigger)
    {
        var verb = trigger == RequestTrigger.Reject ? "reject" : "approve";

        if (!RequestStateMachine.CanFire(Status, trigger))
            throw new InvalidStateTransitionException($"Cannot {verb} a request that is {Status}");

        var step = CurrentStep;
        if (step is null)
            throw new InvalidStateTransitionException("No approval step is awaiting a decision.");

        if (RequesterId == actor.Id)
            throw new ApprovalNotAllowedException($"You cannot {verb} your own request.");

        // Admin fallback: when the requester holds the step's position, nobody else can decide it.
        if (IsAdminFallback(actor, step))
            return step;

        if (!actor.IsApprover || actor.Position != step.Position)
            throw new ApprovalNotAllowedException($"Only {step.Position} is authorized to {verb} step {step.Order}.");

        if (step.Scope == ApprovalScope.RequesterDepartment && actor.DepartmentId != DepartmentId)
            throw new ApprovalNotAllowedException($"Step {step.Order} must only be {verb}d by a {step.Position} in the requester's department.");

        return step;
    }

    private bool IsAdminFallback(User actor, ApprovalStep step) =>
        actor.Role == UserRole.Admin && RequesterPosition == step.Position;

    private string OnBehalfNote(User actor, ApprovalStep step) =>
        IsAdminFallback(actor, step) ? $", decided by Admin on behalf of {step.Position}" : string.Empty;

    private void SkipPendingSteps()
    {
        foreach (var pending in _approvalSteps.Where(s => s.Decision == StepDecision.Pending))
            pending.Skip();
    }
}
