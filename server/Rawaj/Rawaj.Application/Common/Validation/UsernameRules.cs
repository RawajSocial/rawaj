namespace Rawaj.Application.Common.Validation;

/// <summary>
/// Single source of truth for username format constraints, shared by registration and profile-edit
/// validators so the two can never drift out of sync.
/// </summary>
public static class UsernameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 30;
    public const string Pattern = "^[a-zA-Z][a-zA-Z0-9_]*$";
    public const string PatternErrorMessage = "Username must start with a letter and contain only letters, numbers, or underscores.";
}
