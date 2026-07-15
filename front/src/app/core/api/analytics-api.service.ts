import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, BrandAnalyticsOverview, CampaignAnalyticsSummary, PostAnalyticsSnapshot } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/analytics`;

  syncPost(scheduledPostId: string): Observable<PostAnalyticsSnapshot> {
    return this.http
      .post<ApiResponse<PostAnalyticsSnapshot>>(`${this.baseUrl}/posts/${scheduledPostId}/sync`, {})
      .pipe(unwrapApiResponse());
  }

  getPost(scheduledPostId: string): Observable<PostAnalyticsSnapshot[]> {
    return this.http
      .get<ApiResponse<PostAnalyticsSnapshot[]>>(`${this.baseUrl}/posts/${scheduledPostId}`)
      .pipe(unwrapApiResponse());
  }

  getCampaign(campaignId: string): Observable<CampaignAnalyticsSummary> {
    return this.http
      .get<ApiResponse<CampaignAnalyticsSummary>>(`${this.baseUrl}/campaigns/${campaignId}`)
      .pipe(unwrapApiResponse());
  }

  getBrandOverview(brandProfileId: string): Observable<BrandAnalyticsOverview> {
    return this.http
      .get<ApiResponse<BrandAnalyticsOverview>>(`${this.baseUrl}/brand/${brandProfileId}`)
      .pipe(unwrapApiResponse());
  }
}
