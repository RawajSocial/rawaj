import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { ContentItemService } from '../../../services/content-item.service';
import { VisualAssetService } from '../../../services/visual-asset.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { ContentItemSummary } from '../../../model/content-item.model';
import { VisualAssetSummary } from '../../../model/visual-asset.model';
import { GeneratedItem, GenType, TYPE_CFG } from '../../../model/generated-item.model';
import { SeoService } from '../../../services/seo.service';
import { MediaToolbar } from '../media-toolbar/media-toolbar';
import { MediaCard } from '../media-card/media-card';
import { MediaViewer } from '../media-viewer/media-viewer';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';

/** Best-effort mapping from the backend `ContentType` enum to the media library's GenType. */
function contentTypeToGenType(type: ContentItemSummary['contentType'], hasImage: boolean): GenType {
  if (type === 'Story' || type === 'ReelScript') return 'video';
  return hasImage ? 'static-ad' : 'text';
}

@Component({
  selector: 'app-my-media-page',
  standalone: true,
  imports: [RouterLink, MediaToolbar, MediaCard, MediaViewer, Breadcrumb],
  templateUrl: './my-media-page.html',
  styleUrls: [
    '../../../features/on-boarding/onboarding-shared.css',
    './my-media-page.css',
  ],
})
export class MyMediaPage {
  readonly typeCfg = TYPE_CFG;

  private readonly brandProfileService = inject(BrandProfileService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly visualAssetService = inject(VisualAssetService);
  protected readonly brandContextService = inject(BrandContextService);

  readonly brandProfiles = this.brandProfileService.profiles;

  filterType  = signal<GenType | 'all'>('all');
  searchQuery = signal('');
  selectedItem = signal<GeneratedItem | null>(null);

  /** Real media library, merged from real content-items + visual-assets, scoped to the
   *  header's global brand/campaign selection (no page-local brand/campaign filter). */
  private readonly libraryItems = computed<GeneratedItem[]>(() => {
    const brandId = this.brandContextService.selectedBrandProfileId() ?? undefined;
    const campaignId = this.brandContextService.selectedCampaignId();
    const cid = campaignId === 'all' ? undefined : campaignId;
    const brandName = brandId ? this.brandProfileService.getById(brandId)()?.name ?? '' : '';

    const fromContent: GeneratedItem[] = this.contentItemService.items().map((i: ContentItemSummary) => ({
      id: i.contentItemId,
      type: contentTypeToGenType(i.contentType, !!i.imageUrl),
      title: i.content.substring(0, 24) + (i.content.length > 24 ? '…' : ''),
      brand: brandName,
      status: 'generated',
      createdAt: i.createdAt,
      description: i.content,
      textContent: i.imageUrl ? undefined : i.content,
      thumbnailUrl: i.imageUrl ?? undefined,
      brandProfileId: brandId,
      campaignId: cid,
      sourceKind: 'content-item',
    }));

    const contentItemIds = new Set(this.contentItemService.items().map(i => i.contentItemId));

    // A visual asset already shown embedded in its parent content item's card (fromContent
    // above) must not also appear as its own standalone card here — same image, two ids.
    const fromAssets: GeneratedItem[] = this.visualAssetService.assets()
      .filter((a: VisualAssetSummary) => !a.contentItemId || !contentItemIds.has(a.contentItemId))
      .map((a: VisualAssetSummary) => ({
        id: a.visualAssetId,
        type: 'static-ad',
        title: 'صورة مولّدة',
        brand: brandName,
        status: 'generated',
        createdAt: a.createdAt,
        thumbnailUrl: a.fileUrl,
        brandProfileId: brandId,
        campaignId: cid,
        sourceKind: 'visual-asset',
      }));

    return [...fromContent, ...fromAssets];
  });

  readonly filteredItems = computed(() => {
    const ft = this.filterType();
    const q  = this.searchQuery().trim().toLowerCase();
    return this.libraryItems().filter(i => {
      const typeMatch  = ft === 'all' || i.type === ft;
      const queryMatch = !q || i.title.toLowerCase().includes(q) || i.brand.toLowerCase().includes(q);
      return typeMatch && queryMatch;
    });
  });

  readonly counts = computed(() => ({
    all:          this.libraryItems().length,
    'static-ad':  this.libraryItems().filter(i => i.type === 'static-ad').length,
    video:        this.libraryItems().filter(i => i.type === 'video').length,
    text:         this.libraryItems().filter(i => i.type === 'text').length,
  }));

  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'مكتبة الوسائط | رواج',
      description: 'تصفح وابحث في جميع المحتوى الذي أنشأته عبر رواج.',
      keywords: 'رواج, مكتبة الوسائط, محتوى مُولَّد, صور وفيديوهات',
      path: '/dashboard/my-media',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    effect(() => {
      const brandId = this.brandContextService.selectedBrandProfileId();
      const campaignId = this.brandContextService.selectedCampaignId();
      if (!brandId) return;
      this.contentItemService.refresh(brandId, campaignId).subscribe();
      this.visualAssetService.refresh(brandId, campaignId).subscribe();
    });
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    // viewer handles its own ESC; this closes if viewer is not open
    if (!this.selectedItem()) return;
  }

  openItem(item: GeneratedItem): void { this.selectedItem.set(item); }
  closeModal(): void                  { this.selectedItem.set(null); }
}
