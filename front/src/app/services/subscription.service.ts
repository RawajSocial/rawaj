import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import {
  AddOnType,
  AiCreditsUsage,
  ChangeSubscriptionPlanRequest,
  ChangeSubscriptionPlanResponse,
  GetBillingHistoryResponse,
  PublicCoinPricing,
  PurchaseAddOnResponse,
  PurchaseCoinsRequest,
  PurchaseCoinsResponse,
  SubscriptionPlanSummary,
  SubscriptionSummary,
} from '../model/billing.model';

/** Real HTTP-backed subscription/billing service — plans, the tenant's current subscription,
 *  fake-payment purchases (coins/add-ons/plan changes), and billing history. */
@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/subscriptions`;

  private readonly _plans = signal<SubscriptionPlanSummary[]>([]);
  readonly plans = this._plans.asReadonly();

  private readonly _subscription = signal<SubscriptionSummary | null>(null);
  readonly subscription = this._subscription.asReadonly();

  private readonly _aiCreditsUsage = signal<AiCreditsUsage | null>(null);
  readonly aiCreditsUsage = this._aiCreditsUsage.asReadonly();

  /** Anonymous — safe to call from the landing page or public pricing page before login. */
  getPublicCoinPricing(): Observable<ApiResponse<PublicCoinPricing>> {
    return this.http.get<ApiResponse<PublicCoinPricing>>(`${this.baseUrl}/public-coin-pricing`);
  }

  refreshPlans(): Observable<ApiResponse<SubscriptionPlanSummary[]>> {
    return this.http.get<ApiResponse<SubscriptionPlanSummary[]>>(`${this.baseUrl}/plans`).pipe(
      tap(res => {
        if (res.data) this._plans.set(res.data);
      }),
    );
  }

  refreshSubscription(): Observable<ApiResponse<SubscriptionSummary>> {
    return this.http.get<ApiResponse<SubscriptionSummary>>(`${this.baseUrl}/me`).pipe(
      tap(res => {
        if (res.data) this._subscription.set(res.data);
      }),
    );
  }

  refreshAiCreditsUsage(): Observable<ApiResponse<AiCreditsUsage>> {
    return this.http.get<ApiResponse<AiCreditsUsage>>(`${this.baseUrl}/ai-credits`).pipe(
      tap(res => {
        if (res.data) this._aiCreditsUsage.set(res.data);
      }),
    );
  }

  changePlan(request: ChangeSubscriptionPlanRequest): Observable<ApiResponse<ChangeSubscriptionPlanResponse>> {
    return this.http.post<ApiResponse<ChangeSubscriptionPlanResponse>>(`${this.baseUrl}/change-plan`, request).pipe(
      tap(res => {
        if (res.data) this.refreshSubscription().subscribe();
      }),
    );
  }

  purchaseCoins(request: PurchaseCoinsRequest): Observable<ApiResponse<PurchaseCoinsResponse>> {
    return this.http.post<ApiResponse<PurchaseCoinsResponse>>(`${this.baseUrl}/purchase-coins`, request);
  }

  purchaseAddOn(type: AddOnType): Observable<ApiResponse<PurchaseAddOnResponse>> {
    return this.http.post<ApiResponse<PurchaseAddOnResponse>>(`${this.baseUrl}/purchase-add-on`, { type });
  }

  getBillingHistory(page = 1, pageSize = 20): Observable<ApiResponse<GetBillingHistoryResponse>> {
    return this.http.get<ApiResponse<GetBillingHistoryResponse>>(
      `${this.baseUrl}/history?page=${page}&pageSize=${pageSize}`,
    );
  }
}
