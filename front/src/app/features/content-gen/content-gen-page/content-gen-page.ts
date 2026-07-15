import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MediaService } from '../../../services/media.service';
import {
  GeneratedItem, GenType, AdSize, ContentTone, TextType,
  TYPE_CFG, SIZE_CFG, TONE_CFG, TEXT_TYPE_CFG,
} from '../../../model/generated-item.model';
import { ContentApiService } from '../../../core/api/content-api.service';
import { VisualAssetsApiService } from '../../../core/api/visual-assets-api.service';
import { ApiError } from '../../../core/api';
import { ContentType, Language, SocialPlatform } from '../../../core/models';

// Snapchat is intentionally excluded - the backend's SocialPlatform enum has no Snapchat value.
const PLATFORM_OPTS: { value: string; label: string; icon: string; color: string; backend: SocialPlatform }[] = [
  { value: 'instagram', label: 'إنستغرام',  icon: 'fa-brands fa-instagram',  color: '#E1306C', backend: 'Instagram' },
  { value: 'facebook',  label: 'فيسبوك',    icon: 'fa-brands fa-facebook-f', color: '#1877F2', backend: 'Facebook' },
  { value: 'tiktok',    label: 'تيك توك',   icon: 'fa-brands fa-tiktok',     color: '#222',    backend: 'Tiktok' },
  { value: 'x',         label: 'إكس',        icon: 'fa-brands fa-x-twitter',  color: '#14171A', backend: 'Twitter' },
  { value: 'youtube',   label: 'يوتيوب',    icon: 'fa-brands fa-youtube',    color: '#FF0000', backend: 'Youtube' },
  { value: 'linkedin',  label: 'لينكد إن',  icon: 'fa-brands fa-linkedin-in',color: '#0A66C2', backend: 'Linkedin' },
];

const TEXT_TYPE_TO_CONTENT_TYPE: Record<TextType, ContentType> = {
  caption: 'Caption',
  hashtags: 'Caption',
  'ad-copy': 'AdCopy',
  blog: 'Blog',
};

const QUALITY_OPTS = [
  { value: 'standard', label: 'قياسية',     desc: 'سريعة ومناسبة لمعظم الاحتياجات' },
  { value: 'high',     label: 'عالية',      desc: 'تفاصيل أوضح وألوان أكثر دقة' },
  { value: 'ultra',    label: 'فائقة (4K)', desc: 'أعلى جودة، تستغرق وقتاً أطول' },
];

@Component({
  selector: 'app-content-gen-page',
  standalone: true,
  imports: [RouterLink, FormsModule],
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

  // 'video' is intentionally excluded - there is no video generation on the backend today.
  readonly genTypes:  GenType[]      = ['static-ad', 'text'];
  readonly sizes:     AdSize[]       = ['square', 'portrait', 'landscape', 'story'];
  readonly tones:     ContentTone[]  = ['professional', 'casual', 'energetic', 'luxurious'];
  readonly textTypes: TextType[]     = ['caption', 'hashtags', 'ad-copy', 'blog'];

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

  // ── Dropdown open states ──
  typeOpen     = signal(false);
  toneOpen     = signal(false);
  sizeOpen     = signal(false);
  langOpen     = signal(false);
  textTypeOpen = signal(false);
  platformOpen = signal(false);
  qualityOpen  = signal(false);
  filterOpen   = signal(false);

  // ── Gallery ──
  filterType  = signal<GenType | 'all'>('all');
  showSuccess = signal(false);

  // ── Modal ──
  selectedItem      = signal<GeneratedItem | null>(null);
  editMode          = signal(false);
  showDeleteConfirm = signal(false);
  editBrand         = signal('');
  editDesc          = signal('');
  editTone          = signal<ContentTone>('professional');

  // ── Computeds ──
  readonly typeLabel     = computed(() => TYPE_CFG[this.genType()].label);
  readonly toneLabel     = computed(() => TONE_CFG[this.formTone()]);
  readonly sizeLabel     = computed(() => SIZE_CFG[this.formSize()].label);
  readonly langLabel     = computed(() => ({ ar: 'عربي', en: 'English', 'ar-eg': 'عربي مصري' }[this.formLang()] ?? this.formLang()));
  readonly textTypeLabel = computed(() => TEXT_TYPE_CFG[this.formTextType()]);
  readonly platformLabel = computed(() => PLATFORM_OPTS.find(p => p.value === this.formPlatform())?.label ?? '');
  readonly qualityLabel  = computed(() => QUALITY_OPTS.find(q => q.value === this.formQuality())?.label ?? '');
  readonly canGenerate   = computed(() => this.formBrand().trim().length > 0 && this.formDesc().trim().length > 0);
  readonly filterLabel   = computed(() => this.filterType() === 'all' ? 'جميع الأنواع' : TYPE_CFG[this.filterType() as GenType].label);

  readonly filteredItems = computed(() => {
    const ft = this.filterType();
    return this.media.items().filter(i => ft === 'all' || i.type === ft);
  });

  readonly counts = computed(() => ({
    all:         this.media.items().length,
    'static-ad': this.media.items().filter(i => i.type === 'static-ad').length,
    video:       this.media.items().filter(i => i.type === 'video').length,
    text:        this.media.items().filter(i => i.type === 'text').length,
  }));

  // ── Campaign selection (content/images are always generated for a campaign) ──
  readonly generateError = signal<string | null>(null);

  private readonly contentApi = inject(ContentApiService);
  private readonly visualAssetsApi = inject(VisualAssetsApiService);

  constructor(readonly media: MediaService) {}

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    const inDd = (a: string) => !!t.closest(`[data-dd="${a}"]`);
    if (!inDd('type'))     this.typeOpen.set(false);
    if (!inDd('tone'))     this.toneOpen.set(false);
    if (!inDd('size'))     this.sizeOpen.set(false);
    if (!inDd('lang'))     this.langOpen.set(false);
    if (!inDd('texttype')) this.textTypeOpen.set(false);
    if (!inDd('platform')) this.platformOpen.set(false);
    if (!inDd('quality'))  this.qualityOpen.set(false);
    if (!inDd('filter'))   this.filterOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.editMode())          { this.editMode.set(false); return; }
    if (this.showDeleteConfirm()) { this.showDeleteConfirm.set(false); return; }
    this.selectedItem.set(null);
  }

  generate(): void {
    if (!this.canGenerate()) return;

    const campaignId = this.media.selectedCampaignId();
    if (!campaignId) {
      this.generateError.set('اختر حملة أولاً لتوليد المحتوى لها.');
      return;
    }

    const brand = this.formBrand().trim();
    const desc  = this.formDesc().trim();
    const id    = this.media.nextId();
    this.generateError.set(null);

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
    });

    if (this.genType() === 'static-ad') {
      this.visualAssetsApi.generate({ campaignId, type: 'Image', prompt: desc }).subscribe({
        next: (result) => this.media.markGenerated(id, { thumbnailUrl: result.fileUrl }),
        error: (error: unknown) => this.handleGenerationFailure(id, error),
      });
    } else {
      const platform = PLATFORM_OPTS.find((p) => p.value === this.formPlatform())?.backend ?? 'Instagram';
      const language: Language = this.formLang() === 'ar' ? 'Ar' : 'En';
      const additionalInstructions =
        this.formTextType() === 'hashtags' ? `${desc} (اكتب هاشتاقات فقط، بدون نص تعليق)` : desc;

      this.contentApi
        .generate({
          campaignId,
          contentType: TEXT_TYPE_TO_CONTENT_TYPE[this.formTextType()],
          platform,
          language,
          tone: TONE_CFG[this.formTone()],
          additionalInstructions,
        })
        .subscribe({
          next: (result) => this.media.markGenerated(id, { textContent: result.content }),
          error: (error: unknown) => this.handleGenerationFailure(id, error),
        });
    }

    this.showSuccess.set(true);
    setTimeout(() => this.showSuccess.set(false), 5000);
    this.formBrand.set('');
    this.formDesc.set('');
  }

  private handleGenerationFailure(id: string, error: unknown): void {
    this.media.markFailed(id);
    this.generateError.set(
      error instanceof ApiError ? error.message : 'تعذر توليد المحتوى، حاول مرة أخرى.',
    );
  }

  // ── Modal ──
  openItem(item: GeneratedItem): void {
    this.selectedItem.set(item);
    this.editMode.set(false);
    this.showDeleteConfirm.set(false);
  }

  closeModal(): void {
    this.selectedItem.set(null);
    this.editMode.set(false);
    this.showDeleteConfirm.set(false);
  }

  startEdit(): void {
    const item = this.selectedItem();
    if (!item) return;
    this.editBrand.set(item.brand);
    this.editDesc.set(item.description ?? '');
    this.editTone.set(item.tone ?? 'professional');
    this.editMode.set(true);
  }

  cancelEdit(): void { this.editMode.set(false); }

  saveEdit(): void {
    const item = this.selectedItem();
    if (!item) return;
    const updated: GeneratedItem = { ...item, brand: this.editBrand(), description: this.editDesc() || undefined, tone: this.editTone() };
    this.media.update(updated);
    this.selectedItem.set(updated);
    this.editMode.set(false);
  }

  confirmDelete(): void  { this.showDeleteConfirm.set(true); }
  cancelDelete(): void   { this.showDeleteConfirm.set(false); }

  deleteItem(): void {
    const item = this.selectedItem();
    if (!item) return;
    this.media.remove(item.id);
    this.closeModal();
  }

  isVideo(item: GeneratedItem): boolean { return item.type === 'video'; }
  isImage(item: GeneratedItem): boolean { return item.type === 'static-ad' && !!item.thumbnailUrl; }

  thumbnailGradient(type: GenType): string {
    return { 'static-ad': 'linear-gradient(135deg,#fce4ec,#fce4ec66)', 'video': 'linear-gradient(135deg,#fff3e0,#fff3e066)', 'text': 'linear-gradient(135deg,#ede9fe,#ede9fe66)' }[type];
  }

  statusLabel(s: string): string { return ({ generating: 'يُنشأ الآن', generated: 'مكتمل', failed: 'فشل' } as any)[s] ?? s; }
  statusColor(s: string): string { return ({ generating: '#f97316', generated: '#22c55e', failed: '#ef4444' } as any)[s] ?? '#9ca3af'; }
  formatDate(iso: string): string { return new Date(iso).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' }); }
  formatDateTime(iso: string): string { return new Date(iso).toLocaleString('ar-SA', { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' }); }
}
