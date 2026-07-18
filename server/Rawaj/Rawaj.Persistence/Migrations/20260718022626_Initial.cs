using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MaxBrands = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxCampaignsMonthly = table.Column<int>(type: "int", nullable: false),
                    MaxAiCreditsMonthly = table.Column<int>(type: "int", nullable: false),
                    MaxScheduledPosts = table.Column<int>(type: "int", nullable: false),
                    MaxSocialAccounts = table.Column<int>(type: "int", nullable: false),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_subscription_plans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenants_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenants_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_brand_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandVoice = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BrandInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_brand_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_brand_profiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_invitations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_invitations_users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvitationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_members_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_members_users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggeredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InputParams = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputRefId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutputRefType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tokens = table.Column<int>(type: "int", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_jobs_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ai_jobs_users_TriggeredBy",
                        column: x => x.TriggeredBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialHandles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ragged = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitors_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marketing_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Objective = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetPlatforms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    BudgetCurrency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AiPlanJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CampaignType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    IsOnboardingComplete = table.Column<bool>(type: "bit", nullable: false),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CampaignGoal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignDuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignOutcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignBrief = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstagramHandle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessAge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandWords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandTone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasBrandGuidelines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuidelinesFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandColors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentLanguages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniqueValueProposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PricePositioning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StorePresence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingPlatforms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudienceGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgeRanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncomeLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Interests = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PainPoints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuyingBehavior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasExistingCustomers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudiencePlatforms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositioningVs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuccessMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandsAdmired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthlyBudget = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BudgetFrom = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    BudgetTo = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    PlatformRanking = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Goals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timeframe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgencyExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSales = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignPhotoUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hashtags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacebookConnected = table.Column<bool>(type: "bit", nullable: true),
                    FacebookAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstagramConnected = table.Column<bool>(type: "bit", nullable: true),
                    InstagramAccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategistAnswers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_campaigns_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_marketing_campaigns_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "social_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountIdExternal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshTokenEnc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_accounts_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_member_brand_access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_member_brand_access", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_member_brand_access_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_member_brand_access_tenant_members_TenantMemberId",
                        column: x => x.TenantMemberId,
                        principalTable: "tenant_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rag_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VectorId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CompetitorsData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rag_documents_competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rag_documents_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hashtags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cta = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AiPromptUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_items_marketing_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "marketing_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_items_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_items_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_items_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_items_users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Previous = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Current = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_revisions_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_revisions_users_RevisedBy",
                        column: x => x.RevisedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visual_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrandProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AiPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WidthPx = table.Column<int>(type: "int", nullable: true),
                    HeightPx = table.Column<int>(type: "int", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    VersionOf = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visual_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visual_assets_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visual_assets_marketing_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "marketing_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visual_assets_tenant_brand_profiles_BrandProfileId",
                        column: x => x.BrandProfileId,
                        principalTable: "tenant_brand_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visual_assets_visual_assets_VersionOf",
                        column: x => x.VersionOf,
                        principalTable: "visual_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisualAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SocialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiSuggestedTime = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PostId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_posts_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scheduled_posts_social_accounts_SocialAccountId",
                        column: x => x.SocialAccountId,
                        principalTable: "social_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scheduled_posts_visual_assets_VisualAssetId",
                        column: x => x.VisualAssetId,
                        principalTable: "visual_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: true),
                    Reach = table.Column<long>(type: "bigint", nullable: true),
                    Likes = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<int>(type: "int", nullable: true),
                    Shares = table.Column<int>(type: "int", nullable: true),
                    Saves = table.Column<int>(type: "int", nullable: true),
                    Clicks = table.Column<int>(type: "int", nullable: true),
                    EngagementRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_analytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_analytics_scheduled_posts_ScheduledPostId",
                        column: x => x.ScheduledPostId,
                        principalTable: "scheduled_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingCycle", "Cost", "CreatedAt", "Currency", "Features", "IsActive", "MaxAiCreditsMonthly", "MaxBrands", "MaxCampaignsMonthly", "MaxScheduledPosts", "MaxSocialAccounts", "MaxUsers", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "Monthly", 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", "[]", true, 50, 1, 5, 10, 1, 1, "Free" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_jobs_BrandProfileId",
                table: "ai_jobs",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_jobs_TriggeredBy",
                table: "ai_jobs",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_competitors_BrandProfileId",
                table: "competitors",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_BrandProfileId",
                table: "content_items",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_CampaignId",
                table: "content_items",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_CreatedBy",
                table: "content_items",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_ReviewedBy",
                table: "content_items",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_TenantId",
                table: "content_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_content_revisions_ContentItemId",
                table: "content_revisions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_content_revisions_RevisedBy",
                table: "content_revisions",
                column: "RevisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_logs_UserId",
                table: "logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_BrandProfileId",
                table: "marketing_campaigns",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_campaigns_CreatedBy",
                table: "marketing_campaigns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_analytics_ScheduledPostId",
                table: "post_analytics",
                column: "ScheduledPostId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_documents_BrandProfileId",
                table: "rag_documents",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_documents_CompetitorId",
                table: "rag_documents",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_ContentItemId",
                table: "scheduled_posts",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_SocialAccountId",
                table: "scheduled_posts",
                column: "SocialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_posts_VisualAssetId",
                table: "scheduled_posts",
                column: "VisualAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_social_accounts_BrandProfileId",
                table: "social_accounts",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Name",
                table: "subscription_plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_SubscriptionPlanId",
                table: "subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_brand_profiles_TenantId",
                table: "tenant_brand_profiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_Email",
                table: "tenant_invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_InvitedBy",
                table: "tenant_invitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_TenantId_Email",
                table: "tenant_invitations",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_member_brand_access_BrandProfileId",
                table: "tenant_member_brand_access",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_member_brand_access_TenantMemberId_BrandProfileId",
                table: "tenant_member_brand_access",
                columns: new[] { "TenantMemberId", "BrandProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_members_InvitedBy",
                table: "tenant_members",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_members_TenantId_UserId",
                table: "tenant_members",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_members_UserId",
                table: "tenant_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_OwnerUserId",
                table: "tenants",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Subdomain",
                table: "tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_SubscriptionId",
                table: "tenants",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_visual_assets_BrandProfileId",
                table: "visual_assets",
                column: "BrandProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_visual_assets_CampaignId",
                table: "visual_assets",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_visual_assets_ContentItemId",
                table: "visual_assets",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_visual_assets_VersionOf",
                table: "visual_assets",
                column: "VersionOf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_jobs");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "content_revisions");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "post_analytics");

            migrationBuilder.DropTable(
                name: "rag_documents");

            migrationBuilder.DropTable(
                name: "tenant_invitations");

            migrationBuilder.DropTable(
                name: "tenant_member_brand_access");

            migrationBuilder.DropTable(
                name: "scheduled_posts");

            migrationBuilder.DropTable(
                name: "competitors");

            migrationBuilder.DropTable(
                name: "tenant_members");

            migrationBuilder.DropTable(
                name: "social_accounts");

            migrationBuilder.DropTable(
                name: "visual_assets");

            migrationBuilder.DropTable(
                name: "content_items");

            migrationBuilder.DropTable(
                name: "marketing_campaigns");

            migrationBuilder.DropTable(
                name: "tenant_brand_profiles");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "subscription_plans");
        }
    }
}
