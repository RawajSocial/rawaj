using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IAiTextGenerationService
{
    Task<AiTextGenerationResult> GenerateTextAsync(string prompt, CancellationToken cancellationToken);
}
