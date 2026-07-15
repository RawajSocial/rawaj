import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddCompetitorRequest,
  AddCompetitorResponse,
  AnalyzeCompetitorResponse,
  ApiResponse,
  CompetitorSummary,
  PagedResult,
  RagDocumentSummary,
  ScrapeWebsiteResponse,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class CompetitorsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/competitors`;

  add(request: AddCompetitorRequest): Observable<AddCompetitorResponse> {
    return this.http.post<ApiResponse<AddCompetitorResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getByBrand(brandProfileId: string, page = 1, pageSize = 20): Observable<PagedResult<CompetitorSummary>> {
    return this.http
      .get<ApiResponse<PagedResult<CompetitorSummary>>>(`${this.baseUrl}/brand/${brandProfileId}`, {
        params: { page, pageSize },
      })
      .pipe(unwrapApiResponse());
  }

  analyze(competitorId: string): Observable<AnalyzeCompetitorResponse> {
    return this.http
      .post<ApiResponse<AnalyzeCompetitorResponse>>(`${this.baseUrl}/${competitorId}/analyze`, {})
      .pipe(unwrapApiResponse());
  }

  scrapeWebsite(competitorId: string, url: string): Observable<ScrapeWebsiteResponse> {
    return this.http
      .post<ApiResponse<ScrapeWebsiteResponse>>(`${this.baseUrl}/${competitorId}/scrape`, { url })
      .pipe(unwrapApiResponse());
  }

  getAnalysis(competitorId: string): Observable<RagDocumentSummary[]> {
    return this.http
      .get<ApiResponse<RagDocumentSummary[]>>(`${this.baseUrl}/${competitorId}/analysis`)
      .pipe(unwrapApiResponse());
  }
}
