import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';

export interface FeatureFlags {
  videoGeneration: boolean;
  experimentalAi: boolean;
  coinLedger: boolean;
}

const DEFAULT_FLAGS: FeatureFlags = { videoGeneration: false, experimentalAi: false, coinLedger: true };

/** Config-bound feature toggles fetched once at app init — lets a bad rollout be disabled from
 *  the backend's appsettings without a frontend redeploy. Anonymous endpoint, safe pre-login. */
@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/platform`;

  private readonly _flags = signal<FeatureFlags>(DEFAULT_FLAGS);
  readonly flags = this._flags.asReadonly();

  refresh(): void {
    this.http.get<ApiResponse<FeatureFlags>>(`${this.baseUrl}/feature-flags`).subscribe({
      next: res => {
        if (res.data) this._flags.set(res.data);
      },
    });
  }
}
