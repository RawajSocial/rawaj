import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  CampaignDetail,
  CampaignSummary,
  CreateCampaignRequest,
  CreateCampaignResponse,
  GenerateCampaignContentRequest,
  GenerateCampaignContentResponse,
  GenerateMarketingPlanResponse,
  PagedResult,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class CampaignsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/campaigns`;

  create(request: CreateCampaignRequest): Observable<CreateCampaignResponse> {
    return this.http.post<ApiResponse<CreateCampaignResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getAll(brandProfileId?: string, page = 1, pageSize = 20): Observable<PagedResult<CampaignSummary>> {
    let params: Record<string, string | number> = { page, pageSize };
    if (brandProfileId) {
      params = { ...params, brandProfileId };
    }

    return this.http
      .get<ApiResponse<PagedResult<CampaignSummary>>>(this.baseUrl, { params })
      .pipe(unwrapApiResponse());
  }

  getById(campaignId: string): Observable<CampaignDetail> {
    return this.http.get<ApiResponse<CampaignDetail>>(`${this.baseUrl}/${campaignId}`).pipe(unwrapApiResponse());
  }

  generatePlan(campaignId: string): Observable<GenerateMarketingPlanResponse> {
    return this.http
      .post<ApiResponse<GenerateMarketingPlanResponse>>(`${this.baseUrl}/${campaignId}/generate-plan`, {})
      .pipe(unwrapApiResponse());
  }

  generateContent(campaignId: string, request: GenerateCampaignContentRequest): Observable<GenerateCampaignContentResponse> {
    return this.http
      .post<ApiResponse<GenerateCampaignContentResponse>>(`${this.baseUrl}/${campaignId}/generate-content`, request)
      .pipe(unwrapApiResponse());
  }
}
