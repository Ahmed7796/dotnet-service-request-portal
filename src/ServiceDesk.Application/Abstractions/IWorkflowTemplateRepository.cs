using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Abstractions;

public interface IWorkflowTemplateRepository
{
    Task<WorkflowTemplate?> GetByTypeAsync(RequestType type, CancellationToken ct);
}
