import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdCard } from '../ad-card/ad-card';
import { AdStatus, AdFormat } from '../../../model/ad.model';
import { CampaignPlatform } from '../../../model/campaign.model';
import { SeoService } from '../../../services/seo.service';
import { AdService } from '../../../services/ad.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';
import { BrandLock } from '../../../shared/components/brand-lock/brand-lock';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';

@Component({
  selector: 'app-ads-page',
  standalone: true,
  imports: [AdCard, Breadcrumb, BrandLock],
  templateUrl: './ads-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './ads-page.css'],
})
export class AdsPage {
  private readonly seo = inject(SeoService);
  private readonly adService = inject(AdService);
  private readonly router = inject(Router);
  private readonly brandContextService = inject(BrandContextService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);

  constructor() {
    this.seo.setPageSeo({
      title: 'منشوراتك | رواج',
      description: 'إدارة منشوراتك عبر جميع المنصات ومتابعة أدائها وتفعيلها أو إيقافها.',
      keywords: 'رواج, منشورات, إدارة المنشورات, أداء المنشورات',
      path: '/dashboard/ads',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    // Header brand/campaign selection is the single source of truth — refetch on change.
    effect(() => {
      const brandId = this.brandContextService.selectedBrandProfileId();
      const campaignId = this.brandContextService.selectedCampaignId();
      if (!brandId) return;
      this.adService.refresh(brandId, campaignId).subscribe();
    });
  }

  protected readonly ads            = this.adService.ads;
  protected readonly brandProfileCount = this.tenantService.brandProfileCount;
  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<AdStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');
  protected readonly formatFilter   = signal<AdFormat | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);
  protected readonly formatOpen   = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!t.closest('[data-dd="platform"]')) this.platformOpen.set(false);
    if (!t.closest('[data-dd="format"]'))   this.formatOpen.set(false);
  }

  protected setStatus(v: AdStatus | 'all'): void              { this.statusFilter.set(v);   this.statusOpen.set(false); }
  protected setPlatform(v: CampaignPlatform | 'all'): void    { this.platformFilter.set(v); this.platformOpen.set(false); }
  protected setFormat(v: AdFormat | 'all'): void              { this.formatFilter.set(v);   this.formatOpen.set(false); }

  protected get statusLabel():   string { return this.statusOptions.find(o   => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }
  protected get formatLabel():   string { return this.formatOptions.find(o   => o.value === this.formatFilter())?.label   ?? ''; }

  protected readonly statusOptions: { value: AdStatus | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع الحالات' }, { value: 'active', label: 'نشط' },
    { value: 'paused', label: 'موقوف' }, { value: 'pending', label: 'قيد المراجعة' },
    { value: 'rejected', label: 'مرفوض' }, { value: 'completed', label: 'مكتمل' },
  ];

  // No Snapchat option: ads are derived from scheduled posts, whose platform comes from the
  // backend's SocialPlatform enum — which has no Snapchat member, so the filter could only ever
  // return zero results.
  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع المنصات' }, { value: 'instagram', label: 'إنستغرام' },
    { value: 'facebook', label: 'فيسبوك' }, { value: 'tiktok', label: 'تيك توك' },
    { value: 'youtube', label: 'يوتيوب' },
    { value: 'linkedin', label: 'لينكد إن' }, { value: 'x', label: 'إكس (تويتر)' },
  ];

  protected readonly formatOptions: { value: AdFormat | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع الأشكال' }, { value: 'text', label: 'نصي' }, { value: 'image', label: 'صورة' },
    { value: 'video', label: 'فيديو' }, { value: 'carousel', label: 'كاروسيل' },
    { value: 'story', label: 'ستوري' }, { value: 'reel', label: 'ريلز' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    const fm = this.formatFilter();
    return this.ads().filter(a => {
      if (q  && !a.name.toLowerCase().includes(q) && !a.campaignName.toLowerCase().includes(q)) return false;
      if (st !== 'all' && a.status   !== st) return false;
      if (pl !== 'all' && !a.platforms.includes(pl)) return false;
      if (fm !== 'all' && a.format   !== fm) return false;
      return true;
    });
  });

  protected viewAd(id: string): void {
    this.router.navigate(['/dashboard/ads', id]);
  }

  protected startNewCampaign(): void {
    if (!this.requireBrandProfile()) return;
    this.router.navigate(['/on-boarding'], { queryParams: { fresh: 1 } });
  }

  private requireBrandProfile(): boolean {
    if (this.tenantService.brandProfileCount() > 0) return true;
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    this.router.navigate(['/dashboard/brand-profiles/new']);
    return false;
  }
}
