using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // TODO: pass the DbUpdateConcurrencyException as the inner exception once ConcurrencyConflictException has an (message, inner) constructor.
            throw new ConcurrencyConflictException("The request was modified by another user. Reload and try again.");
        }
    }
}
