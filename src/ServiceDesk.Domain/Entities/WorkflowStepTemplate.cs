using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class WorkflowStepTemplate : Entity
{
    public Guid WorkflowTemplateId { get; private set; }
    public int Order { get; private set; }
    public ApproverPosition Position { get; private set; }
    public ApprovalScope Scope { get; private set; }

    private WorkflowStepTemplate() { } // for EF Core

    internal WorkflowStepTemplate(Guid workflowTemplateId, int order, ApproverPosition position, ApprovalScope scope)
    {
        WorkflowTemplateId = workflowTemplateId;
        Order = order;
        Position = position;
        Scope = scope;
    }
}
