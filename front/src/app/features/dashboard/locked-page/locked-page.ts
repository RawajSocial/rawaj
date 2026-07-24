import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';

@Component({
  selector: 'app-locked-page',
  imports: [RouterLink],
  templateUrl: './locked-page.html',
  styleUrls: ['../dashboard-shared.css', './locked-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LockedPage {
  private readonly seo = inject(SeoService);
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly tenantService = inject(TenantService);

  protected readonly returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

  /** Whether Settings (business info) is the unmet condition — if it's already done, the real
   *  blocker is a missing brand profile instead. */
  protected readonly needsActivation = computed(() => !this.tenantService.isOwnTenantActivated());

  constructor() {
    this.seo.setPageSeo({
      title: 'أكمل بيانات حسابك | رواج',
      description: 'أكمل بيانات النشاط التجاري لتفعيل حسابك والوصول لجميع صفحات لوحة التحكم.',
      keywords: 'رواج, تفعيل الحساب',
      path: '/dashboard/locked',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
