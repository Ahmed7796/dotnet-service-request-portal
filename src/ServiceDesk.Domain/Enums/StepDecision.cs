namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Step decisions within a request, once submitted,
/// the step decision is <see cref="Pending"/> Afterwards, it gets <see cref="Approved"/> /<see cref="Rejected"/> /<see cref="Skipped"/> /<see cref="Skipped"/> .
/// </summary>
public enum StepDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Skipped = 3,
    Cancelled = 4,
}