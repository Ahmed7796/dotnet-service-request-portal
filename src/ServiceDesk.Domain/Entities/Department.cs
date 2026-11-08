using ServiceDesk.Domain.Common;

namespace ServiceDesk.Domain.Entities;

public sealed class Department : Entity
{
    public const int MAX_NAME_LENGTH = 30;
    public string Name {get; private set;} = null!;

    // For EF Later...
    private Department(){}

    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MAX_NAME_LENGTH)
            throw new ArgumentException($"Department name must be {MAX_NAME_LENGTH} characters or fewer.", nameof(name));

        Name = trimmed;
    }
}