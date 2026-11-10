using System.Diagnostics;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Workflow;

public static class EscalationPolicy
{
    public static ApproverPosition? Escalate(ApproverPosition position)
    {
        return position switch
        {
            ApproverPosition.DepartmentHead or ApproverPosition.ItManager
                        or ApproverPosition.HrManager or ApproverPosition.FinanceManager => (ApproverPosition?)ApproverPosition.GeneralManager,
            ApproverPosition.GeneralManager => null,
            _ => null,
        };
    }
}
