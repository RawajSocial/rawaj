import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { LoaderService } from '../../../services/loader.service';
import { SeoService } from '../../../services/seo.service';

/**
 * Internal test page for previewing the global page-loader animation on
 * demand, without having to trigger a real route transition or async op.
 */
@Component({
  selector: 'app-loading-test-page',
  imports: [PageHeader],
  templateUrl: './loading-test-page.html',
  styleUrls: ['../dashboard-shared.css', './loading-test-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingTestPage {
  private readonly loaderService = inject(LoaderService);
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'اختبار شاشة التحميل | رواج',
      description: 'صفحة داخلية لمعاينة حركة شاشة التحميل.',
      keywords: 'رواج, اختبار, شاشة التحميل',
      path: '/dashboard/loading-test',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected showFor(seconds: number): void {
    this.loaderService.show();
    setTimeout(() => this.loaderService.hide(), seconds * 1000);
  }
}
