namespace Tickbox.Api.Controllers;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
