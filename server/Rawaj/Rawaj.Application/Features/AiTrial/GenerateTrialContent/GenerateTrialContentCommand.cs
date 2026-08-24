using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialContent;

public record GenerateTrialContentCommand(string Prompt) : IRequest<Result<GenerateTrialContentResponse>>;
