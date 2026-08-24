using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

/// <summary>Fire-and-track, same contract as <c>StartRunResponse</c>: the batch hasn't necessarily
/// produced anything yet by the time this returns — <see cref="RunId"/> is what the caller polls
/// (or listens for over SignalR) to watch it progress, the same way every other pipeline action
/// already works. Returning only once every post and image was ready meant the whole batch appeared
/// on the page at once, after the slowest image, instead of each post as its own image finished.</summary>
public record GenerateCampaignContentResponse(Guid CampaignId, Guid RunId, AiPipelineRunStatus Status);
