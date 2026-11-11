using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class WorkflowTemplateRepository(AppDbContext db) : IWorkflowTemplateRepository
{
    public Task<WorkflowTemplate?> GetByTypeAsync(RequestType type, CancellationToken ct)
    {
        return db.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.RequestType == type, ct);
    }
}
