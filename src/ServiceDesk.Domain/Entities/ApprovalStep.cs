using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class ApprovalStep : Entity
{
    public const int CommentMaxLength = 500;

    public Guid ServiceRequestId { get; private set; }
    public int Order { get; private set; }
    public ApproverPosition Position { get; private set; }
    public ApproverPosition? EscalatedFrom { get; private set; }
    public ApprovalScope Scope { get; private set; }
    public StepDecision Decision { get; private set; } = StepDecision.Pending;
    public Guid? DecidedById { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? Comment { get; private set; }

    private ApprovalStep() { } // For EF Core

    internal ApprovalStep(Guid serviceRequestId, int order, ApproverPosition position, ApprovalScope scope, ApproverPosition? escalatedFrom = null)
    {
        ServiceRequestId = serviceRequestId;
        Order = order;
        Position = position;
        Scope = scope;
        EscalatedFrom = escalatedFrom;
    }

    private void Decide(StepDecision decision, Guid approverId, string? comment, DateTimeOffset now)
    {
        string? trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed != null && trimmed.Length > CommentMaxLength)
            throw new ArgumentException($"The max character limit of a comment is {CommentMaxLength}.", nameof(comment));
        Decision = decision;
        DecidedById = approverId;
        Comment = trimmed;
        DecidedAt = now;
    }

    internal void Approve(Guid approverId, string? comment, DateTimeOffset now)
    {
        Decide(StepDecision.Approved, approverId, comment, now);
    }

    internal void Reject(Guid rejecterId, string reason, DateTimeOffset now)
    {
        Decide(StepDecision.Rejected, rejecterId, reason, now);
    }

    internal void Skip()
    {
        Decision = StepDecision.Skipped;
    }

}
