import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CampaignsApiService } from '../../../core/api/campaigns-api.service';
import { ContentApiService } from '../../../core/api/content-api.service';
import { SchedulingApiService } from '../../../core/api/scheduling-api.service';
import { SocialAccountsApiService } from '../../../core/api/social-accounts-api.service';
import { VisualAssetsApiService } from '../../../core/api/visual-assets-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import {
  CampaignDetail,
  CampaignSummary,
  ContentItemSummary,
  ContentTemplateStyle,
  Language,
  MarketingPlan,
  SocialAccountSummary,
} from '../../../core/models';
import { ScheduleModal } from '../../my-media/schedule-modal/schedule-modal';

const PLATFORM_LABELS: Record<string, string> = {
  Instagram: 'إنستغرام',
  Facebook: 'فيسبوك',
  Tiktok: 'تيك توك',
  Youtube: 'يوتيوب',
  Twitter: 'إكس',
  Linkedin: 'لينكد إن',
};

const TEMPLATE_LABELS: Record<ContentTemplateStyle, string> = {
  Auto: 'تلقائي (تنويع طبيعي)',
  ProductHighlight: 'عرض منتج/خدمة',
  PromotionalOffer: 'عرض ترويجي',
  EducationalTip: 'نصيحة تعليمية',
  EngagementQuestion: 'سؤال تفاعلي',
  BehindTheScenes: 'خلف الكواليس',
  Testimonial: 'شهادة عميل',
  Announcement: 'إعلان/خبر',
};

type ScheduleOutcome = { scheduledAt: string } | { error: string };

@Component({
  selector: 'app-marketing-plan-page',
  imports: [ScheduleModal, RouterLink],
  templateUrl: './marketing-plan-page.html',
  styleUrl: './marketing-plan-page.css',
})
export class MarketingPlanPage {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly contentApi = inject(ContentApiService);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly socialAccountsApi = inject(SocialAccountsApiService);
  private readonly visualAssetsApi = inject(VisualAssetsApiService);
  private readonly tenantService = inject(TenantService);

  protected readonly platformLabels = PLATFORM_LABELS;
  protected readonly templateLabels = TEMPLATE_LABELS;
  protected readonly templateStyles: ContentTemplateStyle[] = [
    'Auto', 'ProductHighlight', 'PromotionalOffer', 'EducationalTip', 'EngagementQuestion', 'BehindTheScenes', 'Testimonial', 'Announcement',
  ];

  protected readonly campaigns = signal<CampaignSummary[]>([]);
  protected readonly loadingCampaigns = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly selectedCampaign = signal<CampaignDetail | null>(null);
  protected readonly loadingDetail = signal(false);
  protected readonly generating = signal(false);
  protected readonly generateError = signal<string | null>(null);

  protected readonly generatedPosts = signal<ContentItemSummary[]>([]);
  protected readonly loadingPosts = signal(false);
  protected readonly postCount = signal(5);
  protected readonly contentLanguage = signal<Language>('Ar');
  protected readonly includeImages = signal(true);
  protected readonly templateStyle = signal<ContentTemplateStyle>('Auto');
  protected readonly generatingContent = signal(false);
  protected readonly contentGenError = signal<string | null>(null);
  protected readonly contentGenNote = signal<string | null>(null);

  protected readonly socialAccounts = signal<SocialAccountSummary[]>([]);
  protected readonly reviewingId = signal<string | null>(null);
  protected readonly regeneratingId = signal<string | null>(null);
  protected readonly regenerateFeedback = signal<Record<string, string | undefined>>({});
  protected readonly openFeedbackFor = signal<string | null>(null);

  protected readonly schedulingAll = signal(false);
  protected readonly scheduleAllError = signal<string | null>(null);
  protected readonly manualScheduleContentItemId = signal<string | null>(null);
  protected readonly schedulingPostId = signal<string | null>(null);
  protected readonly scheduleResultByPost = signal<Record<string, ScheduleOutcome>>({});
  protected readonly generatingImageForPost = signal<string | null>(null);

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

  protected readonly activeAccountsByPlatform = computed(() => {
    const map = new Map<string, SocialAccountSummary>();
    for (const account of this.socialAccounts()) {
      if (account.isActive) map.set(account.platform, account);
    }
    return map;
  });

  protected readonly schedulablePosts = computed(() => {
    const results = this.scheduleResultByPost();
    return this.generatedPosts().filter(
      (p) =>
        p.status === 'Approved' &&
        !!p.suggestedPostAt &&
        this.activeAccountsByPlatform().has(p.platform) &&
        !('scheduledAt' in (results[p.contentItemId] ?? {})),
    );
  });

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.loadCampaigns(brandProfileId);
        this.socialAccountsApi.getByBrand(brandProfileId).subscribe({
          next: (accounts) => this.socialAccounts.set(accounts),
        });
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
    this.scheduleResultByPost.set({});

    this.campaignsApi.getById(campaignId).subscribe({
      next: (detail) => {
        this.selectedCampaign.set(detail);
        this.loadingDetail.set(false);
        this.loadPosts(campaignId);
        this.loadExistingSchedules(campaignId);
      },
      error: (error: unknown) => {
        this.loadingDetail.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل تفاصيل الحملة.');
      },
    });
  }

  /** Posts already scheduled in a previous session still show up as "Approved" (there's no
   * separate ContentStatus for it) - without this, a fresh page load would offer to schedule them
   * again. */
  private loadExistingSchedules(campaignId: string): void {
    this.schedulingApi.getAll(campaignId, 1, 200).subscribe({
      next: (result) => {
        const results: Record<string, ScheduleOutcome> = {};
        for (const post of result.items) {
          if (post.status === 'Pending' || post.status === 'Published') {
            results[post.contentItemId] = { scheduledAt: post.scheduledAt };
          }
        }
        this.scheduleResultByPost.set(results);
      },
    });
  }

  private loadPosts(campaignId: string): void {
    this.loadingPosts.set(true);

    this.contentApi.getByCampaign(campaignId, 1, 50).subscribe({
      next: (result) => {
        this.generatedPosts.set(result.items);
        this.loadingPosts.set(false);
      },
      error: () => this.loadingPosts.set(false),
    });
  }

  protected backToList(): void {
    this.selectedCampaign.set(null);
    this.generatedPosts.set([]);
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

  protected setPostCount(value: string): void {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      this.postCount.set(Math.min(15, Math.max(1, Math.round(parsed))));
    }
  }

  protected setContentLanguage(language: Language): void {
    this.contentLanguage.set(language);
  }

  protected setIncludeImages(value: boolean): void {
    this.includeImages.set(value);
  }

  protected setTemplateStyle(value: ContentTemplateStyle): void {
    this.templateStyle.set(value);
  }

  protected generateContent(): void {
    const campaign = this.selectedCampaign();
    if (!campaign) return;

    this.generatingContent.set(true);
    this.contentGenError.set(null);
    this.contentGenNote.set(null);
    this.scheduleResultByPost.set({});

    this.campaignsApi.generateContent(campaign.campaignId, {
      postCount: this.postCount(),
      language: this.contentLanguage(),
      includeImages: this.includeImages(),
      templateStyle: this.templateStyle(),
    }).subscribe({
      next: (result) => {
        this.generatingContent.set(false);
        if (result.imagesSkippedForCredits > 0) {
          this.contentGenNote.set(
            `تم توليد ${result.imagesGenerated} صورة، وتخطي ${result.imagesSkippedForCredits} بسبب نفاد رصيد الذكاء الاصطناعي الشهري.`,
          );
        }
        this.loadPosts(campaign.campaignId);
      },
      error: (error: unknown) => {
        this.generatingContent.set(false);
        this.contentGenError.set(
          error instanceof ApiError ? error.message : 'تعذر توليد محتوى الحملة، حاول مرة أخرى.',
        );
      },
    });
  }

  protected reviewPost(contentItemId: string, approve: boolean): void {
    this.reviewingId.set(contentItemId);

    this.contentApi.review(contentItemId, approve).subscribe({
      next: (result) => {
        this.reviewingId.set(null);
        this.generatedPosts.update((posts) =>
          posts.map((p) => (p.contentItemId === contentItemId ? { ...p, status: result.status as ContentItemSummary['status'] } : p)),
        );

        if (approve) {
          this.tryAutoSchedule(contentItemId);
        }
      },
      error: () => this.reviewingId.set(null),
    });
  }

  /** Approving a post that has an AI-suggested time and a matching connected account schedules it
   * immediately, so "accept" visibly results in the post being scheduled and appearing on the
   * calendar instead of silently doing nothing further. */
  private tryAutoSchedule(contentItemId: string): void {
    const post = this.generatedPosts().find((p) => p.contentItemId === contentItemId);
    if (!post || !post.suggestedPostAt) return;

    const account = this.activeAccountsByPlatform().get(post.platform);
    if (!account) return;

    this.scheduleOnePost(post.contentItemId, post.visualAssetId, account.socialAccountId, post.suggestedPostAt);
  }

  private scheduleOnePost(
    contentItemId: string,
    visualAssetId: string | null,
    socialAccountId: string,
    scheduledAt: string,
  ): void {
    this.schedulingPostId.set(contentItemId);

    this.schedulingApi
      .schedule({ contentItemId, visualAssetId, socialAccountId, scheduledAt, aiSuggestedTime: true })
      .subscribe({
        next: () => {
          this.schedulingPostId.set(null);
          this.scheduleResultByPost.update((current) => ({ ...current, [contentItemId]: { scheduledAt } }));
        },
        error: (error: unknown) => {
          this.schedulingPostId.set(null);
          this.scheduleResultByPost.update((current) => ({
            ...current,
            [contentItemId]: { error: error instanceof ApiError ? error.message : 'تعذرت الجدولة التلقائية.' },
          }));
        },
      });
  }

  protected scheduledAtFor(contentItemId: string): string | null {
    const outcome = this.scheduleResultByPost()[contentItemId];
    return outcome && 'scheduledAt' in outcome ? outcome.scheduledAt : null;
  }

  protected scheduleErrorFor(contentItemId: string): string | null {
    const outcome = this.scheduleResultByPost()[contentItemId];
    return outcome && 'error' in outcome ? outcome.error : null;
  }

  protected hasNoMatchingAccount(post: ContentItemSummary): boolean {
    return !!post.suggestedPostAt && !this.activeAccountsByPlatform().has(post.platform);
  }

  protected generateImageForPost(post: ContentItemSummary): void {
    const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
    const campaign = this.selectedCampaign();
    if (!brandProfileId || !campaign) return;

    this.generatingImageForPost.set(post.contentItemId);

    this.visualAssetsApi
      .generate({
        brandProfileId,
        campaignId: campaign.campaignId,
        contentItemId: post.contentItemId,
        type: 'Image',
        prompt: post.content,
      })
      .subscribe({
        next: (result) => {
          this.generatingImageForPost.set(null);
          this.generatedPosts.update((posts) =>
            posts.map((p) =>
              p.contentItemId === post.contentItemId
                ? { ...p, imageUrl: result.fileUrl, visualAssetId: result.visualAssetId }
                : p,
            ),
          );
        },
        error: () => this.generatingImageForPost.set(null),
      });
  }

  protected toggleFeedback(contentItemId: string): void {
    this.openFeedbackFor.update((current) => (current === contentItemId ? null : contentItemId));
  }

  protected setFeedback(contentItemId: string, value: string): void {
    this.regenerateFeedback.update((current) => ({ ...current, [contentItemId]: value }));
  }

  protected regeneratePost(contentItemId: string): void {
    const feedback = this.regenerateFeedback()[contentItemId]?.trim();
    if (!feedback) return;

    this.regeneratingId.set(contentItemId);

    this.contentApi.regenerate(contentItemId, feedback).subscribe({
      next: (result) => {
        this.regeneratingId.set(null);
        this.openFeedbackFor.set(null);
        this.generatedPosts.update((posts) =>
          posts.map((p) =>
            p.contentItemId === contentItemId ? { ...p, content: result.content, status: result.status as ContentItemSummary['status'] } : p,
          ),
        );
      },
      error: () => this.regeneratingId.set(null),
    });
  }

  /** Bulk fallback for posts approved before their platform had a connected account - the
   * per-post auto-schedule in reviewPost()/tryAutoSchedule() already handles the common case. */
  protected scheduleAllApproved(): void {
    this.schedulingAll.set(true);
    this.scheduleAllError.set(null);

    const posts = this.schedulablePosts();
    let remaining = posts.length;
    let failures = 0;

    for (const post of posts) {
      const account = this.activeAccountsByPlatform().get(post.platform)!;
      this.schedulingApi
        .schedule({
          contentItemId: post.contentItemId,
          visualAssetId: post.visualAssetId,
          socialAccountId: account.socialAccountId,
          scheduledAt: post.suggestedPostAt!,
          aiSuggestedTime: true,
        })
        .subscribe({
          next: () => {
            this.scheduleResultByPost.update((current) => ({
              ...current,
              [post.contentItemId]: { scheduledAt: post.suggestedPostAt! },
            }));
            remaining -= 1;
            if (remaining === 0) this.finishScheduleAll(failures);
          },
          error: () => {
            failures += 1;
            remaining -= 1;
            if (remaining === 0) this.finishScheduleAll(failures);
          },
        });
    }
  }

  private finishScheduleAll(failures: number): void {
    this.schedulingAll.set(false);
    if (failures > 0) {
      this.scheduleAllError.set(`تعذرت جدولة ${failures} منشور(ات). يمكنك جدولتها يدويًا.`);
    }
  }

  protected openManualSchedule(contentItemId: string): void {
    this.manualScheduleContentItemId.set(contentItemId);
  }

  protected closeManualSchedule(): void {
    this.manualScheduleContentItemId.set(null);
  }

  protected onManualScheduled(): void {
    const campaign = this.selectedCampaign();
    if (campaign) this.loadPosts(campaign.campaignId);
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

  protected formatSuggested(dateIso: string): string {
    return new Date(dateIso).toLocaleString('ar', { dateStyle: 'medium', timeStyle: 'short' });
  }

  protected platformLabel(platform: string): string {
    return this.platformLabels[platform] ?? platform;
  }
}
