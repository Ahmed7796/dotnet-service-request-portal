using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Tests.Domain;

public class EscalationPolicyTests
{
    [Theory]
    [InlineData(ApproverPosition.DepartmentHead, ApproverPosition.GeneralManager)]
    [InlineData(ApproverPosition.FinanceManager, ApproverPosition.GeneralManager)]
    [InlineData(ApproverPosition.HrManager, ApproverPosition.GeneralManager)]
    [InlineData(ApproverPosition.ItManager, ApproverPosition.GeneralManager)]
    [InlineData(ApproverPosition.GeneralManager, null)]
    public void Escalate_Position_ReturnsExpectedEscalation(ApproverPosition position, ApproverPosition? expected)
    {
        Assert.Equal(expected, EscalationPolicy.Escalate(position));
    }
}
