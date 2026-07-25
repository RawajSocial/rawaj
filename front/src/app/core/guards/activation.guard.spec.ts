import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { firstValueFrom, isObservable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TenantService } from '../tenant/tenant.service';
import { brandAccessGuard } from './activation.guard';

const MEMBERSHIP = {
  tenantId: 't1', name: 'Acme', tenantType: 'Business' as const, role: 'Owner' as const,
  isOwner: true, isActivated: true, myCoinBalance: 0, accountSetupCompleted: true,
};

const TENANT_BASE = {
  tenantId: 't1', name: 'Acme', subdomain: 'acme', tenantType: 'Business' as const,
  isActive: true, coinBalance: 50, isActivated: true, planName: 'Free', maxBrands: 1,
  maxUsers: 1, extraBrandsPurchased: 0, extraMarketeersPurchased: 0, defaultBrandProfileId: null,
  phone: '123', industry: 'x', country: 'EG', city: 'Cairo', website: null, agencySize: null,
  servicesOffered: [] as string[],
};

describe('brandAccessGuard', () => {
  let tenantService: TenantService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    // Matches MEMBERSHIP.tenantId so `refreshMemberships()` doesn't think this is a
    // never-visited tenant and auto-trigger its own `switchTenant()` -> `refresh()` call — that's
    // the realistic case for a returning user (the browser already has last session's active
    // tenant cached), which is exactly the scenario the guard's `tenant()`-still-null race targets.
    localStorage.setItem('rawaj_active_tenant_id', 't1');
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    tenantService = TestBed.inject(TenantService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => localStorage.removeItem('rawaj_active_tenant_id'));

  afterEach(() => httpMock.verify());

  function runGuard(): unknown {
    return TestBed.runInInjectionContext(() =>
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      brandAccessGuard({} as any, { url: '/dashboard/campaigns' } as any),
    );
  }

  function loadMemberships(): void {
    tenantService.refreshMemberships().subscribe();
    httpMock
      .expectOne(`${environment.apiUrl}/tenants/memberships`)
      .flush({ status: 'success', data: [MEMBERSHIP] });
  }

  it('allows access when the active-tenant summary is not loaded yet but turns out activated with a brand profile (cold-navigation race)', async () => {
    loadMemberships();
    // This is exactly the state a guard sees on a fresh navigation straight into a protected
    // route (direct link, refresh, post-login redirect): memberships are loaded (app initializer
    // resolves before routing starts), but `tenant()` — which `brandProfileCount` reads — is not,
    // because it's only fetched once the dashboard layout component constructs, and guards run
    // *before* that.
    expect(tenantService.tenant()).toBeNull();

    const result = runGuard();
    expect(isObservable(result)).toBe(true);

    // `firstValueFrom` subscribes immediately, which is what actually dispatches the underlying
    // HTTP request against the testing backend — must happen before `expectOne` can see it.
    const resultPromise = firstValueFrom(result as never);
    httpMock
      .expectOne(`${environment.apiUrl}/tenants/me`)
      .flush({ status: 'success', data: { ...TENANT_BASE, brandProfileCount: 1 } });

    await expect(resultPromise).resolves.toBe(true);
  });

  it('redirects to /dashboard/locked when the active tenant genuinely has no brand profile', async () => {
    loadMemberships();

    const result = runGuard();
    const resultPromise = firstValueFrom(result as never);
    httpMock
      .expectOne(`${environment.apiUrl}/tenants/me`)
      .flush({ status: 'success', data: { ...TENANT_BASE, brandProfileCount: 0 } });

    const resolved = await resultPromise;
    expect(resolved instanceof UrlTree).toBe(true);
  });

  it('allows access synchronously once tenant() is already loaded (steady-state navigation)', () => {
    loadMemberships();
    tenantService.refresh().subscribe();
    httpMock
      .expectOne(`${environment.apiUrl}/tenants/me`)
      .flush({ status: 'success', data: { ...TENANT_BASE, brandProfileCount: 1 } });

    expect(tenantService.tenant()).not.toBeNull();

    const result = runGuard();
    expect(result).toBe(true);
  });
});
