import { TenantMemberRole } from './tenant.model';

export type { TenantMemberRole };

/** Mirrors the backend's `Rawaj.Domain.Enums.InvitationStatus` (serialized as a string). */
export type InvitationStatus = 'Pending' | 'Accepted' | 'Declined';

export const ROLE_LABELS: Record<TenantMemberRole, string> = {
  Owner: 'مالك',
  Admin: 'مدير',
  Editor: 'محرر',
  Viewer: 'مشاهد',
};

export const INVITATION_STATUS_LABELS: Record<InvitationStatus, string> = {
  Pending: 'بانتظار القبول',
  Accepted: 'نشط',
  Declined: 'مرفوضة',
};

/** GET /team-members — one row per member who has been added to (or invited into) the active
 *  tenant. `joinedAt` is null while `invitationStatus` is still `Pending`. */
export interface TeamMemberSummary {
  tenantMemberId: string;
  userId: string;
  email: string;
  fullName: string;
  role: TenantMemberRole;
  invitationStatus: InvitationStatus;
  joinedAt: string | null;
  brandProfileIds: string[];
  allocatedCoins: number;
  spentCoins: number;
}

/** POST /team-members — invites either an existing user (creates a Pending `TenantMember`) or a
 *  brand-new email (creates a `TenantInvitation`); `allocatedCoins` is escrowed out of the tenant's
 *  pool immediately either way. */
export interface AddTeamMemberRequest {
  email: string;
  role: TenantMemberRole;
  brandProfileIds: string[];
  allocatedCoins: number;
}

export interface AddTeamMemberResponse {
  tenantMemberId: string | null;
  invitationId: string | null;
  email: string;
  role: TenantMemberRole;
  requiresRegistration: boolean;
  /** False when the backend's SMTP sender isn't configured yet — the invite record was still
   *  created, but no email was actually attempted. Surfaced so "invite succeeded" never looks
   *  identical to "invite succeeded and an email is on its way." */
  emailConfigured: boolean;
}

/** GET /team-members/pending-invites — invites addressed to the CURRENT user (across tenants),
 *  i.e. the invitee's own "you've been invited" list. */
export interface PendingInviteSummary {
  tenantMemberId: string;
  tenantId: string;
  tenantName: string;
  role: TenantMemberRole;
  brandProfileNames: string[];
  createdAt: string;
}

/** GET /team-members/invitations — the tenant-admin's view of invites sent to emails with no
 *  Rawaj account yet (still awaiting registration, not just acceptance). */
export interface PendingInvitationSummary {
  invitationId: string;
  email: string;
  role: TenantMemberRole;
  allocatedCoins: number;
  createdAt: string;
  expiresAt: string;
}

/** GET /team-members/invitations/{token} — anonymous lookup backing the `/invite` page. Works for
 *  both invite kinds: `requiresRegistration` tells the page whether to show a registration form
 *  (brand-new email) or a "sign in to accept" prompt (existing account). */
export interface InvitationDetailsResponse {
  tenantName: string;
  inviterName: string;
  email: string;
  role: TenantMemberRole;
  requiresRegistration: boolean;
  tenantMemberId: string | null;
  invitationId: string | null;
}

export interface AcceptInvitationAndRegisterRequest {
  token: string;
  username: string;
  password: string;
  fullName: string;
  preferredLanguage: 'Ar' | 'En';
}

export interface AcceptInvitationAndRegisterResponse {
  userId: string;
  email: string;
  fullName: string;
  accessToken: string;
  refreshToken: string;
}

export interface UpdateTeamMemberRequest {
  role: TenantMemberRole;
  brandProfileIds: string[];
}

export interface UpdateTeamMemberResponse {
  tenantMemberId: string;
  role: TenantMemberRole;
  brandProfileIds: string[];
}

export interface AllocateCoinsResponse {
  tenantMemberId: string;
  allocatedCoins: number;
  spentCoins: number;
  tenantCoinBalance: number;
}

/** GET /team-members/activity — the "السجل" (logs) tab: a paginated audit trail of `team.*`
 *  actions in the active tenant, optionally filtered to one member via `userId`. */
export interface TeamActivitySummary {
  id: string;
  action: string;
  message: string | null;
  userId: string | null;
  userFullName: string | null;
  createdAt: string;
}

export interface TeamActivityPageResponse {
  items: TeamActivitySummary[];
  totalCount: number;
}

/** Human-readable Arabic label for a raw `team.*` audit action string, with a sensible fallback
 *  for anything not explicitly mapped (new action kinds shouldn't silently render as blank). */
export const TEAM_ACTIVITY_LABELS: Record<string, string> = {
  'team.invite_sent': 'تمت دعوة عضو جديد',
  'team.invite_accepted': 'قبل العضو الدعوة',
  'team.invite_declined': 'رفض العضو الدعوة',
  'team.invite_revoked': 'تم سحب الدعوة',
  'team.member_removed': 'تمت إزالة عضو',
  'team.role_changed': 'تم تغيير دور العضو',
  'team.brand_access_changed': 'تم تعديل صلاحية الوصول للعلامات التجارية',
  'team.coins_allocated': 'تم تعديل رصيد الكوينز المخصص',
};

export function teamActivityLabel(action: string): string {
  return TEAM_ACTIVITY_LABELS[action] ?? action;
}
