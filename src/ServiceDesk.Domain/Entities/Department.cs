using ServiceDesk.Domain.Common;

namespace ServiceDesk.Domain.Entities;

public sealed class Department : Entity
{
    public const int MaxNameLength = 100;
    public string Name { get; private set; } = null!;

    // For EF Core:
    private Department() { }

    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Department name must be {MaxNameLength} characters or fewer.", nameof(name));

        Name = trimmed;
    }
}