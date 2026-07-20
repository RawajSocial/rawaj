import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MediaService } from '../../../services/media.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { CampaignService } from '../../../services/campaign.service';
import { SeoService } from '../../../services/seo.service';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';
import {
  GeneratedAsset, GenType, AdSize, ContentTone, TextType,
  TYPE_CFG, SIZE_CFG, TONE_CFG, TEXT_TYPE_CFG,
} from '../../../model/generated-item.model';

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
  imports: [RouterLink, Breadcrumb],
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

  readonly genTypes:  GenType[]      = ['static-ad', 'video', 'text'];
  readonly sizes:     AdSize[]       = ['square', 'portrait', 'landscape', 'story'];
  readonly tones:     ContentTone[]  = ['professional', 'casual', 'energetic', 'luxurious'];
  readonly textTypes: TextType[]     = ['caption', 'hashtags', 'ad-copy', 'blog'];

  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  private readonly seo = inject(SeoService);

  readonly brandProfiles = this.brandProfileService.profiles;

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
  formBrandProfileId = signal('');
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
    const brand = this.formBrand().trim();
    const desc  = this.formDesc().trim();
    const id    = this.media.nextId();

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
      brandProfileId: this.formBrandProfileId() || undefined,
      campaignId: this.formCampaignId() || undefined,
      assets: this.formAssets().length ? this.formAssets() : undefined,
    });

    this.currentItemId.set(id);

    setTimeout(() => {
      this.media.markGenerated(id, {
        textContent: this.genType() === 'text' ? `✨ ${desc}\n\n— ${brand}` : undefined,
      });
    }, 3000);

    this.formBrand.set('');
    this.formDesc.set('');
    this.formAssets.set([]);
  }

  applyEdit(): void {
    const item = this.currentItem();
    const prompt = this.editPrompt().trim();
    if (!item || !prompt || item.status === 'generating') return;

    const id = item.id;
    const mergedDesc = `${item.description ?? ''}\n\nتعديل: ${prompt}`.trim();

    this.media.markRegenerating(id);

    setTimeout(() => {
      this.media.markGenerated(id, {
        description: mergedDesc,
        textContent: item.type === 'text' ? `✨ ${mergedDesc}\n\n— ${item.brand}` : item.textContent,
      });
    }, 2500);

    this.editPrompt.set('');
  }

  isVideo(item: { type: GenType }): boolean { return item.type === 'video'; }
  isImage(item: { type: GenType; thumbnailUrl?: string }): boolean { return item.type === 'static-ad' && !!item.thumbnailUrl; }

  thumbnailGradient(type: GenType): string {
    return { 'static-ad': 'linear-gradient(135deg,#fce4ec,#fce4ec66)', 'video': 'linear-gradient(135deg,#fff3e0,#fff3e066)', 'text': 'linear-gradient(135deg,#ede9fe,#ede9fe66)' }[type];
  }

  formatDateTime(iso: string): string { return new Date(iso).toLocaleString('ar-SA', { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' }); }
}
