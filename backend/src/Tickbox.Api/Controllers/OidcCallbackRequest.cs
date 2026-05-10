namespace Tickbox.Api.Controllers;

public sealed record OidcCallbackRequest(string Code, string State);
