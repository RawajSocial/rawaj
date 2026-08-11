import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SeoService } from '../../../services/seo.service';
import { LegalPageLayout } from '../legal-page-layout/legal-page-layout';

/** Public, static Terms & Conditions page — reachable from the sign-up form's consent checkbox and
 *  the site footer. Route `/terms-and-conditions`, no guard. */
@Component({
  selector: 'app-terms-page',
  imports: [LegalPageLayout],
  templateUrl: './terms-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الشروط والأحكام | رواج',
      description: 'الشروط والأحكام الخاصة باستخدام منصة رواج.',
      path: '/terms-and-conditions',
      type: 'website',
    });
  }
}
