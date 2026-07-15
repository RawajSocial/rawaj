import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

export interface GenerateTrialContentResponse {
  generatedText: string;
  remainingTrialsToday: number;
}

export interface GenerateTrialImageResponse {
  imageDataUrl: string;
  remainingTrialsToday: number;
}

/**
 * Standalone AI trial, available without a subscription (a user can try one piece of content or
 * one image to see the product before committing to a paid plan) - the business rule from
 * CreateCampaignCommandHandler's paid-plan gate: campaigns require a subscription, but a trial
 * does not.
 */
@Injectable({ providedIn: 'root' })
export class AiTrialApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/ai-trial`;

  generateContent(prompt: string): Observable<GenerateTrialContentResponse> {
    return this.http
      .post<ApiResponse<GenerateTrialContentResponse>>(`${this.baseUrl}/content`, { prompt })
      .pipe(unwrapApiResponse());
  }

  generateImage(prompt: string): Observable<GenerateTrialImageResponse> {
    return this.http
      .post<ApiResponse<GenerateTrialImageResponse>>(`${this.baseUrl}/image`, { prompt })
      .pipe(unwrapApiResponse());
  }
}
