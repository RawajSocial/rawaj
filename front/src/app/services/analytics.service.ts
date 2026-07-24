import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { CampaignAnalyticsSummary, PostAnalyticsSnapshot } from '../model/analytics.model';

/** Stateless like SocialAccountService — campaign/post analytics are read on exactly two pages
 *  and staleness matters more than sharing a cached signal between them. */
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/analytics`;

  getCampaign(campaignId: string): Observable<ApiResponse<CampaignAnalyticsSummary>> {
    return this.http.get<ApiResponse<CampaignAnalyticsSummary>>(`${this.baseUrl}/campaigns/${campaignId}`);
  }

  getPost(scheduledPostId: string): Observable<ApiResponse<PostAnalyticsSnapshot[]>> {
    return this.http.get<ApiResponse<PostAnalyticsSnapshot[]>>(`${this.baseUrl}/posts/${scheduledPostId}`);
  }

  syncPost(scheduledPostId: string): Observable<ApiResponse<PostAnalyticsSnapshot>> {
    return this.http.post<ApiResponse<PostAnalyticsSnapshot>>(`${this.baseUrl}/posts/${scheduledPostId}/sync`, {});
  }
}
