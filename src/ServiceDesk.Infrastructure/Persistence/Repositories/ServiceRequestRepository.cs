using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class ServiceRequestRepository(AppDbContext db) : IServiceRequestRepository
{
    public Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return db.ServiceRequests
            .Include(r => r.ApprovalSteps)
            .Include(r => r.AuditLogs)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<ServiceRequest>> ListAsync(CancellationToken ct)
    {
        return await db.ServiceRequests
            .AsNoTracking()
            .Include(r => r.ApprovalSteps)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public void Add(ServiceRequest request)
    {
        db.ServiceRequests.Add(request);
    }
}
