using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class WorkflowTemplate : Entity
{
    public const int NameMaxLength = 100;

    public RequestType RequestType { get; private set; }
    public string Name { get; private set; } = null!;
    private readonly List<WorkflowStepTemplate> _steps = [];
    public IReadOnlyList<WorkflowStepTemplate> Steps => _steps.AsReadOnly();

    private WorkflowTemplate() { } // For EF Core

    public WorkflowTemplate(RequestType requestType, string name, IEnumerable<(ApproverPosition Position, ApprovalScope Scope)> steps)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));

        var trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
            throw new ArgumentException($"Workflow name must be at most {NameMaxLength} characters.", nameof(name));

        ArgumentNullException.ThrowIfNull(steps);

        var order = 1;
        foreach (var (position, scope) in steps)
        {
            _steps.Add(new WorkflowStepTemplate(Id, order++, position, scope));
        }

        if (_steps.Count == 0)
            throw new ArgumentException("A workflow needs at least one approval step", nameof(steps));

        Name = trimmedName;
        RequestType = requestType;
    }
}
