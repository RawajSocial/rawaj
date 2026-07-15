import { TenantMemberRole } from '../core/models';

export type TeamMemberRole = TenantMemberRole;

export interface TeamMember {
  id: string; // tenantMemberId
  userId: string;
  name: string;
  email: string;
  role: TeamMemberRole;
  joinedAt: string | null;
}

export const ROLE_LABELS: Record<TeamMemberRole, string> = {
  Owner: 'مالك',
  Admin: 'مدير',
  Editor: 'محرر',
  Viewer: 'مشاهد',
};
