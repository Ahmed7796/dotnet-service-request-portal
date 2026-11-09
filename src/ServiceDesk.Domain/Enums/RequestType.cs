namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Request Types, as requester (in this demo) can apply for equipment, leave, IT access, or a procurement (purchase).
/// </summary>
public enum RequestType
{
    /// <summary>
    /// Requester -> Dept. Head -> IT Manager
    /// </summary>
    Equipment = 0,
    /// <summary>
    ///  Requester -> IT Manager
    /// </summary>
    SoftwareAccess = 1,
    /// <summary>
    /// Requester -> Dept. Head -> HR Manager
    /// </summary>
    Leave = 2,
    /// <summary>
    /// Requester -> Dept. Head -> Finance Manager
    /// </summary>
    Purchase = 3,
}
