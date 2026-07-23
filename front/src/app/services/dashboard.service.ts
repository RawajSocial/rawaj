import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { DashboardActivityItem, DashboardChartsResponse, DashboardOverviewResponse } from '../model/dashboard.model';
import { ContentItemSummary } from '../model/content-item.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/dashboard`;

  private readonly _overview = signal<DashboardOverviewResponse | null>(null);
  private readonly _charts = signal<DashboardChartsResponse | null>(null);
  private readonly _recentContent = signal<ContentItemSummary[]>([]);
  private readonly _activity = signal<DashboardActivityItem[]>([]);

  readonly overview = this._overview.asReadonly();
  readonly charts = this._charts.asReadonly();
  readonly recentContent = this._recentContent.asReadonly();
  readonly activity = this._activity.asReadonly();

  /** Fires all four dashboard widgets' requests for the given brand (+ optional campaign) scope. */
  refresh(
    brandProfileId: string,
    campaignId: string | 'all',
    days = 30,
  ): Observable<
    [
      ApiResponse<DashboardOverviewResponse>,
      ApiResponse<DashboardChartsResponse>,
      ApiResponse<ContentItemSummary[]>,
      ApiResponse<DashboardActivityItem[]>,
    ]
  > {
    const campaignParam = campaignId !== 'all' ? `&campaignId=${encodeURIComponent(campaignId)}` : '';
    const bp = encodeURIComponent(brandProfileId);

    const overview$ = this.http
      .get<ApiResponse<DashboardOverviewResponse>>(`${this.baseUrl}/overview?brandProfileId=${bp}${campaignParam}`)
      .pipe(tap(res => { if (res.data) this._overview.set(res.data); }));

    const charts$ = this.http
      .get<ApiResponse<DashboardChartsResponse>>(`${this.baseUrl}/charts?brandProfileId=${bp}${campaignParam}&days=${days}`)
      .pipe(tap(res => { if (res.data) this._charts.set(res.data); }));

    const recentContent$ = this.http
      .get<ApiResponse<ContentItemSummary[]>>(`${this.baseUrl}/recent-content?brandProfileId=${bp}${campaignParam}&take=10`)
      .pipe(tap(res => { if (res.data) this._recentContent.set(res.data); }));

    const activity$ = this.http
      .get<ApiResponse<DashboardActivityItem[]>>(`${this.baseUrl}/activity?brandProfileId=${bp}${campaignParam}&take=20`)
      .pipe(tap(res => { if (res.data) this._activity.set(res.data); }));

    return forkJoin([overview$, charts$, recentContent$, activity$]);
  }

  clear(): void {
    this._overview.set(null);
    this._charts.set(null);
    this._recentContent.set([]);
    this._activity.set([]);
  }
}
