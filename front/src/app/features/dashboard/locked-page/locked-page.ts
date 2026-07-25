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
  /** Set explicitly by the guard that redirected here ('account-setup' | 'activation' | undefined
   *  for the missing-brand-profile case) — avoids re-deriving which condition actually failed. */
  protected reason(): string | null {
    return this.route.snapshot.queryParamMap.get('reason');
  }

  /** Whether Settings (business info) is the unmet condition — if it's already done, the real
   *  blocker is a missing brand profile instead. Kept for the owner-facing activation message. */
  protected readonly needsActivation = computed(() => !this.tenantService.isOwnTenantActivated());

  /** An invited member can't complete the tenant OWNER's business profile themselves — show a
   *  "waiting on the owner" message instead of a CTA that would fail for them. */
  protected readonly isInvitedMember = computed(() => !this.tenantService.isActiveMemberOwner());

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
