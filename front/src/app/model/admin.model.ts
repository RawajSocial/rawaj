export type TenantAccountKind = 'agency' | 'business';
export type TenantStatus = 'active' | 'trial' | 'suspended';
export type PlatformUserStatus = 'active' | 'pending' | 'suspended';

export interface PlatformTenant {
  id: string;
  name: string;
  kind: TenantAccountKind;
  ownerName: string;
  ownerEmail: string;
  plan: string;
  status: TenantStatus;
  usersCount: number;
  brandProfilesCount: number;
  createdAt: string;
}

export interface PlatformUser {
  id: string;
  name: string;
  email: string;
  tenantName: string;
  tenantId: string;
  role: string;
  status: PlatformUserStatus;
  joinDate: string;
  lastActiveAt: string;
}

export interface SubscriptionPlan {
  id: string;
  name: string;
  price: number;
  billingCycle: 'monthly' | 'yearly';
  maxBrands: number;
  maxUsers: number;
  subscriberCount: number;
}

export const TENANT_KIND_LABELS: Record<TenantAccountKind, string> = {
  agency: 'وكالة',
  business: 'صاحب علامة تجارية',
};

export const TENANT_STATUS_LABELS: Record<TenantStatus, string> = {
  active: 'نشط',
  trial: 'تجريبي',
  suspended: 'موقوف',
};

export const PLATFORM_USER_STATUS_LABELS: Record<PlatformUserStatus, string> = {
  active: 'نشط',
  pending: 'بانتظار القبول',
  suspended: 'موقوف',
};
