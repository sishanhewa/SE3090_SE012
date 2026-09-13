using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.DTOs.Common;
using TalentFlow.Domain.Exceptions;

namespace TalentFlow.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Catches domain exceptions and translates them to standardized API error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            NotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                new ErrorResponse { Error = ex.Message, ErrorCode = "NOT_FOUND" }
            ),
            BusinessConflictException ex => (
                (int)HttpStatusCode.Conflict,
                new ErrorResponse { Error = ex.Message, ErrorCode = ex.Code }
            ),
            ForbiddenException ex => (
                (int)HttpStatusCode.Forbidden,
                new ErrorResponse { Error = ex.Message, ErrorCode = "FORBIDDEN" }
            ),
            UnauthorizedAccessException ex => (
                (int)HttpStatusCode.Unauthorized,
                new ErrorResponse { Error = ex.Message, ErrorCode = "UNAUTHORIZED" }
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                new ErrorResponse { Error = "An unexpected error occurred.", ErrorCode = "INTERNAL_ERROR" }
            )
        };

        // Log the exception with appropriate level
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
