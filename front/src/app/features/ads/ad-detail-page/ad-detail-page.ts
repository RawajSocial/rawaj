import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { KpiCard } from '../../dashboard/kpi-card/kpi-card';
import { AdService } from '../../../services/ad.service';
import { Ad, AdFormat, AdStatus } from '../../../model/ad.model';
import { CAMPAIGN_PLATFORM_META, CampaignPlatform } from '../../../model/campaign.model';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';

/** Shared across every surface that renders a platform badge — see CAMPAIGN_PLATFORM_META. */
const PLATFORM_META = CAMPAIGN_PLATFORM_META;

const STATUS_LABELS: Record<AdStatus, string> = {
  active: 'نشط', paused: 'موقوف', rejected: 'مرفوض', pending: 'قيد المراجعة', completed: 'مكتمل',
};

const POST_TYPE_LABELS: Record<AdFormat, string> = {
  text: 'منشور نصي', image: 'منشور بصورة', video: 'منشور فيديو',
  carousel: 'منشور كاروسيل', story: 'ستوري', reel: 'ريلز',
};

@Component({
  selector: 'app-ad-detail-page',
  imports: [PageHeader, KpiCard, RouterLink],
  templateUrl: './ad-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './ad-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly adService = inject(AdService);
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly router = inject(Router);

  protected readonly adId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly ad = this.adService.getById(this.adId);

  protected readonly platformMeta = PLATFORM_META;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly postTypeLabels = POST_TYPE_LABELS;

  constructor() {
    const a = this.ad();
    this.seo.setPageSeo({
      title: (a ? a.name + ' | ' : '') + 'المنشورات | رواج',
      description: 'تفاصيل المنشور: الإحصائيات والأداء والبيانات الكاملة.',
      keywords: 'رواج, تفاصيل المنشور, أداء المنشور',
      path: '/dashboard/ads/' + this.adId,
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected platformNames(a: Ad): string {
    return a.platforms.map(p => this.platformMeta[p].label).join('، ');
  }

  protected get mediaSrc(): string | null {
    const a = this.ad();
    if (!a) return null;
    if (a.format === 'text') return '/text-post.png';
    if (a.format === 'image') return a.imageUrl ?? null;
    return null;
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1) + 'K';
    return String(n);
  }

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  protected toggleStatus(): void {
    if (this.tenantService.brandProfileCount() === 0) {
      this.errorModalService.show(
        'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
        { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
      );
      this.router.navigate(['/dashboard/brand-profiles/new']);
      return;
    }
    this.adService.toggle(this.adId);
  }
}
