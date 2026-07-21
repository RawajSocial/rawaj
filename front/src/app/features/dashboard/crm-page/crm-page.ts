import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { KpiCard } from '../kpi-card/kpi-card';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { AnalyticsApiService } from '../../../core/api/analytics-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { BrandAnalyticsOverview, SocialPlatform } from '../../../core/models';

const PLATFORM_CFG: Record<SocialPlatform, { icon: string; color: string; label: string }> = {
  Instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
  Facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
  Tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#222',    label: 'تيك توك'  },
  Youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
  Twitter:   { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
  Linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
};

interface KpiData {
  title: string;
  value: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  accentColor: string;
}

@Component({
  selector: 'app-crm-page',
  standalone: true,
  imports: [KpiCard, PageHeader, RouterLink],
  templateUrl: './crm-page.html',
  styleUrls: ['../../campaigns/campaigns-page/campaigns-page.css', './crm-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrmPage {
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly tenantService = inject(TenantService);

  protected readonly overview = signal<BrandAnalyticsOverview | null>(null);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly hasData = computed(() => (this.overview()?.postsTracked ?? 0) > 0);

  /** A registered account works without a tenant - this is the state a brand-new user (or one who
   * hasn't created/joined a business yet) lands in, distinct from "has a business but no post data
   * yet". */
  protected readonly needsTenant = computed(() => this.tenantService.loaded() && !this.tenantService.hasTenant());

  protected readonly kpis = computed<KpiData[]>(() => {
    const o = this.overview();
    if (!o) return [];
    return [
      { title: 'مدى الوصول الإجمالي', value: this.compact(o.totalReach), icon: 'fa-bullseye', iconBg: 'rgba(37,99,235,0.12)', iconColor: '#2563EB', accentColor: '#2563EB' },
      { title: 'مرات الظهور',         value: this.compact(o.totalImpressions), icon: 'fa-eye', iconBg: 'rgba(124,58,237,0.12)', iconColor: '#7C3AED', accentColor: '#7C3AED' },
      { title: 'معدل التفاعل',        value: o.averageEngagementRate !== null ? o.averageEngagementRate + '%' : '—', icon: 'fa-heart', iconBg: 'rgba(236,72,153,0.12)', iconColor: '#EC4899', accentColor: '#EC4899' },
      { title: 'المنشورات المتابَعة', value: String(o.postsTracked), icon: 'fa-paper-plane', iconBg: 'rgba(22,163,74,0.12)', iconColor: '#16A34A', accentColor: '#16A34A' },
    ];
  });

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.load(brandProfileId);
      }
    });
  }

  private load(brandProfileId: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.analyticsApi.getBrandOverview(brandProfileId).subscribe({
      next: (overview) => {
        this.overview.set(overview);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل بيانات الأداء.');
      },
    });
  }

  protected platformCfg(platform: SocialPlatform) {
    return PLATFORM_CFG[platform];
  }

  protected reachShare(reach: number): number {
    const total = this.overview()?.totalReach ?? 0;
    return total > 0 ? Math.round((reach / total) * 100) : 0;
  }

  protected compact(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1).replace(/\.0$/, '') + 'K';
    return String(n);
  }
}
