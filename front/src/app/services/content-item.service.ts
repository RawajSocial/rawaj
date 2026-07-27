import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import {
  ContentItemSummary, GenerateContentItemInput, GenerateContentItemResponse,
  RegenerateContentItemResponse, ReviewContentItemResponse,
} from '../model/content-item.model';

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

  /** Real single-item text generation (توليد المحتوى) — charges coins (first 5/month free). */
  generate(input: GenerateContentItemInput): Observable<ApiResponse<GenerateContentItemResponse>> {
    return this.http.post<ApiResponse<GenerateContentItemResponse>>(`${this.baseUrl}/generate`, input);
  }

  /** Accept/refuse a draft — flips its status to Approved or Rejected. */
  review(contentItemId: string, approve: boolean): Observable<ApiResponse<ReviewContentItemResponse>> {
    return this.http.post<ApiResponse<ReviewContentItemResponse>>(
      `${this.baseUrl}/${contentItemId}/review`, { approve },
    );
  }

  /** Regenerates a draft's copy from free-text feedback, resetting it back to Draft for re-review. */
  regenerate(contentItemId: string, feedback: string): Observable<ApiResponse<RegenerateContentItemResponse>> {
    return this.http.post<ApiResponse<RegenerateContentItemResponse>>(
      `${this.baseUrl}/${contentItemId}/regenerate`, { feedback },
    );
  }

  /** Soft-deletes a content item (and its own generated images) — rejected server-side if it's
   *  still actively scheduled or already published. */
  delete(contentItemId: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${contentItemId}`).pipe(
      tap(res => {
        if (res.data) this._items.update(list => list.filter(i => i.contentItemId !== contentItemId));
      }),
    );
  }
}
