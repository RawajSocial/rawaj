import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, NotificationSummary, PagedResult } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/notifications`;

  getAll(unreadOnly = false, page = 1, pageSize = 20): Observable<PagedResult<NotificationSummary>> {
    return this.http
      .get<ApiResponse<PagedResult<NotificationSummary>>>(this.baseUrl, { params: { unreadOnly, page, pageSize } })
      .pipe(unwrapApiResponse());
  }

  markRead(notificationId: string): Observable<boolean> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/${notificationId}/read`, {})
      .pipe(unwrapApiResponse());
  }

  markAllRead(): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.baseUrl}/read-all`, {}).pipe(unwrapApiResponse());
  }
}
