using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IOAuthStateStore
{
    string Create(OAuthStateContext context);

    /// <summary>
    /// Retrieves and invalidates the context for a state token. Returns null if the state
    /// is unknown, expired, or has already been consumed (states are single-use).
    /// </summary>
    OAuthStateContext? Consume(string state);
}
