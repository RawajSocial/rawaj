using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IAiImageGenerationService
{
    Task<AiImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken);
}
