import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { CampaignService } from '../../../services/campaign.service';
import { BRAND_PROFILE_STATUS_LABELS, BRAND_VOICE_LABELS } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

@Component({
  selector: 'app-brand-profiles-page',
  imports: [RouterLink, PageHeader, TooltipDirective],
  templateUrl: './brand-profiles-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './brand-profiles-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandProfilesPage {
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  private readonly seo = inject(SeoService);

  protected readonly profiles = this.brandProfileService.profiles;
  protected readonly statusLabels = BRAND_PROFILE_STATUS_LABELS;
  protected readonly voiceLabels = BRAND_VOICE_LABELS;

  constructor() {
    this.seo.setPageSeo({
      title: 'ملفات العلامة التجارية | رواج',
      description: 'أنشئ وأدر ملفات العلامة التجارية — المظلة التي تنضوي تحتها حملاتك وإعلاناتك.',
      keywords: 'رواج, العلامة التجارية, ملف العلامة, الحملات',
      path: '/dashboard/brand-profiles',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected campaignCount(brandProfileId: string): number {
    return this.campaignService.byBrandProfile(brandProfileId)().length;
  }

  protected archive(id: string): void {
    this.brandProfileService.archive(id);
  }
}
