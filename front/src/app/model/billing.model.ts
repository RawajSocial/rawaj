/** Mirrors the backend's `Rawaj.Domain.Enums.BillingCycle` (serialized as a string). Only
 *  `Monthly` is ever seeded/used today — see the pricing sheet, which has no annual tier. */
export type BillingCycleValue = 'Monthly' | 'Yearly';

export type SubscriptionStatus = 'Active' | 'Cancelled' | 'PastDue' | 'Trialing';

export type BillingTransactionType = 'CoinPurchase' | 'PlanChange' | 'AddOnPurchase' | 'CoinGrant';

export type AddOnType = 'ExtraBrand' | 'ExtraMarketeer';

/** GET /subscriptions/plans — Rawaj.Application.Features.Billing.GetSubscriptionPlans.SubscriptionPlanSummary */
export interface SubscriptionPlanSummary {
  subscriptionPlanId: string;
  name: string;
  cost: number;
  billingCycle: BillingCycleValue;
  currency: string;
  maxBrands: number;
  maxUsers: number;
  maxCampaignsMonthly: number;
  maxAiCreditsMonthly: number;
  maxScheduledPosts: number;
  maxSocialAccounts: number;
  coinUsageDiscountPercent: number;
  monthlyCoinGrant: number;
  features: string[];
}

/** GET /subscriptions/me — Rawaj.Application.Features.Billing.GetSubscription.GetSubscriptionResponse */
export interface SubscriptionSummary {
  subscriptionId: string;
  subscriptionPlanId: string;
  planName: string;
  planCost: number;
  status: SubscriptionStatus;
  billingCycle: BillingCycleValue;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEndsAt: string | null;
}

/** GET /subscriptions/ai-credits — Rawaj.Application.Common.Policies.AiCreditsUsage */
export interface AiCreditsUsage {
  maxCreditsMonthly: number;
  usedThisMonth: number;
}

/** POST /subscriptions/change-plan request. `agencySize`/`servicesOffered` only need to be sent
 *  the first time a tenant leaves the Free plan. */
export interface ChangeSubscriptionPlanRequest {
  subscriptionPlanId: string;
  agencySize?: string;
  servicesOffered?: string[];
}

export interface ChangeSubscriptionPlanResponse {
  subscriptionId: string;
  planName: string;
  planCost: number;
  status: SubscriptionStatus;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  tenantType: 'Business' | 'Agency';
  newCoinBalance: number;
  coinsGranted: number;
}

export interface CoinPackageSummary {
  coinPackageId: string;
  name: string;
  coins: number;
  bonusCoins: number;
  priceUsd: number;
}

export interface PurchaseCoinsRequest {
  coinPackageId?: string;
  customCoins?: number;
}

export interface PurchaseCoinsResponse {
  coinsGranted: number;
  amountUsd: number;
  newCoinBalance: number;
}

export interface PurchaseAddOnResponse {
  type: AddOnType;
  amountUsd: number;
  extraBrandsPurchased: number;
  extraMarketeersPurchased: number;
}

export interface BillingTransactionSummary {
  id: string;
  type: BillingTransactionType;
  description: string;
  amountUsd: number;
  coinsGranted: number | null;
  createdAt: string;
}

export interface GetBillingHistoryResponse {
  items: BillingTransactionSummary[];
  totalCount: number;
}

/** GET /subscriptions/coin-pricing — the one source of coin/purchase pricing the frontend reads
 *  from, both for the public pricing page and per-button spend-preview captions. */
export interface CoinPriceList {
  contentGeneration: number;
  visualGeneration: number;
  campaignContentGeneration: number;
  marketingPlanGeneration: number;
  scheduling: number;
  /** AI Business Diagnosis (campaign onboarding). */
  businessDiagnosis: number;
  /** Competitor research (campaign onboarding) — charged only when it actually finds data. */
  competitiveAnalysis: number;
  /** Free-text strategy refinement ("عدّل الخطة"). */
  reasoningConversation: number;
}

export interface CoinPricing {
  baseCosts: CoinPriceList;
  discountedCosts: CoinPriceList;
  discountPercent: number;
  freeMarketingPlanAvailable: boolean;
  freeImageGenerationsRemaining: number;
  freeContentGenerationsRemaining: number;
  coinPackages: CoinPackageSummary[];
  customCoinPricePerCoin: number;
  extraBrandPriceUsd: number;
  extraMarketeerPriceUsd: number;
}

/** GET /subscriptions/public-coin-pricing — anonymous, backs the landing page + public pricing
 *  page. No per-tenant discount or free-trial state (those require a signed-in tenant). */
export interface PublicCoinPricing {
  baseCosts: CoinPriceList;
  coinPackages: CoinPackageSummary[];
  customCoinPricePerCoin: number;
  extraBrandPriceUsd: number;
  extraMarketeerPriceUsd: number;
}

export const BILLING_TRANSACTION_TYPE_LABELS: Record<BillingTransactionType, string> = {
  CoinPurchase: 'شراء كوينز',
  PlanChange: 'تغيير الباقة',
  AddOnPurchase: 'شراء إضافة',
  CoinGrant: 'مكافأة كوينز الباقة',
};
