using CleanArchitecture.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace CleanArchitecture.WebAPI.Middlewares;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                CreateProblemDetails(StatusCodes.Status404NotFound, "Not Found", notFound.Message)),

            DomainException domain => (
                StatusCodes.Status400BadRequest,
                CreateProblemDetails(StatusCodes.Status400BadRequest, "Domain Error", domain.Message)),

            ValidationException validation => (
                StatusCodes.Status422UnprocessableEntity,
                CreateValidationProblemDetails(validation)),

            UnauthorizedAccessException unauthorized => (
                StatusCodes.Status401Unauthorized,
                CreateProblemDetails(StatusCodes.Status401Unauthorized, "Unauthorized", unauthorized.Message)),

            _ => (
                StatusCodes.Status500InternalServerError,
                CreateProblemDetails(StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred."))
        };

        problemDetails.Instance = context.Request.Path;
        problemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static ProblemDetails CreateProblemDetails(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred."
        };
    }
}
