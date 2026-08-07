import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';
import { CampaignCard } from '../campaign-card/campaign-card';
import { DeleteCampaignModal } from '../delete-campaign-modal/delete-campaign-modal';
import { CampaignDeleteSummary, CampaignStatus, CampaignPlatform } from '../../../model/campaign.model';
import { SeoService } from '../../../services/seo.service';
import { CampaignService } from '../../../services/campaign.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { BrandLock } from '../../../shared/components/brand-lock/brand-lock';
import { TenantService } from '../../../core/tenant/tenant.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { OnboardingEntryService } from '../../../services/onboarding-entry.service';

@Component({
  selector: 'app-campaigns-page',
  standalone: true,
  imports: [CampaignCard, DeleteCampaignModal, PageHeader, BrandLock, TooltipDirective],
  templateUrl: './campaigns-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './campaigns-page.css'],
})
export class CampaignsPage {
  private readonly seo = inject(SeoService);
  private readonly campaignService = inject(CampaignService);
  private readonly router = inject(Router);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly tenantService = inject(TenantService);
  private readonly onboardingEntryService = inject(OnboardingEntryService);
  protected readonly perms = inject(PermissionService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الحملات التسويقية | رواج',
      description: 'أنشئ حملاتك التسويقية وتابع أداءها وميزانيتها في مكان واحد.',
      keywords: 'رواج, حملات تسويقية, إدارة حملات, ميزانية إعلانية',
      path: '/dashboard/campaigns',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  /** Excludes archived campaigns — the onboarding wizard archives every abandoned draft, which
   *  otherwise filled this grid with cards the user never intentionally created. The archive is a
   *  separate, lazily-fetched list reached through the "مؤرشفة" status filter. */
  protected readonly campaigns = this.campaignService.campaigns;
  private readonly archivedCampaigns = this.campaignService.archivedCampaigns;
  protected readonly viewingArchive = computed(() => this.statusFilter() === 'archived');
  protected readonly loaded = computed(() =>
    this.viewingArchive() ? this.campaignService.archivedLoaded() : this.campaignService.loaded(),
  );
  protected readonly brandProfileCount = this.tenantService.brandProfileCount;
  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<CampaignStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!(e.target as HTMLElement).closest('[data-dd="platform"]')) this.platformOpen.set(false);
  }

  /** Selecting "مؤرشفة" is what fetches the archive — it's a separate request, made only when the
   *  user actually asks to see it rather than on every visit to this page. */
  protected setStatus(v: CampaignStatus | 'all'): void {
    this.statusFilter.set(v);
    this.statusOpen.set(false);
    if (v === 'archived' && !this.campaignService.archivedLoaded()) this.loadArchive();
  }

  private loadArchive(): void {
    this.campaignService.refreshArchived().subscribe({
      error: err => this.errorModalService.show(
        extractApiErrorMessage(err, 'تعذّر تحميل الحملات المؤرشفة.'), { variant: 'error' }),
    });
  }
  protected setPlatform(v: CampaignPlatform | 'all'): void { this.platformFilter.set(v); this.platformOpen.set(false); }

  protected readonly statusOptions: { value: CampaignStatus | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع الحالات' },
    { value: 'active',    label: 'نشطة' },
    { value: 'paused',    label: 'موقوفة' },
    { value: 'completed', label: 'مكتملة' },
    { value: 'draft',     label: 'مسودة' },
    { value: 'archived',  label: 'مؤرشفة' },
  ];

  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع المنصات' },
    { value: 'instagram', label: 'إنستغرام' },
    { value: 'facebook',  label: 'فيسبوك' },
    { value: 'tiktok',    label: 'تيك توك' },
    { value: 'youtube',   label: 'يوتيوب' },
    // No Snapchat option: the backend's SocialPlatform enum doesn't model it, so a campaign can
    // never target it and the filter could only ever return zero results.
    { value: 'linkedin',  label: 'لينكد إن' },
    { value: 'x',         label: 'إكس (تويتر)' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    // Archived campaigns live in their own list and are only shown when explicitly filtered for
    // — archiving is a soft delete, not a permanent one.
    return (st === 'archived' ? this.archivedCampaigns() : this.campaigns()).filter(c => {
      if (q  && !c.name.toLowerCase().includes(q))             return false;
      if (st !== 'all' && c.status !== st)                     return false;
      if (pl !== 'all' && !c.platforms.includes(pl))           return false;
      return true;
    });
  });

  protected get statusLabel():   string { return this.statusOptions.find(o => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }

  protected pauseCampaign(id: string): void {
    if (!this.perms.canEdit() || !this.requireBrandProfile()) return;
    this.campaignService.update(id, { status: 'Paused' }).subscribe({
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إيقاف الحملة.'), { variant: 'error' }),
    });
  }

  protected resumeCampaign(id: string): void {
    if (!this.perms.canEdit() || !this.requireBrandProfile()) return;
    this.campaignService.update(id, { status: 'Active' }).subscribe({
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تفعيل الحملة.'), { variant: 'error' }),
    });
  }

  /** Archiving is the only "remove this campaign" the API offers, and until now nothing in the UI
   *  could trigger it — only the onboarding wizard did, silently, for drafts the user abandoned.
   *  It's reversible (see `restoreCampaign`), which the confirmation says so the user isn't left
   *  guessing whether this is permanent. */
  protected async archiveCampaign(id: string): Promise<void> {
    if (!this.perms.canEdit() || !this.requireBrandProfile()) return;
    const confirmed = await this.confirmDialogService.confirm(
      'سيتم نقل الحملة إلى الأرشيف وإخفاؤها من قائمة حملاتك. يمكنك استعادتها لاحقًا من فلتر "مؤرشفة".',
      { title: 'أرشفة الحملة', confirmLabel: 'أرشفة', variant: 'danger' },
    );
    if (!confirmed) return;

    this.campaignService.archive(id).subscribe({
      error: err => this.errorModalService.show(
        extractApiErrorMessage(err, 'تعذّرت أرشفة الحملة.'), { variant: 'error' }),
    });
  }

  protected restoreCampaign(id: string): void {
    if (!this.perms.canEdit() || !this.requireBrandProfile()) return;
    this.campaignService.unarchive(id).subscribe({
      error: err => this.errorModalService.show(
        extractApiErrorMessage(err, 'تعذّرت استعادة الحملة.'), { variant: 'error' }),
    });
  }

  /** Permanent delete — a dedicated modal (not the generic ConfirmDialogService text prompt) shows
   *  the actual breakdown of what's about to be removed, fetched fresh so the counts are never
   *  stale. `deleteCampaignId` is kept separate from `deleteSummary` so the modal can render its
   *  loading state the instant it opens, before the summary request resolves. */
  protected readonly deleteModalOpen = signal(false);
  protected readonly deleteSummary = signal<CampaignDeleteSummary | null>(null);
  protected readonly deleting = signal(false);
  private deleteCampaignId: string | null = null;

  protected requestDeleteCampaign(id: string): void {
    if (!this.perms.canAdmin()) return;
    this.deleteCampaignId = id;
    this.deleteSummary.set(null);
    this.deleteModalOpen.set(true);
    this.campaignService.getDeleteSummary(id).subscribe({
      next: res => {
        if (res.data) this.deleteSummary.set(res.data);
      },
      error: err => {
        this.cancelDeleteCampaign();
        this.errorModalService.show(
          extractApiErrorMessage(err, 'تعذّر تحميل تفاصيل الحملة.'), { variant: 'error' });
      },
    });
  }

  protected cancelDeleteCampaign(): void {
    this.deleteModalOpen.set(false);
    this.deleteSummary.set(null);
    this.deleteCampaignId = null;
  }

  protected confirmDeleteCampaign(): void {
    const id = this.deleteCampaignId;
    if (!id || this.deleting()) return;

    this.deleting.set(true);
    this.campaignService.delete(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.cancelDeleteCampaign();
      },
      error: err => {
        this.deleting.set(false);
        this.cancelDeleteCampaign();
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر حذف الحملة.'), { variant: 'error' });
      },
    });
  }

  protected viewCampaign(id: string): void {
    this.router.navigate(['/dashboard/campaigns', id]);
  }

  /** Sends the user straight to the step the campaign is waiting on, so the list is a way back
   *  into an unfinished workflow rather than a dead end. Three destinations, not two: a campaign
   *  abandoned mid-wizard (steps 1-7, `onboardingCompletedAt` unset — no brief data collected yet)
   *  must go back into the wizard itself, not into strategy review, which used to run the
   *  research/diagnose pipeline against an empty brief. Once the wizard is finished but the
   *  strategy isn't approved, strategy review is correct; past that, content review is. */
  protected openNextStep(id: string): void {
    const campaign = this.campaignService.getById(id)();
    if (campaign && !campaign.onboardingCompletedAt) {
      this.router.navigate(['/on-boarding'], { queryParams: { resume: id } });
      return;
    }
    if (campaign && !campaign.planApprovedAt) {
      this.router.navigate(['/dashboard/campaigns', id, 'strategy']);
      return;
    }
    this.router.navigate(['/dashboard/campaigns', id, 'content']);
  }

  protected startNewCampaign(): void {
    if (!this.perms.canEdit() || !this.requireBrandProfile()) return;
    void this.onboardingEntryService.startOrResumeOnboarding();
  }

  private requireBrandProfile(): boolean {
    if (this.tenantService.brandProfileCount() > 0) return true;
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    this.router.navigate(['/dashboard/brand-profiles/new']);
    return false;
  }
}
