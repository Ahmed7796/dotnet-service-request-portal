using System.ComponentModel.DataAnnotations;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.DTOs;

public sealed record RejectDto([Required, MaxLength(ApprovalStep.CommentMaxLength)] string Reason);
