using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tickbox.Application.Common;

namespace Tickbox.Api.Filters;

public sealed class ExceptionToProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionToProblemDetailsMiddleware> _logger;

    public ExceptionToProblemDetailsMiddleware(RequestDelegate next, ILogger<ExceptionToProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ProblemDetailsFactory problemDetailsFactory)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var modelState = new ModelStateDictionary();
            foreach (var failure in ex.Errors)
            {
                modelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            var problem = problemDetailsFactory.CreateValidationProblemDetails(context, modelState, StatusCodes.Status400BadRequest);
            await WriteProblemAsync(context, problem, StatusCodes.Status400BadRequest);
        }
        catch (ConflictException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
        }
        catch (AuthenticationFailedException ex)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Server error", "DEBUG: " + ex.GetType().FullName + ": " + ex.Message);
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        await WriteProblemAsync(context, problem, statusCode);
    }

    private static async Task WriteProblemAsync(HttpContext context, ProblemDetails problem, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
