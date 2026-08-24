import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFound {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الصفحة غير موجودة | رواج',
      description: 'الصفحة التي تبحث عنها غير موجودة أو تم نقلها.',
      keywords: 'رواج, 404, صفحة غير موجودة',
      path: '/404',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
