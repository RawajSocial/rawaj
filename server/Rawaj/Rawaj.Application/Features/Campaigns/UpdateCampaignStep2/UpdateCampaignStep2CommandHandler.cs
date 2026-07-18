using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.Common;
using Rawaj.Application.Features.Campaigns.Common.CampaignBriefRequests;
using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.ValueObjects.CampaignBriefs;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaignStep2;

public class UpdateCampaignStep2CommandHandler(
    ICampaignOnboardingService onboardingService,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCampaignStep2Command, Result<CampaignResponse>>
{
    public async Task<Result<CampaignResponse>> Handle(UpdateCampaignStep2Command request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return Result<CampaignResponse>.Failure("Not authenticated.");
        }

        var campaign = await onboardingService.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return Result<CampaignResponse>.Failure("Campaign not found.");
        }

        var hasAccess = await onboardingService.UserHasBrandAccessAsync(userId, campaign.BrandProfileId, cancellationToken);
        if (!hasAccess)
        {
            return Result<CampaignResponse>.Failure("You do not have access to this campaign.");
        }

        if (!CampaignOnboardingStepRules.CanApplyStep(campaign.CurrentStep, 2))
        {
            return Result<CampaignResponse>.Failure("Cannot skip ahead. Complete previous steps first.");
        }

        var briefMatchesType = campaign.CampaignType switch
        {
            MarketingCampaign.CampaignTypes.NewBusiness => request.Brief is NewBusinessCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.NewProduct => request.Brief is NewProductCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.DriveSales => request.Brief is DriveSalesCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.Seasonal => request.Brief is SeasonalCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.Leads => request.Brief is LeadsCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.Awareness => request.Brief is AwarenessCampaignBriefRequest,
            MarketingCampaign.CampaignTypes.Other => request.Brief is OtherCampaignBriefRequest,
            _ => false,
        };

        if (!briefMatchesType)
        {
            return Result<CampaignResponse>.Failure("Campaign brief type does not match the campaign's type.");
        }

        if (request.CampaignName is not null)
        {
            campaign.Name = request.CampaignName;
        }

        campaign.CampaignGoal = request.CampaignGoal;
        campaign.StartDate = request.CampaignStartDate;
        campaign.CampaignDuration = request.CampaignDuration;
        campaign.CampaignOutcome = request.CampaignOutcome;

        campaign.CampaignBrief = request.Brief switch
        {
            NewBusinessCampaignBriefRequest b => new NewBusinessCampaignBrief
            {
                BusinessEstablishDate = b.BusinessEstablishDate,
                BrandIdentityReady = b.BrandIdentityReady,
                BusinessLaunchDate = b.BusinessLaunchDate,
            },
            NewProductCampaignBriefRequest b => new NewProductCampaignBrief
            {
                ProductName = b.ProductName,
                ProductCategory = b.ProductCategory,
                ProductAvailability = b.ProductAvailability,
                ProductPricePoint = b.ProductPricePoint,
            },
            DriveSalesCampaignBriefRequest b => new DriveSalesCampaignBrief
            {
                SalesScope = b.SalesScope,
                HasOffer = b.HasOffer,
                OfferDetails = b.OfferDetails,
                SalesPeriod = b.SalesPeriod,
            },
            SeasonalCampaignBriefRequest b => new SeasonalCampaignBrief
            {
                Occasion = b.Occasion,
                SeasonStart = b.SeasonStart,
                SeasonEnd = b.SeasonEnd,
            },
            LeadsCampaignBriefRequest b => new LeadsCampaignBrief
            {
                LeadAction = b.LeadAction,
                HasLandingPage = b.HasLandingPage,
                LandingPageUrl = b.LandingPageUrl,
                BrandStatusForLeads = b.BrandStatusForLeads,
                ContentFeeling = b.ContentFeeling,
            },
            AwarenessCampaignBriefRequest b => new AwarenessCampaignBrief { MainMessage = b.MainMessage },
            OtherCampaignBriefRequest b => new OtherCampaignBrief { CampaignDescription = b.CampaignDescription },
            _ => throw new InvalidOperationException($"Unsupported campaign brief request type: {request.Brief.GetType().Name}"),
        };

        CampaignOnboardingStepRules.AdvanceStep(campaign, 2);
        campaign.UpdatedAt = DateTime.UtcNow;

        await onboardingService.SaveChangesAsync(cancellationToken);

        return Result<CampaignResponse>.Success(CampaignResponse.FromEntity(campaign));
    }
}
