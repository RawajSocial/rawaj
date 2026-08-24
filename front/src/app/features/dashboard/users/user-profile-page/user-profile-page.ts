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
import { DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { animate, stagger } from 'motion';
import { TeamMemberService } from '../../../../services/team-member.service';
import { BrandProfileService } from '../../../../services/brand-profile.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../../services/confirm-dialog.service';
import {
  INVITATION_STATUS_LABELS,
  ROLE_LABELS,
  teamActivityLabel,
  TeamActivitySummary,
} from '../../../../model/team-member.model';
import { TenantMemberRole } from '../../../../model/tenant.model';
import { AssignProjectsModal } from '../assign-projects-modal/assign-projects-modal';
import { SeoService } from '../../../../services/seo.service';
import { Breadcrumb } from '../../../../shared/components/breadcrumb/breadcrumb';

@Component({
  selector: 'app-user-profile-page',
  imports: [RouterLink, DecimalPipe, DatePipe, AssignProjectsModal, Breadcrumb],
  templateUrl: './user-profile-page.html',
  styleUrls: ['../users-shared.css', './user-profile-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly seo = inject(SeoService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusLabels = INVITATION_STATUS_LABELS;
  protected readonly teamActivityLabel = teamActivityLabel;
  protected readonly brandProfiles = this.brandProfileService.profiles;

  protected readonly roleOptions: { value: TenantMemberRole; label: string }[] = [
    { value: 'Admin', label: 'مدير' },
    { value: 'Editor', label: 'محرر' },
    { value: 'Viewer', label: 'مشاهد' },
  ];

  private readonly memberId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly member = this.teamMemberService.getById(this.memberId);

  protected readonly assignedBrandNames = computed(() => {
    const ids = new Set(this.member()?.brandProfileIds ?? []);
    return this.brandProfiles()
      .filter(b => ids.has(b.id))
      .map(b => b.name);
  });

  protected readonly coinPercent = computed(() => {
    const m = this.member();
    if (!m || m.allocatedCoins === 0) return 0;
    return Math.min(100, Math.round((m.spentCoins / m.allocatedCoins) * 100));
  });

  protected readonly assignModalOpen = signal(false);
  protected readonly coinsEditOpen = signal(false);
  protected readonly coinsInputValue = signal(0);

  protected readonly activityItems = signal<TeamActivitySummary[]>([]);
  protected readonly activityLoading = signal(false);

  private readonly coinBarRef = viewChild<ElementRef<HTMLElement>>('coinBar');
  private readonly cardRefs = viewChild<ElementRef<HTMLElement>>('cardsWrap');

  constructor() {
    this.seo.setPageSeo({
      title: 'الملف الشخصي للمستخدم | رواج',
      description: 'دور العضو، صلاحيات الوصول للعلامات التجارية، ورصيد الكوينز.',
      keywords: 'رواج, ملف المستخدم, الفريق, كوينز',
      path: `/dashboard/users/${this.memberId}`,
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    if (this.teamMemberService.members().length === 0) {
      this.teamMemberService.refresh().subscribe();
    }
    if (this.brandProfileService.profiles().length === 0) {
      this.brandProfileService.refresh().subscribe();
    }
    this.loadActivity();

    afterNextRender(() => {
      const wrap = this.cardRefs()?.nativeElement;
      const cards = wrap ? Array.from(wrap.querySelectorAll<HTMLElement>('.profile-card')) : [];
      if (cards.length) {
        animate(
          cards,
          { opacity: [0, 1], transform: ['translateY(10px)', 'translateY(0)'] },
          { duration: 0.35, delay: stagger(0.06), ease: [0.16, 1, 0.3, 1] },
        );
      }
      const bar = this.coinBarRef()?.nativeElement;
      if (bar) {
        animate(bar, { width: [`0%`, `${this.coinPercent()}%`] }, { duration: 0.6, ease: [0.16, 1, 0.3, 1] });
      }
    });
  }

  private loadActivity(): void {
    const userId = this.member()?.userId;
    this.activityLoading.set(true);
    this.teamMemberService.getActivity(1, 20, userId).subscribe({
      next: res => {
        this.activityLoading.set(false);
        if (res.data) this.activityItems.set(res.data.items);
      },
      error: () => this.activityLoading.set(false),
    });
  }

  protected initials(name: string): string {
    return name.trim().split(/\s+/).slice(0, 2).map(p => p[0]).join('');
  }

  protected async changeRole(role: TenantMemberRole): Promise<void> {
    const m = this.member();
    if (!m || m.role === role) return;
    const confirmed = await this.confirmDialogService.confirm(
      `هل تريد تغيير دور ${m.fullName} إلى "${this.roleLabels[role]}"؟`,
      { title: 'تغيير الدور' },
    );
    if (!confirmed) return;

    this.teamMemberService.updateMember(this.memberId, { role, brandProfileIds: m.brandProfileIds }).subscribe({
      next: res => {
        if (res.status === 'success') {
          this.errorModalService.show('تم تحديث دور العضو بنجاح.', { variant: 'success' });
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر تحديث الدور.');
        }
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر تحديث الدور.'),
    });
  }

  protected onBrandsAssigned(brandProfileIds: string[]): void {
    const m = this.member();
    if (!m) return;
    this.assignModalOpen.set(false);
    this.teamMemberService.updateMember(this.memberId, { role: m.role, brandProfileIds }).subscribe({
      next: res => {
        if (res.status === 'success') {
          this.errorModalService.show('تم تحديث العلامات التجارية المسموح بها.', { variant: 'success' });
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر التحديث.');
        }
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر التحديث.'),
    });
  }

  protected openCoinsEdit(): void {
    this.coinsInputValue.set(this.member()?.allocatedCoins ?? 0);
    this.coinsEditOpen.set(true);
  }

  protected saveCoinsEdit(): void {
    this.teamMemberService.allocateCoins(this.memberId, this.coinsInputValue()).subscribe({
      next: res => {
        this.coinsEditOpen.set(false);
        if (res.status === 'success') {
          this.errorModalService.show('تم تحديث رصيد الكوينز المخصص.', { variant: 'success' });
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر تحديث الكوينز.');
        }
      },
      error: err => {
        this.coinsEditOpen.set(false);
        this.errorModalService.show(err?.error?.message ?? 'تعذّر تحديث الكوينز — تأكد أن رصيد المؤسسة يكفي.');
      },
    });
  }

  protected async deleteMember(): Promise<void> {
    const m = this.member();
    if (!m) return;
    const confirmed = await this.confirmDialogService.confirm(
      `هل تريد إزالة ${m.fullName} من الفريق؟ سيتم إرجاع أي كوينز غير مستخدمة إلى رصيد المؤسسة.`,
      { title: 'إزالة العضو', variant: 'danger', confirmLabel: 'نعم، إزالة' },
    );
    if (!confirmed) return;

    this.teamMemberService.removeMember(this.memberId).subscribe({
      next: res => {
        if (res.status === 'success') {
          this.router.navigate(['/dashboard/users']);
        } else {
          this.errorModalService.show(res.message ?? 'تعذّرت إزالة العضو.');
        }
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّرت إزالة العضو.'),
    });
  }
}
