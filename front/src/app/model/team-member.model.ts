import { InvitationStatus, TenantMemberRole } from '../core/models';

export type TeamMemberRole = TenantMemberRole;

export interface TeamMember {
  id: string; // tenantMemberId
  userId: string;
  name: string;
  email: string;
  role: TeamMemberRole;
  invitationStatus: InvitationStatus;
  joinedAt: string | null;
  brandProfileIds: string[];
}

export const ROLE_LABELS: Record<TeamMemberRole, string> = {
  Owner: 'مالك',
  Admin: 'مدير',
  Editor: 'محرر',
  Viewer: 'مشاهد',
};
