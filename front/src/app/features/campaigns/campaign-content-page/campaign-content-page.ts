import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ContentItemService } from '../../../services/content-item.service';
import { SocialAccountService } from '../../../core/social/social-account.service';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { GetCampaignResponse } from '../../../model/campaign.model';
import { ContentItemSummary } from '../../../model/content-item.model';
import { SocialAccountSummary } from '../../../model/social-account.model';

const CONTENT_TYPE_LABELS: Record<ContentItemSummary['contentType'], string> = {
  Post: 'بوست', Story: 'قصة', ReelScript: 'ريل', AdCopy: 'إعلان', Blog: 'مقال', Caption: 'كابشن',
};

const PLATFORM_LABELS: Record<ContentItemSummary['platform'], string> = {
  Instagram: 'إنستغرام', Facebook: 'فيسبوك', Tiktok: 'تيك توك', Twitter: 'إكس', Youtube: 'يوتيوب', Linkedin: 'لينكدإن',
};

const STATUS_LABELS: Record<ContentItemSummary['status'], string> = {
  Draft: 'مسودة', Reviewed: 'تمت المراجعة', Approved: 'مقبول', Rejected: 'مرفوض', Published: 'منشور',
};

@Component({
  selector: 'app-campaign-content-page',
  imports: [PageHeader],
  templateUrl: './campaign-content-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-content-page.css'],
})
export class CampaignContentPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly socialAccountService = inject(SocialAccountService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly seo = inject(SeoService);

  protected readonly contentTypeLabels = CONTENT_TYPE_LABELS;
  protected readonly platformLabels = PLATFORM_LABELS;
  protected readonly statusLabels = STATUS_LABELS;

  protected readonly campaignId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly campaign = signal<GetCampaignResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly items = this.contentItemService.items;
  protected readonly generating = signal(false);
  protected readonly postCount = signal(6);

  protected readonly socialAccounts = signal<SocialAccountSummary[]>([]);
  protected readonly connectedFacebook = computed(() =>
    this.socialAccounts().find(a => a.platform === 'Facebook' && a.isActive),
  );
  protected readonly scheduling = signal(false);
  protected readonly scheduleSummary = signal<string | null>(null);

  /** Per-item feedback textarea, keyed by contentItemId — only one open at a time. */
  protected readonly regeneratingId = signal<string | null>(null);
  protected readonly regenerateFeedback = signal('');
  protected readonly busyItemId = signal<string | null>(null);

  protected readonly approvedItems = computed(() => this.items().filter(i => i.status === 'Approved'));

  constructor() {
    this.seo.setPageSeo({
      title: 'محتوى الحملة | رواج',
      description: 'راجع منشورات الحملة، اقبلها أو ارفضها أو أعد توليدها، ثم جدولها.',
      keywords: 'رواج, محتوى الحملة, مراجعة المحتوى, جدولة',
      path: '/dashboard/campaigns/' + this.campaignId + '/content',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    if (this.campaignId) this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.campaignService.getCampaign(this.campaignId).subscribe({
      next: res => {
        this.loading.set(false);
        if (!res.data) {
          this.loadError.set('لم يتم العثور على الحملة.');
          return;
        }
        this.campaign.set(res.data);
        this.contentItemService.refresh(res.data.brandProfileId, this.campaignId).subscribe();
        this.socialAccountService.getByBrand(res.data.brandProfileId).subscribe(r => {
          if (r.data) this.socialAccounts.set(r.data);
        });
      },
      error: err => {
        this.loading.set(false);
        this.loadError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.'));
      },
    });
  }

  protected updatePostCount(value: string): void {
    const n = parseInt(value, 10);
    this.postCount.set(isNaN(n) ? 1 : Math.max(1, Math.min(n, 20)));
  }

  protected generateContent(): void {
    const campaign = this.campaign();
    if (!campaign || this.generating()) return;

    if (!campaign.planApprovedAt) {
      this.errorModalService.show(
        'يجب اعتماد استراتيجية الحملة أولاً قبل توليد المحتوى.', { variant: 'warning' },
      );
      return;
    }

    this.generating.set(true);
    this.campaignService.generateContent(this.campaignId, {
      postCount: this.postCount(),
      language: 'Ar',
      includeImages: true,
    }).subscribe({
      next: () => {
        this.generating.set(false);
        this.contentItemService.refresh(campaign.brandProfileId, this.campaignId).subscribe();
      },
      error: err => {
        this.generating.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر توليد المحتوى.'), { variant: 'error' });
      },
    });
  }

  protected review(item: ContentItemSummary, approve: boolean): void {
    if (this.busyItemId()) return;
    this.busyItemId.set(item.contentItemId);
    this.contentItemService.review(item.contentItemId, approve).subscribe({
      next: () => {
        this.busyItemId.set(null);
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId).subscribe();
      },
      error: err => {
        this.busyItemId.set(null);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تحديث حالة المنشور.'), { variant: 'error' });
      },
    });
  }

  protected startRegenerate(item: ContentItemSummary): void {
    this.regeneratingId.set(item.contentItemId);
    this.regenerateFeedback.set('');
  }

  protected cancelRegenerate(): void {
    this.regeneratingId.set(null);
    this.regenerateFeedback.set('');
  }

  protected updateRegenerateFeedback(value: string): void {
    this.regenerateFeedback.set(value);
  }

  protected submitRegenerate(item: ContentItemSummary): void {
    const feedback = this.regenerateFeedback().trim();
    if (!feedback || this.busyItemId()) return;

    this.busyItemId.set(item.contentItemId);
    this.contentItemService.regenerate(item.contentItemId, feedback).subscribe({
      next: () => {
        this.busyItemId.set(null);
        this.regeneratingId.set(null);
        this.regenerateFeedback.set('');
        const brandProfileId = this.campaign()?.brandProfileId;
        if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId).subscribe();
      },
      error: err => {
        this.busyItemId.set(null);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إعادة توليد المنشور.'), { variant: 'error' });
      },
    });
  }

  protected schedulePosts(): void {
    const account = this.connectedFacebook();
    if (!account || this.scheduling()) return;

    this.scheduling.set(true);
    this.scheduleSummary.set(null);
    this.campaignService.schedulePosts(this.campaignId, account.socialAccountId).subscribe({
      next: res => {
        this.scheduling.set(false);
        if (res.data) {
          this.scheduleSummary.set(`تمت جدولة ${res.data.succeededCount} منشور بنجاح${res.data.failedCount > 0 ? `، وفشل ${res.data.failedCount}` : ''}.`);
          const brandProfileId = this.campaign()?.brandProfileId;
          if (brandProfileId) this.contentItemService.refresh(brandProfileId, this.campaignId).subscribe();
        }
      },
      error: err => {
        this.scheduling.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر جدولة منشورات الحملة.'), { variant: 'error' });
      },
    });
  }

  protected goToConnectFacebook(): void {
    void this.router.navigate(['/dashboard/social-accounts']);
  }
}
