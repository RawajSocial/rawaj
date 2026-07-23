import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { VisualAssetSummary } from '../model/visual-asset.model';

@Injectable({ providedIn: 'root' })
export class VisualAssetService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/visual-assets`;

  private readonly _assets = signal<VisualAssetSummary[]>([]);
  readonly assets = this._assets.asReadonly();

  /** Fetches brand (+ optional campaign)-scoped visual assets. `brandProfileId` is required by the backend. */
  refresh(
    brandProfileId: string,
    campaignId?: string | 'all',
    page = 1,
    pageSize = 50,
  ): Observable<ApiResponse<PagedResult<VisualAssetSummary>>> {
    let url = `${this.baseUrl}?brandProfileId=${encodeURIComponent(brandProfileId)}&page=${page}&pageSize=${pageSize}`;
    if (campaignId && campaignId !== 'all') url += `&campaignId=${encodeURIComponent(campaignId)}`;
    return this.http.get<ApiResponse<PagedResult<VisualAssetSummary>>>(url).pipe(
      tap(res => {
        if (res.data) this._assets.set(res.data.items);
      }),
    );
  }

  clear(): void {
    this._assets.set([]);
  }
}
