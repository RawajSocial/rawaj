import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  GenerateVisualAssetRequest,
  GenerateVisualAssetResponse,
  PagedResult,
  VisualAssetSummary,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class VisualAssetsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/visual-assets`;

  generate(request: GenerateVisualAssetRequest): Observable<GenerateVisualAssetResponse> {
    return this.http
      .post<ApiResponse<GenerateVisualAssetResponse>>(`${this.baseUrl}/generate`, request)
      .pipe(unwrapApiResponse());
  }

  getByCampaign(campaignId: string, page = 1, pageSize = 20): Observable<PagedResult<VisualAssetSummary>> {
    return this.http
      .get<ApiResponse<PagedResult<VisualAssetSummary>>>(`${this.baseUrl}/campaign/${campaignId}`, {
        params: { page, pageSize },
      })
      .pipe(unwrapApiResponse());
  }

  review(visualAssetId: string, approve: boolean): Observable<{ visualAssetId: string; isApproved: boolean }> {
    return this.http
      .post<ApiResponse<{ visualAssetId: string; isApproved: boolean }>>(`${this.baseUrl}/${visualAssetId}/review`, {
        approve,
      })
      .pipe(unwrapApiResponse());
  }
}
