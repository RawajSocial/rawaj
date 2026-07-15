import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CampaignsApiService } from '../../../core/api/campaigns-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { CampaignDetail, CampaignSummary, MarketingPlan } from '../../../core/models';

@Component({
  selector: 'app-marketing-plan-page',
  imports: [],
  templateUrl: './marketing-plan-page.html',
  styleUrl: './marketing-plan-page.css',
})
export class MarketingPlanPage {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly tenantService = inject(TenantService);

  protected readonly campaigns = signal<CampaignSummary[]>([]);
  protected readonly loadingCampaigns = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly selectedCampaign = signal<CampaignDetail | null>(null);
  protected readonly loadingDetail = signal(false);
  protected readonly generating = signal(false);
  protected readonly generateError = signal<string | null>(null);

  protected readonly phase = computed<'list' | 'detail'>(() => (this.selectedCampaign() ? 'detail' : 'list'));

  protected readonly parsedPlan = computed<MarketingPlan | null>(() => {
    const raw = this.selectedCampaign()?.aiPlanJson;
    if (!raw) return null;
    try {
      return JSON.parse(raw) as MarketingPlan;
    } catch {
      return null;
    }
  });

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.loadCampaigns(brandProfileId);
      }
    });

    const campaignIdFromQuery = this.route.snapshot.queryParamMap.get('campaignId');
    if (campaignIdFromQuery) {
      this.openCampaign(campaignIdFromQuery);
    }
  }

  private loadCampaigns(brandProfileId: string): void {
    this.loadingCampaigns.set(true);
    this.loadError.set(null);

    this.campaignsApi.getAll(brandProfileId, 1, 50).subscribe({
      next: (result) => {
        this.campaigns.set(result.items);
        this.loadingCampaigns.set(false);
      },
      error: (error: unknown) => {
        this.loadingCampaigns.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الحملات.');
      },
    });
  }

  protected openCampaign(campaignId: string): void {
    this.loadingDetail.set(true);
    this.generateError.set(null);

    this.campaignsApi.getById(campaignId).subscribe({
      next: (detail) => {
        this.selectedCampaign.set(detail);
        this.loadingDetail.set(false);
      },
      error: (error: unknown) => {
        this.loadingDetail.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل تفاصيل الحملة.');
      },
    });
  }

  protected backToList(): void {
    this.selectedCampaign.set(null);
  }

  protected generatePlan(): void {
    const campaign = this.selectedCampaign();
    if (!campaign) return;

    this.generating.set(true);
    this.generateError.set(null);

    this.campaignsApi.generatePlan(campaign.campaignId).subscribe({
      next: (result) => {
        this.generating.set(false);
        this.selectedCampaign.update((current) =>
          current ? { ...current, aiPlanJson: result.aiPlanJson, aiGeneratedAt: result.aiGeneratedAt } : current,
        );
      },
      error: (error: unknown) => {
        this.generating.set(false);
        this.generateError.set(
          error instanceof ApiError ? error.message : 'تعذر توليد الخطة التسويقية، حاول مرة أخرى.',
        );
      },
    });
  }

  protected goToNewCampaign(): void {
    void this.router.navigate(['/on-boarding']);
  }

  protected goToContentGen(): void {
    void this.router.navigate(['/dashboard/content-gen']);
  }

  protected goToCalendar(): void {
    void this.router.navigate(['/dashboard/calendar']);
  }

  protected objectiveEntries(record: Record<string, string> | undefined): [string, string][] {
    return record ? Object.entries(record) : [];
  }
}
