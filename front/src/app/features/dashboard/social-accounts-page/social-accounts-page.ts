import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { SocialAccountService } from '../../../core/social/social-account.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { SocialAccountSummary, SocialPlatform } from '../../../model/social-account.model';

interface PlatformOption {
  key: SocialPlatform;
  label: string;
  icon: string;
  color: string;
}

const PLATFORM_OPTIONS: PlatformOption[] = [
  { key: 'Facebook', label: 'فيسبوك', icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)' },
  { key: 'Instagram', label: 'إنستغرام', icon: 'fa-brands fa-instagram', color: 'var(--color-instagram)' },
  { key: 'Linkedin', label: 'لينكدإن', icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)' },
];

@Component({
  selector: 'app-social-accounts-page',
  imports: [PageHeader, TooltipDirective],
  templateUrl: './social-accounts-page.html',
  styleUrls: ['../dashboard-shared.css', './social-accounts-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SocialAccountsPage {
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  protected readonly perms = inject(PermissionService);
  private readonly socialAccountService = inject(SocialAccountService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly platforms = PLATFORM_OPTIONS;
  protected readonly accounts = signal<SocialAccountSummary[]>([]);
  protected readonly loading = signal(false);

  constructor() {
    this.seo.setPageSeo({
      title: 'ربط حسابات التواصل الاجتماعي | رواج',
      description: 'اربط حسابات علامتك التجارية بمنصات التواصل الاجتماعي عبر OAuth.',
      keywords: 'رواج, ربط الحسابات, OAuth, سوشيال ميديا',
      path: '/dashboard/social-accounts',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    const params = this.route.snapshot.queryParamMap;
    const connected = params.get('connected');
    const error = params.get('error');
    if (connected) {
      this.errorModalService.show(`تم ربط حسابك على ${connected} بنجاح.`, { variant: 'success', title: 'تم الربط' });
      this.router.navigate([], { relativeTo: this.route, queryParams: {} });
    } else if (error) {
      this.errorModalService.show(error, { variant: 'error' });
      this.router.navigate([], { relativeTo: this.route, queryParams: {} });
    }

    effect(() => {
      const brandProfileId = this.tenantService.defaultBrandProfileId();
      if (brandProfileId) this.loadAccounts(brandProfileId);
    });
  }

  protected isConnected(platform: SocialPlatform): SocialAccountSummary | undefined {
    return this.accounts().find(a => a.platform === platform && a.isActive);
  }

  protected connect(platform: SocialPlatform): void {
    if (!this.perms.canAdmin()) return;
    const brandProfileId = this.tenantService.defaultBrandProfileId();
    if (!brandProfileId) return;

    this.loaderService.show();
    this.socialAccountService.getAuthorizationUrl(platform, brandProfileId).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status === 'success' && res.data) {
          window.location.href = res.data.authorizationUrl;
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر بدء عملية الربط.', { variant: 'error' });
        }
      },
      error: () => {
        this.loaderService.hide();
        this.errorModalService.show('تعذّر بدء عملية الربط. يرجى المحاولة مرة أخرى.', { variant: 'error' });
      },
    });
  }

  protected disconnect(account: SocialAccountSummary): void {
    if (!this.perms.canAdmin()) return;
    this.loaderService.show();
    this.socialAccountService.disconnect(account.socialAccountId).subscribe({
      next: res => {
        this.loaderService.hide();
        if (res.status === 'success') {
          this.accounts.update(list => list.filter(a => a.socialAccountId !== account.socialAccountId));
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر فصل الحساب.', { variant: 'error' });
        }
      },
      error: () => {
        this.loaderService.hide();
        this.errorModalService.show('تعذّر فصل الحساب. يرجى المحاولة مرة أخرى.', { variant: 'error' });
      },
    });
  }

  private loadAccounts(brandProfileId: string): void {
    this.loading.set(true);
    this.socialAccountService.getByBrand(brandProfileId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.data) this.accounts.set(res.data);
      },
      error: () => this.loading.set(false),
    });
  }
}
