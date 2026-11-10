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
        request.Title = trimmedTitle;
        request.Description = trimmedDescription;
        request.DepartmentId = requester.DepartmentId;
        request.Status = RequestStatus.Submitted;
        request.Type = workflow.RequestType;
        request.CreatedAt = now;
        request.UpdatedAt = now;

        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            request._approvalSteps.Add(new ApprovalStep(request.Id, step.Order, step.Position, step.Scope));
        }

        request._auditLogs.Add(new AuditLog(request.Id, request.RequesterId, AuditAction.Submitted, null, RequestStatus.Submitted, "Submitted.", now));
        return request;
    }

    public void Approve(User approver, string? comment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(approver);

        if (!RequestStateMachine.CanFire(Status, RequestTrigger.ApproveStep))
            throw new InvalidStateTransitionException($"Cannot approve a request that is {Status}");

        var step = CurrentStep;
        if (step is null)
            throw new InvalidStateTransitionException("No approval step is awaiting a decision.");

        if (RequesterId == approver.Id)
            throw new ApprovalNotAllowedException("You cannot approve your own request.");

        if (!approver.IsApprover || approver.Position != step.Position)
            throw new ApprovalNotAllowedException($"Only {step.Position} is authorized to approve step {step.Order}.");

        if (step.Scope == ApprovalScope.RequesterDepartment && approver.DepartmentId != DepartmentId)
            throw new ApprovalNotAllowedException($"Step {step.Order} must only be approved by a {step.Position} in the requester's department.");

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
            $"Step {step.Order} ({step.Position}) approved.",
            now));
    }
}
