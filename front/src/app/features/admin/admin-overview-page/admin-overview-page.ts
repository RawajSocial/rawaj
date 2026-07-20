import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { TENANT_KIND_LABELS, TENANT_STATUS_LABELS } from '../../../model/admin.model';
import { SeoService } from '../../../services/seo.service';

@Component({
  selector: 'app-admin-overview-page',
  imports: [RouterLink],
  templateUrl: './admin-overview-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../admin-shared.css', './admin-overview-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminOverviewPage {
  private readonly adminService = inject(AdminService);
  private readonly seo = inject(SeoService);

  protected readonly stats = this.adminService.stats;
  protected readonly tenants = this.adminService.tenants;
  protected readonly kindLabels = TENANT_KIND_LABELS;
  protected readonly statusLabels = TENANT_STATUS_LABELS;

  protected readonly recentTenants = computed(() => [...this.tenants()].slice(0, 5));

  constructor() {
    this.seo.setPageSeo({
      title: 'نظرة عامة | إدارة المنصة | رواج',
      description: 'إحصائيات عامة عن المستخدمين والوكالات والاشتراكات في منصة رواج.',
      keywords: 'رواج, إدارة المنصة, إحصائيات',
      path: '/admin',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
