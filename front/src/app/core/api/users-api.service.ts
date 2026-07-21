import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  ChangeMyPasswordRequest,
  MyProfile,
  UpdateMyProfileRequest,
  UpdateMyProfileResponse,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/users`;

  getMyProfile(): Observable<MyProfile> {
    return this.http.get<ApiResponse<MyProfile>>(`${this.baseUrl}/me`).pipe(unwrapApiResponse());
  }

  updateMyProfile(request: UpdateMyProfileRequest): Observable<UpdateMyProfileResponse> {
    return this.http.put<ApiResponse<UpdateMyProfileResponse>>(`${this.baseUrl}/me`, request).pipe(unwrapApiResponse());
  }

  changeMyPassword(request: ChangeMyPasswordRequest): Observable<boolean> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/me/change-password`, request)
      .pipe(unwrapApiResponse());
  }
}
