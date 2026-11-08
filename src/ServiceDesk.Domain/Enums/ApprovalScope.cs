namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Some requests might require an approval/rejection from non-department heads, such as leave requests (HR), and some
/// might require explicit departmental head approval (e.g. Requester(Maintenance) -> Head of Maintenance (Approve/Reject))
/// </summary>
public enum ApprovalScope
{
    RequesterDepartment = 0,
    Global = 1,

}
