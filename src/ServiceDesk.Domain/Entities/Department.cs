using ServiceDesk.Domain.Common;

namespace ServiceDesk.Domain.Entities;

public sealed class Department : Entity
{
    public string Name {get; private set;} = null!;
}