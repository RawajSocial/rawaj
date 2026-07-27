import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { SeoService } from '../../../services/seo.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { ApprovalOnboardingData, OnboardingPlanApproval } from '../../on-boarding/onboarding-plan-approval/onboarding-plan-approval';

/** Standalone "review the strategy" page for a campaign the user is coming back to — the
 *  research/diagnose/generate-plan/refine-plan/approve-plan pipeline previously only existed
 *  inside the one-shot onboarding wizard, so a campaign card or content-page banner pointing at
 *  "review the strategy" for anything but a brand-new campaign had nowhere real to send the user.
 *  Reuses OnboardingPlanApproval, which hydrates from the campaign's already-generated plan
 *  instead of re-running (and re-charging for) the pipeline when one already exists. */
@Component({
  selector: 'app-campaign-strategy-page',
  imports: [PageHeader, OnboardingPlanApproval],
  templateUrl: './campaign-strategy-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../campaign-detail-page/campaign-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignStrategyPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly seo = inject(SeoService);

  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');
  protected readonly campaign = computed(() => this.campaignService.getById(this.campaignId())());

  /** The onboarding wizard's collected answers, persisted server-side as the campaign's
   *  `briefJson` — parsed here so Target audience/Brand voice render with real data on resume
   *  too, not just mid-wizard where the wizard still holds this in local state. */
  protected readonly briefData = signal<ApprovalOnboardingData | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  constructor() {
    effect(() => {
      const c = this.campaign();
      const id = this.campaignId();
      this.seo.setPageSeo({
        title: 'استراتيجية ' + (c ? c.name : 'الحملة') + ' | رواج',
        description: 'راجع استراتيجية الحملة واعتمدها لبدء توليد المحتوى.',
        keywords: 'رواج, استراتيجية الحملة, اعتماد الخطة',
        path: '/dashboard/campaigns/' + id + '/strategy',
        image: '/home-hero-light.png',
        type: 'website',
        noIndex: true,
      });
    });

    // Re-loads whenever the route's campaign id actually changes — the router reuses this
    // component instance across in-app navigation between two campaigns' strategy pages.
    effect(() => {
      const id = this.campaignId();
      this.briefData.set(null);
      this.loadError.set(null);
      if (!id) return;

      this.loading.set(true);
      this.campaignService.getCampaign(id).subscribe({
        next: res => {
          this.loading.set(false);
          if (!res.data) {
            this.loadError.set('لم يتم العثور على الحملة.');
            return;
          }
          this.briefData.set(this.parseBriefJson(res.data.briefJson));
        },
        error: err => {
          this.loading.set(false);
          this.loadError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.'));
        },
      });
    });
  }

  private parseBriefJson(raw: string | null | undefined): ApprovalOnboardingData | null {
    if (!raw) return null;
    try { return JSON.parse(raw) as ApprovalOnboardingData; } catch { return null; }
  }

  /** Once approved, the content page is the natural next stop and can pick up content generation
   *  itself — same handoff the onboarding wizard uses at the end of its own approval step. */
  protected onApprove(): void {
    void this.router.navigate(['/dashboard/campaigns', this.campaignId(), 'content'], {
      queryParams: { autogenerate: 1 },
    });
  }

  protected onBack(): void {
    void this.router.navigate(['/dashboard/campaigns', this.campaignId()]);
  }
}
