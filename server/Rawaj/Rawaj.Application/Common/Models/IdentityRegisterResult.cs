namespace Rawaj.Application.Common.Models;

public class IdentityRegisterResult
{
    public bool Succeeded { get; }
    public Guid UserId { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private IdentityRegisterResult(bool succeeded, Guid userId, IReadOnlyCollection<string> errors)
    {
        Succeeded = succeeded;
        UserId = userId;
        Errors = errors;
    }

    public static IdentityRegisterResult Success(Guid userId) => new(true, userId, []);

    public static IdentityRegisterResult Failure(IReadOnlyCollection<string> errors) => new(false, Guid.Empty, errors);
}
