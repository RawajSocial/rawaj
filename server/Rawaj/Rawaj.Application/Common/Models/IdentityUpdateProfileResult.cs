namespace Rawaj.Application.Common.Models;

public class IdentityUpdateProfileResult
{
    public bool Succeeded { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private IdentityUpdateProfileResult(bool succeeded, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static IdentityUpdateProfileResult Success() => new(true, []);

    public static IdentityUpdateProfileResult Failure(IReadOnlyCollection<string> errors) => new(false, errors);
}
