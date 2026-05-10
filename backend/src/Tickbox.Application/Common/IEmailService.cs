namespace Tickbox.Application.Common;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken);
    Task SendEmailChangeVerificationAsync(string email, string token, CancellationToken cancellationToken);
}
