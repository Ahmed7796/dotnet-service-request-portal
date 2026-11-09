using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class User : Entity
{
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 100;
    public const int EmployeeNumberLength = 4;

    public string Email { get; private set; } = null!;
    public string EmployeeNumber { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public ApproverPosition? Position { get; private set; }
    public Guid DepartmentId { get; private set; }

    private User(){} // For EF Core

    private User(
        string email,
        string employeeNumber,
        string passwordHash,
        string displayName,
        Guid departmentId,
        UserRole role,
        ApproverPosition? position)
    {
        if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required.", nameof(email));

        var trimmedEmail = email.Trim();
        if (trimmedEmail.Length > EmailMaxLength || !trimmedEmail.Contains("@"))
        throw new ArgumentException($"Email must be a valid address and below {EmailMaxLength} characters.", nameof(email));

        if (string.IsNullOrWhiteSpace(employeeNumber))
        throw new ArgumentException("Employee number is required.", nameof(employeeNumber));
        
        var trimmedEmployeeNumber = employeeNumber.Trim();
        if (trimmedEmployeeNumber.Length != EmployeeNumberLength || !trimmedEmployeeNumber.All(char.IsDigit))
        throw new ArgumentException($"Employee number must be exactly {EmployeeNumberLength} digits.", nameof(employeeNumber));

        if (string.IsNullOrEmpty(passwordHash))
        throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(displayName))
        throw new ArgumentException ($"Display name is required.", nameof(displayName));

        var trimmedDisplayName = displayName.Trim();
        if (trimmedDisplayName.Length > DisplayNameMaxLength)
        throw new ArgumentException ($"Display name must be {DisplayNameMaxLength} characters at most.", nameof(displayName));

        if (departmentId == Guid.Empty)
        throw new ArgumentException("Department ID cannot be empty.", nameof(departmentId));

        Email = trimmedEmail.ToLowerInvariant();
        EmployeeNumber = trimmedEmployeeNumber.Trim();
        PasswordHash = passwordHash;
        DisplayName = trimmedDisplayName.Trim();
        DepartmentId = departmentId;
        Role = role;
        Position = position; 
    }
}
