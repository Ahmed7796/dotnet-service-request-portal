using NSubstitute;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.DTOs;
using ServiceDesk.Application.Services;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Tests.Application;

public class ServiceRequestServiceTests
{
    private static readonly Guid OpsDeptId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly IServiceRequestRepository _requests = Substitute.For<IServiceRequestRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkflowTemplateRepository _workflows = Substitute.For<IWorkflowTemplateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private readonly User _requester = User.CreateRequester("requester@mail.com", "1001", "hash", "Omar", OpsDeptId);
    private readonly User _opsHead = User.CreateApprover("opshead@mail.com", "2001", "hash", "Layla", OpsDeptId, ApproverPosition.DepartmentHead);

    private readonly WorkflowTemplate _equipmentWorkflow = new WorkflowTemplate(RequestType.Equipment, "Equipment request", [(ApproverPosition.DepartmentHead, ApprovalScope.RequesterDepartment), (ApproverPosition.ItManager, ApprovalScope.Global)]);

    private readonly ServiceRequestService _service;

    public ServiceRequestServiceTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
        _service = new ServiceRequestService(_requests, _users, _workflows, _unitOfWork, _timeProvider);
    }

    private ServiceRequest SubmitValid()
    {
        return ServiceRequest.Submit(_requester, "New laptop", "Current one is old", _equipmentWorkflow, Now);
    }

    [Fact]
    public async Task ApproveAsync_RequestNotFound_ThrowsAndDoesNotSave()
    {
        var requestId = Guid.NewGuid();
        _requests.GetByIdAsync(requestId, Arg.Any<CancellationToken>()).Returns((ServiceRequest?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveAsync(requestId, _opsHead.Id, null, CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_ApproverNotFound_Throws()
    {
        var request = SubmitValid();
        var approverId = Guid.NewGuid();
        _requests.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(approverId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveAsync(request.Id, approverId, null, CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_HappyPath_SavesOnceAndReturnsInReview()
    {
        var request = SubmitValid();
        _requests.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);
        _users.GetByIdAsync(_opsHead.Id, Arg.Any<CancellationToken>()).Returns(_opsHead);

        var dto = await _service.ApproveAsync(request.Id, _opsHead.Id, "ok", CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(RequestStatus.InReview, dto.Status);
        Assert.Equal(Now, dto.Steps[0].DecidedAt);
    }

    [Fact]
    public async Task SubmitAsync_UnknownWorkflow_ThrowsAndDoesNotSave()
    {
        _users.GetByIdAsync(_requester.Id, Arg.Any<CancellationToken>()).Returns(_requester);
        _workflows.GetByTypeAsync(RequestType.Leave, Arg.Any<CancellationToken>()).Returns((WorkflowTemplate?)null);

        var dto = new SubmitServiceRequestDto(RequestType.Leave, "Annual leave", "Two weeks in December");

        await Assert.ThrowsAsync<NotFoundException>(() => _service.SubmitAsync(_requester.Id, dto, CancellationToken.None));

        _requests.DidNotReceive().Add(Arg.Any<ServiceRequest>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
