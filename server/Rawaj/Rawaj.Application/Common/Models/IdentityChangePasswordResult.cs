namespace Rawaj.Application.Common.Models;

public class IdentityChangePasswordResult
{
    public bool Succeeded { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private IdentityChangePasswordResult(bool succeeded, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static IdentityChangePasswordResult Success() => new(true, []);

    public static IdentityChangePasswordResult Failure(IReadOnlyCollection<string> errors) => new(false, errors);
}
