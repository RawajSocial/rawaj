import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SeoService } from '../../../services/seo.service';
import { LegalPageLayout } from '../legal-page-layout/legal-page-layout';

/** Public, static Privacy Policy page — reachable from the sign-up form's consent checkbox and the
 *  site footer. Route `/privacy-policy`, no guard. */
@Component({
  selector: 'app-privacy-policy-page',
  imports: [LegalPageLayout],
  templateUrl: './privacy-policy-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyPolicyPage {
  private readonly seo = inject(SeoService);

  constructor() {
    this.seo.setPageSeo({
      title: 'سياسة الخصوصية | رواج',
      description: 'سياسة الخصوصية الخاصة بمنصة رواج — كيف نجمع بياناتك ونستخدمها ونحميها.',
      path: '/privacy-policy',
      type: 'website',
    });
  }
}
