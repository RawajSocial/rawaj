import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { animate, stagger } from 'motion';
import { TeamMemberService } from '../../../../services/team-member.service';
import { ROLE_LABELS, TeamMember } from '../../../../model/team-member.model';
import { UserFormModal, UserFormValue, UserUpdateValue } from '../user-form-modal/user-form-modal';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { ApiError } from '../../../../core/api';
import { TenantService } from '../../../../core/tenant/tenant.service';

type StatusTab = 'all' | 'active' | 'pending';

@Component({
  selector: 'app-users-page',
  imports: [RouterLink, UserFormModal, PageHeader],
  templateUrl: './users-page.html',
  styleUrls: ['../../../campaigns/campaigns-page/campaigns-page.css', './users-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly tenantService = inject(TenantService);

  protected readonly roleLabels = ROLE_LABELS;

  protected readonly members = this.teamMemberService.members;
  protected readonly loading = this.teamMemberService.loading;
  protected readonly loadError = this.teamMemberService.loadError;

  /** Owner/Admin manage the team; Editor/Viewer only ever reach this page as read-only. */
  protected readonly canManageMembers = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin';
  });

  protected readonly searchQuery = signal('');
  protected readonly statusTab = signal<StatusTab>('all');
  protected readonly formModalOpen = signal(false);
  protected readonly editingMember = signal<TeamMember | null>(null);
  protected readonly inviting = signal(false);
  protected readonly inviteError = signal<string | null>(null);
  protected readonly removingId = signal<string | null>(null);

  protected readonly tabs: { value: StatusTab; label: string }[] = [
    { value: 'all', label: 'الكل' },
    { value: 'active', label: 'نشط' },
    { value: 'pending', label: 'بانتظار القبول' },
  ];

  protected readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const tab = this.statusTab();
    return this.members().filter(m => {
      if (tab === 'active' && m.invitationStatus !== 'Accepted') return false;
      if (tab === 'pending' && m.invitationStatus !== 'Pending') return false;
      if (q && !m.name.toLowerCase().includes(q) && !m.email.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  private readonly rowRefs = viewChildren<ElementRef<HTMLElement>>('row');

  constructor() {
    this.teamMemberService.load();
    afterNextRender(() => this.animateRows());
  }

  protected statusLabel(member: TeamMember): string {
    switch (member.invitationStatus) {
      case 'Accepted': return 'نشط';
      case 'Declined': return 'مرفوضة';
      default: return 'بانتظار القبول';
    }
  }

  protected initials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map(p => p[0])
      .join('');
  }

  protected setTab(tab: StatusTab): void {
    this.statusTab.set(tab);
    queueMicrotask(() => this.animateRows());
  }

  protected onSearch(value: string): void {
    this.searchQuery.set(value);
    queueMicrotask(() => this.animateRows());
  }

  protected openInvite(): void {
    this.editingMember.set(null);
    this.inviteError.set(null);
    this.formModalOpen.set(true);
  }

  protected openEdit(member: TeamMember): void {
    this.editingMember.set(member);
    this.inviteError.set(null);
    this.formModalOpen.set(true);
  }

  protected closeFormModal(): void {
    this.formModalOpen.set(false);
    this.editingMember.set(null);
  }

  protected onInviteSaved(value: UserFormValue): void {
    this.inviting.set(true);
    this.inviteError.set(null);

    this.teamMemberService.invite(value.email, value.role, value.brandProfileIds).subscribe({
      next: () => {
        this.inviting.set(false);
        this.closeFormModal();
        queueMicrotask(() => this.animateRows());
      },
      error: (error: unknown) => {
        this.inviting.set(false);
        this.inviteError.set(error instanceof ApiError ? error.message : 'تعذر إضافة العضو.');
      },
    });
  }

  protected onMemberUpdated(value: UserUpdateValue): void {
    const member = this.editingMember();
    if (!member) return;

    this.inviting.set(true);
    this.inviteError.set(null);

    this.teamMemberService.update(member.id, value.role, value.brandProfileIds).subscribe({
      next: () => {
        this.inviting.set(false);
        this.closeFormModal();
        queueMicrotask(() => this.animateRows());
      },
      error: (error: unknown) => {
        this.inviting.set(false);
        this.inviteError.set(error instanceof ApiError ? error.message : 'تعذر حفظ التغييرات.');
      },
    });
  }

  protected removeMember(member: TeamMember): void {
    if (!window.confirm(`هل أنت متأكد من إزالة ${member.name} من الفريق؟`)) return;

    this.removingId.set(member.id);
    this.teamMemberService.remove(member.id).subscribe({
      next: () => {
        this.removingId.set(null);
        queueMicrotask(() => this.animateRows());
      },
      error: (error: unknown) => {
        this.removingId.set(null);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر إزالة العضو.');
      },
    });
  }

  protected trackById(_index: number, member: TeamMember): string {
    return member.id;
  }

  private animateRows(): void {
    const rows = this.rowRefs().map(r => r.nativeElement);
    if (!rows.length) return;
    animate(
      rows,
      { opacity: [0, 1], transform: ['translateY(8px)', 'translateY(0)'] },
      { duration: 0.32, delay: stagger(0.035), ease: [0.16, 1, 0.3, 1] },
    );
  }
}
