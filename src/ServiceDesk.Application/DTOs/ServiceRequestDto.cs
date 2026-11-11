using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs;

public sealed record ServiceRequestDto(Guid Id,
string Title,
string Description,
RequestType Type,
RequestStatus Status,
Guid RequesterId,
Guid DepartmentId,
DateTimeOffset CreatedAt,
DateTimeOffset UpdatedAt,
List<ApprovalStepDto> Steps)
{
    public static ServiceRequestDto FromEntity(ServiceRequest r)
    {
        return new ServiceRequestDto(r.Id,
        r.Title,
        r.Description,
        r.Type,
        r.Status,
        r.RequesterId,
        r.DepartmentId,
        r.CreatedAt,
        r.UpdatedAt,
        r.ApprovalSteps.OrderBy(s => s.Order).Select(s => new ApprovalStepDto(s.Order,
        s.Position,
        s.Scope,
        s.Decision,
        s.DecidedById,
        s.DecidedAt,
        s.Comment,
        s.EscalatedFrom)).ToList());
    }
}
