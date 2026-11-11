using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.DTOs;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Services;

public sealed class ServiceRequestService(IServiceRequestRepository requests, IUserRepository users, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<ServiceRequestDto> ApproveAsync(Guid requestId, Guid approverId, string? comment, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct) ?? throw new NotFoundException($"Request {requestId} not found.");

        var approver = await users.GetByIdAsync(approverId, ct) ?? throw new NotFoundException($"User {approverId} not found.");

        request.Approve(approver, comment, timeProvider.GetUtcNow());
    
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceRequestDto.FromEntity(request);
    }
}
