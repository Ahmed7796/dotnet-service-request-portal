namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Auditing: <see cref="Submitted"/> to log a request has been submitted,
/// <see cref="StepApproved"/>, <see cref="StepRejected"/> for workflow insights,
/// <see cref="Fulfilled"/> and <see cref="Cancelled"/> for final log update.     
/// </summary>
public enum AuditAction
{
    Submitted = 0,
    StepApproved = 1,
    StepRejected = 2,
    Fulfilled = 3,
    Cancelled = 4,
} 