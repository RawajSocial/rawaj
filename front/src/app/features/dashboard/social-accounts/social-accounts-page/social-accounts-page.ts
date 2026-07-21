import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SocialAccountsApiService } from '../../../../core/api/social-accounts-api.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { ApiError } from '../../../../core/api';
import { SocialAccountSummary, SocialPlatform } from '../../../../core/models';

const PLATFORM_CFG: Record<SocialPlatform, { icon: string; color: string; label: string }> = {
  Instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
  Facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
  Tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#222',    label: 'تيك توك'  },
  Youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
  Twitter:   { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
  Linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
};

// Only Facebook/Instagram have a working OAuth provider today (see Rawaj.Infrastructure's
// DependencyInjection) - the rest are left in PLATFORM_CFG for later but hidden from the connect
// list so we don't offer a connection we can't actually complete.
const ALL_PLATFORMS: SocialPlatform[] = ['Facebook', 'Instagram'];

@Component({
  selector: 'app-social-accounts-page',
  imports: [PageHeader],
  templateUrl: './social-accounts-page.html',
  styleUrls: ['../../dashboard-shared.css', './social-accounts-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SocialAccountsPage {
  private readonly socialAccountsApi = inject(SocialAccountsApiService);
  private readonly tenantService = inject(TenantService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly platforms = ALL_PLATFORMS;

  protected readonly accounts = signal<SocialAccountSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly connectingPlatform = signal<SocialPlatform | null>(null);
  protected readonly connectError = signal<string | null>(null);
  protected readonly disconnectingId = signal<string | null>(null);

  protected readonly banner = signal<{ kind: 'success' | 'error'; text: string } | null>(null);

  protected readonly canManageAccounts = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin';
  });

  protected readonly accountsByPlatform = computed(() => {
    const map = new Map<SocialPlatform, SocialAccountSummary>();
    for (const account of this.accounts()) {
      map.set(account.platform, account);
    }
    return map;
  });

  constructor() {
    this.consumeCallbackParams();

    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.load(brandProfileId);
      }
    });
  }

  private consumeCallbackParams(): void {
    const params = this.route.snapshot.queryParamMap;
    const connected = params.get('connected');
    const error = params.get('error');

    if (connected) {
      const label = PLATFORM_CFG[connected as SocialPlatform]?.label ?? connected;
      this.banner.set({ kind: 'success', text: `تم ربط حساب ${label} بنجاح.` });
    } else if (error) {
      this.banner.set({ kind: 'error', text: error });
    }

    if (connected || error) {
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
    }
  }

  private load(brandProfileId: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.socialAccountsApi.getByBrand(brandProfileId).subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الحسابات المرتبطة.');
      },
    });
  }

  protected platformCfg(platform: SocialPlatform) {
    return PLATFORM_CFG[platform];
  }

  protected connect(platform: SocialPlatform): void {
    const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
    if (!brandProfileId) return;

    this.connectingPlatform.set(platform);
    this.connectError.set(null);

    this.socialAccountsApi.getAuthorizationUrl(platform, brandProfileId).subscribe({
      next: (result) => {
        window.location.href = result.authorizationUrl;
      },
      error: (error: unknown) => {
        this.connectingPlatform.set(null);
        this.connectError.set(
          error instanceof ApiError ? error.message : 'تعذر بدء عملية الربط، حاول مرة أخرى.',
        );
      },
    });
  }

  protected disconnect(account: SocialAccountSummary): void {
    this.disconnectingId.set(account.socialAccountId);

    this.socialAccountsApi.disconnect(account.socialAccountId).subscribe({
      next: () => {
        this.disconnectingId.set(null);
        const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
        if (brandProfileId) this.load(brandProfileId);
      },
      error: (error: unknown) => {
        this.disconnectingId.set(null);
        this.connectError.set(
          error instanceof ApiError ? error.message : 'تعذر فصل الحساب، حاول مرة أخرى.',
        );
      },
    });
  }

  protected dismissBanner(): void {
    this.banner.set(null);
  }
}
