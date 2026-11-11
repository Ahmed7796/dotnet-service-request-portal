using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions;

public interface IServiceRequestRepository
{
    Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<ServiceRequest>> ListAsync(CancellationToken ct);

    void Add(ServiceRequest request);

}
