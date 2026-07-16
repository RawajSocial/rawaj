import { Injectable, computed, signal } from '@angular/core';
import {
  ROLE_PERMISSION_PRESETS,
  TeamMember,
  TeamMemberPermissions,
  TeamMemberRole,
  TeamProject,
} from '../model/team-member.model';

const MOCK_PROJECTS: TeamProject[] = [
  { id: 'p1', name: 'حملة رمضان الكريم ٢٠٢٥' },
  { id: 'p2', name: 'إطلاق منتج العيد' },
  { id: 'p3', name: 'حملة الصيف — التوعية' },
  { id: 'p4', name: 'محتوى إنستغرام الأسبوعي' },
  { id: 'p5', name: 'إعلانات تيك توك — الجمعة البيضاء' },
];

const MOCK_MEMBERS: TeamMember[] = [
  {
    id: 'u1',
    name: 'سارة الأحمد',
    email: 'sara@agency.com',
    avatarColor: 'linear-gradient(135deg, #7C3AED, #2563EB)',
    role: 'admin',
    status: 'active',
    department: 'الإدارة',
    joinDate: '2025-01-10',
    lastActiveAt: 'منذ ساعتين',
    assignedProjectIds: ['p1', 'p2', 'p3'],
    permissions: ROLE_PERMISSION_PRESETS.admin,
    creditUsage: { used: 8200, limit: 10000 },
  },
  {
    id: 'u2',
    name: 'عمر خالد',
    email: 'omar@agency.com',
    avatarColor: 'linear-gradient(135deg, #FACC15, #F97316)',
    role: 'editor',
    status: 'active',
    department: 'التسويق',
    joinDate: '2025-02-05',
    lastActiveAt: 'منذ 5 دقائق',
    assignedProjectIds: ['p1', 'p4'],
    permissions: ROLE_PERMISSION_PRESETS.editor,
    creditUsage: { used: 3400, limit: 5000 },
  },
  {
    id: 'u3',
    name: 'ليلى ناصر',
    email: 'laila@agency.com',
    avatarColor: 'linear-gradient(135deg, #0EA5E9, #06B6D4)',
    role: 'moderator',
    status: 'pending',
    department: 'الدعم',
    joinDate: '2025-06-01',
    lastActiveAt: 'لم يسجّل الدخول بعد',
    assignedProjectIds: [],
    permissions: ROLE_PERMISSION_PRESETS.moderator,
    creditUsage: { used: 0, limit: 2000 },
    invitedAt: '2025-06-01',
  },
  {
    id: 'u4',
    name: 'يوسف حسن',
    email: 'youssef@agency.com',
    avatarColor: 'linear-gradient(135deg, #16A34A, #22C55E)',
    role: 'viewer',
    status: 'suspended',
    department: 'المبيعات',
    joinDate: '2024-11-20',
    lastActiveAt: 'منذ أسبوعين',
    assignedProjectIds: ['p2'],
    permissions: ROLE_PERMISSION_PRESETS.viewer,
    creditUsage: { used: 900, limit: 2000 },
  },
];

@Injectable({ providedIn: 'root' })
export class TeamMemberService {
  private readonly _members = signal<TeamMember[]>(MOCK_MEMBERS);
  readonly members = this._members.asReadonly();

  readonly projects = signal<TeamProject[]>(MOCK_PROJECTS);

  readonly activeCount = computed(() => this._members().filter(m => m.status === 'active').length);
  readonly pendingCount = computed(() => this._members().filter(m => m.status === 'pending').length);

  // TODO: replace with the real authenticated session once auth exists —
  // this stands in for "who am I" so invited-marketer views (e.g. "my
  // projects") have someone to resolve assignments against.
  readonly currentUserId = signal('u2');
  readonly currentUser = computed(() => this._members().find(m => m.id === this.currentUserId()));

  getById(id: string) {
    return computed(() => this._members().find(m => m.id === id));
  }

  projectNames(ids: string[]): string[] {
    const all = this.projects();
    return ids.map(id => all.find(p => p.id === id)?.name).filter((n): n is string => !!n);
  }

  inviteMember(data: { name: string; email: string; role: TeamMemberRole; department: string }): TeamMember {
    const member: TeamMember = {
      id: 'u' + Date.now(),
      name: data.name,
      email: data.email,
      avatarColor: this.randomAvatarColor(),
      role: data.role,
      status: 'pending',
      department: data.department,
      joinDate: new Date().toISOString().slice(0, 10),
      lastActiveAt: 'لم يسجّل الدخول بعد',
      assignedProjectIds: [],
      permissions: ROLE_PERMISSION_PRESETS[data.role],
      creditUsage: { used: 0, limit: 2000 },
      invitedAt: new Date().toISOString().slice(0, 10),
    };
    this._members.update(list => [member, ...list]);
    return member;
  }

  updateMember(id: string, changes: Partial<Pick<TeamMember, 'name' | 'email' | 'role' | 'department'>>): void {
    this._members.update(list =>
      list.map(m => {
        if (m.id !== id) return m;
        const next = { ...m, ...changes };
        if (changes.role && changes.role !== m.role) {
          next.permissions = ROLE_PERMISSION_PRESETS[changes.role];
        }
        return next;
      }),
    );
  }

  updatePermissions(id: string, permissions: TeamMemberPermissions): void {
    this._members.update(list => list.map(m => (m.id === id ? { ...m, permissions } : m)));
  }

  removeMember(id: string): void {
    this._members.update(list => list.filter(m => m.id !== id));
  }

  suspendMember(id: string): void {
    this._members.update(list => list.map(m => (m.id === id ? { ...m, status: 'suspended' } : m)));
  }

  reactivateMember(id: string): void {
    this._members.update(list => list.map(m => (m.id === id ? { ...m, status: 'active' } : m)));
  }

  /** Admin sets a brand-new password for the member (the original invite-set password is never known to the admin). */
  resetPassword(id: string, _newPassword: string): void {
    this._members.update(list =>
      list.map(m => (m.id === id ? { ...m, lastPasswordResetAt: new Date().toISOString().slice(0, 10) } : m)),
    );
  }

  assignProjects(id: string, projectIds: string[]): void {
    this._members.update(list => list.map(m => (m.id === id ? { ...m, assignedProjectIds: projectIds } : m)));
  }

  private randomAvatarColor(): string {
    const palettes = [
      'linear-gradient(135deg, #7C3AED, #2563EB)',
      'linear-gradient(135deg, #FACC15, #F97316)',
      'linear-gradient(135deg, #0EA5E9, #06B6D4)',
      'linear-gradient(135deg, #16A34A, #22C55E)',
      'linear-gradient(135deg, #EC4899, #F43F5E)',
    ];
    return palettes[Math.floor(Math.random() * palettes.length)];
  }
}
