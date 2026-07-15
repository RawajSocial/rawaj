import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, ChangeSubscriptionPlanResponse, CurrentSubscription, SubscriptionPlanSummary } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class BillingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/subscriptions`;

  getPlans(): Observable<SubscriptionPlanSummary[]> {
    return this.http.get<ApiResponse<SubscriptionPlanSummary[]>>(`${this.baseUrl}/plans`).pipe(unwrapApiResponse());
  }

  getMine(): Observable<CurrentSubscription> {
    return this.http.get<ApiResponse<CurrentSubscription>>(`${this.baseUrl}/me`).pipe(unwrapApiResponse());
  }

  changePlan(subscriptionPlanId: string): Observable<ChangeSubscriptionPlanResponse> {
    return this.http
      .post<ApiResponse<ChangeSubscriptionPlanResponse>>(`${this.baseUrl}/change-plan`, { subscriptionPlanId })
      .pipe(unwrapApiResponse());
  }
}
