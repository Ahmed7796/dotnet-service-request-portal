namespace ServiceDesk.Domain.Enums;

/// <summary><see cref="Requester"/> : Basic submission permission, 
/// <see cref="Approver"/> : Workflow staff (HR/Dept Head, Etc.), <see cref="Admin"/> : Super-user
/// </summary>
public enum UserRole
{
    Requester = 0,
    Approver = 1,
    Admin = 2,
}