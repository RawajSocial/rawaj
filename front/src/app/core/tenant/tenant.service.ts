import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../model/auth.model';
import {
  TenantMembership, TenantSummary, UpdateTenantProfileRequest, UpdateTenantProfileResponse,
  UpgradeToAgencyRequest, UpgradeToAgencyResponse,
} from '../../model/tenant.model';

const ACTIVE_TENANT_KEY = 'rawaj_active_tenant_id';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenants`;

  private readonly _tenant = signal<TenantSummary | null>(null);
  readonly tenant = this._tenant.asReadonly();

  private readonly _memberships = signal<TenantMembership[]>([]);
  readonly memberships = this._memberships.asReadonly();

  /** The tenant the app is currently acting in — sent as `X-Tenant-Id` by `tenantInterceptor` on
   *  every request. Persisted so a reload keeps whichever organization the user last switched to. */
  private readonly _activeTenantId = signal<string | null>(localStorage.getItem(ACTIVE_TENANT_KEY));
  readonly activeTenantId = this._activeTenantId.asReadonly();

  readonly isAgency = computed(() => this._tenant()?.tenantType === 'Agency');
  readonly isActivated = computed(() => this._tenant()?.isActivated ?? false);
  readonly coinBalance = computed(() => this._tenant()?.coinBalance ?? 0);
  readonly defaultBrandProfileId = computed(() => this._tenant()?.defaultBrandProfileId ?? null);
  readonly brandProfileCount = computed(() => this._tenant()?.brandProfileCount ?? 0);
  readonly maxBrands = computed(() => this._tenant()?.maxBrands ?? 1);
  readonly maxUsers = computed(() => this._tenant()?.maxUsers ?? 1);
  readonly extraBrandsPurchased = computed(() => this._tenant()?.extraBrandsPurchased ?? 0);
  readonly extraMarketeersPurchased = computed(() => this._tenant()?.extraMarketeersPurchased ?? 0);

  /** Whether the user's OWN tenant (the one they own, not necessarily the active one) is
   *  activated — used to gate pages while an invited member's own business info is incomplete. */
  readonly isOwnTenantActivated = computed(() => {
    const own = this._memberships().find(m => m.isOwner);
    return own?.isActivated ?? this.isActivated();
  });

  refresh(): Observable<ApiResponse<TenantSummary>> {
    return this.http.get<ApiResponse<TenantSummary>>(`${this.baseUrl}/me`).pipe(
      tap(res => {
        if (res.data) this._tenant.set(res.data);
      }),
    );
  }

  refreshMemberships(): Observable<ApiResponse<TenantMembership[]>> {
    return this.http.get<ApiResponse<TenantMembership[]>>(`${this.baseUrl}/memberships`).pipe(
      tap(res => {
        if (!res.data) return;
        this._memberships.set(res.data);
        // If we don't have an active tenant yet (or it's no longer one of ours), default to the
        // user's own tenant.
        if (!res.data.some(m => m.tenantId === this._activeTenantId())) {
          const ownTenant = res.data.find(m => m.isOwner) ?? res.data[0];
          if (ownTenant) this.switchTenant(ownTenant.tenantId);
        }
      }),
    );
  }

  /** Switches which tenant subsequent requests act in, then reloads that tenant's summary. */
  switchTenant(tenantId: string): void {
    this._activeTenantId.set(tenantId);
    localStorage.setItem(ACTIVE_TENANT_KEY, tenantId);
    this.refresh().subscribe();
  }

  updateProfile(request: UpdateTenantProfileRequest): Observable<ApiResponse<UpdateTenantProfileResponse>> {
    return this.http.put<ApiResponse<UpdateTenantProfileResponse>>(`${this.baseUrl}/me/profile`, request).pipe(
      tap(res => {
        if (res.data) {
          this._tenant.update(t => t && {
            ...t,
            isActivated: res.data!.isActivated,
            coinBalance: res.data!.coinBalance,
            ...request,
          });
          // `isOwnTenantActivated` reads this array, not `_tenant` — without patching it here it
          // stays stale (activation guards keep redirecting to /dashboard/locked) until the next
          // full `refreshMemberships()` call, i.e. next login.
          this._memberships.update(members =>
            members.map(m => (m.tenantId === res.data!.tenantId ? { ...m, isActivated: res.data!.isActivated } : m)),
          );
        }
      }),
    );
  }

  upgradeToAgency(request: UpgradeToAgencyRequest): Observable<ApiResponse<UpgradeToAgencyResponse>> {
    return this.http.post<ApiResponse<UpgradeToAgencyResponse>>(`${this.baseUrl}/me/upgrade-to-agency`, request).pipe(
      tap(res => {
        if (res.data) {
          this._tenant.update(t => t && {
            ...t,
            tenantType: res.data!.tenantType,
            planName: res.data!.planName,
            maxBrands: res.data!.maxBrands,
            agencySize: request.agencySize,
            servicesOffered: request.servicesOffered,
          });
        }
      }),
    );
  }

  clear(): void {
    this._tenant.set(null);
    this._memberships.set([]);
    this._activeTenantId.set(null);
    localStorage.removeItem(ACTIVE_TENANT_KEY);
  }
}
