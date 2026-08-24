namespace Rawaj.Application.Features.Campaigns.GenerateBusinessDiagnosis;

public record GenerateBusinessDiagnosisResponse(Guid CampaignId, string DiagnosisJson, DateTime DiagnosedAt);
