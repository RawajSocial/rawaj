import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { KpiCard } from '../../dashboard/kpi-card/kpi-card';
import { AdService } from '../../../services/ad.service';
import { Ad, AdFormat, AdStatus } from '../../../model/ad.model';
import { CampaignPlatform } from '../../../model/campaign.model';
import { SeoService } from '../../../services/seo.service';

interface PlatformMeta {
  icon: string;
  color: string;
  label: string;
}

const PLATFORM_META: Record<CampaignPlatform, PlatformMeta> = {
  instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)', label: 'إنستغرام' },
  facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)',  label: 'فيسبوك' },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    label: 'تيك توك' },
  youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)',   label: 'يوتيوب' },
  x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)',         label: 'إكس' },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  label: 'سناب شات' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  label: 'لينكد إن' },
};

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
    this.adService.toggle(this.adId);
  }
}
