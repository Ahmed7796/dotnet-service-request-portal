using System.ComponentModel.DataAnnotations;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs;

public sealed record SubmitServiceRequestDto(
    [Required] RequestType Type,
    [Required, MaxLength(ServiceRequest.TitleMaxLength)] string Title,
    [Required, MaxLength(ServiceRequest.DescriptionMaxLength)] string Description
);
