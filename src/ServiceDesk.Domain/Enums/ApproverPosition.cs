namespace ServiceDesk.Domain.Enums;

/// <summary>
/// Approver positions, Some requests require DeptHead approval, some require IT/HR, and Finance. (Placeholders for now)
/// And approvers (Dept. Head, ITManager, HRManager escalate to GM.)
/// </summary>
public enum ApproverPosition
{
    DepartmentHead = 0,
    ItManager = 1,
    HrManager = 2,
    FinanceManager = 3,
    GeneralManager = 4,
}