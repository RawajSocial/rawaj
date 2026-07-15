import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CampaignCard } from '../campaign-card/campaign-card';
import { Campaign, CampaignStatus, CampaignPlatform } from '../../../model/campaign.model';
import { CampaignsApiService } from '../../../core/api/campaigns-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';

@Component({
  selector: 'app-campaigns-page',
  standalone: true,
  imports: [RouterLink, CampaignCard],
  templateUrl: './campaigns-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './campaigns-page.css'],
})
export class CampaignsPage {
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);

  protected readonly campaigns = signal<Campaign[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<CampaignStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.loadCampaigns(brandProfileId);
      }
    });
  }

  private loadCampaigns(brandProfileId: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.campaignsApi.getAll(brandProfileId, 1, 50).subscribe({
      next: (result) => {
        this.campaigns.set(
          result.items.map((c) => ({
            id: c.campaignId,
            name: c.name,
            status: c.status,
            platforms: [],
            objective: null,
            budgetAmount: null,
            budgetCurrency: null,
            startDate: c.startDate,
            endDate: c.endDate,
            createdAt: c.createdAt,
          })),
        );
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الحملات.');
      },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!(e.target as HTMLElement).closest('[data-dd="platform"]')) this.platformOpen.set(false);
  }

  protected setStatus(v: CampaignStatus | 'all'): void   { this.statusFilter.set(v);   this.statusOpen.set(false); }
  protected setPlatform(v: CampaignPlatform | 'all'): void { this.platformFilter.set(v); this.platformOpen.set(false); }

  protected readonly statusOptions: { value: CampaignStatus | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع الحالات' },
    { value: 'Active',    label: 'نشطة' },
    { value: 'Paused',    label: 'موقوفة' },
    { value: 'Completed', label: 'مكتملة' },
    { value: 'Draft',     label: 'مسودة' },
    { value: 'Archived',  label: 'مؤرشفة' },
  ];

  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع المنصات' },
    { value: 'Instagram', label: 'إنستغرام' },
    { value: 'Facebook',  label: 'فيسبوك' },
    { value: 'Tiktok',    label: 'تيك توك' },
    { value: 'Youtube',   label: 'يوتيوب' },
    { value: 'Linkedin',  label: 'لينكد إن' },
    { value: 'Twitter',   label: 'إكس (تويتر)' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    return this.campaigns().filter(c => {
      if (q  && !c.name.toLowerCase().includes(q))             return false;
      if (st !== 'all' && c.status !== st)                     return false;
      if (pl !== 'all' && !c.platforms.includes(pl))           return false;
      return true;
    });
  });

  protected get statusLabel():   string { return this.statusOptions.find(o => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }

  protected viewCampaign(id: string): void {
    void this.router.navigate(['/dashboard/marketing-plan'], { queryParams: { campaignId: id } });
  }
}
