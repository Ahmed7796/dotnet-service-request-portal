using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.DTOs;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Services;

public sealed class ServiceRequestService(IServiceRequestRepository requests, IUserRepository users, IWorkflowTemplateRepository workflows, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<ServiceRequestDto> SubmitAsync(Guid requesterId, SubmitServiceRequestDto dto, CancellationToken ct)
    {
        var requester = await users.GetByIdAsync(requesterId, ct) ?? throw new NotFoundException($"User {requesterId} not found.");

        var workflow = await workflows.GetByTypeAsync(dto.Type, ct) ?? throw new NotFoundException($"No workflow is configured for {dto.Type} requests.");

        var request = ServiceRequest.Submit(requester, dto.Title, dto.Description, workflow, timeProvider.GetUtcNow());
        requests.Add(request);

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }

    public async Task<ServiceRequestDto> GetByIdAsync(Guid requestId, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        return ServiceRequestDto.FromEntity(request);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> ListAsync(CancellationToken ct)
    {
        var all = await requests.ListAsync(ct);

        return all.Select(ServiceRequestDto.FromEntity).ToList();
    }

    public async Task<ServiceRequestDto> ApproveAsync(Guid requestId, Guid approverId, string? comment, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        var approver = await users.GetByIdAsync(approverId, ct) ?? throw new NotFoundException($"User {approverId} not found.");

        request.Approve(approver, comment, timeProvider.GetUtcNow());
    
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }

    public async Task<ServiceRequestDto> RejectAsync(Guid requestId, Guid approverId, string reason, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        var approver = await users.GetByIdAsync(approverId, ct) ?? throw new NotFoundException($"User {approverId} not found.");

        request.Reject(approver, reason, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }

    public async Task<ServiceRequestDto> CancelAsync(Guid requestId, Guid requesterId, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        var requester = await users.GetByIdAsync(requesterId, ct) ?? throw new NotFoundException($"User {requesterId} not found.");

        request.Cancel(requester, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }

    public async Task<ServiceRequestDto> FulfilAsync(Guid requestId, Guid adminId, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        var admin = await users.GetByIdAsync(adminId, ct) ?? throw new NotFoundException($"User {adminId} not found.");

        request.Fulfil(admin, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }
}
