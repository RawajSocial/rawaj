import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { KpiCard } from '../../kpi-card/kpi-card';
import { AnalyticsApiService } from '../../../../core/api/analytics-api.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { ApiError } from '../../../../core/api';
import { BrandAnalyticsOverview } from '../../../../core/models';

const PLATFORM_LABELS: Record<string, string> = {
  Instagram: 'إنستغرام',
  Facebook: 'فيسبوك',
  Tiktok: 'تيك توك',
  Youtube: 'يوتيوب',
  Twitter: 'إكس',
  Linkedin: 'لينكد إن',
};

@Component({
  selector: 'app-analytics-page',
  imports: [PageHeader, KpiCard],
  templateUrl: './analytics-page.html',
  styleUrls: ['../../dashboard-shared.css', './analytics-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnalyticsPage {
  private readonly analyticsApi = inject(AnalyticsApiService);
  protected readonly tenantService = inject(TenantService);

  protected readonly platformLabels = PLATFORM_LABELS;

  protected readonly overview = signal<BrandAnalyticsOverview | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (!brandProfileId) {
        this.overview.set(null);
        return;
      }

      this.loading.set(true);
      this.error.set(null);

      this.analyticsApi.getBrandOverview(brandProfileId).subscribe({
        next: (result) => {
          this.overview.set(result);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.error.set(error instanceof ApiError ? error.message : 'تعذر تحميل التحليلات.');
        },
      });
    });
  }

  protected platformLabel(platform: string): string {
    return this.platformLabels[platform] ?? platform;
  }

  protected formatPercent(value: number | null): string {
    return value === null ? '—' : `${(value * 100).toFixed(1)}%`;
  }
}
