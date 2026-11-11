using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.DTOs;
using ServiceDesk.Application.Services;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
public sealed class ServiceRequestsController(ServiceRequestService service) : ControllerBase
{
    public const string UserIdHeader = "X-User-Id";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestDto>>> List(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceRequestDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await service.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> Submit(SubmitServiceRequestDto dto, CancellationToken ct)
    {
        var created = await service.SubmitAsync(GetCurrentUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ServiceRequestDto>> Approve(Guid id, ApproveDto dto, CancellationToken ct)
    {
        return Ok(await service.ApproveAsync(id, GetCurrentUserId(), dto.Comment, ct));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ServiceRequestDto>> Reject(Guid id, RejectDto dto, CancellationToken ct)
    {
        return Ok(await service.RejectAsync(id, GetCurrentUserId(), dto.Reason, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ServiceRequestDto>> Cancel(Guid id, CancellationToken ct)
    {
        return Ok(await service.CancelAsync(id, GetCurrentUserId(), ct));
    }

    [HttpPost("{id:guid}/fulfil")]
    public async Task<ActionResult<ServiceRequestDto>> Fulfil(Guid id, CancellationToken ct)
    {
        return Ok(await service.FulfilAsync(id, GetCurrentUserId(), ct));
    }

    // TODO: replace with the JWT sub claim
    private Guid GetCurrentUserId()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var value) || !Guid.TryParse(value, out var userId))
            throw new ArgumentException($"A valid {UserIdHeader} header is required.", UserIdHeader);

        return userId;
    }
}
