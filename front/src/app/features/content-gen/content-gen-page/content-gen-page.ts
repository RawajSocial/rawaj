import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MediaService } from '../../../services/media.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { CampaignService } from '../../../services/campaign.service';
import { ContentItemService } from '../../../services/content-item.service';
import { VisualAssetService } from '../../../services/visual-asset.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { SocialAccountService } from '../../../core/social/social-account.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { CoinPricingService } from '../../../services/coin-pricing.service';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { FeatureFlagsService } from '../../../services/feature-flags.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { nextOccurrence, toDateInputValue, toTimeInputValue } from '../../../shared/utils/posting-time.util';
import { ContentItemSummary } from '../../../model/content-item.model';
import { SocialAccountSummary } from '../../../model/social-account.model';
import {
  GeneratedAsset, GenType, AdSize, ContentTone, TextType,
  TYPE_CFG, SIZE_CFG, TONE_CFG, TEXT_TYPE_CFG,
} from '../../../model/generated-item.model';

/** Maps the page's free-text-type picker to the backend's ContentType enum — there's no
 *  dedicated "hashtags" content type server-side, so it rides along on Caption. */
const TEXT_TYPE_TO_CONTENT_TYPE: Record<TextType, ContentItemSummary['contentType']> = {
  caption: 'Caption', hashtags: 'Caption', 'ad-copy': 'AdCopy', blog: 'Blog',
};

/** Maps the page's platform picker (includes snapchat, which the backend doesn't model) to the
 *  backend's SocialPlatform — falls back to Instagram so generation never blocks on an unmapped one. */
const PLATFORM_TO_BACKEND: Record<string, ContentItemSummary['platform']> = {
  instagram: 'Instagram', facebook: 'Facebook', tiktok: 'Tiktok', x: 'Twitter', youtube: 'Youtube', linkedin: 'Linkedin',
};

const PLATFORM_OPTS = [
  { value: 'instagram', label: 'إنستغرام',  icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)' },
  { value: 'facebook',  label: 'فيسبوك',    icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)' },
  { value: 'tiktok',    label: 'تيك توك',   icon: 'fa-brands fa-tiktok',     color: 'var(--color-tiktok)' },
  { value: 'x',         label: 'إكس',        icon: 'fa-brands fa-x-twitter',  color: 'var(--color-x)' },
  { value: 'snapchat',  label: 'سناب شات',  icon: 'fa-brands fa-snapchat',   color: 'var(--color-snapchat)' },
  { value: 'youtube',   label: 'يوتيوب',    icon: 'fa-brands fa-youtube',    color: 'var(--color-youtube)' },
  { value: 'linkedin',  label: 'لينكد إن',  icon: 'fa-brands fa-linkedin-in',color: 'var(--color-linkedin)' },
];

const QUALITY_OPTS = [
  { value: 'standard', label: 'قياسية',     desc: 'سريعة ومناسبة لمعظم الاحتياجات' },
  { value: 'high',     label: 'عالية',      desc: 'تفاصيل أوضح وألوان أكثر دقة' },
  { value: 'ultra',    label: 'فائقة (4K)', desc: 'أعلى جودة، تستغرق وقتاً أطول' },
];

@Component({
  selector: 'app-content-gen-page',
  standalone: true,
  imports: [RouterLink, Breadcrumb, TooltipDirective],
  templateUrl: './content-gen-page.html',
  styleUrls: [
    '../../../features/on-boarding/onboarding-shared.css',
    './content-gen-page.css',
  ],
})
export class ContentGenPage {
  readonly typeCfg     = TYPE_CFG;
  readonly sizeCfg     = SIZE_CFG;
  readonly toneCfg     = TONE_CFG;
  readonly textTypeCfg = TEXT_TYPE_CFG;
  readonly platforms   = PLATFORM_OPTS;
  readonly qualities   = QUALITY_OPTS;

  private readonly featureFlagsService = inject(FeatureFlagsService);

  readonly genTypes = computed<GenType[]>(() =>
    this.featureFlagsService.flags().videoGeneration ? ['static-ad', 'video', 'text'] : ['static-ad', 'text'],
  );
  readonly sizes:     AdSize[]       = ['square', 'portrait', 'landscape', 'story'];
  readonly tones:     ContentTone[]  = ['professional', 'casual', 'energetic', 'luxurious'];
  readonly textTypes: TextType[]     = ['caption', 'hashtags', 'ad-copy', 'blog'];

  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly visualAssetService = inject(VisualAssetService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly socialAccountService = inject(SocialAccountService);
  private readonly brandContextService = inject(BrandContextService);
  protected readonly tenantService = inject(TenantService);
  protected readonly perms = inject(PermissionService);
  private readonly coinPricingService = inject(CoinPricingService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly seo = inject(SeoService);

  readonly brandProfiles = this.brandProfileService.profiles;

  // ── Wallet selector ──
  // A member invited into someone else's tenant belongs to (at least) two: their own account and
  // the tenant that invited them. Which one is active decides both which brand profiles are
  // selectable above AND whose coin balance gets charged for this generation (CoinPolicy debits
  // the ACTIVE tenant's wallet) — so this must be explicit, never silently defaulted.
  readonly showWalletSelector = computed(() => this.tenantService.memberships().length > 1);
  readonly activeMembership = computed(() =>
    this.tenantService.memberships().find(m => m.tenantId === this.tenantService.activeTenantId()),
  );
  walletOpen = signal(false);

  switchWallet(tenantId: string): void {
    this.walletOpen.set(false);
    if (tenantId === this.tenantService.activeTenantId()) return;
    this.tenantService.switchTenant(tenantId);
    // Brand/campaign selections belong to the PREVIOUS tenant — reset them, then reload the new
    // tenant's lists (the brand-profile/campaign services are tenant-scoped via X-Tenant-Id).
    this.formBrandProfileId.set('');
    this.formCampaignId.set('');
    this.brandProfileService.refresh().subscribe();
    this.campaignService.refresh().subscribe();
  }

  // ── Form signals ──
  genType      = signal<GenType>('static-ad');
  formBrand    = signal('');
  formDesc     = signal('');
  formTone     = signal<ContentTone>('professional');
  formSize     = signal<AdSize>('square');
  formLang     = signal('ar');
  formTextType = signal<TextType>('caption');
  formPlatform = signal('instagram');
  formQuality  = signal('high');
  // Seeded from the global header brand selection; "no campaign" (`''`) is
  // always the default here regardless of the global campaign filter.
  formBrandProfileId = signal(this.brandContextService.selectedBrandProfileId() ?? '');
  formCampaignId      = signal('');
  formAssets   = signal<GeneratedAsset[]>([]);

  readonly availableCampaigns = computed(() => {
    const bp = this.formBrandProfileId();
    return bp ? this.campaignService.byBrandProfile(bp)() : this.campaignService.campaigns();
  });

  // ── Dropdown open states ──
  typeOpen     = signal(false);
  toneOpen     = signal(false);
  sizeOpen     = signal(false);
  langOpen     = signal(false);
  textTypeOpen = signal(false);
  platformOpen = signal(false);
  qualityOpen  = signal(false);
  brandProfileOpen = signal(false);
  campaignOpen      = signal(false);

  // ── Current output (single preview, not a gallery) ──
  currentItemId = signal<string | null>(null);
  readonly currentItem = computed(() => {
    const id = this.currentItemId();
    return id ? this.media.items().find(i => i.id === id) ?? null : null;
  });

  // ── Edit-with-prompt (re-generate on top of the current result) ──
  editPrompt = signal('');
  readonly canApplyEdit = computed(() => this.editPrompt().trim().length > 0);

  // ── Inline schedule panel — only reachable for text generations, which have a real ContentItem
  // id to schedule against. Image-only generations here have no ContentItem (SchedulePostCommand
  // requires one), so scheduling them isn't offered from this page. ──
  readonly canScheduleCurrent = computed(() => {
    const item = this.currentItem();
    return !!item && item.type === 'text' && item.status === 'generated';
  });

  private readonly approvedContentId = signal<string | null>(null);
  readonly isApproved = computed(() => this.approvedContentId() === this.currentItem()?.id);
  approving = signal(false);

  schedulePanelOpen = signal(false);
  scheduleAccounts = signal<SocialAccountSummary[]>([]);
  scheduleAccountId = signal('');
  scheduleDate = signal('');
  scheduleTime = signal('');
  scheduling = signal(false);
  readonly scheduledIds = signal<Set<string>>(new Set());

  /** Reloads the connected-account list whenever the brand changes, so the schedule panel's
   *  picker never offers a stale account from a previously-selected brand. */
  private readonly reloadScheduleAccounts = effect(() => {
    const brandProfileId = this.formBrandProfileId();
    if (!brandProfileId) { this.scheduleAccounts.set([]); return; }
    this.socialAccountService.getByBrand(brandProfileId).subscribe(res => {
      if (res.data) this.scheduleAccounts.set(res.data);
    });
  });

  // ── Computeds ──
  readonly typeLabel     = computed(() => TYPE_CFG[this.genType()].label);
  readonly toneLabel     = computed(() => TONE_CFG[this.formTone()]);
  readonly sizeLabel     = computed(() => SIZE_CFG[this.formSize()].label);
  readonly langLabel     = computed(() => ({ ar: 'عربي', en: 'English', 'ar-eg': 'عربي مصري' }[this.formLang()] ?? this.formLang()));
  readonly textTypeLabel = computed(() => TEXT_TYPE_CFG[this.formTextType()]);
  readonly platformLabel = computed(() => PLATFORM_OPTS.find(p => p.value === this.formPlatform())?.label ?? '');
  readonly qualityLabel  = computed(() => QUALITY_OPTS.find(q => q.value === this.formQuality())?.label ?? '');
  readonly brandProfileLabel = computed(() => this.brandProfiles().find(p => p.id === this.formBrandProfileId())?.name ?? 'بدون تحديد');
  readonly campaignLabel     = computed(() => this.availableCampaigns().find(c => c.id === this.formCampaignId())?.name ?? 'بدون تحديد');
  readonly canGenerate   = computed(() => this.formBrand().trim().length > 0 && this.formDesc().trim().length > 0);

  /** What this generation will actually cost — real numbers from the backend's coin-pricing
   *  endpoint (already reflects this tenant's plan discount), so it can never drift from what
   *  would actually be charged once this page is wired to the real generation endpoints. */
  readonly spendPreviewLabel = computed(() => {
    const pricing = this.coinPricingService.pricing();
    if (!pricing) return '';

    const isImageType = this.genType() !== 'text';
    const freeRemaining = isImageType ? pricing.freeImageGenerationsRemaining : pricing.freeContentGenerationsRemaining;
    if (freeRemaining > 0) {
      return `توليد مجاني ضمن باقتك التجريبية (متبقي ${freeRemaining})`;
    }

    const cost = isImageType ? pricing.discountedCosts.visualGeneration : pricing.discountedCosts.contentGeneration;
    return `سيتم خصم ${cost.toLocaleString('ar-SA')} كوين من رصيدك عند التوليد`;
  });

  constructor(readonly media: MediaService) {
    this.seo.setPageSeo({
      title: 'توليد المحتوى بالذكاء الاصطناعي | رواج',
      description: 'أنشئ صورًا وفيديوهات وكابشنز احترافية بالذكاء الاصطناعي خلال ثوانٍ.',
      keywords: 'رواج, توليد محتوى, ذكاء اصطناعي, تصميم إعلانات',
      path: '/dashboard/content-gen',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    this.coinPricingService.ensureLoaded();
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    const inDd = (a: string) => !!t.closest(`[data-dd="${a}"]`);
    if (!inDd('type'))         this.typeOpen.set(false);
    if (!inDd('tone'))         this.toneOpen.set(false);
    if (!inDd('size'))         this.sizeOpen.set(false);
    if (!inDd('lang'))         this.langOpen.set(false);
    if (!inDd('texttype'))     this.textTypeOpen.set(false);
    if (!inDd('platform'))     this.platformOpen.set(false);
    if (!inDd('quality'))      this.qualityOpen.set(false);
    if (!inDd('brandprofile')) this.brandProfileOpen.set(false);
    if (!inDd('campaign'))     this.campaignOpen.set(false);
    if (!inDd('wallet'))       this.walletOpen.set(false);
  }

  setBrandProfile(id: string): void {
    this.formBrandProfileId.set(id);
    this.formCampaignId.set('');
    this.brandProfileOpen.set(false);
  }

  onAssetsChange(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (!files) return;
    Array.from(files).forEach(file => {
      const reader = new FileReader();
      reader.onload = e => {
        this.formAssets.update(list => [...list, { name: file.name, url: e.target?.result as string }]);
      };
      reader.readAsDataURL(file);
    });
    (event.target as HTMLInputElement).value = '';
  }

  removeAsset(index: number): void {
    this.formAssets.update(list => list.filter((_, i) => i !== index));
  }

  generate(): void {
    if (!this.canGenerate()) return;

    // Backend supports generation with no brand profile ("standalone" — trying the product out
    // before a brand profile exists) — send undefined rather than an empty string so it's omitted
    // from the request instead of failing GUID binding server-side.
    const brandProfileId = this.formBrandProfileId() || undefined;

    if (this.genType() === 'video') {
      this.errorModalService.show(
        'توليد الفيديو غير متاح حالياً — جرّب إعلاناً ثابتاً أو محتوى نصياً.', { variant: 'info' },
      );
      return;
    }

    const brand = this.formBrand().trim();
    const desc  = this.formDesc().trim();
    const campaignId = this.formCampaignId() || undefined;
    const id = this.media.nextId();
    const isText = this.genType() === 'text';

    this.media.add({
      id,
      type: this.genType(),
      title: desc.substring(0, 24) + (desc.length > 24 ? '…' : ''),
      brand,
      status: 'generating',
      createdAt: new Date().toISOString(),
      size: this.genType() === 'static-ad' ? this.formSize() : undefined,
      tone: this.formTone(),
      language: this.formLang(),
      description: desc,
      brandProfileId,
      campaignId,
      assets: this.formAssets().length ? this.formAssets() : undefined,
    });
    this.currentItemId.set(id);

    if (isText) {
      this.contentItemService.generate({
        brandProfileId,
        campaignId,
        contentType: TEXT_TYPE_TO_CONTENT_TYPE[this.formTextType()],
        platform: PLATFORM_TO_BACKEND[this.formPlatform()] ?? 'Instagram',
        language: this.formLang() === 'en' ? 'En' : 'Ar',
        tone: this.formTone(),
        additionalInstructions: desc,
      }).subscribe({
        next: res => {
          if (res.data) {
            // Swap the local placeholder id for the real backend id so later actions
            // (accept/refuse/regenerate) target the actual content item.
            this.media.replaceId(id, res.data.contentItemId);
            this.media.markGenerated(res.data.contentItemId, { textContent: res.data.content });
            if (this.currentItemId() === id) this.currentItemId.set(res.data.contentItemId);
          } else {
            this.media.markFailed(id);
          }
          this.refreshCoinBalance();
        },
        error: err => {
          this.media.markFailed(id);
          this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر توليد المحتوى.'), { variant: 'error' });
        },
      });
    } else {
      this.visualAssetService.generate({
        brandProfileId,
        campaignId,
        type: 'Ad',
        prompt: desc,
      }).subscribe({
        next: res => {
          if (res.data) {
            this.media.replaceId(id, res.data.visualAssetId);
            this.media.markGenerated(res.data.visualAssetId, { thumbnailUrl: res.data.fileUrl });
            if (this.currentItemId() === id) this.currentItemId.set(res.data.visualAssetId);
          } else {
            this.media.markFailed(id);
          }
          this.refreshCoinBalance();
        },
        error: err => {
          this.media.markFailed(id);
          this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر توليد الصورة.'), { variant: 'error' });
        },
      });
    }

    this.formBrand.set('');
    this.formDesc.set('');
    this.formAssets.set([]);
  }

  applyEdit(): void {
    const item = this.currentItem();
    const prompt = this.editPrompt().trim();
    // Only a successfully-generated item has a real backend id to regenerate/re-edit against.
    if (!item || !prompt || item.status !== 'generated') return;

    const id = item.id;
    const mergedDesc = `${item.description ?? ''}\n\nتعديل: ${prompt}`.trim();
    this.media.markRegenerating(id);

    if (item.type === 'text') {
      // A real backend ContentItem id looks like a GUID; a locally-added item that never
      // resolved (e.g. generation failed) has nothing to regenerate against server-side.
      this.contentItemService.regenerate(id, prompt).subscribe({
        next: res => {
          if (res.data) {
            this.media.markGenerated(id, { description: mergedDesc, textContent: res.data.content });
          } else {
            this.media.markFailed(id);
          }
          this.refreshCoinBalance();
        },
        error: err => {
          this.media.markFailed(id);
          this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تعديل المحتوى.'), { variant: 'error' });
        },
      });
    } else {
      // There's no dedicated "edit" endpoint for images yet — re-run generation with the
      // original brief plus the requested change folded into the prompt. Brand-optional, same
      // as the initial generation.
      const brandProfileId = item.brandProfileId || this.formBrandProfileId() || undefined;
      this.visualAssetService.generate({
        brandProfileId,
        campaignId: item.campaignId,
        type: 'Ad',
        prompt: mergedDesc,
      }).subscribe({
        next: res => {
          if (res.data) {
            // Re-editing produces a brand new visual asset id (no true "edit" endpoint exists yet).
            this.media.replaceId(id, res.data.visualAssetId);
            this.media.markGenerated(res.data.visualAssetId, { description: mergedDesc, thumbnailUrl: res.data.fileUrl });
            if (this.currentItemId() === id) this.currentItemId.set(res.data.visualAssetId);
          } else {
            this.media.markFailed(id);
          }
          this.refreshCoinBalance();
        },
        error: err => {
          this.media.markFailed(id);
          this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تعديل الصورة.'), { variant: 'error' });
        },
      });
    }

    this.editPrompt.set('');
  }

  /** Coin spends here (content-gen, regenerate) happen outside the tenant-refresh flows that
   *  already cover billing/invite actions — pull the new balance so the header updates immediately. */
  private refreshCoinBalance(): void {
    this.tenantService.refresh().subscribe();
    this.coinPricingService.refresh().subscribe();
  }

  // ── Approve → pick account → pick time → schedule ──

  approveCurrent(): void {
    const item = this.currentItem();
    if (!item || this.approving() || !this.perms.canEdit()) return;

    this.approving.set(true);
    this.contentItemService.review(item.id, true).subscribe({
      next: () => {
        this.approving.set(false);
        this.approvedContentId.set(item.id);
        this.openSchedulePanel();
      },
      error: err => {
        this.approving.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر قبول المحتوى.'), { variant: 'error' });
      },
    });
  }

  openSchedulePanel(): void {
    this.schedulePanelOpen.set(true);
    if (this.scheduleAccounts().length > 0 && !this.scheduleAccountId()) {
      this.scheduleAccountId.set(this.scheduleAccounts()[0].socialAccountId);
    }
    if (!this.scheduleDate()) this.prefillScheduleTime();
  }

  closeSchedulePanel(): void {
    this.schedulePanelOpen.set(false);
  }

  private prefillScheduleTime(): void {
    const brandProfileId = this.formBrandProfileId();
    const backendPlatform = PLATFORM_TO_BACKEND[this.formPlatform()] ?? 'Instagram';
    if (!brandProfileId) return;

    this.scheduledPostService.getPostingTimeSuggestions(brandProfileId, [backendPlatform]).subscribe({
      next: res => {
        const suggestion = res.data?.[0];
        const target = suggestion
          ? nextOccurrence(suggestion.dayOfWeek, suggestion.hour)
          : nextOccurrence(new Date().getDay(), 12); // fallback: same weekday, noon, pushed to next safe slot
        this.scheduleDate.set(toDateInputValue(target));
        this.scheduleTime.set(toTimeInputValue(target));
      },
      error: () => {
        const fallback = nextOccurrence(new Date().getDay(), 12);
        this.scheduleDate.set(toDateInputValue(fallback));
        this.scheduleTime.set(toTimeInputValue(fallback));
      },
    });
  }

  updateScheduleAccount(value: string): void { this.scheduleAccountId.set(value); }
  updateScheduleDate(value: string): void { this.scheduleDate.set(value); }
  updateScheduleTime(value: string): void { this.scheduleTime.set(value); }

  submitSchedule(): void {
    const item = this.currentItem();
    const accountId = this.scheduleAccountId();
    if (!item || !accountId || !this.scheduleDate() || !this.scheduleTime() || this.scheduling() || !this.perms.canEdit()) return;

    this.scheduling.set(true);
    this.scheduledPostService.schedule({
      contentItemId: item.id,
      socialAccountId: accountId,
      scheduledAt: `${this.scheduleDate()}T${this.scheduleTime()}:00`,
    }).subscribe({
      next: () => {
        this.scheduling.set(false);
        this.schedulePanelOpen.set(false);
        this.scheduledIds.update(set => new Set(set).add(item.id));
        this.errorModalService.show('تمت جدولة المنشور بنجاح.', { variant: 'success' });
      },
      error: err => {
        this.scheduling.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر جدولة المنشور.'), { variant: 'error' });
      },
    });
  }

  isVideo(item: { type: GenType }): boolean { return item.type === 'video'; }
  isImage(item: { type: GenType; thumbnailUrl?: string }): boolean { return item.type === 'static-ad' && !!item.thumbnailUrl; }

  thumbnailGradient(type: GenType): string {
    return { 'static-ad': 'linear-gradient(135deg,#fce4ec,#fce4ec66)', 'video': 'linear-gradient(135deg,#fff3e0,#fff3e066)', 'text': 'linear-gradient(135deg,#ede9fe,#ede9fe66)' }[type];
  }

  formatDateTime(iso: string): string { return new Date(iso).toLocaleString('ar-SA', { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' }); }
}
