using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions;
/// <summary>
/// Throws 409 Conflict - ConcurrencyConflictException - When two resources are changed simultaneously. 
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
