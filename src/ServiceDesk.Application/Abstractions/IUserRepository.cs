using ServiceDesk.Domain.Entities;
namespace ServiceDesk.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    // Could be Email or Employee Number, useful for multi-identifier login.
    Task<User?> GetByEmailOrEmployeeNumber(string identifier);
}
