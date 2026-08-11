const PLAN_FEATURE_LABELS: Record<string, string> = {
  priority_support: 'دعم فني ذو أولوية',
  advanced_analytics: 'تحليلات متقدمة',
  dedicated_account_manager: 'مدير حساب مخصص',
};

/** Subscription-plan `features` are stored as backend keys (e.g. "priority_support"); this maps
 *  them to their Arabic display label, falling back to the raw key for anything not yet mapped. */
export function planFeatureLabel(key: string): string {
  return PLAN_FEATURE_LABELS[key] ?? key;
}
