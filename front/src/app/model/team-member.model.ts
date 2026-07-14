export type TeamMemberRole = 'admin' | 'editor' | 'moderator' | 'viewer';

export type TeamMemberStatus = 'active' | 'pending' | 'suspended';

export interface TeamMemberPermissions {
  manageUsers: boolean;
  viewUsers: boolean;
  manageCampaigns: boolean;
  viewCampaigns: boolean;
  manageAds: boolean;
  viewAds: boolean;
  manageContent: boolean;
  viewContent: boolean;
  manageBilling: boolean;
  viewBilling: boolean;
  manageSettings: boolean;
  support: boolean;
}

export interface CreditUsage {
  used: number;
  limit: number;
}

export interface TeamMember {
  id: string;
  name: string;
  email: string;
  avatarColor: string;
  role: TeamMemberRole;
  status: TeamMemberStatus;
  department: string;
  joinDate: string;
  lastActiveAt: string;
  assignedProjectIds: string[];
  permissions: TeamMemberPermissions;
  creditUsage: CreditUsage;
  invitedAt?: string;
  lastPasswordResetAt?: string;
}

export interface TeamProject {
  id: string;
  name: string;
}

export const ROLE_LABELS: Record<TeamMemberRole, string> = {
  admin: 'مدير',
  editor: 'محرر',
  moderator: 'مشرف',
  viewer: 'مشاهد',
};

export const STATUS_LABELS: Record<TeamMemberStatus, string> = {
  active: 'نشط',
  pending: 'بانتظار القبول',
  suspended: 'موقوف',
};

export const ROLE_PERMISSION_PRESETS: Record<TeamMemberRole, TeamMemberPermissions> = {
  admin: {
    manageUsers: true, viewUsers: true,
    manageCampaigns: true, viewCampaigns: true,
    manageAds: true, viewAds: true,
    manageContent: true, viewContent: true,
    manageBilling: true, viewBilling: true,
    manageSettings: true, support: true,
  },
  editor: {
    manageUsers: false, viewUsers: true,
    manageCampaigns: true, viewCampaigns: true,
    manageAds: true, viewAds: true,
    manageContent: true, viewContent: true,
    manageBilling: false, viewBilling: true,
    manageSettings: false, support: false,
  },
  moderator: {
    manageUsers: false, viewUsers: true,
    manageCampaigns: false, viewCampaigns: true,
    manageAds: false, viewAds: true,
    manageContent: false, viewContent: true,
    manageBilling: false, viewBilling: false,
    manageSettings: false, support: true,
  },
  viewer: {
    manageUsers: false, viewUsers: true,
    manageCampaigns: false, viewCampaigns: true,
    manageAds: false, viewAds: true,
    manageContent: false, viewContent: true,
    manageBilling: false, viewBilling: false,
    manageSettings: false, support: false,
  },
};
