import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AdminService } from '../../../services/admin.service';
import {
  TENANT_KIND_LABELS, TENANT_STATUS_LABELS, TenantAccountKind, TenantStatus,
} from '../../../model/admin.model';
import { SeoService } from '../../../services/seo.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

type KindTab = 'all' | TenantAccountKind;

@Component({
  selector: 'app-admin-tenants-page',
  imports: [TooltipDirective],
  templateUrl: './admin-tenants-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../admin-shared.css', './admin-tenants-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminTenantsPage {
  private readonly adminService = inject(AdminService);
  private readonly seo = inject(SeoService);

  protected readonly kindLabels = TENANT_KIND_LABELS;
  protected readonly statusLabels = TENANT_STATUS_LABELS;
  protected readonly tenants = this.adminService.tenants;

  protected readonly searchQuery = signal('');
  protected readonly kindTab = signal<KindTab>('all');

  protected readonly tabs: { value: KindTab; label: string }[] = [
    { value: 'all', label: 'الكل' },
    { value: 'agency', label: 'وكالات' },
    { value: 'business', label: 'أصحاب علامات تجارية' },
  ];

  protected readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const tab = this.kindTab();
    return this.tenants().filter(t => {
      if (tab !== 'all' && t.kind !== tab) return false;
      if (q && !t.name.toLowerCase().includes(q) && !t.ownerEmail.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  constructor() {
    this.seo.setPageSeo({
      title: 'الوكالات والعلامات التجارية | إدارة المنصة | رواج',
      description: 'إدارة الوكالات وأصحاب العلامات التجارية المسجّلين في رواج.',
      keywords: 'رواج, إدارة المنصة, الوكالات, العلامات التجارية',
      path: '/admin/tenants',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected toggleSuspend(id: string, status: TenantStatus): void {
    this.adminService.setTenantStatus(id, status === 'suspended' ? 'active' : 'suspended');
  }
}
