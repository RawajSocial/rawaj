import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { ContentItemSummary } from '../model/content-item.model';

@Injectable({ providedIn: 'root' })
export class ContentItemService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/content-items`;

  private readonly _items = signal<ContentItemSummary[]>([]);
  readonly items = this._items.asReadonly();

  /** Fetches brand (+ optional campaign)-scoped content items. `brandProfileId` is required by the backend. */
  refresh(
    brandProfileId: string,
    campaignId?: string | 'all',
    page = 1,
    pageSize = 50,
  ): Observable<ApiResponse<PagedResult<ContentItemSummary>>> {
    let url = `${this.baseUrl}?brandProfileId=${encodeURIComponent(brandProfileId)}&page=${page}&pageSize=${pageSize}`;
    if (campaignId && campaignId !== 'all') url += `&campaignId=${encodeURIComponent(campaignId)}`;
    return this.http.get<ApiResponse<PagedResult<ContentItemSummary>>>(url).pipe(
      tap(res => {
        if (res.data) this._items.set(res.data.items);
      }),
    );
  }

  clear(): void {
    this._items.set([]);
  }
}
