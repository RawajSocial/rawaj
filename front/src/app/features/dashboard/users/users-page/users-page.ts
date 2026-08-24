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
import { DatePipe } from '@angular/common';
import { animate, stagger } from 'motion';
import { TeamMemberService } from '../../../../services/team-member.service';
import { BrandProfileService } from '../../../../services/brand-profile.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../../services/confirm-dialog.service';
import {
  INVITATION_STATUS_LABELS,
  InvitationStatus,
  ROLE_LABELS,
  teamActivityLabel,
  TeamActivitySummary,
} from '../../../../model/team-member.model';
import { UserFormModal, UserFormValue } from '../user-form-modal/user-form-modal';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { TooltipDirective } from '../../../../shared/directives/tooltip.directive';

type StatusTab = 'all' | 'Accepted' | 'Pending' | 'logs';

/** A unified row for the table — an accepted/pending `TenantMember` (has a real user account) or
 *  a `TenantInvitation` to an email with no account yet. Both are "team members" from the owner's
 *  point of view, so they share one table instead of two. */
interface UserRow {
  kind: 'member' | 'invitation';
  id: string;
  displayName: string;
  email: string;
  role: string;
  status: InvitationStatus;
  allocatedCoins: number;
  spentCoins: number;
  brandCount: number;
  joinedAt: string | null;
}

@Component({
  selector: 'app-users-page',
  imports: [RouterLink, DatePipe, UserFormModal, PageHeader, TooltipDirective],
  templateUrl: './users-page.html',
  styleUrls: ['../../../campaigns/campaigns-page/campaigns-page.css', './users-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly seo = inject(SeoService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusLabels = INVITATION_STATUS_LABELS;
  protected readonly teamActivityLabel = teamActivityLabel;
  protected readonly brandProfiles = this.brandProfileService.profiles;

  protected readonly loading = signal(true);
  protected readonly searchQuery = signal('');
  protected readonly statusTab = signal<StatusTab>('all');
  protected readonly inviteModalOpen = signal(false);

  protected readonly tabs: { value: StatusTab; label: string }[] = [
    { value: 'all', label: 'الكل' },
    { value: 'Accepted', label: 'نشط' },
    { value: 'Pending', label: 'بانتظار القبول' },
    { value: 'logs', label: 'السجل' },
  ];

  protected readonly rows = computed<UserRow[]>(() => {
    const members = this.teamMemberService.members().map<UserRow>(m => ({
      kind: 'member',
      id: m.tenantMemberId,
      displayName: m.fullName,
      email: m.email,
      role: m.role,
      status: m.invitationStatus,
      allocatedCoins: m.allocatedCoins,
      spentCoins: m.spentCoins,
      brandCount: m.brandProfileIds.length,
      joinedAt: m.joinedAt,
    }));
    const invitations = this.teamMemberService.pendingInvitations().map<UserRow>(inv => ({
      kind: 'invitation',
      id: inv.invitationId,
      displayName: inv.email,
      email: inv.email,
      role: inv.role,
      status: 'Pending',
      allocatedCoins: inv.allocatedCoins,
      spentCoins: 0,
      brandCount: 0,
      joinedAt: null,
    }));
    return [...members, ...invitations];
  });

  protected readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const tab = this.statusTab();
    return this.rows().filter(r => {
      if (tab !== 'all' && tab !== 'logs' && r.status !== tab) return false;
      if (q && !r.displayName.toLowerCase().includes(q) && !r.email.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  // ── Activity log tab ──
  protected readonly activityItems = signal<TeamActivitySummary[]>([]);
  protected readonly activityLoading = signal(false);
  protected readonly activityPage = signal(1);
  protected readonly activityTotal = signal(0);
  protected readonly activityPageSize = 20;
  protected readonly activityHasMore = computed(
    () => this.activityItems().length < this.activityTotal(),
  );

  private readonly rowRefs = viewChildren<ElementRef<HTMLElement>>('row');

  constructor() {
    this.seo.setPageSeo({
      title: 'المستخدمون | رواج',
      description: 'ادعُ أعضاء فريقك، وزّع المهام، وتابع صلاحياتهم من مكان واحد.',
      keywords: 'رواج, المستخدمون, إدارة الفريق, صلاحيات',
      path: '/dashboard/users',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
    afterNextRender(() => this.animateRows());
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.teamMemberService.refresh().subscribe({
      next: () => {
        this.loading.set(false);
        queueMicrotask(() => this.animateRows());
      },
      error: () => this.loading.set(false),
    });
    if (this.brandProfileService.profiles().length === 0) {
      this.brandProfileService.refresh().subscribe();
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
    if (tab === 'logs' && this.activityItems().length === 0) {
      this.loadActivity(1);
    }
    queueMicrotask(() => this.animateRows());
  }

  protected loadActivity(page: number): void {
    this.activityLoading.set(true);
    this.teamMemberService.getActivity(page, this.activityPageSize).subscribe({
      next: res => {
        this.activityLoading.set(false);
        if (!res.data) return;
        this.activityPage.set(page);
        this.activityTotal.set(res.data.totalCount);
        this.activityItems.update(items => (page === 1 ? res.data!.items : [...items, ...res.data!.items]));
      },
      error: () => this.activityLoading.set(false),
    });
  }

  protected loadMoreActivity(): void {
    this.loadActivity(this.activityPage() + 1);
  }

  protected onSearch(value: string): void {
    this.searchQuery.set(value);
    queueMicrotask(() => this.animateRows());
  }

  protected openInvite(): void {
    this.inviteModalOpen.set(true);
  }

  protected onInviteSaved(value: UserFormValue): void {
    this.teamMemberService
      .invite({
        email: value.email,
        role: value.role,
        brandProfileIds: value.brandProfileIds,
        allocatedCoins: value.allocatedCoins,
      })
      .subscribe({
        next: res => {
          this.inviteModalOpen.set(false);
          if (res.status === 'success') {
            if (res.data && !res.data.emailConfigured) {
              this.errorModalService.show(
                'تم إنشاء الدعوة بنجاح، لكن لم يتم إعداد البريد الإلكتروني على الخادم بعد — لن يصل بريد للعضو. أبلغ مدير النظام لإعداد بيانات البريد الإلكتروني.',
                { variant: 'warning' },
              );
            } else {
              this.errorModalService.show(
                res.data?.requiresRegistration
                  ? 'تم إرسال دعوة بالبريد الإلكتروني — سينضم العضو فور إنشاء حسابه.'
                  : 'تم إرسال الدعوة بنجاح، بانتظار قبول العضو.',
                { variant: 'success' },
              );
            }
            queueMicrotask(() => this.animateRows());
          } else {
            this.errorModalService.show(res.message ?? 'تعذّر إرسال الدعوة.');
          }
        },
        error: err => {
          this.inviteModalOpen.set(false);
          this.errorModalService.show(err?.error?.message ?? 'تعذّر إرسال الدعوة.');
        },
      });
  }

  protected async removeRow(row: UserRow, event: Event): Promise<void> {
    event.stopPropagation();
    event.preventDefault();
    const confirmed = await this.confirmDialogService.confirm(
      row.kind === 'invitation'
        ? `هل تريد سحب الدعوة المرسلة إلى ${row.email}؟ سيتم إرجاع أي كوينز مخصصة إلى رصيد المؤسسة.`
        : `هل تريد إزالة ${row.displayName} من الفريق؟ سيتم إرجاع أي كوينز غير مستخدمة إلى رصيد المؤسسة.`,
      { title: row.kind === 'invitation' ? 'سحب الدعوة' : 'إزالة العضو', variant: 'danger', confirmLabel: 'نعم، متابعة' },
    );
    if (!confirmed) return;

    const action$ = row.kind === 'invitation'
      ? this.teamMemberService.revokeInvitation(row.id)
      : this.teamMemberService.removeMember(row.id);

    action$.subscribe({
      next: res => {
        if (res.status !== 'success') this.errorModalService.show(res.message ?? 'تعذّرت العملية.');
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّرت العملية.'),
    });
  }

  protected trackById(_index: number, row: UserRow): string {
    return row.id;
  }

  protected roleLabel(role: string): string {
    return this.roleLabels[role as keyof typeof this.roleLabels] ?? role;
  }

  protected statusLabel(status: string): string {
    return this.statusLabels[status as keyof typeof this.statusLabels] ?? status;
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
