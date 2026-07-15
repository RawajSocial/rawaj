import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  CancelScheduledPostResponse,
  PagedResult,
  PublishScheduledPostResponse,
  SchedulePostRequest,
  SchedulePostResponse,
  ScheduledPostSummary,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class SchedulingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/scheduled-posts`;

  schedule(request: SchedulePostRequest): Observable<SchedulePostResponse> {
    return this.http.post<ApiResponse<SchedulePostResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getAll(campaignId?: string, page = 1, pageSize = 20): Observable<PagedResult<ScheduledPostSummary>> {
    let params: Record<string, string | number> = { page, pageSize };
    if (campaignId) {
      params = { ...params, campaignId };
    }

    return this.http
      .get<ApiResponse<PagedResult<ScheduledPostSummary>>>(this.baseUrl, { params })
      .pipe(unwrapApiResponse());
  }

  cancel(scheduledPostId: string): Observable<CancelScheduledPostResponse> {
    return this.http
      .post<ApiResponse<CancelScheduledPostResponse>>(`${this.baseUrl}/${scheduledPostId}/cancel`, {})
      .pipe(unwrapApiResponse());
  }

  publishNow(scheduledPostId: string): Observable<PublishScheduledPostResponse> {
    return this.http
      .post<ApiResponse<PublishScheduledPostResponse>>(`${this.baseUrl}/${scheduledPostId}/publish-now`, {})
      .pipe(unwrapApiResponse());
  }
}
