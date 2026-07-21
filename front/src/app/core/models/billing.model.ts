import { BillingCycle, SubscriptionStatus } from './enums';

export interface SubscriptionPlanSummary {
  subscriptionPlanId: string;
  name: string;
  cost: number;
  billingCycle: BillingCycle;
  currency: string;
  maxBrands: number;
  maxUsers: number;
  maxCampaignsMonthly: number;
  maxAiCreditsMonthly: number;
  maxScheduledPosts: number;
  maxSocialAccounts: number;
  features: string[];
}

export interface CurrentSubscription {
  subscriptionId: string;
  planName: string;
  planCost: number;
  status: SubscriptionStatus;
  billingCycle: BillingCycle;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEndsAt: string | null;
}

export interface ChangeSubscriptionPlanResponse {
  subscriptionId: string;
  planName: string;
  planCost: number;
  status: SubscriptionStatus;
  currentPeriodStart: string;
  currentPeriodEnd: string;
}

export interface AiCreditsUsage {
  maxCreditsMonthly: number;
  usedThisMonth: number;
  hasCreditsRemaining: boolean;
}
