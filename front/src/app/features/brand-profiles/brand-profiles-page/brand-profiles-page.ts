import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { CampaignService } from '../../../services/campaign.service';
import { BRAND_PROFILE_STATUS_LABELS, BRAND_VOICE_LABELS } from '../../../model/brand-profile.model';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { isBrandLimitReached, promptBrandLimitUpgrade } from '../../../shared/utils/upgrade-prompts.util';

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
  private readonly tenantService = inject(TenantService);
  private readonly seo = inject(SeoService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);
  private readonly router = inject(Router);

  protected readonly profiles = this.brandProfileService.profiles;
  protected readonly statusLabels = BRAND_PROFILE_STATUS_LABELS;
  protected readonly voiceLabels = BRAND_VOICE_LABELS;

  /** Archiving is Admin-only server-side (ArchiveBrandProfileCommand) — hide the action for
   *  Editor/Viewer rather than letting them hit a 403 after clicking it. */
  protected readonly canArchive = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin';
  });

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

    this.brandProfileService.refresh().subscribe();
  }

  protected campaignCount(brandProfileId: string): number {
    return this.campaignService.byBrandProfile(brandProfileId)().length;
  }

  /** Gate before entering the wizard, not after — the old behavior let the user fill out every
   *  step and only found out about the plan limit on final submit. */
  protected createBrandProfile(): void {
    if (isBrandLimitReached(this.tenantService)) {
      promptBrandLimitUpgrade(this.tenantService, this.errorModalService);
      return;
    }
    void this.router.navigate(['/dashboard/brand-profiles/new']);
  }

  protected archive(id: string): void {
    this.loaderService.show();
    this.brandProfileService.archive(id).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status !== 'success') {
          this.errorModalService.show(res.message ?? 'تعذّرت أرشفة ملف العلامة التجارية.', { variant: 'error' });
        }
      },
      error: err => {
        this.loaderService.hide();
        this.errorModalService.show(
          extractApiErrorMessage(err, 'تعذّرت أرشفة ملف العلامة التجارية. يرجى المحاولة مرة أخرى.'),
          { variant: 'error' },
        );
      },
    });
  }
}
