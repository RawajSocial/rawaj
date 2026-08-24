import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { CoinPricing } from '../model/billing.model';
import { TenantService } from '../core/tenant/tenant.service';

/** Real HTTP-backed coin pricing — the one place the frontend reads coin costs/discounts/free-trial
 *  quotas/purchase prices from, used by both the public pricing page and the per-button
 *  spend-preview captions, so displayed numbers can never drift from what the backend will charge. */
@Injectable({ providedIn: 'root' })
export class CoinPricingService {
  private readonly http = inject(HttpClient);
  private readonly tenantService = inject(TenantService);
  private readonly baseUrl = `${environment.apiUrl}/subscriptions`;

  private readonly _pricing = signal<CoinPricing | null>(null);
  readonly pricing = this._pricing.asReadonly();

  /** Fetches unconditionally — call after a purchase/plan-change so discounts/free-trial quotas
   *  reflect the new state. */
  refresh(): Observable<ApiResponse<CoinPricing>> {
    return this.http.get<ApiResponse<CoinPricing>>(`${this.baseUrl}/coin-pricing`).pipe(
      tap(res => {
        if (res.data) this._pricing.set(res.data);
      }),
    );
  }

  /** Loads once and caches — safe to call from every page that needs pricing without worrying
   *  about redundant network calls. */
  ensureLoaded(): void {
    if (!this._pricing()) {
      this.refresh().subscribe();
    }
  }

  /** Call after ANY coin-spending action (in both the success AND error branch — a bulk endpoint
   *  can return 200 having spent some coins, and an error can mean the balance moved in another
   *  tab). Pulls the fresh tenant balance for the header chip and re-reads pricing, since free-trial
   *  counters and `freeMarketingPlanAvailable` change as quotas are consumed. */
  refreshAfterSpend(): void {
    this.tenantService.refresh().subscribe();
    this.refresh().subscribe();
  }
}
