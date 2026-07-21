namespace Rawaj.Application.Common.Models;

public class AiImageGenerationResult
{
    public bool Succeeded { get; }
    public byte[]? ImageBytes { get; }
    public string? ContentType { get; }
    public string? ErrorMessage { get; }

    private AiImageGenerationResult(bool succeeded, byte[]? imageBytes, string? contentType, string? errorMessage)
    {
        Succeeded = succeeded;
        ImageBytes = imageBytes;
        ContentType = contentType;
        ErrorMessage = errorMessage;
    }

    public static AiImageGenerationResult Success(byte[] imageBytes, string contentType) =>
        new(true, imageBytes, contentType, null);

    public static AiImageGenerationResult Failure(string errorMessage) => new(false, null, null, errorMessage);
}
