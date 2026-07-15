import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, GetAuthorizationUrlResponse, SocialAccountSummary, SocialPlatform } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class SocialAccountsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/social-accounts`;

  getByBrand(brandProfileId: string): Observable<SocialAccountSummary[]> {
    return this.http
      .get<ApiResponse<SocialAccountSummary[]>>(`${this.baseUrl}/brand/${brandProfileId}`)
      .pipe(unwrapApiResponse());
  }

  getAuthorizationUrl(platform: SocialPlatform, brandProfileId: string): Observable<GetAuthorizationUrlResponse> {
    return this.http
      .post<ApiResponse<GetAuthorizationUrlResponse>>(
        `${this.baseUrl}/${platform.toLowerCase()}/authorization-url`,
        {},
        { params: { brandProfileId } },
      )
      .pipe(unwrapApiResponse());
  }

  disconnect(socialAccountId: string): Observable<{ socialAccountId: string; isActive: boolean }> {
    return this.http
      .post<ApiResponse<{ socialAccountId: string; isActive: boolean }>>(
        `${this.baseUrl}/${socialAccountId}/disconnect`,
        {},
      )
      .pipe(unwrapApiResponse());
  }
}
