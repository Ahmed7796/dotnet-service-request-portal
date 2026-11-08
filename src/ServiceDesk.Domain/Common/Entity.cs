namespace ServiceDesk.Domain.Common;

// Entity Abstract Class (Cannot be Instantiated)
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}