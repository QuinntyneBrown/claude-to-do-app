using Microsoft.Extensions.Logging;
using Tickbox.Application.Common;

namespace Tickbox.Infrastructure.Email;

/// <summary>
/// No-op email service for environments without a real provider. Logs the intended action
/// (without the plaintext token) so the deferred integration is visible. The token can be
/// retrieved from the database in dev / test when needed.
/// </summary>
public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[no-op email] would send password-reset to {Email}", email);
        return Task.CompletedTask;
    }

    public Task SendEmailChangeVerificationAsync(string email, string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[no-op email] would send email-change-verification to {Email}", email);
        return Task.CompletedTask;
    }
}
