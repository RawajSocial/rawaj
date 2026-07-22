export type TenantType = 'Business' | 'Agency';
export type TenantMemberRole = 'Owner' | 'Admin' | 'Editor' | 'Viewer';

export const TENANT_MEMBER_ROLE_LABELS: Record<TenantMemberRole, string> = {
  Owner: 'مالك',
  Admin: 'مدير',
  Editor: 'محرر',
  Viewer: 'مشاهد',
};

export interface TenantSummary {
  tenantId: string;
  name: string;
  subdomain: string;
  tenantType: TenantType;
  role: TenantMemberRole;
  isActive: boolean;
  coinBalance: number;
  isActivated: boolean;
  planName: string;
  maxBrands: number;
  brandProfileCount: number;
  defaultBrandProfileId: string | null;
  phone: string | null;
  industry: string | null;
  country: string | null;
  city: string | null;
  website: string | null;
  agencySize: string | null;
  servicesOffered: string[];
}

export interface UpdateTenantProfileRequest {
  phone?: string;
  industry?: string;
  country?: string;
  city?: string;
  website?: string;
}

export interface UpdateTenantProfileResponse {
  tenantId: string;
  isActivated: boolean;
  coinBalance: number;
  activationRewardGranted: boolean;
}

export interface UpgradeToAgencyRequest {
  agencySize: string;
  servicesOffered: string[];
  phone?: string;
  industry?: string;
  country?: string;
  city?: string;
  website?: string;
}

export interface UpgradeToAgencyResponse {
  tenantId: string;
  tenantType: TenantType;
  planName: string;
  maxBrands: number;
}

export const AGENCY_UPGRADE_REQUIRED_SENTINEL = 'AGENCY_UPGRADE_REQUIRED';
