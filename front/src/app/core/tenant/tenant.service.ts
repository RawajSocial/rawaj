import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../model/auth.model';
import {
  TenantSummary, UpdateTenantProfileRequest, UpdateTenantProfileResponse,
  UpgradeToAgencyRequest, UpgradeToAgencyResponse,
} from '../../model/tenant.model';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenants`;

  private readonly _tenant = signal<TenantSummary | null>(null);
  readonly tenant = this._tenant.asReadonly();

  readonly isAgency = computed(() => this._tenant()?.tenantType === 'Agency');
  readonly isActivated = computed(() => this._tenant()?.isActivated ?? false);
  readonly coinBalance = computed(() => this._tenant()?.coinBalance ?? 0);
  readonly defaultBrandProfileId = computed(() => this._tenant()?.defaultBrandProfileId ?? null);
  readonly brandProfileCount = computed(() => this._tenant()?.brandProfileCount ?? 0);
  readonly maxBrands = computed(() => this._tenant()?.maxBrands ?? 1);

  refresh(): Observable<ApiResponse<TenantSummary>> {
    return this.http.get<ApiResponse<TenantSummary>>(`${this.baseUrl}/me`).pipe(
      tap(res => {
        if (res.data) this._tenant.set(res.data);
      }),
    );
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
  }
}
