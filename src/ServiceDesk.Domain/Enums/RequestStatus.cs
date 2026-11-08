namespace ServiceDesk.Domain.Enums;

/// <summary>Submitted -> InReview -> Approved -> Fulfilled; Rejected or Cancelled before approval.</summary>
public enum RequestStatus
{
    Submitted = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    Fulfilled = 4,
    Cancelled = 5,
}