import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  ContentItemDetail,
  ContentItemSummary,
  ContentRevisionSummary,
  GenerateContentItemRequest,
  GenerateContentItemResponse,
  PagedResult,
  RegenerateContentItemResponse,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class ContentApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/content-items`;

  generate(request: GenerateContentItemRequest): Observable<GenerateContentItemResponse> {
    return this.http
      .post<ApiResponse<GenerateContentItemResponse>>(`${this.baseUrl}/generate`, request)
      .pipe(unwrapApiResponse());
  }

  getByCampaign(campaignId: string, page = 1, pageSize = 20): Observable<PagedResult<ContentItemSummary>> {
    return this.http
      .get<ApiResponse<PagedResult<ContentItemSummary>>>(`${this.baseUrl}/campaign/${campaignId}`, {
        params: { page, pageSize },
      })
      .pipe(unwrapApiResponse());
  }

  getById(contentItemId: string): Observable<ContentItemDetail> {
    return this.http
      .get<ApiResponse<ContentItemDetail>>(`${this.baseUrl}/${contentItemId}`)
      .pipe(unwrapApiResponse());
  }

  review(contentItemId: string, approve: boolean): Observable<{ contentItemId: string; status: string; reviewedAt: string }> {
    return this.http
      .post<ApiResponse<{ contentItemId: string; status: string; reviewedAt: string }>>(
        `${this.baseUrl}/${contentItemId}/review`,
        { approve },
      )
      .pipe(unwrapApiResponse());
  }

  regenerate(contentItemId: string, feedback: string): Observable<RegenerateContentItemResponse> {
    return this.http
      .post<ApiResponse<RegenerateContentItemResponse>>(`${this.baseUrl}/${contentItemId}/regenerate`, { feedback })
      .pipe(unwrapApiResponse());
  }

  getRevisions(contentItemId: string): Observable<ContentRevisionSummary[]> {
    return this.http
      .get<ApiResponse<ContentRevisionSummary[]>>(`${this.baseUrl}/${contentItemId}/revisions`)
      .pipe(unwrapApiResponse());
  }
}
