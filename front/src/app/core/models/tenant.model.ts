import { TenantMemberRole, TenantType } from './enums';

export interface MyTenant {
  tenantId: string;
  name: string;
  subdomain: string;
  tenantType: TenantType;
  role: TenantMemberRole;
  isActive: boolean;
}

export interface CreateTenantRequest {
  name: string;
  subdomain: string;
  tenantType: TenantType;
}

export interface CreateTenantResponse {
  tenantId: string;
  name: string;
  subdomain: string;
  tenantType: TenantType;
  subscriptionId: string;
}

export interface TeamMemberSummary {
  tenantMemberId: string;
  userId: string;
  email: string;
  fullName: string;
  role: TenantMemberRole;
  joinedAt: string | null;
}

export interface AddTeamMemberRequest {
  email: string;
  role: TenantMemberRole;
}

export interface AddTeamMemberResponse {
  tenantMemberId: string;
  userId: string;
  email: string;
  role: TenantMemberRole;
}
