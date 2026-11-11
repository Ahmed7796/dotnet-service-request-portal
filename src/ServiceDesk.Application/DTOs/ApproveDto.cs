using System.ComponentModel.DataAnnotations;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.DTOs;

public sealed record ApproveDto([MaxLength(ApprovalStep.CommentMaxLength)] string? Comment);
