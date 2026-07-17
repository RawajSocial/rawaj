import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  BrandProfileSummary,
  CreateBrandProfileRequest,
  CreateBrandProfileResponse,
  GenerateOnboardingQuestionsResponse,
  UpdateBrandProfileRequest,
  UpdateBrandProfileResponse,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class BrandProfilesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/brand-profiles`;

  create(request: CreateBrandProfileRequest): Observable<CreateBrandProfileResponse> {
    return this.http.post<ApiResponse<CreateBrandProfileResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getAll(): Observable<BrandProfileSummary[]> {
    return this.http.get<ApiResponse<BrandProfileSummary[]>>(this.baseUrl).pipe(unwrapApiResponse());
  }

  update(brandProfileId: string, request: UpdateBrandProfileRequest): Observable<UpdateBrandProfileResponse> {
    return this.http
      .put<ApiResponse<UpdateBrandProfileResponse>>(`${this.baseUrl}/${brandProfileId}`, request)
      .pipe(unwrapApiResponse());
  }

  generateOnboardingQuestions(brandProfileId: string, onboardingContext: unknown): Observable<GenerateOnboardingQuestionsResponse> {
    return this.http
      .post<ApiResponse<GenerateOnboardingQuestionsResponse>>(
        `${this.baseUrl}/${brandProfileId}/onboarding-questions`,
        { onboardingContext },
      )
      .pipe(unwrapApiResponse());
  }
}
