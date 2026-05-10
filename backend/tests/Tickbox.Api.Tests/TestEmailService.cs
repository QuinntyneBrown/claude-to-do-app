using Tickbox.Application.Common;

namespace Tickbox.Api.Tests;

public sealed class TestEmailService : IEmailService
{
    public static string? LastResetToken { get; private set; }
    public static string? LastResetEmail { get; private set; }
    public static string? LastEmailChangeToken { get; private set; }
    public static string? LastEmailChangeAddress { get; private set; }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
    {
        LastResetEmail = email;
        LastResetToken = token;
        return Task.CompletedTask;
    }

    public Task SendEmailChangeVerificationAsync(string email, string token, CancellationToken cancellationToken)
    {
        LastEmailChangeAddress = email;
        LastEmailChangeToken = token;
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        LastResetToken = null;
        LastResetEmail = null;
        LastEmailChangeToken = null;
        LastEmailChangeAddress = null;
    }
}
