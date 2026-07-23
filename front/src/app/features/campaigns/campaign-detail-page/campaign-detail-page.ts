import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { KpiCard } from '../../dashboard/kpi-card/kpi-card';
import { CampaignService } from '../../../services/campaign.service';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { CampaignObjective, CampaignPlatform, CampaignStatus } from '../../../model/campaign.model';
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

const STATUS_LABELS: Record<CampaignStatus, string> = {
  active: 'نشطة', paused: 'موقوفة', completed: 'مكتملة', draft: 'مسودة', archived: 'مؤرشفة',
};

const OBJECTIVE_LABELS: Record<CampaignObjective, string> = {
  awareness: 'الوعي بالعلامة', traffic: 'زيارات الموقع', engagement: 'التفاعل', leads: 'توليد عملاء', sales: 'رفع المبيعات',
};

@Component({
  selector: 'app-campaign-detail-page',
  imports: [PageHeader, KpiCard, RouterLink],
  templateUrl: './campaign-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly campaignService = inject(CampaignService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly seo = inject(SeoService);

  protected readonly campaignId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly campaign = this.campaignService.getById(this.campaignId);

  protected readonly brandProfile = computed(() => {
    const c = this.campaign();
    return c ? this.brandProfileService.getById(c.brandProfileId)() : undefined;
  });

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly objectiveLabels = OBJECTIVE_LABELS;

  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  private readonly campaignPosts = this.scheduledPostService.byCampaign(this.campaignId);

  /** "Upcoming" = not yet published, optionally narrowed to one platform. */
  protected readonly upcomingPosts = computed(() => {
    const filter = this.platformFilter();
    return this.campaignPosts()
      .filter(p => p.status === 'scheduled')
      .filter(p => filter === 'all' || p.platform === filter)
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
  });

  private readonly performanceSeries = computed(() => {
    const c = this.campaign();
    return c ? this.buildTrend(c.reach, this.campaignId) : [];
  });

  protected readonly performanceLinePath = computed(() => this.buildLinePath(this.performanceSeries()));
  protected readonly performanceAreaPath = computed(() => this.buildAreaPath(this.performanceSeries()));

  constructor() {
    const c = this.campaign();
    this.seo.setPageSeo({
      title: (c ? c.name + ' | ' : '') + 'الحملات | رواج',
      description: 'تفاصيل الحملة: الإحصائيات، الأداء، والمنشورات القادمة.',
      keywords: 'رواج, تفاصيل الحملة, أداء الحملة, إحصائيات',
      path: '/dashboard/campaigns/' + this.campaignId,
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected platformMeta(p: CampaignPlatform): PlatformMeta {
    return PLATFORM_META[p];
  }

  protected setPlatformFilter(p: CampaignPlatform | 'all'): void {
    this.platformFilter.set(p);
  }

  protected budgetPct(): number {
    const c = this.campaign();
    if (!c || c.budget <= 0) return 0;
    return Math.min(100, Math.round((c.spent / c.budget) * 100));
  }

  protected compact(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1) + 'K';
    return String(n);
  }

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  /** Deterministic pseudo-random daily trend so each campaign gets a
   *  stable-looking (not literally-flat) performance chart without a
   *  backend — seeded off the campaign id so it doesn't reshuffle on
   *  every change-detection run. */
  private buildTrend(peak: number, seed: string): number[] {
    let s = seed.split('').reduce((a, ch) => a + ch.charCodeAt(0), 0) || 1;
    const rand = (): number => {
      s = (s * 9301 + 49297) % 233280;
      return s / 233280;
    };
    const points = 14;
    const base = Math.max(peak / points, 1);
    return Array.from({ length: points }, (_, i) =>
      Math.round(base * (0.5 + rand()) * (0.6 + (i / points) * 0.8)),
    );
  }

  private buildLinePath(data: number[]): string {
    if (data.length < 2) return '';
    const points = this.scalePoints(data);
    let d = `M ${points[0].x} ${points[0].y}`;
    for (let i = 1; i < points.length; i++) {
      const prev = points[i - 1];
      const curr = points[i];
      const cpx = (prev.x + curr.x) / 2;
      d += ` C ${cpx} ${prev.y} ${cpx} ${curr.y} ${curr.x} ${curr.y}`;
    }
    return d;
  }

  private buildAreaPath(data: number[]): string {
    if (data.length < 2) return '';
    const line = this.buildLinePath(data);
    const { W, H, PAD } = this.chartBox;
    return `${line} L ${W - PAD} ${H - PAD} L ${PAD} ${H - PAD} Z`;
  }

  private readonly chartBox = { W: 600, H: 200, PAD: 10 };

  private scalePoints(data: number[]): { x: number; y: number }[] {
    const { W, H, PAD } = this.chartBox;
    const max = Math.max(...data, 1);
    const scaleX = (i: number): number => PAD + (i / (data.length - 1)) * (W - PAD * 2);
    const scaleY = (v: number): number => H - PAD - (v / max) * (H - PAD * 2);
    return data.map((v, i) => ({ x: scaleX(i), y: scaleY(v) }));
  }
}
