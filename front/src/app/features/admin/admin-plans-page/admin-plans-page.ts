import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AdminService } from '../../../services/admin.service';
import { SeoService } from '../../../services/seo.service';

@Component({
  selector: 'app-admin-plans-page',
  imports: [],
  templateUrl: './admin-plans-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', '../admin-shared.css', './admin-plans-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPlansPage {
  private readonly adminService = inject(AdminService);
  private readonly seo = inject(SeoService);

  protected readonly plans = this.adminService.plans;

  constructor() {
    this.seo.setPageSeo({
      title: 'الاشتراكات والباقات | إدارة المنصة | رواج',
      description: 'إدارة باقات الاشتراك المتاحة في منصة رواج وعدد المشتركين في كل باقة.',
      keywords: 'رواج, إدارة المنصة, الاشتراكات, الباقات',
      path: '/admin/plans',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
