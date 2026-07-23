import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { BrandProfileService } from '../../../../services/brand-profile.service';
import { CampaignService } from '../../../../services/campaign.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { SeoService } from '../../../../services/seo.service';
import { BRAND_PROFILE_STATUS_LABELS } from '../../../../model/brand-profile.model';
import { CampaignStatus } from '../../../../model/campaign.model';

const CAMPAIGN_STATUS_LABELS: Record<CampaignStatus, string> = {
  active: 'نشطة',
  paused: 'متوقفة',
  completed: 'مكتملة',
  draft: 'مسودة',
  archived: 'مؤرشفة',
};

/** "مشاريعي" is the brand profiles (and their campaigns) the current user can access in the ACTIVE
 *  tenant — real data end to end, not the old mock `TeamProject` list. The brand-profile/campaign
 *  services already scope their results to whatever the backend allows this member to see (their
 *  own brand-access grants for Editor/Viewer, everything for Owner/Admin), so no extra client-side
 *  filtering is needed here. */
@Component({
  selector: 'app-my-projects-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './my-projects-page.html',
  styleUrls: ['../../dashboard-shared.css', './my-projects-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyProjectsPage {
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  protected readonly tenantService = inject(TenantService);
  private readonly seo = inject(SeoService);

  protected readonly statusLabels = BRAND_PROFILE_STATUS_LABELS;
  protected readonly campaignStatusLabels = CAMPAIGN_STATUS_LABELS;

  protected readonly loading = signal(true);
  protected readonly brandProfiles = this.brandProfileService.profiles;

  protected readonly campaignsByBrand = computed(() => {
    const campaigns = this.campaignService.campaigns();
    const map = new Map<string, typeof campaigns>();
    for (const c of campaigns) {
      const list = map.get(c.brandProfileId) ?? [];
      list.push(c);
      map.set(c.brandProfileId, list);
    }
    return map;
  });

  constructor() {
    this.seo.setPageSeo({
      title: 'مشاريعي | رواج',
      description: 'العلامات التجارية وحملاتها التي يمكنك العمل عليها.',
      keywords: 'رواج, مشاريعي, العلامات التجارية, الحملات',
      path: '/dashboard/my-projects',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    this.loading.set(true);
    this.brandProfileService.refresh().subscribe({
      next: () => {
        this.campaignService.refresh().subscribe({
          next: () => this.loading.set(false),
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  protected campaignsFor(brandProfileId: string) {
    return this.campaignsByBrand().get(brandProfileId) ?? [];
  }
}
