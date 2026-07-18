using Rawaj.Domain.Common;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;
using Rawaj.Domain.ValueObjects;
using Rawaj.Domain.ValueObjects.CampaignBriefs;

namespace Rawaj.Domain.Entities.Campaigns;

public class MarketingCampaign : BaseEntity
{
    public Guid BrandProfileId { get; set; }
    public Guid CreatedBy { get; set; }
    public string? Name { get; set; }
    public string? Objective { get; set; }
    public List<string> TargetPlatforms { get; set; } = [];
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? BudgetCurrency { get; set; }
    public CampaignStatus Status { get; set; }
    public string? AiPlanJson { get; set; }
    public DateTime? AiGeneratedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Onboarding tracking
    public string CampaignType { get; set; } = null!;
    public int CurrentStep { get; set; }
    public bool IsOnboardingComplete { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }

    // Step 2 — common fields (shared, typed). Type-specific fields live in CampaignBrief (JSON).
    public string? CampaignGoal { get; set; }
    public string? CampaignDuration { get; set; }
    public string? CampaignOutcome { get; set; }
    public CampaignBrief? CampaignBrief { get; set; }

    // Step 3 — Brand & Identity
    public string? BrandName { get; set; }
    public string? Tagline { get; set; }
    public string? InstagramHandle { get; set; }
    public string? Website { get; set; }
    public string? Sector { get; set; }
    public string? Location { get; set; }
    public string? BusinessAge { get; set; }
    public string? Stage { get; set; }
    public List<string>? BrandWords { get; set; }
    public List<string>? BrandTone { get; set; }
    public string? HasBrandGuidelines { get; set; }
    public string? GuidelinesFileUrl { get; set; }
    public List<string>? BrandColors { get; set; }
    public string? LogoFileUrl { get; set; }
    public List<string>? ContentLanguages { get; set; }
    public string? ProductDescription { get; set; }
    public string? UniqueValueProposition { get; set; }
    public string? PricePositioning { get; set; }
    public string? StorePresence { get; set; }
    public List<string>? ExistingPlatforms { get; set; }

    // Step 4 — Target Audience
    public string? AudienceGender { get; set; }
    public string? CustomerType { get; set; }
    public List<string>? AgeRanges { get; set; }
    public List<string>? IncomeLevel { get; set; }
    public string? CustomerLocation { get; set; }
    public string? EducationLevel { get; set; }
    public string? TargetDescription { get; set; }
    public List<string>? Interests { get; set; }
    public string? PainPoints { get; set; }
    public List<string>? BuyingBehavior { get; set; }
    public string? HasExistingCustomers { get; set; }
    public List<string>? AudiencePlatforms { get; set; }

    // Step 5 — Strategy & Positioning
    public string? PositioningVs { get; set; }
    public List<string>? SuccessMetrics { get; set; }
    public List<string>? BrandsAdmired { get; set; }
    public string? MonthlyBudget { get; set; }
    public decimal? BudgetFrom { get; set; }
    public decimal? BudgetTo { get; set; }
    public List<string>? PlatformRanking { get; set; }
    public List<string>? Goals { get; set; }
    public string? Timeframe { get; set; }
    public string? AgencyExperience { get; set; }
    public string? TargetSales { get; set; }

    // Step 6 — Assets + Social Connections
    public List<string>? CampaignPhotoUrls { get; set; }
    public string? Hashtags { get; set; }
    public string? AdditionalNotes { get; set; }
    public bool? FacebookConnected { get; set; }
    public string? FacebookAccountName { get; set; }
    public bool? InstagramConnected { get; set; }
    public string? InstagramAccountName { get; set; }

    // Step 7 — AI Strategist Q&A
    public List<StrategistQA>? StrategistAnswers { get; set; }

    public TenantBrandProfile BrandProfile { get; set; } = null!;
    public ICollection<ContentItem> ContentItems { get; set; } = [];
    public ICollection<VisualAsset> VisualAssets { get; set; } = [];

    public static class CampaignTypes
    {
        public const string NewBusiness = "new-business";
        public const string NewProduct = "new-product";
        public const string DriveSales = "drive-sales";
        public const string Seasonal = "seasonal";
        public const string Leads = "leads";
        public const string Awareness = "awareness";
        public const string Other = "other";

        public static readonly string[] All =
            [NewBusiness, NewProduct, DriveSales, Seasonal, Leads, Awareness, Other];
    }
}
