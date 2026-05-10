namespace Tickbox.Api.Controllers;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
