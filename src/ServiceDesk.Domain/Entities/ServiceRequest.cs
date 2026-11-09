
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

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
        if(trimmedTitle.Length > TitleMaxLength)
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

    public ApprovalStep? CurrentStep()
    {
        return _approvalSteps.Where(s => s.Decision == StepDecision.Pending).OrderBy(s => s.Order).FirstOrDefault();        
    }
}
