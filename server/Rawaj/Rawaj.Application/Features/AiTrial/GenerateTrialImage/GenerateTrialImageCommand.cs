using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialImage;

public record GenerateTrialImageCommand(string Prompt) : IRequest<Result<GenerateTrialImageResponse>>;
