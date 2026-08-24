using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IAiTextGenerationService
{
    /// <summary>
    /// Generates text. <paramref name="options"/> is optional and its defaults reproduce the
    /// behaviour every existing caller had before it existed — a call that passes nothing is
    /// unchanged.
    /// </summary>
    Task<AiTextGenerationResult> GenerateTextAsync(
        string prompt, CancellationToken cancellationToken, AiTextGenerationOptions? options = null);
}
