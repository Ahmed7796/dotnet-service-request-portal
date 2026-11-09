using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class AuditLog : Entity
{
    public const int DetailsMaxLength = 200;

    public Guid ServiceRequestId { get; private set; }
    public Guid ActorId { get; private set; }
    public AuditAction Action { get; private set; }
    public RequestStatus? StatusBefore { get; private set; }  
    public RequestStatus StatusAfter { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private AuditLog() { } // For EF Core

    internal AuditLog(Guid serviceRequestId, Guid actorId, AuditAction action, RequestStatus? statusBefore, RequestStatus statusAfter, string? details, DateTimeOffset occurredAt)
    {
        var detailsTrimmed = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (detailsTrimmed != null && detailsTrimmed.Length > DetailsMaxLength)
        detailsTrimmed = detailsTrimmed[..DetailsMaxLength]; // If the details exceed character limit, just cut it to 200 characters and store, else store as it is.

        ServiceRequestId = serviceRequestId;
        ActorId = actorId;
        Action = action;
        StatusBefore = statusBefore;
        StatusAfter = statusAfter;
        Details = detailsTrimmed;
        OccurredAt = occurredAt;
    }

}
