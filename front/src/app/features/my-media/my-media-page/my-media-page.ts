import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MediaService } from '../../../services/media.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { CampaignService } from '../../../services/campaign.service';
import { GeneratedItem, GenType, TYPE_CFG } from '../../../model/generated-item.model';
import { SeoService } from '../../../services/seo.service';
import { MediaToolbar } from '../media-toolbar/media-toolbar';
import { MediaCard } from '../media-card/media-card';
import { MediaViewer } from '../media-viewer/media-viewer';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';

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
  private readonly campaignService = inject(CampaignService);

  readonly brandProfiles = this.brandProfileService.profiles;

  filterType  = signal<GenType | 'all'>('all');
  searchQuery = signal('');
  selectedItem = signal<GeneratedItem | null>(null);

  brandProfileFilter = signal('');
  campaignFilter      = signal('');
  brandProfileFilterOpen = signal(false);
  campaignFilterOpen      = signal(false);

  readonly availableCampaignsForFilter = computed(() => {
    const bp = this.brandProfileFilter();
    return bp ? this.campaignService.byBrandProfile(bp)() : this.campaignService.campaigns();
  });

  readonly brandProfileFilterLabel = computed(() =>
    this.brandProfiles().find(p => p.id === this.brandProfileFilter())?.name ?? 'كل العلامات التجارية',
  );
  readonly campaignFilterLabel = computed(() =>
    this.availableCampaignsForFilter().find(c => c.id === this.campaignFilter())?.name ?? 'كل الحملات',
  );

  readonly filteredItems = computed(() => {
    const ft = this.filterType();
    const q  = this.searchQuery().trim().toLowerCase();
    const bp = this.brandProfileFilter();
    const cp = this.campaignFilter();
    return this.media.items().filter(i => {
      const typeMatch  = ft === 'all' || i.type === ft;
      const queryMatch = !q || i.title.toLowerCase().includes(q) || i.brand.toLowerCase().includes(q);
      const brandMatch = !bp || i.brandProfileId === bp;
      const campaignMatch = !cp || i.campaignId === cp;
      return typeMatch && queryMatch && brandMatch && campaignMatch;
    });
  });

  readonly counts = computed(() => ({
    all:          this.media.items().length,
    'static-ad':  this.media.items().filter(i => i.type === 'static-ad').length,
    video:        this.media.items().filter(i => i.type === 'video').length,
    text:         this.media.items().filter(i => i.type === 'text').length,
  }));

  private readonly seo = inject(SeoService);

  constructor(private media: MediaService) {
    this.seo.setPageSeo({
      title: 'مكتبة الوسائط | رواج',
      description: 'تصفح وابحث في جميع المحتوى الذي أنشأته عبر رواج.',
      keywords: 'رواج, مكتبة الوسائط, محتوى مُولَّد, صور وفيديوهات',
      path: '/dashboard/my-media',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('[data-dd="brandprofile"]')) this.brandProfileFilterOpen.set(false);
    if (!t.closest('[data-dd="campaign"]'))      this.campaignFilterOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    // viewer handles its own ESC; this closes if viewer is not open
    if (!this.selectedItem()) return;
  }

  setBrandProfileFilter(id: string): void {
    this.brandProfileFilter.set(id);
    this.campaignFilter.set('');
    this.brandProfileFilterOpen.set(false);
  }

  setCampaignFilter(id: string): void {
    this.campaignFilter.set(id);
    this.campaignFilterOpen.set(false);
  }

  openItem(item: GeneratedItem): void { this.selectedItem.set(item); }
  closeModal(): void                  { this.selectedItem.set(null); }
}
