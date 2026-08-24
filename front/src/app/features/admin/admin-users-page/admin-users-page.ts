import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AdminService } from '../../../services/admin.service';
import { PLATFORM_USER_STATUS_LABELS, PlatformUserStatus } from '../../../model/admin.model';
import { SeoService } from '../../../services/seo.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

type StatusTab = 'all' | PlatformUserStatus;

@Component({
  selector: 'app-admin-users-page',
  imports: [TooltipDirective],
  templateUrl: './admin-users-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../admin-shared.css', './admin-users-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersPage {
  private readonly adminService = inject(AdminService);
  private readonly seo = inject(SeoService);

  protected readonly statusLabels = PLATFORM_USER_STATUS_LABELS;
  protected readonly users = this.adminService.users;

  protected readonly searchQuery = signal('');
  protected readonly statusTab = signal<StatusTab>('all');

  protected readonly tabs: { value: StatusTab; label: string }[] = [
    { value: 'all', label: 'الكل' },
    { value: 'active', label: 'نشط' },
    { value: 'pending', label: 'بانتظار القبول' },
    { value: 'suspended', label: 'موقوف' },
  ];

  protected readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const tab = this.statusTab();
    return this.users().filter(u => {
      if (tab !== 'all' && u.status !== tab) return false;
      if (q && !u.name.toLowerCase().includes(q) && !u.email.toLowerCase().includes(q) && !u.tenantName.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  constructor() {
    this.seo.setPageSeo({
      title: 'المستخدمون | إدارة المنصة | رواج',
      description: 'إدارة جميع مستخدمي منصة رواج عبر كل الحسابات.',
      keywords: 'رواج, إدارة المنصة, المستخدمون',
      path: '/admin/users',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected initials(name: string): string {
    return name.trim().split(/\s+/).slice(0, 2).map(p => p[0]).join('');
  }

  protected toggleSuspend(id: string, status: PlatformUserStatus): void {
    if (status === 'suspended') {
      this.adminService.reactivateUser(id);
    } else {
      this.adminService.suspendUser(id);
    }
  }
}
