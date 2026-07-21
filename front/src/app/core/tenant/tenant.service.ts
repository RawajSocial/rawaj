import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { BrandProfilesApiService } from '../api/brand-profiles-api.service';
import { TenantsApiService } from '../api/tenants-api.service';
import { BrandProfileSummary, MyTenant } from '../models';

const ACTIVE_BRAND_KEY = 'rawaj.tenant.activeBrandProfileId';

/**
 * The backend resolves the active tenant server-side from the JWT (a user currently belongs to
 * exactly one tenant - see TenantAuthorizationBehavior), so there is no tenant-switcher here.
 * This just caches "my tenant" + its brand profiles after login so every dashboard page can read
 * them synchronously instead of re-fetching. The active *brand* within that tenant is a client-side
 * choice (see the header's brand switcher) persisted in localStorage so it survives a reload.
 */
@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly tenantsApi = inject(TenantsApiService);
  private readonly brandProfilesApi = inject(BrandProfilesApiService);

  private readonly tenantSignal = signal<MyTenant | null>(null);
  private readonly brandProfilesSignal = signal<BrandProfileSummary[]>([]);
  private readonly activeBrandProfileIdSignal = signal<string | null>(localStorage.getItem(ACTIVE_BRAND_KEY));
  private readonly loadedSignal = signal(false);

  readonly tenant = this.tenantSignal.asReadonly();
  readonly brandProfiles = this.brandProfilesSignal.asReadonly();
  readonly loaded = this.loadedSignal.asReadonly();

  readonly activeBrandProfile = computed(() => {
    const id = this.activeBrandProfileIdSignal();
    return this.brandProfilesSignal().find((b) => b.brandProfileId === id) ?? null;
  });

  readonly hasTenant = computed(() => this.tenantSignal() !== null);

  /** Call once after login/register, and on app init if a session already exists. */
  loadContext(): Observable<{ tenant: MyTenant | null; brandProfiles: BrandProfileSummary[] }> {
    return this.tenantsApi.getMine().pipe(
      catchError(() => of(null)),
      switchMap((tenant) => {
        this.tenantSignal.set(tenant);

        if (!tenant) {
          this.brandProfilesSignal.set([]);
          this.loadedSignal.set(true);
          return of({ tenant, brandProfiles: [] as BrandProfileSummary[] });
        }

        return this.brandProfilesApi.getAll().pipe(
          map((brandProfiles) => {
            this.brandProfilesSignal.set(brandProfiles);
            this.setDefaultActiveBrand(brandProfiles);
            this.loadedSignal.set(true);
            return { tenant, brandProfiles };
          }),
        );
      }),
    );
  }

  refreshBrandProfiles(): Observable<BrandProfileSummary[]> {
    return this.brandProfilesApi.getAll().pipe(
      tap((brandProfiles) => {
        this.brandProfilesSignal.set(brandProfiles);
        this.setDefaultActiveBrand(brandProfiles);
      }),
    );
  }

  setActiveBrandProfile(brandProfileId: string): void {
    this.activeBrandProfileIdSignal.set(brandProfileId);
    localStorage.setItem(ACTIVE_BRAND_KEY, brandProfileId);
  }

  clear(): void {
    this.tenantSignal.set(null);
    this.brandProfilesSignal.set([]);
    this.activeBrandProfileIdSignal.set(null);
    this.loadedSignal.set(false);
    localStorage.removeItem(ACTIVE_BRAND_KEY);
  }

  private setDefaultActiveBrand(brandProfiles: BrandProfileSummary[]): void {
    const current = this.activeBrandProfileIdSignal();
    if (current && brandProfiles.some((b) => b.brandProfileId === current)) {
      return;
    }

    const defaultBrand = brandProfiles.find((b) => b.isDefault) ?? brandProfiles[0] ?? null;
    this.setActiveOrClear(defaultBrand?.brandProfileId ?? null);
  }

  private setActiveOrClear(brandProfileId: string | null): void {
    this.activeBrandProfileIdSignal.set(brandProfileId);
    if (brandProfileId) {
      localStorage.setItem(ACTIVE_BRAND_KEY, brandProfileId);
    } else {
      localStorage.removeItem(ACTIVE_BRAND_KEY);
    }
  }
}
