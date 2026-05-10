namespace Tickbox.Api.Controllers;

public sealed record CompletePasswordResetRequest(string Token, string NewPassword);
