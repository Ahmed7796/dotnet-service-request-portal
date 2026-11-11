using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Api.ErrorHandling;

/// <summary>
/// Maps known exceptions to Problem Details. Anything unmapped returns false and falls through to a generic 500.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        (int Status, string Title)? mapping = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid input"),
            ApprovalNotAllowedException => (StatusCodes.Status403Forbidden, "Action not allowed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            InvalidStateTransitionException => (StatusCodes.Status409Conflict, "Invalid state transition"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            _ => null,
        };

        if (mapping is null)
            return false;

        httpContext.Response.StatusCode = mapping.Value.Status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapping.Value.Status,
                Title = mapping.Value.Title,
                Detail = exception.Message,
            },
        });
    }
}
