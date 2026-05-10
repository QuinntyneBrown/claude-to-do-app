using FluentValidation;
using FluentValidation.Results;

namespace Tickbox.Application.Common;

/// <summary>
/// Helper that translates a single property/message into a FluentValidation
/// <see cref="ValidationException"/> so it surfaces as HTTP 400 application/problem+json
/// through the existing ExceptionToProblemDetailsMiddleware.
/// </summary>
public sealed class ValidationFailureException : ValidationException
{
    public ValidationFailureException(string propertyName, string message)
        : base(new[] { new ValidationFailure(propertyName, message) })
    {
    }
}
