import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SeoService } from '../../../services/seo.service';

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

  protected readonly returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

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
