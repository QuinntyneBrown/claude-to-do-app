namespace Tickbox.Domain;

public enum SecurityAuditKind
{
    SignInFailed = 0,
    SignInLocked = 1,
    PasswordChanged = 2,
    PasswordResetRequested = 3,
    PasswordResetUsed = 4,
    AccountDeleted = 5
}
