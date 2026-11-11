using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs;

public sealed record ApprovalStepDto(
    int Order,
    ApproverPosition Position,
    ApprovalScope Scope,
    StepDecision Decision,
    Guid? DecidedById,
    DateTimeOffset? DecidedAt,
    string? Comment,
    ApproverPosition? EscalatedFrom
)
{ }
