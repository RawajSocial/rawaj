import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { animate, stagger } from 'motion';
import { TeamMemberService } from '../../../../services/team-member.service';
import {
  ROLE_LABELS,
  STATUS_LABELS,
  TeamMemberPermissions,
} from '../../../../model/team-member.model';
import { UserFormModal, UserFormValue } from '../user-form-modal/user-form-modal';
import { ResetPasswordModal } from '../reset-password-modal/reset-password-modal';
import { AssignProjectsModal } from '../assign-projects-modal/assign-projects-modal';

interface PermissionRow {
  manageKey: keyof TeamMemberPermissions;
  manageLabel: string;
  viewKey?: keyof TeamMemberPermissions;
  viewLabel?: string;
}

const PERMISSION_ROWS: PermissionRow[] = [
  { manageKey: 'manageUsers', manageLabel: 'إدارة المستخدمين', viewKey: 'viewUsers', viewLabel: 'عرض المستخدمين' },
  { manageKey: 'manageCampaigns', manageLabel: 'إدارة الحملات', viewKey: 'viewCampaigns', viewLabel: 'عرض الحملات' },
  { manageKey: 'manageAds', manageLabel: 'إدارة الإعلانات', viewKey: 'viewAds', viewLabel: 'عرض الإعلانات' },
  { manageKey: 'manageContent', manageLabel: 'إدارة المحتوى', viewKey: 'viewContent', viewLabel: 'عرض المحتوى' },
  { manageKey: 'manageBilling', manageLabel: 'إدارة الفواتير', viewKey: 'viewBilling', viewLabel: 'عرض الفواتير' },
  { manageKey: 'manageSettings', manageLabel: 'إدارة الإعدادات' },
  { manageKey: 'support', manageLabel: 'الدعم الفني' },
];

@Component({
  selector: 'app-user-profile-page',
  imports: [RouterLink, DecimalPipe, UserFormModal, ResetPasswordModal, AssignProjectsModal],
  templateUrl: './user-profile-page.html',
  styleUrls: ['../users-shared.css', './user-profile-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly teamMemberService = inject(TeamMemberService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly permissionRows = PERMISSION_ROWS;
  protected readonly projects = this.teamMemberService.projects;

  private readonly memberId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly member = this.teamMemberService.getById(this.memberId);

  protected readonly assignedProjectNames = computed(() =>
    this.teamMemberService.projectNames(this.member()?.assignedProjectIds ?? []),
  );

  protected readonly creditPercent = computed(() => {
    const usage = this.member()?.creditUsage;
    if (!usage || usage.limit === 0) return 0;
    return Math.min(100, Math.round((usage.used / usage.limit) * 100));
  });

  protected readonly editModalOpen = signal(false);
  protected readonly resetModalOpen = signal(false);
  protected readonly assignModalOpen = signal(false);

  private readonly creditBarRef = viewChild<ElementRef<HTMLElement>>('creditBar');
  private readonly cardRefs = viewChild.required<ElementRef<HTMLElement>>('cardsWrap');

  constructor() {
    afterNextRender(() => {
      const wrap = this.cardRefs().nativeElement;
      const cards = Array.from(wrap.querySelectorAll<HTMLElement>('.profile-card'));
      if (cards.length) {
        animate(
          cards,
          { opacity: [0, 1], transform: ['translateY(10px)', 'translateY(0)'] },
          { duration: 0.35, delay: stagger(0.06), ease: [0.16, 1, 0.3, 1] },
        );
      }

      const bar = this.creditBarRef()?.nativeElement;
      if (bar) {
        animate(bar, { width: [`0%`, `${this.creditPercent()}%`] }, { duration: 0.6, ease: [0.16, 1, 0.3, 1] });
      }
    });
  }

  protected initials(name: string): string {
    return name.trim().split(/\s+/).slice(0, 2).map(p => p[0]).join('');
  }

  protected onEditSaved(value: UserFormValue): void {
    this.teamMemberService.updateMember(this.memberId, value);
    this.editModalOpen.set(false);
  }

  protected onPasswordReset(newPassword: string): void {
    this.teamMemberService.resetPassword(this.memberId, newPassword);
    this.resetModalOpen.set(false);
  }

  protected onProjectsAssigned(projectIds: string[]): void {
    this.teamMemberService.assignProjects(this.memberId, projectIds);
    this.assignModalOpen.set(false);
  }

  protected togglePermission(key: keyof TeamMemberPermissions): void {
    const current = this.member()?.permissions;
    if (!current) return;
    this.teamMemberService.updatePermissions(this.memberId, { ...current, [key]: !current[key] });
  }

  protected toggleSuspend(): void {
    const m = this.member();
    if (!m) return;
    if (m.status === 'suspended') this.teamMemberService.reactivateMember(this.memberId);
    else this.teamMemberService.suspendMember(this.memberId);
  }

  protected deleteMember(): void {
    this.teamMemberService.removeMember(this.memberId);
    this.router.navigate(['/dashboard/users']);
  }
}
