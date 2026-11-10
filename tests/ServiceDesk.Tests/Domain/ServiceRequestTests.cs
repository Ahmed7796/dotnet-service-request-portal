using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Tests.Domain;

public class ServiceRequestTests
{
    private static readonly Guid FinanceDeptId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private readonly User _requester = User.CreateRequester("user@mail.com", "0001", "hash", "Ali", FinanceDeptId);

    private readonly WorkflowTemplate _equipmentWorkflow = new WorkflowTemplate(RequestType.Equipment, "Equipment Approval", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.ItManager, ApprovalScope.Global)]);


    private ServiceRequest SubmitValid()
    {
        return ServiceRequest.Submit(_requester, "New laptop", "Current one is old", _equipmentWorkflow, Now);
    }

    [Fact]
    public void Submit_ValidInput_StatusIsSubmitted()
    {
        var request = SubmitValid();
        Assert.Equal(RequestStatus.Submitted, request.Status);
    }

    [Fact]
    public void Submit_SetsRequesterDepartmentAndType()
    {
        var request = SubmitValid();
        Assert.Equal(_requester.Id, request.RequesterId);
        Assert.Equal(FinanceDeptId, request.DepartmentId);
        Assert.Equal(RequestType.Equipment, request.Type);
    }

    [Fact]
    public void Submit_SetsTimeStamps()
    {
        var request = SubmitValid();
        Assert.Equal(Now, request.CreatedAt);
        Assert.Equal(Now, request.UpdatedAt);
    }

    [Fact]
    public void Submit_CopiesWorkflowSteps_InOrder()
    {
        var request = SubmitValid();
        Assert.Collection(request.ApprovalSteps,
        first =>
        {
            Assert.Equal(1, first.Order);
            Assert.Equal(ApproverPosition.DepartmentHead, first.Position);
            Assert.Equal(ApprovalScope.RequesterDepartment, first.Scope);
        },
        second =>
        {
            Assert.Equal(2, second.Order);
            Assert.Equal(ApproverPosition.ItManager, second.Position);
            Assert.Equal(ApprovalScope.Global, second.Scope);
        }
        );
    }

    [Fact]
    public void Submit_AllStepsArePending()
    {
        var request = SubmitValid();
        Assert.All(request.ApprovalSteps,
        step =>
        {
            Assert.Equal(StepDecision.Pending, step.Decision);
        });
    }

    [Fact]
    public void Submit_CurrentStepIsFirstStep()
    {
        var request = SubmitValid();
        var currentStep = request.CurrentStep;

        Assert.NotNull(currentStep);
        Assert.Equal(1, currentStep.Order);
    }

    [Fact]
    public void Submit_AddsSubmittedAuditEntry()
    {
        var request = SubmitValid();
        var log = Assert.Single(request.AuditLogs);
        Assert.Equal(AuditAction.Submitted, log.Action);
        Assert.Equal(_requester.Id, log.ActorId);
        Assert.Null(log.StatusBefore);
        Assert.Equal(RequestStatus.Submitted, log.StatusAfter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_BlankTitle_Throws(string title)
    {
        var ex = Assert.Throws<ArgumentException>(() => ServiceRequest.Submit(_requester, title, "desc", _equipmentWorkflow, Now));
        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void Submit_TitleTooLong_Throws()
    {
        string title = new string('x', ServiceRequest.TitleMaxLength + 1);

        var ex = Assert.Throws<ArgumentException>(()
        => ServiceRequest.Submit(_requester, title, "desc", _equipmentWorkflow, Now));
        Assert.Equal("title", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_BlankDescription_Throws(string description)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        ServiceRequest.Submit(_requester, "new laptop", description, _equipmentWorkflow, Now));
        Assert.Equal("description", ex.ParamName);
    }
}
