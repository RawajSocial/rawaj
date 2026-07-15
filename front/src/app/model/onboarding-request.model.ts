// ─────────────────────────────────────────────────────────────────────────────
// Onboarding Request Model
// Full payload sent to the backend after all 7 onboarding steps are complete.
// ─────────────────────────────────────────────────────────────────────────────

// ── Shared primitives ─────────────────────────────────────────────────────────

export interface SocialConnectionInfo {
  connected: boolean;
  accountName?: string;
}

export interface StrategistQA {
  question: string;
  answer: string;
}

// ── Step 2: Campaign brief — one sub-type per campaign scenario ───────────────

export type CampaignType =
  | 'new-business'
  | 'new-product'
  | 'drive-sales'
  | 'seasonal'
  | 'leads'
  | 'awareness'
  | 'other';

/** Fields shared by every campaign type (from brief-common) */
export interface CampaignBriefCommon {
  campaignType: CampaignType;
  /** Display name for the campaign, entered by user */
  campaignName?: string;
  campaignGoal?: string;
  campaignStartDate?: string;
  /** e.g. "شهر واحد", "3 أشهر" */
  campaignDuration?: string;
  /** High-level desired outcome, e.g. "زيادة المبيعات" */
  campaignOutcome?: string;
}

/** Extra fields for "new-business" campaign type */
export interface BriefNewBusiness {
  businessEstablishDate?: string;
  /** "ready" | "in-progress" | "not-yet" */
  brandIdentityReady?: string;
  businessLaunchDate?: string;
}

/** Extra fields for "new-product" campaign type */
export interface BriefNewProduct {
  productName?: string;
  productCategory?: string;
  /** "available" | "pre-order" | "coming-soon" */
  productAvailability?: string;
  /** e.g. "affordable" | "premium" | "luxury" */
  productPricePoint?: string;
}

/** Extra fields for "drive-sales" campaign type */
export interface BriefDriveSales {
  /** "online" | "offline" | "both" */
  salesScope?: string;
  /** "yes" | "no" */
  hasOffer?: string;
  offerDetails?: string;
  /** e.g. "weekend", "month-end", "always" */
  salesPeriod?: string;
}

/** Extra fields for "seasonal" campaign type */
export interface BriefSeasonal {
  /** Name of the occasion / event */
  occasion?: string;
  seasonStart?: string;
  seasonEnd?: string;
}

/** Extra fields for "leads" (generate leads) campaign type */
export interface BriefLeads {
  /** Desired lead action, e.g. "book appointment", "download guide" */
  leadAction?: string;
  /** "yes" | "no" */
  hasLandingPage?: string;
  landingPageUrl?: string;
  /** Brand awareness stage, e.g. "known" | "new" */
  brandStatusForLeads?: string;
  /** Desired content feel, e.g. "formal" | "casual" */
  contentFeeling?: string;
}

/** Extra fields for "awareness" campaign type */
export interface BriefAwareness {
  mainMessage?: string;
}

/** Extra fields for "other" campaign type */
export interface BriefOther {
  campaignDescription?: string;
}

/**
 * Union of all possible campaign brief payloads.
 * The `campaignType` discriminant field determines which extra fields are present.
 */
export type CampaignBrief =
  | (CampaignBriefCommon & BriefNewBusiness  & { campaignType: 'new-business' })
  | (CampaignBriefCommon & BriefNewProduct   & { campaignType: 'new-product'  })
  | (CampaignBriefCommon & BriefDriveSales   & { campaignType: 'drive-sales'  })
  | (CampaignBriefCommon & BriefSeasonal     & { campaignType: 'seasonal'     })
  | (CampaignBriefCommon & BriefLeads        & { campaignType: 'leads'        })
  | (CampaignBriefCommon & BriefAwareness    & { campaignType: 'awareness'    })
  | (CampaignBriefCommon & BriefOther        & { campaignType: 'other'        });

// ── Step 3: Brand & Identity ──────────────────────────────────────────────────

export interface BrandIdentity {
  brandName?: string;
  tagline?: string;
  /** Instagram handle */
  instagram?: string;
  website?: string;
  /** Business sector / industry */
  sector?: string;
  /** City or region */
  location?: string;
  /** How long the business has been running */
  businessAge?: string;
  /** Growth stage: "startup" | "growing" | "established" */
  stage?: string;
  /** Three personality words chosen by user */
  brandWords?: [string?, string?, string?];
  /** Tone tags: "formal", "friendly", "bold", etc. */
  brandTone?: string[];
  /** "yes" | "no" */
  hasGuidelines?: string;
  guidelinesFileUrl?: string;
  brandColors?: string[];
  logoFileUrl?: string;
  /** Content languages, e.g. ["ar", "en"] */
  languages?: string[];
  productDesc?: string;
  uniqueValue?: string;
  /** Positioning vs price: "economy" | "mid" | "premium" | "luxury" */
  pricePositioning?: string;
  /** "online" | "offline" | "both" */
  storePresence?: string;
  /** Currently active platforms */
  existingPlatforms?: string[];
}

// ── Step 4: Target Audience ───────────────────────────────────────────────────

export interface TargetAudience {
  /** "female" | "male" | "all" */
  gender?: 'female' | 'male' | 'all';
  /** "b2c" | "b2b" | "both" */
  customerType?: string;
  /** Age brackets, e.g. ["18-24", "25-34"] */
  ageRanges?: string[];
  /** Income brackets */
  incomeLevel?: string[];
  /** City or region */
  customerLocation?: string;
  /** Education level */
  educationLevel?: string;
  /** Free-text audience description */
  targetDescription?: string;
  /** Interest categories */
  interests?: string[];
  /** Core pain point or problem to solve */
  painPoints?: string;
  /** Buying behaviour tags */
  buyingBehavior?: string[];
  /** "yes" | "no" */
  hasExistingCustomers?: string;
  /** Platforms where audience lives */
  audiencePlatforms?: string[];
}

// ── Step 5: Strategy & Positioning ───────────────────────────────────────────

export interface Strategy {
  /** Key competitive differentiator or rival brand name */
  positioningVs?: string;
  /** Desired campaign outcome description */
  campaignOutcome?: string;
  /** KPI tags: "followers", "sales", "leads", etc. */
  successMetrics?: string[];
  /** Up to 3 admired brands */
  brandsAdmired?: [string?, string?, string?];
  /** Monthly budget range as a string, e.g. "5000-10000" */
  monthlyBudget?: string;
  /** Parsed budget min (SAR) — derived from monthlyBudget or set directly */
  budgetFrom?: number;
  /** Parsed budget max (SAR) */
  budgetTo?: number;
  /** Ordered platform slugs, e.g. ["instagram", "facebook"] */
  platformRanking?: string[];
  /** Legacy goals list (kept for backward compat with plan builder) */
  goals?: string[];
  /** Desired number of months */
  timeframe?: string;
  /** "experienced" | "new" | "none" */
  agencyExperience?: string;
  targetSales?: string;
}

// ── Step 6: Campaign Assets ───────────────────────────────────────────────────

export interface CampaignAssets {
  /** URLs of uploaded campaign photos */
  campaignPhotoUrls?: string[];
  /** Raw hashtag string entered by user */
  hashtags?: string;
  additionalNotes?: string;
}

// ── Step 6 (social connections) — also in this step ─────────────────────────

export interface SocialConnections {
  facebook?:  SocialConnectionInfo;
  instagram?: SocialConnectionInfo;
}

// ── Step 7: AI Strategist Q&A ─────────────────────────────────────────────────

export interface AIStrategistSession {
  /**
   * All question–answer pairs from the AI strategist chat.
   * Questions are generated dynamically but the 5 default questions are:
   * 1. Competitors
   * 2. Biggest marketing challenge
   * 3. How customers currently find you
   * 4. Influencer marketing experience
   * 5. Any additional details
   */
  answers: StrategistQA[];
}

// ── Root request object ───────────────────────────────────────────────────────

/**
 * Full onboarding request payload.
 * Sent to POST /api/onboarding (or equivalent) once the user approves the plan.
 */
export interface OnboardingRequest {
  // ── Meta ──────────────────────────────────────────────────────────────────
  /** Unique plan identifier generated on the client (UUID or timestamp-based) */
  planId: string;
  /** ISO timestamp of submission */
  submittedAt: string;

  // ── Step 1 (handled by step 1 component — account type selection)
  /** Account / subscription type selected in step 1 */
  accountType?: string;

  // ── Step 2 — Campaign brief (discriminated union by campaignType)
  campaign: CampaignBrief;

  // ── Step 3 — Brand & identity
  brand: BrandIdentity;

  // ── Step 4 — Target audience
  audience: TargetAudience;

  // ── Step 5 — Strategy & positioning
  strategy: Strategy;

  // ── Step 6 — Assets + social connections
  assets: CampaignAssets;
  socialConnections: SocialConnections;

  // ── Step 7 — AI strategist interview
  aiStrategist: AIStrategistSession;
}

// ── Builder helper ────────────────────────────────────────────────────────────

/**
 * Maps the flat `OnboardingData` (as stored in localStorage) to the
 * structured `OnboardingRequest` ready for the backend.
 */
export function buildOnboardingRequest(
  planId: string,
  data: Record<string, unknown>,
): OnboardingRequest {
  const d = data as Record<string, unknown>;
  const str = <T>(k: string): T | undefined => d[k] as T | undefined;
  const arr = <T>(k: string): T[] => (d[k] as T[] | undefined) ?? [];

  return {
    planId,
    submittedAt: new Date().toISOString(),

    accountType: str<string>('accountType'),

    campaign: {
      campaignType:     (str<string>('campaignType') ?? 'other') as CampaignType,
      campaignName:     str<string>('campaignName'),
      campaignGoal:     str<string>('campaignGoal'),
      campaignStartDate: str<string>('campaignStartDate'),
      campaignDuration: str<string>('campaignDuration'),
      campaignOutcome:  str<string>('campaignOutcome'),
      // new-business
      businessEstablishDate: str<string>('businessEstablishDate'),
      brandIdentityReady:    str<string>('brandIdentityReady'),
      businessLaunchDate:    str<string>('businessLaunchDate'),
      // new-product
      productName:         str<string>('productName'),
      productCategory:     str<string>('productCategory'),
      productAvailability: str<string>('productAvailability'),
      productPricePoint:   str<string>('productPricePoint'),
      // drive-sales
      salesScope:  str<string>('salesScope'),
      hasOffer:    str<string>('hasOffer'),
      offerDetails: str<string>('offerDetails'),
      salesPeriod: str<string>('salesPeriod'),
      // seasonal
      occasion:    str<string>('occasion'),
      seasonStart: str<string>('seasonStart'),
      seasonEnd:   str<string>('seasonEnd'),
      // leads
      leadAction:          str<string>('leadAction'),
      hasLandingPage:      str<string>('hasLandingPage'),
      landingPageUrl:      str<string>('landingPageUrl'),
      brandStatusForLeads: str<string>('brandStatusForLeads'),
      contentFeeling:      str<string>('contentFeeling'),
      // awareness
      mainMessage: str<string>('mainMessage'),
      // other
      campaignDescription: str<string>('campaignDescription'),
    } as CampaignBrief,

    brand: {
      brandName:         str<string>('brandName'),
      tagline:           str<string>('tagline'),
      instagram:         str<string>('instagram'),
      website:           str<string>('website'),
      sector:            str<string>('sector'),
      location:          str<string>('location'),
      businessAge:       str<string>('businessAge'),
      stage:             str<string>('stage'),
      brandWords:        [str<string>('brandWord1'), str<string>('brandWord2'), str<string>('brandWord3')],
      brandTone:         arr<string>('brandTone'),
      hasGuidelines:     str<string>('hasGuidelines'),
      guidelinesFileUrl: str<string>('guidelinesFile'),
      brandColors:       arr<string>('brandColors'),
      logoFileUrl:       str<string>('logoFile'),
      languages:         arr<string>('languages'),
      productDesc:       str<string>('productDesc'),
      uniqueValue:       str<string>('uniqueValue'),
      pricePositioning:  str<string>('pricePositioning'),
      storePresence:     str<string>('storePresence'),
      existingPlatforms: arr<string>('existingPlatforms'),
    },

    audience: {
      gender:              str<'female' | 'male' | 'all'>('gender'),
      customerType:        str<string>('customerType'),
      ageRanges:           arr<string>('ageRanges'),
      incomeLevel:         arr<string>('incomeLevel'),
      customerLocation:    str<string>('customerLocation'),
      educationLevel:      str<string>('educationLevel'),
      targetDescription:   str<string>('targetDescription'),
      interests:           arr<string>('interests'),
      painPoints:          str<string>('painPoints'),
      buyingBehavior:      arr<string>('buyingBehavior'),
      hasExistingCustomers: str<string>('hasExistingCustomers'),
      audiencePlatforms:   arr<string>('audiencePlatforms'),
    },

    strategy: {
      positioningVs:   str<string>('positioningVs'),
      campaignOutcome: str<string>('campaignOutcome'),
      successMetrics:  arr<string>('successMetrics'),
      brandsAdmired:   [str<string>('brandAdmire1'), str<string>('brandAdmire2'), str<string>('brandAdmire3')],
      monthlyBudget:   str<string>('monthlyBudget'),
      budgetFrom:      d['budgetFrom'] as number | undefined,
      budgetTo:        d['budgetTo']   as number | undefined,
      platformRanking: arr<string>('platformRanking'),
      goals:           arr<string>('goals'),
      timeframe:       str<string>('timeframe'),
      agencyExperience: str<string>('agencyExperience'),
      targetSales:     str<string>('targetSales'),
    },

    assets: {
      campaignPhotoUrls: arr<string>('campaignPhotos'),
      hashtags:          str<string>('hashtags'),
      additionalNotes:   str<string>('additionalNotes'),
    },

    socialConnections: {
      facebook:  (d['socialConnections'] as Record<string, SocialConnectionInfo> | undefined)?.['facebook'],
      instagram: (d['socialConnections'] as Record<string, SocialConnectionInfo> | undefined)?.['instagram'],
    },

    aiStrategist: {
      answers: arr<StrategistQA>('strategistAnswers'),
    },
  };
}
