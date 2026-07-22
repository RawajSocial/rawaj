import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../model/auth.model';
import {
  DisconnectSocialAccountResponse, GetAuthorizationUrlResponse, SocialAccountSummary, SocialPlatform,
} from '../../model/social-account.model';

@Injectable({ providedIn: 'root' })
export class SocialAccountService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/social-accounts`;

  getByBrand(brandProfileId: string): Observable<ApiResponse<SocialAccountSummary[]>> {
    return this.http.get<ApiResponse<SocialAccountSummary[]>>(`${this.baseUrl}/brand/${brandProfileId}`);
  }

  getAuthorizationUrl(platform: SocialPlatform, brandProfileId: string): Observable<ApiResponse<GetAuthorizationUrlResponse>> {
    return this.http.post<ApiResponse<GetAuthorizationUrlResponse>>(
      `${this.baseUrl}/${platform}/authorization-url?brandProfileId=${brandProfileId}`,
      {},
    );
  }

  disconnect(socialAccountId: string): Observable<ApiResponse<DisconnectSocialAccountResponse>> {
    return this.http.post<ApiResponse<DisconnectSocialAccountResponse>>(
      `${this.baseUrl}/${socialAccountId}/disconnect`,
      {},
    );
  }
}
